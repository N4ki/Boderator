using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Modules
{
    public class Signups : ModuleBase<SocketCommandContext>
    {
        public IServiceProvider _map { get; set; }
        public DiscordSocketClient _client { get; set; }
        public Config _config { get; set; }

        public Signups()
        {
            //_map = map;
        }

        [Command("zrob-zapisy")]
        [RequireContext(ContextType.DM)]
        public async Task StartSignups([Remainder]string title)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
                await ReplyAsync("O tu chuju, najpierw dokończ definiowanie poprzednich zapisów!");
            else
            {
                if (_client.GetGuild(_config.AFGuild).GetUser(Context.User.Id).Roles.Any(x => x.Id == _config.MissionMakerRole))
                {
                    var mission = new SignupsData.SignupsInstance();

                    mission.Title = title;
                    mission.Owner = Context.User.Id;
                    mission.Editing = true;

                    signups.Missions.Add(mission);
                    await ReplyAsync("Teraz podaj opis misji.");
                }
                else
                    await ReplyAsync("Luju ty, nie jestes uprawniony do tworzenia misji!");
            }
        }

        [Command("opis")]
        [RequireContext(ContextType.DM)]
        public async Task Description([Remainder]string description)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);

                mission.Description = description;

                if (Context.Message.Attachments.Count > 0)
                {
                    mission.Attachment = Context.Message.Attachments.ElementAt(0).Url;
                }

                await ReplyAsync("Teraz podaj nazwe modlisty.");
            }
            else
            {
                await ReplyAsync("Najpierw zdefiniuj nazwę misji cymbale.");
            }
        }

        [Command("modlista")]
        [RequireContext(ContextType.DM)]
        public async Task Modlist([Remainder]string modlist)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);

                mission.Modlist = modlist;

                await ReplyAsync("Teraz podaj datę misji.");
            }
            else
            {
                await ReplyAsync("Najpierw zdefiniuj nazwę misji cymbale.");
            }
        }

        [Command("data")]
        [RequireContext(ContextType.DM)]
        public async Task Date([Remainder]DateTime date)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);

                mission.Date = date;

                await ReplyAsync("Teraz zdefiniuj zespoły.");
            }
            else
            {
                await ReplyAsync("Najpierw zdefiniuj nazwę misji cymbale.");
            }
        }

        [Command("dodaj-druzyne")]
        [RequireContext(ContextType.DM)]
        public async Task AddTeam([Remainder]string teamText)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);
                string rolePattern = @"(\<.+?\>)?( (.+?))? (\[[0-9]+\])";
                MatchCollection matches = Regex.Matches(teamText, rolePattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

                if(matches.Count > 0)
                {
                    var team = new SignupsData.SignupsInstance.Team();
                    team.Name = teamText;
                    foreach (Match match in matches)
                    {
                        team.Slots.Add(match.Groups[3].Value, int.Parse(match.Groups[4].Value.Substring(1, match.Groups[4].Value.Length-2)));
                    }
                    mission.Teams.Add(team);
                    await ReplyAsync("Jeszcze coś?");
                }
                else
                {
                    await ReplyAsync("Zjebałeś, nie dałeś żadnych slotów do zespołu, spróbuj jeszcze raz, tylko tym razem sie popraw.");
                }
            }
            else
            {
                await ReplyAsync("A może byś mi najpierw powiedział do jakiej misji chcesz dodać ten zespół?");
            }
        }

        [Command("koniec")]
        [RequireContext(ContextType.DM)]
        public async Task EndSignups()
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);
                if (CheckMissionComplete(mission))
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle(mission.Title)
                        .WithDescription(mission.Description)
                        .WithFooter(mission.Date.ToString())
                        .WithAuthor(Context.User);

                    if (mission.Attachment != null)
                        embed.WithImageUrl(mission.Attachment);

                    foreach (var team in mission.Teams)
                    {
                        var slots = "";
                        foreach (var slot in team.Slots)
                        {
                            slots += slot.Key + ": " + slot.Value + "\n";
                        }
                        embed.AddField(team.Name, slots);
                    }

                    await ReplyAsync(embed: embed.Build());
                    await ReplyAsync("Potwierdzasz? Później nie będzie można tego zmienić.");
                }
                else
                {
                    await ReplyAsync("Nie uzupełniłeś wszystkich informacji ciołku!");
                }
            }
            else
            {
                await ReplyAsync("Co ty chcesz kończyć jak nic nie zacząłeś?");
            }
        }

        [Command("anuluj")]
        [RequireContext(ContextType.DM)]
        public async Task CancelSignups()
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                signups.Missions.Remove(signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id));

                await ReplyAsync("I tak nikt nie chce grać na twoich misjach.");
            }
            else
                await ReplyAsync("Siebie anuluj, nie tworzysz żadnej misji aktualnie.");
        }

        [Command("potwierdzam")]
        [RequireContext(ContextType.DM)]
        public async Task ConfirmSignups()
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);
                if (CheckMissionComplete(mission))
                {
                    mission.Editing = false;

                    var guild = _client.GetGuild(_config.AFGuild);

                    var signupChnnel = await guild.CreateTextChannelAsync(mission.Title, x => x.CategoryId = _config.SignupsCategory);

                    mission.SignupChannel = signupChnnel.Id;

                    var everyone = guild.EveryoneRole;
                    var armaforces = guild.GetRole(_config.SignupRank);
                    var botRole = guild.GetRole(_config.BotRole);

                    var everyonePermissions = new OverwritePermissions(
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

                    var armaforcesPermissions = new OverwritePermissions(
                        PermValue.Deny,
                        PermValue.Deny,
                        PermValue.Allow,
                        PermValue.Allow,
                        PermValue.Deny,
                        PermValue.Deny,
                        PermValue.Deny,
                        PermValue.Deny,
                        PermValue.Deny,
                        PermValue.Allow,
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

                    await signupChnnel.AddPermissionOverwriteAsync(botRole, botPermissions);
                    await signupChnnel.AddPermissionOverwriteAsync(armaforces, armaforcesPermissions);
                    await signupChnnel.AddPermissionOverwriteAsync(everyone, everyonePermissions);

                    var mainEmbed = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle(mission.Title)
                        .WithDescription(mission.Description)
                        .AddField("Data:", mission.Date.ToString())
                        .WithAuthor(Context.User);

                    if (mission.Attachment != null)
                        mainEmbed.WithImageUrl(mission.Attachment);

                    if (mission.Modlist != null)
                        mainEmbed.AddField("Modlista", mission.Modlist);
                    else
                        mainEmbed.AddField("Modlista", "Dafault");

                    await signupChnnel.SendMessageAsync("@everyone", embed: mainEmbed.Build());

                    foreach (var team in mission.Teams)
                    {
                        var teamEmbed = new EmbedBuilder()
                            .WithColor(Color.Green)
                            .WithTitle(team.Name);

                        var teamMsg = await signupChnnel.SendMessageAsync(embed: teamEmbed.Build());
                        team.TeamMsg = teamMsg.Id;

                        foreach (var slot in team.Slots)
                        {
                            try
                            {
                                var emote = Emote.Parse(slot.Key);
                                await teamMsg.AddReactionAsync(emote);
                            }
                            catch (Exception e)
                            {
                                var emoji = new Emoji(slot.Key);
                                await teamMsg.AddReactionAsync(emoji);
                            }
                        }
                    }
                }
                else
                {
                    await ReplyAsync("Nie uzupełniłeś wszystkich informacji ciołku!");
                }
            }
            else
            {
                await ReplyAsync("A może byś mi najpierw powiedział do jakiej misji chcesz dodać ten zespół?");
            }
        }

        private bool CheckMissionComplete(SignupsData.SignupsInstance mission)
        {
            if (mission.Title == null ||
                mission.Description == null ||
                mission.Date == null ||
                mission.Teams.Count == 0)
                return false;
            else
                return true;
        }
    }
}
