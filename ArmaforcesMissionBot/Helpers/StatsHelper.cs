using ArmaforcesMissionBot.Extensions;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ArmaforcesMissionBot.Modules.Stats;

namespace ArmaforcesMissionBot.Helpers
{
	public class StatsHelper
	{
        private class ChannelTask
        {
            public Task<IEnumerable<IMessage>> _task;
            public Dictionary<string, EmoteUsage> _localEmoteStats = new Dictionary<string, EmoteUsage>();
            public int _messagesLoaded = 0;
            public IMessage _oldestLoadedMessage;
            public int _lastMessagesLoaded = 0;

            public ChannelTask(IReadOnlyCollection<GuildEmote> emotes)
            {
                foreach (var emote in emotes)
                {
                    _localEmoteStats.Add(emote.ToString(), new EmoteUsage());
                }
            }
        }

        public class Statistics
        {
            public Dictionary<string, EmoteUsage> _emotesUsage = new Dictionary<string, EmoteUsage>();
            public Dictionary<ulong, IMessage> _newestMessageInCache = new Dictionary<ulong, IMessage>();
            public Dictionary<ulong, List<IMessage>> _messages = new Dictionary<ulong, List<IMessage>>();

            public Statistics(SocketGuild guild)
            {
                foreach (var emote in guild.Emotes)
                {
                    _emotesUsage.Add(emote.ToString(), new EmoteUsage());
                }

                foreach (var channel in guild.Channels)
                {
                    if (channel is ITextChannel textChannel)
                        _messages[textChannel.Id] = new List<IMessage>();
                }
            }
        }

        private const int _messagesInBatch = 2000;
        private static Statistics _cache = null;

