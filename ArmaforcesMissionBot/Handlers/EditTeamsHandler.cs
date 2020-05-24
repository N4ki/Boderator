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
    public class EditTeamsHandler : IInstallable
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

            if (reaction.User.IsSpecified && signups.Missions.Any(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New && x.Owner == reaction.User.Value.Id && x.EditTeamsMessage == message.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New && x.Owner == reaction.User.Value.Id);

                var reactedMessage = await channel.GetMessageAsync(message.Id) as IUserMessage;

                switch (reaction.Emote.ToString())
                {
                    case "⬆":
                        if (mission.HighlightedTeam > 0)
                        {
                            if (mission.IsMoving)
                            {
                                var tmp = mission.Teams[mission.HighlightedTeam - 1];
                                mission.Teams[mission.HighlightedTeam - 1] = mission.Teams[mission.HighlightedTeam];
                                mission.Teams[mission.HighlightedTeam] = tmp;
                            }
                            mission.HighlightedTeam--;
                        }
                        else
                        {
                            if (mission.IsMoving)
                            {
                                var tmp = mission.Teams[mission.Teams.Count-1];
                                mission.Teams[mission.Teams.Count - 1] = mission.Teams[mission.HighlightedTeam];
                                mission.Teams[mission.HighlightedTeam] = tmp;
                            }
                            mission.HighlightedTeam = mission.Teams.Count - 1;
                        }
                        await reactedMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        break;
                    case "⬇":
                        if (mission.HighlightedTeam < mission.Teams.Count - 1)
                        {
                            if (mission.IsMoving)
                            {
                                var tmp = mission.Teams[mission.HighlightedTeam + 1];
                                mission.Teams[mission.HighlightedTeam + 1] = mission.Teams[mission.HighlightedTeam];
                                mission.Teams[mission.HighlightedTeam] = tmp;
                            }
                            mission.HighlightedTeam++;
                        }
                        else
                        {
                            if (mission.IsMoving)
                            {
                                var tmp = mission.Teams[0];
                                mission.Teams[0] = mission.Teams[mission.HighlightedTeam];
                                mission.Teams[mission.HighlightedTeam] = tmp;
                            }
                            mission.HighlightedTeam = 0;
                        }
                        await reactedMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        break;
                    case "📍":
                        mission.IsMoving = true;
                        break;
                    case "✂":
                        foreach(var slot in mission.Teams.ElementAt(mission.HighlightedTeam).Slots)
                        {
                            foreach(var signed in slot.Signed)
                            {
                                mission.SignedUsers.Remove(signed);
                            }
                        }
                        mission.Teams.RemoveAt(mission.HighlightedTeam);
                        if (mission.HighlightedTeam > mission.Teams.Count - 1)
                            mission.HighlightedTeam--;
                        break;
                    case "🔒":
                        mission.EditTeamsMessage = 0;
                        await reactedMessage.DeleteAsync();
                        return;
                }

                var oldEmbed = reactedMessage.Embeds.First();

                var newEmbed = new EmbedBuilder()
                {
                    Color = oldEmbed.Color,
                    Title = oldEmbed.Title,
                    Description = Helpers.MiscHelper.BuildEditTeamsPanel(mission.Teams, mission.HighlightedTeam)
                };

                await reactedMessage.ModifyAsync(x => x.Embed = newEmbed.Build());
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var signups = _services.GetService<SignupsData>();

            if (signups.Missions.Any(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New && reaction.User.IsSpecified && x.Owner == reaction.User.Value.Id && x.EditTeamsMessage == message.Id))
            {
                var mission = signups.Missions.Single(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New && x.Owner == reaction.User.Value.Id);
                switch (reaction.Emote.ToString())
                {
                    case "📍":
                        mission.IsMoving = false;
                        break;
                }
            }
        }
    }
}
