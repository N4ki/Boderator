using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LinqToDB;
using Timer = System.Timers.Timer;
using ArmaforcesMissionBot.DataClasses.SQL;

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

            _timer = new Timer();
            _timer.Interval = 2000;

            _timer.Elapsed += CheckReactionTimes;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var reactionStringAnimatedVersion = reaction.Emote.ToString().Insert(1, "a");

            using (var db = new DbBoderator())
            {
	            if (reaction.User.IsSpecified && !reaction.User.Value.IsBot && db.Missions.Any(x => x.SignupChannel == channel.Id))
	            {
		            await HandleReactionChange(message, channel, reaction);
		            Console.WriteLine($"[{DateTime.Now}] {reaction.User.Value.Username} added reaction {reaction.Emote.Name}");

		            await _services.GetService<RuntimeData>().GetTeamSemaphore(message.Id).WaitAsync(-1);
		            try
		            {
			            var count = db.Signed.Count(q => q.TeamID == message.Id && (q.Emoji == reaction.Emote.ToString() || q.Emoji == reactionStringAnimatedVersion));

			            var slots = db.Slots.Where(q => q.TeamID == message.Id && (q.Emoji == reaction.Emote.ToString() || q.Emoji == reactionStringAnimatedVersion) && q.Count > count);

			            if (slots.Any())
			            {
				            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;

				            var embed = teamMsg.Embeds.Single();

				            var query =
					            from u in db.Signed.Where(q => q.UserID == reaction.UserId)
					            join s in db.Slots on new { u.Emoji, u.TeamID } equals new { s.Emoji, s.TeamID }
                                join t in db.Teams on s.TeamID equals t.TeamMsg
                                where t.MissionID == channel.Id
					            select t;

				            if (!query.Any())
				            {
				            	var signed = new SignedTbl(reaction.User.Value.Id, reaction.Emote.ToString(), message.Id);
				            	db.Insert(signed);

				            	var newDescription = Helpers.MiscHelper.BuildTeamSlots(message.Id);

				            	var team = db.Teams.Single(q => q.TeamMsg == message.Id);

				            	var newEmbed = new EmbedBuilder
				            	{
				            		Title = team.Name,
				            		Color = embed.Color
				            	};

				            	if (newDescription.Count == 2)
				            		newEmbed.WithDescription(newDescription[0] + newDescription[1]);
				            	else if (newDescription.Count == 1)
				            		newEmbed.WithDescription(newDescription[0]);

				            	if (embed.Footer.HasValue)
				            		newEmbed.WithFooter(embed.Footer.Value.Text);
				            	else
				            		newEmbed.WithFooter(team.Pattern);

				            	await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
				            }
				            else
				            {
				            	await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
				            }
			            }
			            else
			            {
				            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
				            await teamMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
			            }
					}
		            finally
		            {
			            _services.GetService<RuntimeData>().GetTeamSemaphore(message.Id).Release();
		            }
				}
	            else if (db.Missions.Any(x => x.SignupChannel == channel.Id) && reaction.UserId != _client.CurrentUser.Id)
	            {
		            var user = _client.GetUser(reaction.UserId);
		            Console.WriteLine($"Naprawiam reakcje po spamie {user.Username}");
		            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
		            await teamMsg.RemoveReactionAsync(reaction.Emote, user);
	            }
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var reactionStringAnimatedVersion = reaction.Emote.ToString().Insert(1, "a");

            using (var db = new DbBoderator())
            {
	            if (db.Missions.Any(x => x.SignupChannel == channel.Id))
	            {
		            var user = await (channel as IGuildChannel).Guild.GetUserAsync(reaction.UserId);
					Console.WriteLine($"[{DateTime.Now}] {user.Username} removed reaction {reaction.Emote.Name}");

					await _services.GetService<RuntimeData>().GetTeamSemaphore(message.Id).WaitAsync(-1);
		            try
		            {
			            var signed = db.Signed.Single(q => q.UserID == reaction.UserId && q.TeamID == message.Id && (q.Emoji == reaction.Emote.ToString() || q.Emoji == reactionStringAnimatedVersion));
			            if (signed != null)
			            {
				            var teamMsg = await channel.GetMessageAsync(message.Id) as IUserMessage;
				            var embed = teamMsg.Embeds.Single();

				            db.Delete(signed);

				            var newDescription = Helpers.MiscHelper.BuildTeamSlots(message.Id);

				            var team = db.Teams.Single(q => q.TeamMsg == message.Id);

                            var newEmbed = new EmbedBuilder
				            {
					            Title = team.Name,
					            Color = embed.Color
				            };

				            if (newDescription.Count == 2)
					            newEmbed.WithDescription(newDescription[0] + newDescription[1]);
				            else if (newDescription.Count == 1)
					            newEmbed.WithDescription(newDescription[0]);

				            if (embed.Footer.HasValue)
					            newEmbed.WithFooter(embed.Footer.Value.Text);
				            else
					            newEmbed.WithFooter(team.Pattern);

				            await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                        }
		            }
		            finally
		            {
			            _services.GetService<RuntimeData>().GetTeamSemaphore(message.Id).Release();
		            }
                }
            }
        }

        private async Task HandleReactionChange(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
	        var signups = _services.GetService<RuntimeData>();

			await signups.BanAccess.WaitAsync(-1);
            try
            {
                if (!signups.ReactionTimes.ContainsKey(reaction.User.Value.Id))
                {
                    signups.ReactionTimes[reaction.User.Value.Id] = new Queue<DateTime>();
                }

                signups.ReactionTimes[reaction.User.Value.Id].Enqueue(DateTime.Now);

                Console.WriteLine($"[{ DateTime.Now}] { reaction.User.Value.Username} spam counter: { signups.ReactionTimes[reaction.User.Value.Id].Count}");

                if (signups.ReactionTimes[reaction.User.Value.Id].Count >= 10 && !signups.SpamBans.ContainsKey(reaction.User.Value.Id))
                {
                    await Helpers.BanHelper.BanUserSpam(_services, reaction.User.Value);
                }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        private async void CheckReactionTimes(object source, ElapsedEventArgs e)
        {
            var signups = _services.GetService<RuntimeData>();

            await signups.BanAccess.WaitAsync(-1);
            try
            {
                foreach(var user in signups.ReactionTimes)
                {
                    while (user.Value.Count > 0 && user.Value.Peek() < e.SignalTime.AddSeconds(-30))
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
