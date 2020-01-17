using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ArmaforcesMissionBot.DataClasses.SignupsData;

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
            await LoadBanHistory(guild);
            await LoadMissionsArchive(guild);
        }

        private async Task LoadMissions(SocketGuild guild)
        {
            var signups = _services.GetService<SignupsData>();

            var channels = guild.CategoryChannels.Single(x => x.Id == _config.SignupsCategory);

            Console.WriteLine($"[{DateTime.Now.ToString()}] Loading missions");

            foreach (var channel in channels.Channels.Where(x => x.Id != _config.SignupsArchive && x.Id != _config.CreateMissionChannel && x.Id != _config.HallOfShameChannel).Reverse())
            {
                if (signups.Missions.Any(x => x.SignupChannel == channel.Id))
                    continue;
                var mission = new ArmaforcesMissionBotSharedClasses.Mission();

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

                foreach (var message in messagesNormal)
                {
                    if (message.Embeds.Count == 0)
                        continue;

                    if (message.Author.Id != _client.CurrentUser.Id)
                        continue;

                    var embed = message.Embeds.Single();
                    if (embed.Author == null)
                    {
                        string unicodeEmoji = @"(?:\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])";
                        string emote = $@"((?:<?:.+?:(?:[0-9]+>)?)|{unicodeEmoji})";
                        string slotCount = @"(\[[0-9]+\])";
                        string slotName = @"([^\|]*?)?";
                        string rolePattern = $@"[ ]*{emote}[ ]*{slotCount}[ ]*{slotName}[ ]*(?:\|)?";

                        MatchCollection matches;
                        var pattern = "";
                        if (embed.Footer.HasValue)
                        {
                            pattern = embed.Footer.Value.Text;
                            matches = Regex.Matches(pattern, rolePattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                        }
                        else
                        {
                            matches = Regex.Matches(embed.Title, rolePattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                        }

                        if (matches.Count > 0)
                        {
                            var team = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                            team.Name = embed.Title;
                            foreach (Match match in matches.Reverse())
                            {
                                var icon = match.Groups[1].Value;
                                if (icon[0] == ':')
                                {
                                    var emotes = Program.GetEmotes();
                                    var foundEmote = emotes.Single(x => x.Name == icon.Substring(1, icon.Length - 2));
                                    var animated = foundEmote.Animated ? "a" : "";
                                    icon = $"<{animated}:{foundEmote.Name}:{foundEmote.Id}>";
                                    pattern = pattern.Replace(match.Groups[1].Value, icon);
                                }
                                var count = match.Groups[2].Value;
                                var name = match.Groups[3].Success ? match.Groups[3].Value : "";
                                var slot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot(
                                    name,
                                    icon,
                                    int.Parse(count.Substring(1, count.Length - 2)));
                                team.Slots.Add(slot);

                                Console.WriteLine($"New slot {slot.Emoji} [{slot.Count}] {slot.Name}");
                            }

                            if (embed.Description != null)
                            {
                                try
                                {
                                    string signedPattern = @"(.+)-\<\@\!([0-9]+)\>";
                                    MatchCollection signedMatches = Regex.Matches(embed.Description, signedPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                                    foreach (Match match in signedMatches.Reverse())
                                    {
                                        mission.SignedUsers.Add(ulong.Parse(match.Groups[2].Value.Substring(3, match.Groups[2].Value.Length - 4)));
                                        Console.WriteLine($"{match.Groups[1].Value} : {match.Groups[2].Value}");
                                        team.Slots.Single(x => x.Emoji == match.Groups[1].Value).Signed.Add(ulong.Parse(match.Groups[2].Value));
                                    }
                                }
                                catch(Exception e)
                                {
                                    Console.WriteLine($"Failed loading team {team.Name} : {e.Message}");
                                }
                            }

                            team.TeamMsg = message.Id;
                            if (embed.Footer.HasValue)
                                team.Pattern = pattern;
                            mission.Teams.Add(team);
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
                                        mission.CloseTime = DateTime.Parse(field.Value);
                                    else
                                    {
                                        mission.CloseTime = mission.Date.AddMinutes(-timeDifference);
                                    }
                                    break;

                            }
                        }
                    }
                }

                mission.Teams.Reverse(); // As teams were read backwards due to reading messages backwards

                signups.Missions.Add(mission);
            }

            // Sort channels by date
            signups.Missions.Sort((x, y) =>
            {
                return x.Date.CompareTo(y.Date);
            });

            /*{
                var banChannel = guild.Channels.Single(x => x.Id == _config.BanAnnouncementChannel) as SocketTextChannel;
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
            }*/
        }

        private async Task LoadBanHistory(SocketGuild guild)
        {
            var signups = _services.GetService<SignupsData>();

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
                                    new Tuple<uint, DateTime, BanType>(
                                        uint.Parse(match.Groups[2].Value),
                                        DateTime.Parse(match.Groups[3].Value),
                                        (BanType)Enum.Parse(typeof(BanType), match.Groups[4].Value)));
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
                    if (field.Name == "Zamknięcie zapisów:" ||
                        field.Name == "Modlista:" ||
                        field.Name == "Modlista")
                        continue;

                    string signedPattern = @"(.+)-(\<\@\!([0-9]+)\>)?";
                    MatchCollection signedMatches = Regex.Matches(field.Value, signedPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                    foreach (Match match in signedMatches.Reverse())
                    {
                        slots++;
                        if(match.Groups[2].Success)
                            signedUsers++;
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
