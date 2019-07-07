using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Handlers
{
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
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var signups = _services.GetService<SignupsData>();

            if (reaction.User.IsSpecified && !reaction.User.Value.IsBot && signups.Missions.Any(x => x.SignupChannel == channel.Id))
            {
                var mission = signups.Missions.Single(x => x.SignupChannel == channel.Id);

                if(mission.Teams.Any(x => x.TeamMsg == message.Id))
                {
                    var team = mission.Teams.Single(x => x.TeamMsg == message.Id);
                    if(team.Slots.Any(x => x.Key == reaction.Emote.ToString() && x.Value > 0))
                    {
                        var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;

                        var embed = teamMsg.Embeds.Single();

                        if (!await CheckIsSignedUpAsync(mission, message, channel, reaction.User.Value.Mention))
                        {
                            var newDescription = embed.Description == null ? "" : embed.Description + "\n";
                            newDescription += reaction.Emote.ToString() + "-" + reaction.User.Value.Mention;

                            var newEmbed = new EmbedBuilder
                            {
                                Title = embed.Title,
                                Description = newDescription
                            };

                            await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                            var slot = team.Slots.Single(x => x.Key == reaction.Emote.ToString());
                            team.Slots[slot.Key]--;
                        }
                        else
                        {
                            await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        }
                    }
                    else if(team.Slots.Any(x => x.Key == reaction.Emote.ToString()))
                    {
                        var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
                        await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    }
                }
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var signups = _services.GetService<SignupsData>();

            if (reaction.User.IsSpecified && !reaction.User.Value.IsBot && signups.Missions.Any(x => x.SignupChannel == channel.Id))
            {
                var mission = signups.Missions.Single(x => x.SignupChannel == channel.Id);

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

                            var newDescription = embed.Description.Remove(
                                embed.Description.IndexOf(textEmote),
                                removeNewLine ? textEmote.Length + 1 : textEmote.Length);

                            var newEmbed = new EmbedBuilder
                            {
                                Title = embed.Title,
                                Description = newDescription
                            };

                            await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                            var slot = team.Slots.Single(x => x.Key == reaction.Emote.ToString());
                            team.Slots[slot.Key]++;
                        }
                    }
                }
            }
        }

        private async Task<bool> CheckIsSignedUpAsync(SignupsData.SignupsInstance mission, Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, string mention)
        {
            foreach (var team in mission.Teams)
            {
                var teamMsg = await channel.GetMessageAsync(team.TeamMsg) as IUserMessage;
                var embed = teamMsg.Embeds.Single();

                if (embed.Description != null && embed.Description.Contains(mention))
                    return true;
            }
            return false;
        }
    }
}
