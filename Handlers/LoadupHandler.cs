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
            _client.GuildAvailable += LoadMissions;
        }

        private async Task LoadMissions(SocketGuild guild)
        {
            var signups = _services.GetService<SignupsData>();

            var channels = guild.CategoryChannels.Single(x => x.Id == _config.SignupsCategory);

            foreach(var channel in channels.Channels.Where(x => x.Id != _config.SignupsArchive && x.Id != _config.CreateMissionChannel && x.Id != _config.HallOfShameChannel).Reverse())
            {
                if (signups.Missions.Any(x => x.SignupChannel == channel.Id))
                    continue;
                var mission = new SignupsData.SignupsInstance();

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

                foreach(var message in messagesNormal)
                {
                    if (message.Embeds.Count == 0)
                        continue;

                    var embed = message.Embeds.Single();
                    if (embed.Author == null)
                    {
                        string rolePattern = @"(\<.+?\>)?(?: (.+?))?(?: )+(\[[0-9]+\])";
                        MatchCollection matches = Regex.Matches(embed.Title, rolePattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

                        if (matches.Count > 0)
                        {
                            var team = new SignupsData.SignupsInstance.Team();
                            team.Name = embed.Title;
                            foreach (Match match in matches.Reverse())
                            {
                                team.Slots.Add(match.Groups[2].Value, int.Parse(match.Groups[3].Value.Substring(1, match.Groups[3].Value.Length - 2)));
                            }

                            if (embed.Description != null)
                            {
                                string signedPattern = @"(.+)-(\<\@\![0-9]+\>)";
                                MatchCollection signedMatches = Regex.Matches(embed.Description, signedPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                                foreach (Match match in signedMatches.Reverse())
                                {
                                    mission.SignedUsers.Add(ulong.Parse(match.Groups[2].Value.Substring(3, match.Groups[2].Value.Length-4)));
                                    team.Signed.Add(match.Groups[2].Value, match.Groups[1].Value);
                                }
                            }

                            team.TeamMsg = message.Id;
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
                        foreach(var field in embed.Fields)
                        {
                            switch(field.Name)
                            {
                                case "Data:":
                                    mission.Date = DateTime.Parse(field.Value);
                                    break;
                                case "Modlista:":
                                    mission.Modlist = field.Value;
                                    break;
                                case "Zamknięcie zapisów:":
                                    mission.CloseTime = uint.Parse(field.Value);
                                    break;

                            }
                        }
                    }
                }

                signups.Missions.Add(mission);
            }

            {
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
            }

            {
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
                            }
                        }
                        finally
                        {
                            signups.BanAccess.Release();
                        }
                    }
                }
            }
        }
    }
}
