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

            foreach(var channel in channels.Channels.Where(x => x.Id != _config.SignupsArchive && x.Id != _config.CreateMissionChannel))
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
                                team.Slots.Add(match.Groups[3].Value, int.Parse(match.Groups[4].Value.Substring(1, match.Groups[4].Value.Length - 2)));
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
                                case "Modlista":
                                    mission.Modlist = field.Value;
                                    break;
                            }
                        }
                    }
                }

                signups.Missions.Add(mission);
            }
        }
    }
}
