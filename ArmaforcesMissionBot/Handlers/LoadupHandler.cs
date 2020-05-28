using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.DataClasses.SQL;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LinqToDB;

//using static ArmaforcesMissionBot.DataClasses.RuntimeData;

namespace ArmaforcesMissionBot.Handlers
{
    public class LoadupHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _client.GuildAvailable += Load;
        }

        private async Task Load(SocketGuild guild)
        {
            Console.WriteLine($"[{DateTime.Now.ToString()}] Loading up from: {guild.Name}");

            await LoadMissions(guild);
            await LoadBans(guild);
            await LoadBanHistory(guild);
            await LoadMissionsArchive(guild);
        }

        private async Task LoadMissions(SocketGuild guild)
        {
            var channels = guild.CategoryChannels.Single(x => x.Id == _config.SignupsCategory);

            Console.WriteLine($"[{DateTime.Now.ToString()}] Loading missions");

            foreach (var channel in channels.Channels.Where(x => x.Id != _config.SignupsArchive && x.Id != _config.CreateMissionChannel && x.Id != _config.HallOfShameChannel).Reverse())
            {
	            using (var db = new DataClasses.SQL.DbBoderator())
	            {
		            if (db.Missions.Any(x => x.SignupChannel == channel.Id))
			            continue;
		            var mission = new MissionTbl();

		            var textChannel = channel as SocketTextChannel;
		            var messages = textChannel.GetMessagesAsync();
		            List<IMessage> messagesNormal = new List<IMessage>();
		            await messages.ForEachAsync(async x =>
		            {
			            foreach (var it in x)
			            {
				            messagesNormal.Add(it);
			            }
		            });

		            mission.SignupChannel = channel.Id;
		            db.Insert(mission);

		            foreach (var message in messagesNormal)
		            {
			            if (message.Embeds.Count == 0)
				            continue;

			            if (message.Author.Id != _client.CurrentUser.Id)
				            continue;

			            var embed = message.Embeds.Single();
			            if (embed.Author == null)
			            {
				            var pattern = "";
				            if (embed.Footer.HasValue)
					            pattern = embed.Footer.Value.Text;
				            else
					            pattern = embed.Title;

				            MatchCollection matches = Helpers.MiscHelper.GetSlotMatchesFromText(pattern);

				            if (matches.Count > 0)
				            {
					            var team = new TeamTbl();
					            team.Name = embed.Title;
					            team.TeamMsg = message.Id;
					            team.Mission = mission;
					            team.Pattern = "";
					            db.Insert(team);
					            foreach (Match match in matches.Reverse())
					            {
						            var icon = match.Groups[1].Value;
						            if (icon[0] == ':')
						            {
							            var emotes = Program.GetEmotes();
							            var foundEmote = emotes.Single(x => x.Name == icon.Substring(1, icon.Length - 2));
							            var animated = foundEmote.Animated ? "a" : "";
							            icon = $"<{animated}:{foundEmote.Name}:{foundEmote.Id}>";
							            team.Pattern = team.Pattern.Replace(match.Groups[1].Value, icon);
						            }

						            var count = match.Groups[2].Value;
						            var name = match.Groups[3].Success ? match.Groups[3].Value : "";
						            var slot = new SlotTbl(
							            name,
							            icon,
							            int.Parse(count.Substring(1, count.Length - 2)),
							            false,
							            team.TeamMsg);

						            if (!embed.Footer.HasValue)
							            team.Name = team.Name.Replace(match.Groups[0].Value, "");
						            team.Pattern += $"{match.Groups[0]} ";
						            db.Insert(slot);

						            Console.WriteLine($"New slot {slot.Emoji} [{slot.Count}] {slot.Name}");
					            }

					            team.Name = team.Name.Replace("|", "");

					            if (embed.Description != null)
					            {
						            try
						            {
							            string signedPattern = @"(.+)(?:\(.*\))?-\<\@\!([0-9]+)\>";
							            MatchCollection signedMatches = Regex.Matches(embed.Description, signedPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
							            foreach (Match match in signedMatches.Reverse())
							            {
								            var signedID = ulong.Parse(match.Groups[2].Value);
								            var emojiString = match.Groups[1].Value;
								            Console.WriteLine($"{emojiString} : {match.Groups[2].Value} ({signedID})");
                                            var signed = new SignedTbl(signedID, emojiString, team.TeamMsg);
                                            db.Insert(signed);
							            }
						            }
						            catch (Exception e)
						            {
							            Console.WriteLine($"Failed loading team {team.Name} : {e.Message}");
						            }
					            }

					            db.Update(team);
				            }
			            }
			            else
			            {
				            mission.Title = embed.Title;
				            mission.Description = embed.Description;
				            var user = embed.Author.Value.Name.Split("#");
				            mission.Owner = _client.GetUser(user[0], user[1]).Id;
				            // Do I need author id again?
				            mission.Attachment = embed.Image.HasValue ? embed.Image.Value.Url : null;
				            foreach (var field in embed.Fields)
				            {
					            switch (field.Name)
					            {
						            case "Data:":
							            mission.Date = DateTime.Parse(field.Value);
							            break;
						            case "Modlista:":
							            mission.Modlist = field.Value;
							            break;
						            case "Zamknięcie zapisów:":
							            uint timeDifference;
							            if (!uint.TryParse(field.Value, out timeDifference))
								            mission.CloseDate = DateTime.Parse(field.Value);
							            else
							            {
								            mission.CloseDate = mission.Date.AddMinutes(-timeDifference);
							            }

							            break;

					            }
				            }
			            }
		            }

		            db.Update(mission);
	            }
            }
        }

        private async Task LoadBans(SocketGuild guild)
        {
            var signups = _services.GetService<RuntimeData>();

            Console.WriteLine($"[{DateTime.Now.ToString()}] Loading bans");

            var banChannel = guild.Channels.Single(x => x.Id == _config.HallOfShameChannel) as SocketTextChannel;
            var messages = banChannel.GetMessagesAsync();
            List<IMessage> messagesNormal = new List<IMessage>();
            await messages.ForEachAsync(async x =>
            {
                foreach (var it in x)
                {
                    messagesNormal.Add(it);
                }
            });

            foreach (var message in messagesNormal)
            {
                if (message.Embeds.Count == 1 && message.Content == "Bany na zapisy:" && message.Author.Id == _client.CurrentUser.Id)
                {
                    if (signups.SignupBans.Count > 0)
                        continue;
                    signups.SignupBansMessage = message.Id;

                    await signups.BanAccess.WaitAsync(-1);
                    try
                    {
                        if (message.Embeds.First().Description != null)
                        {
                            string banPattern = @"(\<\@\![0-9]+\>)-(.*)(?:$|\n)";
                            MatchCollection banMatches = Regex.Matches(message.Embeds.First().Description, banPattern, RegexOptions.IgnoreCase);
                            foreach (Match match in banMatches)
                            {
                                signups.SignupBans.Add(ulong.Parse(match.Groups[1].Value.Substring(3, match.Groups[1].Value.Length - 4)), DateTime.Parse(match.Groups[2].Value));
                            }
                        }
                    }
                    finally
                    {
                        signups.BanAccess.Release();
                    }
                }
                if (message.Embeds.Count == 1 && message.Content == "Bany za spam reakcjami:" && message.Author.Id == _client.CurrentUser.Id)
                {
                    if (signups.SpamBans.Count > 0)
                        continue;
                    signups.SpamBansMessage = message.Id;

                    await signups.BanAccess.WaitAsync(-1);
                    try
                    {
                        if (message.Embeds.First().Description != null)
                        {
                            string banPattern = @"(\<\@\![0-9]+\>)-(.*)(?:$|\n)";
                            MatchCollection banMatches = Regex.Matches(message.Embeds.First().Description, banPattern, RegexOptions.IgnoreCase);
                            foreach (Match match in banMatches)
                            {
                                signups.SpamBans.Add(ulong.Parse(match.Groups[1].Value.Substring(3, match.Groups[1].Value.Length - 4)), DateTime.Parse(match.Groups[2].Value));
                            }
                        }
                    }
                    finally
                    {
                        signups.BanAccess.Release();
                    }
                }
            }
        }

        private async Task LoadBanHistory(SocketGuild guild)
        {
            var signups = _services.GetService<RuntimeData>();

            var channels = guild.CategoryChannels.Single(x => x.Id == _config.SignupsCategory);

            Console.WriteLine($"[{DateTime.Now.ToString()}] Loading ban history");
            // History of bans
            var shameChannel = guild.Channels.Single(x => x.Id == _config.HallOfShameChannel) as SocketTextChannel;
            var messages = shameChannel.GetMessagesAsync();
            List<IMessage> messagesNormal = new List<IMessage>();
            await messages.ForEachAsync(async x =>
            {
                foreach (var it in x)
                {
                    messagesNormal.Add(it);
                }
            });

            foreach (var message in messagesNormal)
            {
                if (message.Embeds.Count == 1 && message.Content == "Historia banów na zapisy:" && message.Author.Id == _client.CurrentUser.Id)
                {
                    if (signups.SignupBansHistory.Count > 0)
                        continue;
                    signups.SignupBansHistoryMessage = message.Id;

                    await signups.BanAccess.WaitAsync(-1);
                    try
                    {
                        if (message.Embeds.First().Description != null)
                        {
                            string banPattern = @"(\<\@\![0-9]+\>)-([0-9]+)-([0-9]+)(?:$|\n)";
                            MatchCollection banMatches = Regex.Matches(message.Embeds.First().Description, banPattern, RegexOptions.IgnoreCase);
                            foreach (Match match in banMatches)
                            {
                                signups.SignupBansHistory.Add(
                                    ulong.Parse(match.Groups[1].Value.Substring(3, match.Groups[1].Value.Length - 4)),
                                    new Tuple<uint, uint>(
                                        uint.Parse(match.Groups[2].Value),
                                        uint.Parse(match.Groups[3].Value)));
                            }
                            Console.WriteLine($"[{DateTime.Now.ToString()}] Loaded signup ban history");
                        }
                    }
                    finally
                    {
                        signups.BanAccess.Release();
                    }
                }
                if (message.Embeds.Count == 1 && message.Content == "Historia banów za spam reakcjami:" && message.Author.Id == _client.CurrentUser.Id)
                {
                    if (signups.SpamBansHistory.Count > 0)
                        continue;
                    signups.SpamBansHistoryMessage = message.Id;

                    await signups.BanAccess.WaitAsync(-1);
                    try
                    {
                        if (message.Embeds.First().Description != null)
                        {
                            string banPattern = @"(\<\@\![0-9]+\>)-([0-9]+)-(.*)-(.*)(?:$|\n)";
                            MatchCollection banMatches = Regex.Matches(message.Embeds.First().Description, banPattern, RegexOptions.IgnoreCase);
                            foreach (Match match in banMatches)
                            {
                                signups.SpamBansHistory.Add(
                                    ulong.Parse(match.Groups[1].Value.Substring(3, match.Groups[1].Value.Length - 4)),
                                    new Tuple<uint, DateTime, RuntimeData.BanType>(
                                        uint.Parse(match.Groups[2].Value),
                                        DateTime.Parse(match.Groups[3].Value),
                                        (RuntimeData.BanType)Enum.Parse(typeof(RuntimeData.BanType), match.Groups[4].Value)));
                            }
                            Console.WriteLine($"[{DateTime.Now.ToString()}] Loaded reaction spam ban history");
                        }
                    }
                    finally
                    {
                        signups.BanAccess.Release();
                    }
                }
            }
        }

        private async Task LoadMissionsArchive(SocketGuild guild)
        {
            var archive = _services.GetService<MissionsArchiveData>();

            var channels = guild.CategoryChannels.Single(x => x.Id == _config.SignupsCategory);

            Console.WriteLine($"[{DateTime.Now.ToString()}] Loading mission history");
            archive.ArchiveMissions.Clear();

            // History of missions
            var archiveChannel = guild.Channels.Single(x => x.Id == _config.SignupsArchive) as SocketTextChannel;
            var messages = archiveChannel.GetMessagesAsync(limit: 10000);
            List<IMessage> messagesNormal = new List<IMessage>();
            await messages.ForEachAsync(async x =>
            {
                foreach (var it in x)
                {
                    messagesNormal.Add(it);
                }
            });

            foreach (var message in messagesNormal)
            {
                if (message.Embeds.Count == 0)
                    continue;

                if (message.Author.Id != _client.CurrentUser.Id)
                    continue;

                var embed = message.Embeds.Single();

                var newArchiveMission = new MissionsArchiveData.Mission();

                newArchiveMission.Title = embed.Title;
                if(!DateTime.TryParse(embed.Footer.Value.Text, out newArchiveMission.Date))
                {
                    Console.WriteLine($"Loading failed on mission date: {embed.Footer.Value.Text}");
                    continue;
                }
                newArchiveMission.CloseTime = message.Timestamp.DateTime;
                newArchiveMission.Description = embed.Description;
                newArchiveMission.Attachment = embed.Image.HasValue ? embed.Image.Value.Url : null;

                ulong signedUsers = 0;
                ulong slots = 0;
                foreach(var field in embed.Fields)
                {
                    switch(field.Name)
                    {
                        case "Zamknięcie zapisów:":
                        case "Data:":
                            break;
                        case "Modlista:":
                        case "Modlista":
                            newArchiveMission.Modlist = field.Value;
                            break;
                        default:
                            string signedPattern = @"(.+)(?:\(.*\))?-(\<\@\!([0-9]+)\>)?";
                            MatchCollection signedMatches = Regex.Matches(field.Value, signedPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                            foreach (Match match in signedMatches.Reverse())
                            {
                                slots++;
                                if (match.Groups[2].Success)
                                    signedUsers++;
                            }
                            break;
                    }
                }

                newArchiveMission.FreeSlots = slots - signedUsers;
                newArchiveMission.AllSlots = slots;

                archive.ArchiveMissions.Add(newArchiveMission);
            }

            // Sort channels by date
            archive.ArchiveMissions.Sort((x, y) =>
            {
                return x.Date.CompareTo(y.Date);
            });

            Console.WriteLine($"[{DateTime.Now.ToString()}] Loaded {archive.ArchiveMissions.Count} archive missions");
        }
    }
}
