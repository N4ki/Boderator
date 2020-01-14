using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Handlers
{
    public class DialogHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;
        private OpenedDialogs _dialogs;

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _dialogs = map.GetService<OpenedDialogs>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _client.ReactionAdded += HandleReactionAdded;
            _client.ReactionRemoved += HandleReactionRemoved;
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (_dialogs.Dialogs.Any(x => x.DialogID == message.Id && x.DialogOwner == reaction.UserId))
            {
                var dialog = _dialogs.Dialogs.Single(x => x.DialogID == message.Id);

                if (dialog != null && reaction.UserId != _client.CurrentUser.Id)
                {
                    if (dialog.Buttons.ContainsKey(reaction.Emote.ToString()))
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        dialog.Buttons[reaction.Emote.ToString()].Invoke(dialog);
                    }
                }
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
        }
    }
}
