using ArmaforcesMissionBot.Attributes;
using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.Handlers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArmaforcesMissionBot.DataClasses.SQL;
using LinqToDB;
using static ArmaforcesMissionBot.DataClasses.OpenedDialogs;

namespace ArmaforcesMissionBot.Modules
{
    [Name("Zapisy")]
    public class Signups : ModuleBase<SocketCommandContext>
    {
        public IServiceProvider _map { get; set; }
        public DiscordSocketClient _client { get; set; }
        public Config _config { get; set; }
        public OpenedDialogs _dialogs { get; set; }
        public CommandService _commands { get; set; }

        public Signups()
        {
            //_map = map;
        }

        [Command("zrob-zapisy")]
        [Summary("Tworzy nową misję, jako parametr przyjmuje nazwę misji.")]
        [ContextDMOrChannel]
        public async Task StartSignups([Remainder]string title)
        {
            var runtimeData = _map.GetService<RuntimeData>();

            if(runtimeData.HasMission(Context.User.Id))
	            await ReplyAsync("O ty luju, najpierw dokończ definiowanie poprzednich zapisów!");
            else
            {
                if (_client.GetGuild(_config.AFGuild).GetUser(Context.User.Id).Roles.Any(x => x.Id == _config.MissionMakerRole))
                {
	                var mission = runtimeData.GetEditedMission(Context.User.Id, true);

                    mission.Title = title;
                    mission.Owner = Context.User.Id;
                    mission.Date = DateTime.Now;
                    mission.CloseDate = mission.Date.AddMinutes(-60);

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
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);

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
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);

                var request = WebRequest.Create($"https://server.armaforces.com:8888/modsets/{modlist.Split('/').Last()}.csv");
                try
                {
                    WebResponse response = request.GetResponse();
                    mission.Modlist = $"https://modlist.armaforces.com/#/download/{modlist.Split('/').Last()}";

                    await ReplyAsync("Teraz podaj datę misji.");
                }
                catch
                {
                    await ReplyAsync("Ten link lub nazwa modlisty nie jest prawidłowy dzbanie!");
                }
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
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);

                mission.Date = date;
                if (!mission.CustomClose)
                    mission.CloseDate = date.AddMinutes(-60);

