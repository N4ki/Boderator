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
using System.Timers;

namespace ArmaforcesMissionBot.Handlers
{
    public static class StringExtensionMethods
    {
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
    public class SignupHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;
        private Timer _timer;

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _client.ReactionAdded += HandleReactionAdded;
            _client.ReactionRemoved += HandleReactionRemoved;
            _client.ChannelDestroyed += HandleChannelRemoved;

            _timer = new Timer();
            _timer.Interval = 2000;

            _timer.Elapsed += CheckReactionTimes;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var signups = _services.GetService<SignupsData>();

            if (reaction.User.IsSpecified && !reaction.User.Value.IsBot && signups.Missions.Any(x => x.SignupChannel == channel.Id))
            {
                var mission = signups.Missions.Single(x => x.SignupChannel == channel.Id);

                if (signups.SignupBans.ContainsKey(reaction.User.Value.Id) && signups.SignupBans[reaction.User.Value.Id] > mission.Date)
                {
                    await reaction.User.Value.SendMessageAsync("Masz bana na zapisy, nie możesz zapisać się na misję, która odbędzie się w czasie trwania bana.");
                    var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
                    await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    return;
                }

                await HandleReactionChange(message, channel, reaction, signups);

                await mission.Access.WaitAsync();
                try
                {
                    if (mission.Teams.Any(x => x.TeamMsg == message.Id))
                    {
                        var team = mission.Teams.Single(x => x.TeamMsg == message.Id);
                        if (team.Slots.Any(x => x.Key == reaction.Emote.ToString() && x.Value > team.Signed.Where(y => y.Value == x.Key).Count()))
                        {
                            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;

                            var embed = teamMsg.Embeds.Single();

                            if (!mission.SignedUsers.Any(x => x == reaction.User.Value.Id))
                            {
                                var slot = team.Slots.Single(x => x.Key == reaction.Emote.ToString());
                                team.Signed.Add(reaction.User.Value.Mention, slot.Key);
                                mission.SignedUsers.Add(reaction.User.Value.Id);

                                var newDescription = Helpers.MiscHelper.BuildTeamSlots(team);

                                var newEmbed = new EmbedBuilder
                                {
                                    Title = embed.Title,
                                    Description = newDescription,
                                    Color = embed.Color
                                };

                                await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                            }
                            else
                            {
                                await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            }
                        }
                        else if (team.Slots.Any(x => x.Key == reaction.Emote.ToString()))
                        {
                            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
                            await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        }
                    }
                }
                finally
                {
                    mission.Access.Release();
                }
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var signups = _services.GetService<SignupsData>();

            if (reaction.User.IsSpecified && !reaction.User.Value.IsBot && signups.Missions.Any(x => x.SignupChannel == channel.Id))
            {
                var mission = signups.Missions.Single(x => x.SignupChannel == channel.Id);

                await mission.Access.WaitAsync();
                try
                {
                    if (mission.Teams.Any(x => x.TeamMsg == message.Id))
                    {
                        var team = mission.Teams.Single(x => x.TeamMsg == message.Id);
                        if (team.Slots.Any(x => x.Key == reaction.Emote.ToString()))
                        {
                            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
                            var embed = teamMsg.Embeds.Single();

                            var textEmote = reaction.Emote.ToString() + "-" + reaction.User.Value.Mention;

                            if (team.Signed.Any(x => x.Key == reaction.User.Value.Mention))
                            {
                                var slot = team.Slots.Single(x => x.Key == reaction.Emote.ToString());
                                team.Signed.Remove(reaction.User.Value.Mention);
                                mission.SignedUsers.Remove(reaction.User.Value.Id);

                                var newDescription = Helpers.MiscHelper.BuildTeamSlots(team);

                                var newEmbed = new EmbedBuilder
                                {
                                    Title = embed.Title,
                                    Description = newDescription,
                                    Color = embed.Color
                                };

                                await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                            }
                        }
                    }
                }
                finally
                {
                    mission.Access.Release();
                }
            }
        }

        private async Task HandleChannelRemoved(SocketChannel channel)
        {
            var signups = _services.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.SignupChannel == channel.Id))
            {
                signups.Missions.RemoveAll(x => x.SignupChannel == channel.Id);
            }
        }

        private async Task HandleReactionChange(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction, SignupsData signups)
        {
            await signups.BanAccess.WaitAsync();
            try
            {
                if (!signups.ReactionTimes.ContainsKey(reaction.User.Value.Id))
                {
                    signups.ReactionTimes[reaction.User.Value.Id] = new Queue<DateTime>();
                }

                signups.ReactionTimes[reaction.User.Value.Id].Enqueue(DateTime.Now);

                Console.WriteLine(reaction.User.Value.Username + " " + signups.ReactionTimes[reaction.User.Value.Id].Count);

                if (signups.ReactionTimes[reaction.User.Value.Id].Count >= 4 && !signups.SpamBans.ContainsKey(reaction.User.Value.Id))
                {
                    signups.SpamBans.Add(reaction.User.Value.Id, DateTime.Now.AddHours(1));
                    await reaction.User.Value.SendMessageAsync("Za spamowanie reakcji w zapisach został Ci odebrany dostęp na godzinę.");

                    var guild = _client.GetGuild(_config.AFGuild);
                    var contemptChannel = guild.GetTextChannel(_config.PublicContemptChannel);
                    await contemptChannel.SendMessageAsync($"Ten juj chebany {reaction.User.Value.Mention} dostał bana na zapisy na godzine za spam reakcją do zapisów. Wiecie co z nim zrobić.");

                    await Helpers.BanHelper.MakeSpamBanMessage(_services, guild);

                    var socketChannel = channel as SocketTextChannel;
                    await socketChannel.AddPermissionOverwriteAsync(reaction.User.Value, new OverwritePermissions(
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
                        PermValue.Deny));

                    foreach (var mission in signups.Missions)
                    {
                        var missionChannel = guild.GetTextChannel(mission.SignupChannel);
                        missionChannel.AddPermissionOverwriteAsync(reaction.User.Value, new OverwritePermissions(
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
                        PermValue.Deny));
                    }
                }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        private async void CheckReactionTimes(object source, ElapsedEventArgs e)
        {
            var signups = _services.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync();
            try
            {
                foreach(var user in signups.ReactionTimes)
                {
                    while (user.Value.Count > 0 && user.Value.Peek() < e.SignalTime.AddSeconds(-5))
                        user.Value.Dequeue();
                }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }
    }
}
