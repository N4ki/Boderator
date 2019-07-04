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
                var mission = new SignupsData.SignupsInstance();

                mission.Title = title;
                mission.Owner = Context.User.Id;
                mission.Editing = true;

                signups.Missions.Add(mission);
                await ReplyAsync("Teraz podaj opis misji.");
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

                await ReplyAsync("Teraz zdefiniuj podaj datę misji.");
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

                var embed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle(mission.Title)
                    .WithDescription(mission.Description + " " + mission.Attachment)
                    .WithFooter(mission.Date.ToString())
                    .WithAuthor(Context.User);

                foreach(var team in mission.Teams)
                {
                    var slots = "";
                    foreach(var slot in team.Slots)
                    {
                        slots += slot.Key + ": " + slot.Value + "\n";
                    }
                    embed.AddField(team.Name, slots);
                }
                
                // CHANGE THAT!
                mission.Editing = true;

                await ReplyAsync(embed: embed.Build());

                var guild = _client.GetGuild(_config.AFGuild);

                var signupChnnel = await guild.CreateTextChannelAsync(mission.Title, x => x.CategoryId = _config.SignupsCategory);

                mission.SignupChannel = signupChnnel.Id;

                var everyone = guild.EveryoneRole;
                var armaforces = guild.GetRole(_config.SignupRank);

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
                    PermValue.Deny,
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

                await signupChnnel.AddPermissionOverwriteAsync(everyone, everyonePermissions);
                await signupChnnel.AddPermissionOverwriteAsync(armaforces, armaforcesPermissions);

                await signupChnnel.SendMessageAsync("Woop woop");
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
            await ReplyAsync("I tak nikt nie chce grać na twoich misjach.");
        }
    }
}
