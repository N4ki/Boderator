using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.Extensions;
using ArmaforcesMissionBot.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Modules
{
    [Name("Statystyki")]
    [Group("stats")]
    public class Stats : ModuleBase<SocketCommandContext>
	{
        public IServiceProvider _map { get; set; }
        public DiscordSocketClient _client { get; set; }
        public Config _config { get; set; }
        public OpenedDialogs _dialogs { get; set; }
        public CommandService _commands { get; set; }

        public class EmoteUsage
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

            public static EmoteUsage operator +(EmoteUsage a, EmoteUsage b)
            {
                return new EmoteUsage(a._messages + b._messages, a._embeds + b._embeds, a._reactions + b._reactions);
            }
        }

        public Stats()
        {
            //_map = map;
        }

        [Command("emoji", RunMode = RunMode.Async)]
        [Summary("Wyświetla statystyki emotek serwerowych.")]
        [RequireOwner]
        public async Task OverallEmojiStats(bool reloadCache = false)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Analiza")
                .WithDescription("Przeanalizowano: 0 wiadomości\n" +
                    $"Pozostało kanałów: {Context.Guild.Channels.Count}");
            var status = await ReplyAsync(embed: builder.Build());

            var cache = await StatsHelper.GetCache(Context.Guild, status, reloadCache);

            await StatsHelper.PrintStats(Context.Guild.Emotes, cache._emotesUsage, "_messages", "Wiadomości", Context.Channel);
            await StatsHelper.PrintStats(Context.Guild.Emotes, cache._emotesUsage, "_embeds", "Embedy", Context.Channel);
            await StatsHelper.PrintStats(Context.Guild.Emotes, cache._emotesUsage, "_reactions", "Reakcje", Context.Channel);
            await StatsHelper.PrintTotal(Context.Guild.Emotes, cache._emotesUsage, Context.Channel);
        }

        [Command("policzEmotke", RunMode = RunMode.Async)]
        [Summary("Wyświetla ile razy używana była podana emotka na serwerze.")]
        [RequireOwner]
        public async Task StatsEmoji(string emote, IUser user = null)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Analiza")
                .WithDescription("Przeanalizowano: 0 wiadomości\n" +
                    $"Pozostało kanałów: {Context.Guild.Channels.Count}");
            var status = await ReplyAsync(embed: builder.Build());

            var cache = await StatsHelper.GetCache(Context.Guild, status, false);

            int countMessages = 0;
            int countEmbeds = 0;
            int countReactions = 0;

            int lastUpdate = 0;
            int analyzedMessages = 0;

            foreach (var channel in Context.Guild.Channels)
            {
                if (channel is ITextChannel textChannel)
                {
                    foreach (var message in cache._messages[textChannel.Id])
                    {
                        var emoteString = emote;

                        if (user == null || message.Author.Id == user.Id)
                        {
                            countMessages += message.Content.CountStrings(emoteString);
                            foreach (var embed in message.Embeds)
                            {
                                if (embed.Title != null)
                                    countEmbeds += embed.Title.CountStrings(emoteString);
                                if (embed.Description != null)
                                    countEmbeds += embed.Description.CountStrings(emoteString);
                                if (embed.Footer.HasValue)
                                    countEmbeds += embed.Footer.Value.Text.CountStrings(emoteString);
                                foreach (var embedField in embed.Fields)
                                {
                                    if (embedField.Name != null)
                                        countEmbeds += embedField.Name.CountStrings(emoteString);
                                    if (embedField.Value != null)
                                        countEmbeds += embedField.Value.CountStrings(emoteString);
                                }
                            }
                        }
                        foreach (var reaction in message.Reactions)
                        {
                            if (reaction.Key.ToString() == emoteString)
                            {
                                if(user == null)
                                    countReactions += reaction.Value.ReactionCount;
                                else if((await message.GetReactionUsersAsync(reaction.Key, 10000).FlattenAsync()).Where(x => x.Id == user.Id).Any())
                                    countReactions++;
                            }
                        }
                        analyzedMessages++;
                        if (lastUpdate + 1000 <= analyzedMessages)
                        {
                            builder = new EmbedBuilder
                            {
                                Title = status.Embeds.First().Title,
                                Description = $"Przeanalizowano: {analyzedMessages} wiadomości\n"
                            };
                            lastUpdate = analyzedMessages;

                            await status.ModifyAsync(x => x.Embed = builder.Build());
                        }
                    }
                }
            }

            var newEmbed = new EmbedBuilder
            {
                Title = status.Embeds.First().Title,
                Description = status.Embeds.First().Description
            };
            newEmbed.AddField("Wiadomości", $"{emote}: {countMessages}");
            newEmbed.AddField("Embedy", $"{emote}: {countEmbeds}");
            newEmbed.AddField("Reakcje", $"{emote}: {countReactions}");
            newEmbed.AddField("Razem", $"{emote}: {countMessages + countEmbeds + countReactions}");

            await status.ModifyAsync(x => x.Embed = newEmbed.Build());
        }
    }
}
