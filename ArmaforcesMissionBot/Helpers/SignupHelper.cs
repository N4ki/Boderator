using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBotSharedClasses;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
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
                mission.Teams.Count == 0)
                return false;
            else
                return true;
        }

        public static async Task<RestTextChannel> CreateChannelForMission(SocketGuild guild, Mission mission, SignupsData signups)
        {
            mission.Editing = false;

            // Sort channels by date
            signups.Missions.Sort((x, y) =>
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
            var armaforces = guild.GetRole(Program.GetConfig().SignupRank);
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

            // FUCK YOU DISCORD TEAM! THAT WORKED PERFECTLY WELL TILL YOU DECIDED TO MAKE UPDATE TO API AND BREAK EVERYTHING. FUCK YOU!
            /*try
            {
                await signupChannel.AddPermissionOverwriteAsync(botRole, botPermissions);

                await signups.BanAccess.WaitAsync(-1);
                try
                {
                    foreach (var ban in signups.SpamBans)
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
                    signups.BanAccess.Release();
                }

                await signupChannel.AddPermissionOverwriteAsync(everyone, everyoneStartPermissions);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }*/
            

            return signupChannel;
        }

        public static async Task CreateMissionMessagesOnChannel(SocketGuild guild, Mission mission, RestTextChannel signupChannel)
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
                mainEmbed.AddField("Modlista:", "Dafault");

            await signupChannel.SendMessageAsync("", embed: mainEmbed.Build());

            foreach (var team in mission.Teams)
            {
                var description = Helpers.MiscHelper.BuildTeamSlots(team);

                var teamEmbed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle(team.Name)
                    .WithDescription(description);

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
    }
}