        public static async Task PrintStats(IReadOnlyCollection<GuildEmote> emotes, Dictionary<string, EmoteUsage> emotesUsage, string fieldName, string name, ISocketMessageChannel channel)
        {
            var embed = new EmbedBuilder().WithTitle(name);
            string message = "";
            Type statType = typeof(EmoteUsage);

            foreach (var emote in emotesUsage.OrderByDescending(x => statType.GetField(fieldName).GetValue(x.Value)))
            {
                string emoteString = emote.Key;
                if (emotes.Any(x => x.ToString() == emoteString))
                {
                    string messageTmp = $"{emoteString}: {statType.GetField(fieldName).GetValue(emote.Value)}\n";

                    if (message.Length + messageTmp.Length > 1024)
                    {
                        embed.AddField(name, message, true);
                        message = messageTmp;
                    }
                    else
                        message += messageTmp;
                }
            }
            embed.AddField(name, message, true);

            await channel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task PrintTotal(IReadOnlyCollection<GuildEmote> emotes, Dictionary<string, EmoteUsage> emotesUsage, ISocketMessageChannel channel)
        {
            var embed = new EmbedBuilder().WithTitle("Razem");
            string message = "";
            Type statType = typeof(EmoteUsage);

            foreach (var emote in emotesUsage.OrderByDescending(x => x.Value._messages + x.Value._embeds + x.Value._reactions))
            {
                string emoteString = emote.Key;
                if (emotes.Any(x => x.ToString() == emoteString))
                {
                    string messageTmp = $"{emoteString}: {emote.Value._messages + emote.Value._embeds + emote.Value._reactions}\n";

                    if (message.Length + messageTmp.Length > 1024)
                    {
                        embed.AddField("Razem", message, true);
                        message = messageTmp;
                    }
                    else
                        message += messageTmp;
                }
            }
            embed.AddField("Razem", message, true);

            await channel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task<Statistics> GetCache(SocketGuild guild, IUserMessage statusMsg, bool reloadCache = false)
        {
            var loadedMessages = 0;

            Direction searchDir = Direction.Before;
            // Initialize cache if not present
            if (_cache == null || reloadCache)
                _cache = new Statistics(guild);
            else
                searchDir = Direction.After;

            Dictionary<ITextChannel, ChannelTask> tasks = new Dictionary<ITextChannel, ChannelTask>();

            // Get all channels and create tasks to read all messages
            foreach (var channel in guild.Channels)
            {
                if (channel is ITextChannel textChannel)
                {
                    ChannelTask task = new ChannelTask(guild.Emotes);

                    if (searchDir == Direction.Before)
                        task._task = textChannel.GetMessagesAsync(limit: _messagesInBatch).FlattenAsync();
                    else if (_cache._newestMessageInCache.ContainsKey(textChannel.Id))
                        task._task = textChannel.GetMessagesAsync(_cache._newestMessageInCache[textChannel.Id], searchDir, limit: _messagesInBatch).FlattenAsync();
                    else
                        continue;
                    tasks.Add(textChannel, task);
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Added task {textChannel.Name} ({tasks.Count})");
                    System.Threading.Thread.Sleep(200);
                }
            }

            while (tasks.Any())
            {
                var completedList = tasks.Where(task => task.Value._task.IsCompleted).ToList();

                foreach (var task in completedList)
                {
                    if (task.Value._task.IsFaulted)
                    {
                        tasks.Remove(task.Key);
                        continue;
                    }

                    var messages = task.Value._task.Result;
                    _cache._messages[task.Key.Id].AddRange(messages);
                    if (messages.Any())
                    {
                        if (!_cache._newestMessageInCache.ContainsKey(task.Key.Id))
                            _cache._newestMessageInCache[task.Key.Id] = messages.First();

                        if (searchDir == Direction.Before && _cache._newestMessageInCache[task.Key.Id].Timestamp < messages.First().Timestamp)
                            _cache._newestMessageInCache[task.Key.Id] = messages.First();
                        else if (searchDir == Direction.After && _cache._newestMessageInCache[task.Key.Id].Timestamp < messages.Last().Timestamp)
                            _cache._newestMessageInCache[task.Key.Id] = messages.Last();
                    }
                    foreach (var message in messages)
                    {
                        foreach (var emote in new Dictionary<string, EmoteUsage>(task.Value._localEmoteStats))
                        {
                            var emoteString = emote.Key;
                            if (!task.Value._localEmoteStats.ContainsKey(emoteString))
                                task.Value._localEmoteStats[emoteString] = new EmoteUsage();

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
                                if (reaction.Key.ToString() == emote.Key.ToString())
                                {
                                    if (!task.Value._localEmoteStats.ContainsKey(reaction.Key.ToString()))
                                        task.Value._localEmoteStats[reaction.Key.ToString()] = new EmoteUsage();

                                    task.Value._localEmoteStats[reaction.Key.ToString()]._reactions += reaction.Value.ReactionCount;
                                }
                            }
                        }
                    }
                    if (messages.Any())
                        task.Value._oldestLoadedMessage = messages.Last();
                    task.Value._messagesLoaded += messages.Count();
                    loadedMessages += task.Value._messagesLoaded - task.Value._lastMessagesLoaded;

                    if (task.Value._messagesLoaded == task.Value._lastMessagesLoaded + _messagesInBatch)
                    {

                        task.Value._lastMessagesLoaded = task.Value._messagesLoaded;
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Updated task {task.Key.Name} ({task.Value._messagesLoaded}) - [{task.Value._oldestLoadedMessage.Timestamp.ToString()}], tasks: {tasks.Count}");
                        if (searchDir == Direction.Before)
                            task.Value._task = task.Key.GetMessagesAsync(task.Value._oldestLoadedMessage, searchDir, limit: _messagesInBatch).FlattenAsync();
                        else
                            task.Value._task = task.Key.GetMessagesAsync(_cache._newestMessageInCache[task.Key.Id], searchDir, limit: _messagesInBatch).FlattenAsync();
                        System.Threading.Thread.Sleep(200);
                    }
                    else
                    {
                        foreach (var emote in task.Value._localEmoteStats)
                        {
                            if (!_cache._emotesUsage.ContainsKey(emote.Key))
                                _cache._emotesUsage[emote.Key] = new EmoteUsage();

                            _cache._emotesUsage[emote.Key] += emote.Value;
                        }
                        tasks.Remove(task.Key);
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Updated task {task.Key.Name} ({task.Value._messagesLoaded}), tasks: {tasks.Count}");
                    }
                }

                if (completedList.Any())
                {
                    var newEmbed = new EmbedBuilder
                    {
                        Title = statusMsg.Embeds.First().Title,
                        Description = $"Przeanalizowano: {loadedMessages} wiadomości\n" +
                            $"Pozostało kanałów: {tasks.Count}"
                    };

                    await statusMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                }
            }

            return _cache;
        }
    }
}
