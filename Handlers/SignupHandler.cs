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

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _client.ReactionAdded += HandleReactionAdded;
            _client.ReactionRemoved += HandleReactionRemoved;
            _client.ChannelDestroyed += HandleChannelRemoved;
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
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
                        if (team.Slots.Any(x => x.Key == reaction.Emote.ToString() && x.Value > 0))
                        {
                            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;

                            var embed = teamMsg.Embeds.Single();

                            if (!mission.SignedUsers.Any(x => x == reaction.User.Value.Id))
                            {
                                var regex = new Regex(Regex.Escape(reaction.Emote.ToString()) + @"-(?:$|\n)");
                                var newDescription = regex.Replace(embed.Description, reaction.Emote.ToString() + "-" + reaction.User.Value.Mention + "\n", 1);
                                //var newDescription = embed.Description.Replace(reaction.Emote.ToString() + "-", reaction.Emote.ToString() + "-" + reaction.User.Value.Mention);

                                var newEmbed = new EmbedBuilder
                                {
                                    Title = embed.Title,
                                    Description = newDescription,
                                    Color = embed.Color
                                };

                                await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                                var slot = team.Slots.Single(x => x.Key == reaction.Emote.ToString());
                                team.Slots[slot.Key]--;
                                mission.SignedUsers.Add(reaction.User.Value.Id);
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

                            if (embed.Description != null && embed.Description.Contains(textEmote))
                            {
                                var removeNewLine = !embed.Description.EndsWith(textEmote) && !embed.Description.StartsWith(textEmote);

                                var newDescription = embed.Description.Replace(reaction.Emote.ToString() + "-" + reaction.User.Value.Mention, reaction.Emote.ToString() + "-");

                                var newEmbed = new EmbedBuilder
                                {
                                    Title = embed.Title,
                                    Description = newDescription,
                                    Color = embed.Color
                                };

                                await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                                var slot = team.Slots.Single(x => x.Key == reaction.Emote.ToString());
                                team.Slots[slot.Key]++;
                                mission.SignedUsers.Remove(reaction.User.Value.Id);
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

            if(signups.Missions.Any(x => x.SignupChannel == channel.Id))
            {
                signups.Missions.RemoveAll(x => x.SignupChannel == channel.Id);
            }
        }
    }
}
