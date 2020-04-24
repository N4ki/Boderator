using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.Extensions;
using ArmaforcesMissionBot.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Modules
{
    [Name("Różne")]
    public class Misc : ModuleBase<SocketCommandContext>
    {
        public IServiceProvider _map { get; set; }
        public DiscordSocketClient _client { get; set; }
        public Config _config { get; set; }
        public OpenedDialogs _dialogs { get; set; }
        public CommandService _commands { get; set; }

        public Misc()
        {
            //_map = map;
        }

        [Command("snipe")]
        [Summary("Wyświetla ostatnio usunięte wiadomości z tego kanału.")]
        public async Task Snipe(int count = 1)
        {
            count = Math.Min(count, 5);
            foreach (var message in MessageHandler._cachedDeletedMessages[Context.Channel.Id].Take(count))
            {
                var embed = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithAuthor(message.Author)
                        .WithDescription(message.Content)
                        .WithTimestamp(message.CreatedAt);
                if (message.Attachments.Any())
                {
                    MemoryStream stream = new MemoryStream();
                    stream.Write(MessageHandler._cachedImages[message.Id], 0, MessageHandler._cachedImages[message.Id].Length);
                    stream.Position = 0;
                    embed.WithImageUrl($"attachment://{message.Attachments.First().Filename}");
                    await Context.Channel.SendFileAsync(stream, message.Attachments.First().Filename, embed: embed.Build());
                }
                else
                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
            }
        }

        [Command("editsnipe")]
        [Summary("Wyświetla ostatnio edytowane wiadomości z tego kanału.")]
        public async Task EditSnipe(int count = 1)
        {
            count = Math.Min(count, 5);
            foreach (var message in MessageHandler._cachedEditedMessages[Context.Channel.Id].Take(count))
            {
                var embed = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithAuthor(message.Author)
                        .WithDescription(message.Content)
                        .WithTimestamp(message.CreatedAt);
                await Context.Channel.SendMessageAsync("", embed: embed.Build());
            }
        }

        [Command("help")]
        [Summary("Wyświetla tą wiadomość.")]
        public async Task Help()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Dostępne komendy:");

            foreach (var module in _commands.Modules)
            {
                string description = "";
                foreach (var command in module.Commands)
                {
                    if ((await command.CheckPreconditionsAsync(Context, _map)).IsSuccess)
                    {
                        var addition = $"**AF!{command.Name}** - {command.Summary}\n";
                        if (description.Length + addition.Length > 1024)
                        {
                            embed.AddField(module.Name, description);
                            description = "";
                        }
                        description += addition;
                    }
                }

                if (description != "")
                    embed.AddField(module.Name, description);
            }

            await ReplyAsync(embed: embed.Build());
        }

        class EmoteUsage
        {
            public int _messages = 0;
            public int _embeds = 0;
            public int _reactions = 0;

            public EmoteUsage() 
            {
            }

            public EmoteUsage(int messages, int embeds, int reactions)
            {
                _messages = messages;
                _embeds = embeds;
                _reactions = reactions;
            }

            public static EmoteUsage operator+(EmoteUsage a, EmoteUsage b)
            {
                return new EmoteUsage(a._messages + b._messages, a._embeds + b._embeds, a._reactions + b._reactions);
            }
        }

        class ChannelTask
        {
            public Task _task;
            public Dictionary<string, EmoteUsage> _localEmoteStats = new Dictionary<string, EmoteUsage>();
            public int _messagesLoaded = 0;
            public IMessage _oldestLoadedMessage;
            public int _lastMessagesLoaded = 0;
        }

        [Command("emojiStats", RunMode = RunMode.Async)]
        [Summary("Wyświetla statystyki emotek serwerowych")]
        [RequireOwner]
        public async Task Stats()
        {
            const int messagesInBatch = 1000;

            var builder = new EmbedBuilder()
                .WithTitle("Analiza")
                .WithDescription("Przeanalizowano: 0 wiadomości\n" +
                    $"Pozostało kanałów: {Context.Guild.Channels.Count}");
            var status = await ReplyAsync(embed: builder.Build());
            var loadedMessages = 0;

            Dictionary<ITextChannel, ChannelTask> tasks = new Dictionary<ITextChannel, ChannelTask>();
            
            Dictionary<string, EmoteUsage> emotesUsage = new Dictionary<string, EmoteUsage>();
            foreach (var emote in Context.Guild.Emotes)
            {
                emotesUsage.Add(emote.ToString(), new EmoteUsage());
            }

            // Get all channels and create tasks to read all messages
            foreach (var channel in Context.Guild.Channels)
            {
                if (channel is ITextChannel)
                {
                    ChannelTask task = new ChannelTask();
                    foreach (var emote in Context.Guild.Emotes)
                    {
                        task._localEmoteStats.Add(emote.ToString(), new EmoteUsage());
                    }

                    Action<IReadOnlyCollection<IMessage>> foreachLambda = async messages =>
                    {
                        foreach (var message in messages)
                        {
                            foreach (var emote in new Dictionary<string, EmoteUsage>(task._localEmoteStats))
                            {
                                var emoteString = emote.Key;
                                task._localEmoteStats[emoteString]._messages += message.Content.CountStrings(emoteString);
                                foreach(var embed in message.Embeds)
                                {
                                    if(embed.Title != null)
                                        task._localEmoteStats[emoteString]._embeds += embed.Title.CountStrings(emoteString);
                                    if(embed.Description != null)
                                        task._localEmoteStats[emoteString]._embeds += embed.Description.CountStrings(emoteString);
                                    if(embed.Footer.HasValue)
                                        task._localEmoteStats[emoteString]._embeds += embed.Footer.Value.Text.CountStrings(emoteString);
                                    foreach(var embedField in embed.Fields)
                                    {
                                        if(embedField.Name != null)
                                            task._localEmoteStats[emoteString]._embeds += embedField.Name.CountStrings(emoteString);
                                        if (embedField.Value != null)
                                            task._localEmoteStats[emoteString]._embeds += embedField.Value.CountStrings(emoteString);
                                    }
                                }
                                foreach(var reaction in message.Reactions)
                                {
                                    if(reaction.Key is Emote)
                                        task._localEmoteStats[reaction.Key.ToString()]._reactions += reaction.Value.ReactionCount;
                                }
                            }
                        }
                        if(messages.Any())
                            task._oldestLoadedMessage = messages.Last();
                        task._messagesLoaded += messages.Count;
                    };

                    var messages = (channel as ITextChannel).GetMessagesAsync(limit: messagesInBatch);
                    task._task = messages.ForEachAsync(foreachLambda);
                    tasks.Add((channel as ITextChannel), task);
                }
            }

            while(tasks.Any())
            {
                var completedList = tasks.Where(task => task.Value._task.IsCompleted).ToList();


                foreach (var task in completedList)
                {
                    // Double check?
                    if(task.Value._task.IsCompleted)
                    {
                        loadedMessages += task.Value._messagesLoaded - task.Value._lastMessagesLoaded;

                        if (task.Value._messagesLoaded == task.Value._lastMessagesLoaded + messagesInBatch)
                        {
                            Action<IReadOnlyCollection<IMessage>> foreachLambda = async messages =>
                            {
                                foreach (var message in messages)
                                {
                                    foreach (var emote in new Dictionary<string, EmoteUsage>(task.Value._localEmoteStats))
                                    {
                                        var emoteString = emote.Key;
                                        task.Value._localEmoteStats[emoteString]._messages += message.Content.CountStrings(emoteString);
                                        foreach (var embed in message.Embeds)
                                        {
                                            if (embed.Title != null)
                                                task.Value._localEmoteStats[emoteString]._embeds += embed.Title.CountStrings(emoteString);
                                            if (embed.Description != null)
                                                task.Value._localEmoteStats[emoteString]._embeds += embed.Description.CountStrings(emoteString);
                                            if (embed.Footer.HasValue)
                                                task.Value._localEmoteStats[emoteString]._embeds += embed.Footer.Value.Text.CountStrings(emoteString);
                                            foreach (var embedField in embed.Fields)
                                            {
                                                if (embedField.Name != null)
                                                    task.Value._localEmoteStats[emoteString]._embeds += embedField.Name.CountStrings(emoteString);
                                                if (embedField.Value != null)
                                                    task.Value._localEmoteStats[emoteString]._embeds += embedField.Value.CountStrings(emoteString);
                                            }
                                        }
                                        foreach (var reaction in message.Reactions)
                                        {
                                            if (reaction.Key is Emote)
                                                task.Value._localEmoteStats[reaction.Key.ToString()]._reactions += reaction.Value.ReactionCount;
                                        }
                                    }
                                }
                                if(messages.Any())
                                    task.Value._oldestLoadedMessage = messages.Last();
                                task.Value._messagesLoaded += messages.Count;
                            };

                            task.Value._lastMessagesLoaded = task.Value._messagesLoaded;
                            task.Value._task = task.Key.GetMessagesAsync(task.Value._oldestLoadedMessage, Direction.Before, limit: messagesInBatch).ForEachAsync(foreachLambda);
                        }
                        else
                        {
                            foreach (var emote in task.Value._localEmoteStats)
                            {
                                emotesUsage[emote.Key] += emote.Value;
                            }
                            tasks.Remove(task.Key);
                        }
                    }
                }

                if (completedList.Any())
                {
                    var newEmbed = new EmbedBuilder
                    {
                        Title = status.Embeds.First().Title,
                        Description = $"Przeanalizowano: {loadedMessages} wiadomości\n" +
                            $"Pozostało kanałów: {tasks.Count}"
                    };

                    await status.ModifyAsync(x => x.Embed = newEmbed.Build());
                }
            }

            var embed = new EmbedBuilder
            {
                Title = status.Embeds.First().Title,
                Description = $"Przeanalizowano: {loadedMessages} wiadomości\n" +
                    $"Pozostało kanałów: {tasks.Count}"
            };

            string messagesStats = "";
            string embedsStats = "";
            string reactionsStats = "";
            string totalStats = "";

            foreach (var emote in emotesUsage)
            {
                messagesStats += $"{emote.Key}: {emote.Value._messages}\n";
                embedsStats += $"{emote.Key}: {emote.Value._embeds}\n";
                reactionsStats += $"{emote.Key}: {emote.Value._reactions}\n";
                totalStats += $"{emote.Key}: {emote.Value._messages + emote.Value._embeds +emote.Value._reactions}\n";
            }
            embed.AddField("Wiadomości", messagesStats, true);
            embed.AddField("Embedy", embedsStats, true);
            embed.AddField("Reakcje", reactionsStats, true);
            embed.AddField("Razem", totalStats, true);

            await status.ModifyAsync(x => x.Embed = embed.Build());
        }
    }
}