                await ReplyAsync("Teraz zdefiniuj zespoły.");
            }
            else
            {
                await ReplyAsync("Najpierw zdefiniuj nazwę misji cymbale.");
            }
        }

        [Command("zamkniecie")]
        [Summary("Definiowanie czasu kiedy powinny zamknąć się zapisy, tak jak data w formacie RRRR-MM-DD GG:MM")]
        [ContextDMOrChannel]
        public async Task Close([Remainder]DateTime closeDate)
        {
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);

                if (closeDate < mission.Date)
                {
                    mission.CloseDate = closeDate;
                    mission.CustomClose = true;
                    await ReplyAsync("Wspaniale!");
                }
                else
                {
                    await ReplyAsync("Zamknięcie zapisów musi być przed datą misji baranie!");
                }
            }
            else
            {
                await ReplyAsync("Najpierw zdefiniuj nazwę misji cymbale.");
            }
        }

        [Command("dodaj-sekcje", RunMode = RunMode.Async)]
        [Summary("Definiowanie sekcji w formacie `Nazwa | emotka [liczba] opcjonalna_nazwa_slota`, gdzie `Nazwa` to nazwa sekcji, " +
                 "emotka to emotka używana do zapisywania się na rolę, [liczba] to liczba miejsc w danej roli. " +
                 "Przykład `Zulu | :wsciekly_zulu: [1]` lub `Alpha 1 | :wsciekly_zulu: [1] Dowódca | 🚑 [1] Medyk | :beton: [5] BPP`" +
                 " może być podanych kilka różnych emotek. Kolejność dodawania " +
                 "sekcji pozostaje jako kolejność wyświetlania na zapisach. Prebeton odbywa się poprzez dopisanie na " +
                 "końcu roli osoby, która powinna być prebetonowana dla przykładu " +
                 "zabetonowany slot TL w standardowej sekcji będzie wyglądać tak `Alpha 1 | :wsciekly_zulu: [1] Dowódca @Ilddor#2556 | 🚑 [1] Medyk | :beton: [4] BPP`.")]
        [ContextDMOrChannel]
        public async Task AddTeam([Remainder]string teamText)
        {
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);

                var slotTexts = teamText.Split("|");

                if (slotTexts.Length > 1)
                {
                    var team = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                    team.Name = slotTexts[0];
                    team.Pattern = "";

                    foreach (var slotText in slotTexts)
                    {
                        MatchCollection matches = Helpers.MiscHelper.GetSlotMatchesFromText(slotText);
                        if (matches.Count == 0)
                            continue;

                        Match match = matches.First();

                        if(match.Success)
                        {
                            var slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(match.Groups[1].Value, int.Parse(match.Groups[2].Value.Substring(1, match.Groups[2].Value.Length - 2)));
                            if(match.Groups.Count == 4)
                            {
                                slot.Name = match.Groups[3].Value;
                                foreach(var user in Context.Message.MentionedUsers)
                                {
                                    if(slot.Name.Contains(user.Mention))
                                    {
                                        slot.Name = slot.Name.Replace(user.Mention, "");
                                        slot.Signed.Add(user.Id);
                                    }
                                }
                            }
                            team.Slots.Add(slot);

                            if (team.Pattern.Length > 0)
                                team.Pattern += "| ";
                            team.Pattern += $"{slot.Emoji} [{slot.Count}] {slot.Name} ";
                        }
                    }

                    var embed = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle(team.Name)
                        .WithDescription(Helpers.MiscHelper.BuildTeamSlots(team)[0])
                        .WithFooter(team.Pattern);

                    Helpers.MiscHelper.CreateConfirmationDialog(
                        Context,
                        embed.Build(),
                        (Dialog dialog) =>
                        {
                            Context.Channel.DeleteMessageAsync(dialog.DialogID);
                            _dialogs.Dialogs.Remove(dialog);
                            mission.Teams.Add(team);
                            foreach(var slot in team.Slots)
                            {
                                foreach(var signed in slot.Signed)
                                {
                                    mission.SignedUsers.Add(signed);
                                }
                            }
                            ReplyAsync("OK!");
                        }, 
                        (Dialog dialog) =>
                        {
                            Context.Channel.DeleteMessageAsync(dialog.DialogID);
                            _dialogs.Dialogs.Remove(dialog);
                            ReplyAsync("OK Boomer");
                        });
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
                 "jako drugi parametr sumaryczną liczbę osób na sekcję.")]
        [ContextDMOrChannel]
        public async Task AddTeam(string teamName, int teamSize = 6)
        {
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);
                // SL
                var team = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                team.Name = teamName + " SL | <:wsciekly_zulu:426139721001992193> [1] | 🚑 [1]";
                var slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "Dowódca",
                    "<:wsciekly_zulu:426139721001992193>",
                    1);
                team.Slots.Add(slot);

                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "Medyk",
                    "🚑",
                    1);
                team.Slots.Add(slot);
                team.Pattern = "<:wsciekly_zulu:426139721001992193> [1] | 🚑 [1]";
                mission.Teams.Add(team);

                // team 1
                team = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                team.Name = teamName + " 1 | <:wsciekly_zulu:426139721001992193> [1] | 🚑 [1] | <:beton:437603383373987853> [" + (teamSize-2).ToString() + "]";
                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "Dowódca",
                    "<:wsciekly_zulu:426139721001992193>",
                    1);
                team.Slots.Add(slot);

                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "Medyk",
                    "🚑",
                    1);
                team.Slots.Add(slot);

                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "BPP",
                    "<:beton:437603383373987853>",
                    teamSize - 2);
                team.Slots.Add(slot);
                team.Pattern = "<:wsciekly_zulu:426139721001992193> [1] | 🚑 [1] | <:beton:437603383373987853> [" + (teamSize - 2).ToString() + "]";
                mission.Teams.Add(team);

                // team 2
                team = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                team.Name = teamName + " 2 | <:wsciekly_zulu:426139721001992193> [1] | 🚑 [1] | <:beton:437603383373987853> [" + (teamSize - 2).ToString() + "]";
                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "Dowódca",
                    "<:wsciekly_zulu:426139721001992193>",
                    1);
                team.Slots.Add(slot);

                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "Medyk",
                    "🚑",
                    1);
                team.Slots.Add(slot);

                slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                    "BPP",
                    "<:beton:437603383373987853>",
                    teamSize - 2);
                team.Slots.Add(slot);
                team.Pattern = "<:wsciekly_zulu:426139721001992193> [1] | 🚑 [1] | <:beton:437603383373987853> [" + (teamSize - 2).ToString() + "]";
                mission.Teams.Add(team);

                await ReplyAsync("Jeszcze coś?");
            }
            else
            {
                await ReplyAsync("A może byś mi najpierw powiedział do jakiej misji chcesz dodać ten zespół?");
            }
        }

        [Command("dodaj-rezerwe")]
        [Summary(
	        "Dodaje rezerwę o nieograniczonej liczbie miejsc, przy podaniu w parametrze liczby udostępnia taką liczbę miejsc na kanale dla rekrutów z możliwością zapisu dla nich.")]
        [ContextDMOrChannel]
        public async Task AddReserve(int slots = 0)
        {
	        var runtimeData = _map.GetService<RuntimeData>();

	        if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);
                // SL
                var team = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                team.Slots.Add(new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
	                "Rezerwa",
                    "🚑",
	                slots));
                team.Pattern = $"Rezerwa 🚑 [{slots}]";
                mission.Teams.Add(team);

                await ReplyAsync("Jeszcze coś?");
	        }
	        else
	        {
		        await ReplyAsync("A ta rezerwa to do czego?");
	        }
        }

        [Command("edytuj-sekcje")]
        [Summary("Wyświetla panel do ustawiania kolejnosci sekcji oraz usuwania. Strzałki przesuwają zaznaczenie/sekcje. " +
                 "Pinezka jest do \"złapania\" sekcji w celu przesunięcia. Nożyczki usuwają zaznaczoną sekcję. Kłódka kończy edycję sekcji.")]
        [ContextDMOrChannel]
        public async Task EditTeams()
        {
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);

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
        [Summary("Wyświetla dialog z potwierdzeniem zebranych informacji o misji.")]
        [ContextDMOrChannel]
        public async Task EndSignups()
        {
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
	            var mission = runtimeData.GetEditedMission(Context.User.Id);
                if (Helpers.SignupHelper.CheckMissionComplete(mission))
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle(mission.Title)
                        .WithDescription(mission.Description)
                        .WithFooter(mission.Date.ToString())
                        .AddField("Zamknięcie zapisów:", mission.CloseDate.ToString(CultureInfo.InvariantCulture))
                        .WithAuthor(Context.User);

                    if (mission.Attachment != null)
                        embed.WithImageUrl(mission.Attachment);

                    if (mission.Modlist == null)
                        mission.Modlist = "https://modlist.armaforces.com/#/download/default";

                    embed.AddField("Modlista:", mission.Modlist);

                    Helpers.MiscHelper.BuildTeamsEmbed(mission.Teams, embed);

                    Helpers.MiscHelper.CreateConfirmationDialog(
                       Context,
                       embed.Build(),
                       (Dialog dialog) =>
                       {
                           _dialogs.Dialogs.Remove(dialog);
                           _ = Helpers.SignupHelper.CreateSignupChannel(runtimeData, Context.User.Id, Context.Channel);
                           ReplyAsync("No to lecim!");
                       },
                       (Dialog dialog) =>
                       {
                           Context.Channel.DeleteMessageAsync(dialog.DialogID);
                           _dialogs.Dialogs.Remove(dialog);
                           ReplyAsync("Poprawiaj to szybko!");
                       });
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
            var runtimeData = _map.GetService<RuntimeData>();

            if (runtimeData.HasMission(Context.User.Id))
            {
                runtimeData.RemoveMission(Context.User.Id);

                await ReplyAsync("I tak nikt nie chce grać na twoich misjach.");
            }
            else
                await ReplyAsync("Siebie anuluj, nie tworzysz żadnej misji aktualnie.");
        }

        [Command("anuluj-misje")]
        [Summary("Po podaniu indeksu misji jako parametru anuluje całe zapisy usuwając kanał zapisów.")]
        [ContextDMOrChannel]
        public async Task CancelMission(ITextChannel channel)
        {
            using (var db = new DbBoderator())
            {
	            var mission = db.Missions.Single(q => q.Owner == Context.User.Id && q.Date > DateTime.Now);
	            if (mission != null)
	            {
		            var guild = _client.GetGuild(_config.AFGuild);
		            await guild.GetTextChannel(mission.SignupChannel).DeleteAsync();
		            db.Delete(mission);
	            }
            }

            await ReplyAsync("I tak by sie zjebała.");
        }

        [Command("edytuj-misje")]
        [Summary("Po podaniu indeksu misji jako parametru włączy edycje danej misji (część bez zespołów).")]
        [ContextDMOrChannel]
        public async Task EditMission(int missionNo)
        {
	        await ReplyAsync("Trwają prace konserwacyjne, prosimy spróbować później");
            /*var signups = _map.GetService<RuntimeData>();

            int index = 0;

            foreach (var mission in signups.Missions.Where(x => x.Owner == Context.User.Id && x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing))
            {
                if (index++ == missionNo)
                {
                    // Don't want to write another function just to copy class, and performance isn't a problem here so just serialize it and deserialize
                    var serialized = JsonConvert.SerializeObject(mission);
                    signups.BeforeEditMissions[Context.User.Id] = JsonConvert.DeserializeObject<ArmaforcesMissionBotSharedClasses.Mission>(serialized);
                    mission.Editing = ArmaforcesMissionBotSharedClasses.Mission.EditEnum.Started;
                    await ReplyAsync("Luzik, co chcesz zmienić?");
                }
            }*/
        }

        [Command("zapisz-zmiany")]
        [Summary("Zapisuje zmiany w aktualnie edytowanej misji, jesli w parametrze zostanie podana wartość true to zostanie wysłane ogłoszenie o zmianach w misji.")]
        [ContextDMOrChannel]
        public async Task SaveChanges(bool announce = false)
        {
	        await ReplyAsync("Trwają prace konserwacyjne, prosimy spróbować później");
            /*var signups = _map.GetService<RuntimeData>();

            if (signups.Missions.Any(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.Started && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.Started && x.Owner == Context.User.Id);

                await mission.Access.WaitAsync(-1);
                try
                {
                    if (Helpers.SignupHelper.CheckMissionComplete(mission))
                    {
                        var guild = Program.GetClient().GetGuild(Program.GetConfig().AFGuild);

                        var channel = await Helpers.SignupHelper.UpdateMission(guild, mission, signups);

                        mission.Editing = ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing;

                        if(announce)
                            await channel.SendMessageAsync("@everyone Misja uległa modyfikacji, proszę zapoznać się z nowymi informacjami i dostosować swój beton.");

                        await ReplyAsync("Się robi szefie!");
                    }
                    else
                    {
                        await ReplyAsync("Nie uzupełniłeś wszystkich informacji ciołku!");
                    }
                }
                catch (Exception e)
                {
                    await ReplyAsync($"Oj, coś poszło nie tak: {e.Message}");
                }
                finally
                {
                    mission.Access.Release();
                }
            }*/
        }

        [Command("anuluj-edycje")]
        [Summary("Anuluje aktualną edycję misji bez zapisywania zmian.")]
        [ContextDMOrChannel]
        public async Task CancelChanges(bool announce = false)
        {
	        await ReplyAsync("Trwają prace konserwacyjne, prosimy spróbować później");
            /*var signups = _map.GetService<RuntimeData>();

            if (signups.Missions.Any(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.Started && x.Owner == Context.User.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.Started && x.Owner == Context.User.Id);
                
                await mission.Access.WaitAsync(-1);
                try
                {
                    // Don't want to write another function just to copy class, and performance isn't a problem here so just serialize it and deserialize
                    signups.Missions.Remove(mission);
                    var serialized = JsonConvert.SerializeObject(signups.BeforeEditMissions[Context.User.Id]);
                    var oldMission = JsonConvert.DeserializeObject<ArmaforcesMissionBotSharedClasses.Mission>(serialized);
                    signups.Missions.Add(oldMission);

                    oldMission.Editing = ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing;
                    await ReplyAsync("I dobrze, tylko byś ludzi wkurwiał...");
                }
                catch (Exception e)
                {
                    await ReplyAsync($"Oj, coś poszło nie tak: {e.Message}");
                }
                finally
                {
                    mission.Access.Release();
                }
            }*/
        }

        [Command("upgrade")]
        [Summary("Wykonuje potrzebne upgrade'y kanałów, może jej użyć tylko Ilddor.")]
        [RequireOwner]
        public async Task Upgrade()
        {
            // TODO: Make admin module for such things
        }
    }
}
