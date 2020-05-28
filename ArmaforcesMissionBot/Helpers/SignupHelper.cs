using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBotSharedClasses;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ArmaforcesMissionBot.Helpers
{
    public class SignupHelper
    {
        public static bool CheckMissionComplete(ArmaforcesMissionBotSharedClasses.Mission mission)
        {
            if (mission.Title == null ||
                mission.Description == null ||
                mission.Date == null ||
                mission.Teams.Count == 0  ||
                mission.CloseTime > mission.Date)
                return false;
            else
                return true;
        }

        public static async Task<RestTextChannel> CreateChannelForMission(SocketGuild guild, Mission mission, RuntimeData runtime)
        {
            // Sort channels by date
            runtime.Missions.Sort((x, y) =>
            {
                return x.Date.CompareTo(y.Date);
            });

            var signupChannel = await guild.CreateTextChannelAsync(mission.Title, x =>
            {
                x.CategoryId = Program.GetConfig().SignupsCategory;
                // Kurwa dlaczego to nie działa
                var index = (int)(mission.Date - new DateTime(2019, 1, 1)).TotalMinutes;
                // really hacky solution to avoid recalculating indexes for each channel integer should have 
                // space for around 68 years, and this bot is not going to work this long for sure
                x.Position = index;
            });

            var everyone = guild.EveryoneRole;
            var armaforces = guild.GetRole(Program.GetConfig().SignupRole);
            var botRole = guild.GetRole(Program.GetConfig().BotRole);

            var banPermissions = new OverwritePermissions(
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny);

            var botPermissions = new OverwritePermissions(
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Deny);

            var everyoneStartPermissions = new OverwritePermissions(
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny);

            try
            {
                await signupChannel.AddPermissionOverwriteAsync(botRole, botPermissions);

                await runtime.BanAccess.WaitAsync(-1);
                try
                {
                    foreach (var ban in runtime.SpamBans)
                    {
                        await signupChannel.AddPermissionOverwriteAsync(Program.GetGuildUser(ban.Key), banPermissions);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    runtime.BanAccess.Release();
                }

                await signupChannel.AddPermissionOverwriteAsync(everyone, everyoneStartPermissions);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            

            return signupChannel;
        }

        public static async Task<EmbedBuilder> CreateMainEmbed(SocketGuild guild, Mission mission)
        {
            var mainEmbed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle(mission.Title)
                    .WithDescription(mission.Description)
                    .AddField("Data:", mission.Date.ToString())
                    .AddField("Zamknięcie zapisów:", mission.CloseTime.ToString())
                    .WithAuthor(guild.GetUser(mission.Owner));

            if (mission.Attachment != null)
                mainEmbed.WithImageUrl(mission.Attachment);

            if (mission.Modlist != null)
                mainEmbed.AddField("Modlista:", mission.Modlist);
            else
                mainEmbed.AddField("Modlista:", "https://modlist.armaforces.com/#/download/default");

            return mainEmbed;
        }

        public static async Task CreateMissionMessagesOnChannel(SocketGuild guild, Mission mission, RestTextChannel signupChannel)
        {
            var mainEmbed = await CreateMainEmbed(guild, mission);

            if(mission.AttachmentBytes != null)
            {
                mainEmbed.ImageUrl = $"attachment://{mission.FileName}";
                Stream stream = new MemoryStream(mission.AttachmentBytes);
                var tmpMessage = await signupChannel.SendFileAsync(stream, mission.FileName, "", embed: mainEmbed.Build());
                mission.Attachment = tmpMessage.Embeds.First().Image.Value.Url;
                mission.AttachmentBytes = null;
                mission.FileName = null;
            }
            else
                await signupChannel.SendMessageAsync("", embed: mainEmbed.Build());

            foreach (var team in mission.Teams)
            {
                var description = Helpers.MiscHelper.BuildTeamSlots(team);

                var teamEmbed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle(team.Name)
                    .WithFooter(team.Pattern);

                if (description.Count == 2)
                    teamEmbed.WithDescription(description[0] + description[1]);
                else if (description.Count == 1)
                    teamEmbed.WithDescription(description[0]);

                var teamMsg = await signupChannel.SendMessageAsync(embed: teamEmbed.Build());
                team.TeamMsg = teamMsg.Id;

                var reactions = new IEmote[team.Slots.Count];
                int num = 0;
                foreach (var slot in team.Slots)
                {
                    try
                    {
                        var emote = Emote.Parse(HttpUtility.HtmlDecode(slot.Emoji));
                        reactions[num++] = emote;
                    }
                    catch (Exception e)
                    {
                        var emoji = new Emoji(HttpUtility.HtmlDecode(slot.Emoji));
                        reactions[num++] = emoji;
                    }
                }
                await teamMsg.AddReactionsAsync(reactions);
            }

            // Make channel visible and notify everyone
            var everyone = guild.EveryoneRole;
            await signupChannel.RemovePermissionOverwriteAsync(everyone);
            await signupChannel.SendMessageAsync("@everyone");
        }

        public static async Task<SocketTextChannel> UpdateMission(SocketGuild guild, Mission mission, RuntimeData runtime)
        {
            // Sort channels by date
            runtime.Missions.Sort((x, y) =>
            {
                return x.Date.CompareTo(y.Date);
            });

            var signupChannel = guild.GetChannel(mission.SignupChannel) as SocketTextChannel;

            await signupChannel.ModifyAsync(x =>
            {
                x.CategoryId = Program.GetConfig().SignupsCategory;
                // Kurwa dlaczego to nie działa
                var index = (int)(mission.Date - new DateTime(2019, 1, 1)).TotalMinutes;
                // really hacky solution to avoid recalculating indexes for each channel integer should have 
                // space for around 68 years, and this bot is not going to work this long for sure
                x.Position = index;
            });

            var mainEmbed = await CreateMainEmbed(guild, mission);

            var messages = signupChannel.GetMessagesAsync(1000);

            await messages.ForEachAsync(x =>
            {
                foreach (var missionMsg in x)
                {
                    if (missionMsg.Embeds.Count != 0 &&
                        missionMsg.Author.Id == Program.GetClient().CurrentUser.Id)
                    {
                        var embed = missionMsg.Embeds.Single();
                        if (embed.Author != null)
                        {
                            (missionMsg as IUserMessage).ModifyAsync(message => message.Embed = mainEmbed.Build());
                        }
                    }
                }
            });

            return signupChannel;
        }

        public static async Task CreateSignupChannel(RuntimeData runtime, ulong ownerID, ISocketMessageChannel channnel)
        {
            if (runtime.Missions.Any(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New && x.Owner == ownerID))
            {
                var mission = runtime.Missions.Single(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New && x.Owner == ownerID);
                await mission.Access.WaitAsync(-1);
                try
                {
                    if (Helpers.SignupHelper.CheckMissionComplete(mission))
                    {
                        var guild = Program.GetClient().GetGuild(Program.GetConfig().AFGuild);

                        var signupChannel = await Helpers.SignupHelper.CreateChannelForMission(guild, mission, runtime);
                        mission.SignupChannel = signupChannel.Id;

                        await Helpers.SignupHelper.CreateMissionMessagesOnChannel(guild, mission, signupChannel);

                        mission.Editing = ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing;
                    }
                    else
                    {
                        await channnel.SendMessageAsync("Nie uzupełniłeś wszystkich informacji ciołku!");
                    }
                }
                catch (Exception e)
                {
                    await channnel.SendMessageAsync($"Oj, coś poszło nie tak: {e.Message}");
                }
                finally
                {
                    mission.Access.Release();
                }
            }
            else
            {
                await channnel.SendMessageAsync("A może byś mi najpierw powiedział co ty chcesz potwierdzić?");
            }
        }
    }
}
