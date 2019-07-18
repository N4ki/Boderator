﻿using ArmaforcesMissionBot.Attributes;
using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.Commands;
using Discord.Rest;
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
        public CommandService _commands { get; set; }

        public Signups()
        {
            //_map = map;
        }

        [Command("help")]
        [Summary("Wyświetla tą wiadomość")]
        public async Task Help()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Dostępne komendy:");

            string description = "";

            foreach(var command in _commands.Commands)
            {
                description += $"**AF!{command.Name}** - {command.Summary}\n";
            }

            embed.WithDescription(description);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("zrob-zapisy")]
        [Summary("Tworzy nową misję, jako parametr przyjmuje nazwę misji.")]
        [ContextDMOrChannel]
        public async Task StartSignups([Remainder]string title)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
                await ReplyAsync("O ty luju, najpierw dokończ definiowanie poprzednich zapisów!");
            else
            {
                if (_client.GetGuild(_config.AFGuild).GetUser(Context.User.Id).Roles.Any(x => x.Id == _config.MissionMakerRole))
                {
                    var mission = new SignupsData.SignupsInstance();

                    mission.Title = title;
                    mission.Owner = Context.User.Id;
                    mission.Editing = true;

                    signups.Missions.Add(mission);


                    await ReplyAsync("Zdefiniuj reszte misji.");
                }
                else
                    await ReplyAsync("Luju ty, nie jestes uprawniony do tworzenia misji!");
            }
        }

        [Command("opis")]
        [Summary("Definicja opisu misji, dodając obrazek dodajesz obrazek do wołania misji.")]
        [ContextDMOrChannel]
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
        [Summary("Nazwa modlisty lub link do niej.")]
        [ContextDMOrChannel]
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
        [Summary("Definicja daty w formacie RRRR-MM-DD GG:MM")]
        [ContextDMOrChannel]
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

        [Command("dodaj-sekcje")]
        [Summary("Definiowanie sekcji w formacie `Nazwa emotka [liczba]`, gdzie `Nazwa` to nazwa sekcji, " +
                 "emotka to emotka używana do zapisywania się na rolę, [liczba] to liczba miejsc w danej roli. " +
                 "Przykład `Zulu :wsciekly_zulu: [1]` może być podanych kilka różnych emotek. Kolejność dodawania " +
                 "sekcji pozostaje jako kolejność wyświetlania na zapisach. Prebeton odbywa się poprzez dopisanie na " +
                 "końcu osoby oraz roli jaką przyjmie w danej sekcji w formacie `wzmianka emotka` i tak dla przykładu " +
                 "zabetonowany slot Zulu będzie wyglądać tak `Zulu :wsciekly_zulu: [1] @Ilddor#2556 :wsciekly_zulu:`.")]
        [ContextDMOrChannel]
        public async Task AddTeam([Remainder]string teamText)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);
                string rolePattern = @"(\<.+?\>)?(?: (.+?))?(?: )+(\[[0-9]+\])";
                MatchCollection matches = Regex.Matches(teamText, rolePattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

                string prebetonPattern = @"\<?@(.+?)\>?(?: )+(.+?)?(?: )*";
                MatchCollection prebetonMatches = Regex.Matches(teamText, prebetonPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

                if (matches.Count > 0)
                {
                    var team = new SignupsData.SignupsInstance.Team();
                    foreach (Match match in matches.Reverse())
                    {
                        team.Slots.Add(match.Groups[2].Value, int.Parse(match.Groups[3].Value.Substring(1, match.Groups[3].Value.Length-2)));
                    }

                    // Prebetons!
                    if(prebetonMatches.Count > 0)
                    {
                        foreach(Match prebeton in prebetonMatches.Reverse())
                        {
                            var username = prebeton.Groups[1].Value.Split("#");
                            SocketUser prebetonUser;
                            if (username.Length == 1)
                                prebetonUser = _client.GetUser(ulong.Parse(username[0]));
                            else
                                prebetonUser = _client.GetUser(username[0], username[1]);
                            team.Signed[prebetonUser.Mention] = prebeton.Groups[2].Value;
                            mission.SignedUsers.Add(prebetonUser.Id);
                            teamText = teamText.Replace(prebeton.Groups[0].Value, "");
                        }
                    }

                    team.Name = teamText;
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

        [Command("dodaj-standardowa-druzyne")]
        [Summary("Definiuje druzyne o podanej nazwie (jeden wyraz) skladajaca sie z SL i dwóch sekcji, " +
                 "w kazdej sekcji jest dowódca, medyk i 4 bpp domyślnie. Liczbę bpp można zmienić podając " +
                 "jako drugi parametr liczbę osób na sekcję, dla przykładu liczba 5 utworzy tylko 3 miejsca dla bpp.")]
        [ContextDMOrChannel]
        public async Task AddTeam(string teamName, int teamSize = 6)
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);
                // SL
                var team = new SignupsData.SignupsInstance.Team();
                team.Name = teamName + " SL <:wsciekly_zulu:426139721001992193> [1] 🚑 [1]";
                team.Slots.Add("<:wsciekly_zulu:426139721001992193>", 1);
                team.Slots.Add("🚑", 1);
                mission.Teams.Add(team);

                // team 1
                team = new SignupsData.SignupsInstance.Team();
                team.Name = teamName + " 1 <:wsciekly_zulu:426139721001992193> [1] 🚑 [1] <:beton:437603383373987853> [" + (teamSize-2).ToString() + "]";
                team.Slots.Add("<:wsciekly_zulu:426139721001992193>", 1);
                team.Slots.Add("🚑", 1);
                team.Slots.Add("<:beton:437603383373987853>", teamSize - 2);
                mission.Teams.Add(team);

                // team 2
                team = new SignupsData.SignupsInstance.Team();
                team.Name = teamName + " 2 <:wsciekly_zulu:426139721001992193> [1] 🚑 [1] <:beton:437603383373987853> [" + (teamSize - 2).ToString() + "]";
                team.Slots.Add("<:wsciekly_zulu:426139721001992193>", 1);
                team.Slots.Add("🚑", 1);
                team.Slots.Add("<:beton:437603383373987853>", teamSize - 2);
                mission.Teams.Add(team);

                await ReplyAsync("Jeszcze coś?");
            }
            else
            {
                await ReplyAsync("A może byś mi najpierw powiedział do jakiej misji chcesz dodać ten zespół?");
            }
        }

        [Command("edytuj-sekcje")]
        [Summary("Wyświetla panel do ustawiania kolejnosci sekcji oraz usuwania.")]
        [ContextDMOrChannel] // ⬆ ⬇ 📍 ✂ 🔒
        public async Task EditTeams()
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing && x.Owner == Context.User.Id);

                var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Sekcje:")
                .WithDescription(Helpers.MiscHelper.BuildEditTeamsPanel(mission.Teams, mission.HighlightedTeam));

                var message = await Context.Channel.SendMessageAsync(embed: embed.Build());
                mission.EditTeamsMessage = message.Id;
                mission.HighlightedTeam = 0;

                var reactions = new IEmote[5];
                reactions[0] = new Emoji("⬆");
                reactions[1] = new Emoji("⬇");
                reactions[2] = new Emoji("📍");
                reactions[3] = new Emoji("✂");
                reactions[4] = new Emoji("🔒");

                await message.AddReactionsAsync(reactions);
            }
        }

        [Command("koniec")]
        [Summary("Wyświetla podsumowanie zebranych informacji o misji przed wysłaniem.")]
        [ContextDMOrChannel]
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

                    Helpers.MiscHelper.BuildTeamsEmbed(mission.Teams, embed);

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
        [Summary("Anuluje tworzenie misji, usuwa wszystkie zdefiniowane o niej informacje. Nie anuluje to już stworzonych zapisów.")]
        [ContextDMOrChannel]
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
        [Summary("Potwierdza daną misję, spowoduje to stworzenie nowego kanału zapisów i zawołanie wszystkich członków Armaforces na zapisy.")]
        [ContextDMOrChannel]
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
                        PermValue.Inherit,
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
                        PermValue.Inherit,
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
                    //await signupChnnel.AddPermissionOverwriteAsync(armaforces, armaforcesPermissions);
                    //await signupChnnel.AddPermissionOverwriteAsync(everyone, everyonePermissions);

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
                        /*var description = "";
                        foreach(var slot in team.Slots)
                        {
                            for (var i = 0; i < slot.Value; i++)
                                description += slot.Key + "-\n";
                        }

                        foreach (var prebeton in team.Signed)
                        {
                            var regex = new Regex(Regex.Escape(prebeton.Value) + @"-(?:$|\n)");
                            description = regex.Replace(description, prebeton.Value + "-" + prebeton.Key + "\n", 1);
                            team.Slots[prebeton.Value]--;
                        }*/

                        var description = Helpers.MiscHelper.BuildTeamSlots(team);

                        var teamEmbed = new EmbedBuilder()
                            .WithColor(Color.Green)
                            .WithTitle(team.Name)
                            .WithDescription(description);

                        var teamMsg = await signupChnnel.SendMessageAsync(embed: teamEmbed.Build());
                        team.TeamMsg = teamMsg.Id;

                        var reactions = new IEmote[team.Slots.Count];
                        int num = 0;
                        foreach (var slot in team.Slots)
                        {
                            try
                            {
                                var emote = Emote.Parse(slot.Key);
                                reactions[num++] = emote;
                                //await teamMsg.AddReactionAsync(emote);
                            }
                            catch (Exception e)
                            {
                                var emoji = new Emoji(slot.Key);
                                reactions[num++] = emoji;
                                //await teamMsg.AddReactionAsync(emoji);
                            }
                        }
                        await teamMsg.AddReactionsAsync(reactions);
                    }
                }
                else
                {
                    await ReplyAsync("Nie uzupełniłeś wszystkich informacji ciołku!");
                }
            }
            else
            {
                await ReplyAsync("A może byś mi najpierw powiedział co ty chcesz potwierdzić?");
            }
        }

        [Command("aktualne-misje")]
        [ContextDMOrChannel]
        public async Task ListMissions()
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Owner == Context.User.Id && x.Editing == false))
            {
                var mainEmbed = new EmbedBuilder()
                            .WithColor(Color.Orange);

                int index = 0;

                foreach (var mission in signups.Missions.Where(x => x.Owner == Context.User.Id && x.Editing == false))
                {
                    mainEmbed.AddField(index++.ToString(), mission.Title);
                }

                await ReplyAsync(embed: mainEmbed.Build());
            }
            else
            {
                await ReplyAsync("Jesteś leniem i nie masz żadnych aktualnie trwających zapisów na twoje misje.");
            }
        }

        [Command("anuluj-misje")]
        [ContextDMOrChannel]
        public async Task CancelMission(int missionNo)
        {
            var signups = _map.GetService<SignupsData>();

            int index = 0;

            foreach (var mission in signups.Missions.Where(x => x.Owner == Context.User.Id && x.Editing == false))
            {
                if (index++ == missionNo)
                {
                    await mission.Access.WaitAsync();
                    try
                    {
                        var guild = _client.GetGuild(_config.AFGuild);
                        await guild.GetTextChannel(mission.SignupChannel).DeleteAsync();
                    }
                    finally
                    {
                        mission.Access.Release();
                    }
                }
            }

            await ReplyAsync("I tak by sie zjebała.");
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
