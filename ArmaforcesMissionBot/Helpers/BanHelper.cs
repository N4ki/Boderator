using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.DataClasses.SQL;
using Discord;
using Discord.WebSocket;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Helpers
{
    public class BanHelper
    {
        public static async Task<ulong> MakeBanMessage(IServiceProvider map, SocketGuild guild, Dictionary<ulong, DateTime> bans, ulong banMessageId, ulong banAnnouncementChannel, string messageText)
        {
            try
            {
                var message = "";

                var list = bans.ToList();

                list.Sort((x, y) => x.Value.CompareTo(y.Value));

                foreach (var ban in list)
                {
                    if (guild.GetUser(ban.Key) != null)
                        message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.ToString()}\n";
                    else
                        message += $"<@!{ban.Key}>-{ban.Value.ToString()}\n";
                }

                var embed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(message);

                if (banMessageId != 0)
                {
                    var banAnnouncemens = guild.GetTextChannel(banAnnouncementChannel);
                    var banMessage = await banAnnouncemens.GetMessageAsync(banMessageId) as IUserMessage;
                    await banMessage.ModifyAsync(x => x.Embed = embed.Build());
                    return banMessageId;
                }
                else
                {
                    var banAnnouncemens = guild.GetTextChannel(banAnnouncementChannel);
                    var sentMessage = await banAnnouncemens.SendMessageAsync(messageText, embed: embed.Build());
                    return sentMessage.Id;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] MakeBanMessageFailed: {e.Message}");
            }

            return banMessageId;
        }

        public static async Task<ulong> MakeBanMessage<T>(SocketGuild guild, ulong banMessageId, ulong banAnnouncementChannel, string messageText)
        {
            try
	        {
		        using (var db = new DbBoderator())
		        {
			        var message = "";

			        if (typeof(T) == typeof(SignupBansTbl))
			        {
				        var list = db.SignupBans.Where(q => q.End > DateTime.Now).ToList();

				        list.Sort((x, y) => x.End.CompareTo(y.End));

				        foreach (var ban in list)
				        {
					        if (guild.GetUser(ban.UserID) != null)
						        message += $"{guild.GetUser(ban.UserID).Mention}-{ban.End}\n";
					        else
						        message += $"<@!{ban.UserID}>-{ban.End}\n";
				        }
			        }
                    else if (typeof(T) == typeof(SpamBansTbl))
			        {
				        var list = db.SpamBans.Where(q => q.End > DateTime.Now).ToList();

				        list.Sort((x, y) => x.End.CompareTo(y.End));

				        foreach (var ban in list)
				        {
					        if (guild.GetUser(ban.UserID) != null)
						        message += $"{guild.GetUser(ban.UserID).Mention}-{ban.End}\n";
					        else
						        message += $"<@!{ban.UserID}>-{ban.End}\n";
				        }
                    }

			        var embed = new EmbedBuilder()
				        .WithColor(Color.Green)
				        .WithDescription(message);

			        if (banMessageId != 0)
			        {
				        var banAnnouncemens = guild.GetTextChannel(banAnnouncementChannel);
				        var banMessage = await banAnnouncemens.GetMessageAsync(banMessageId) as IUserMessage;
				        await banMessage.ModifyAsync(x => x.Embed = embed.Build());
				        return banMessageId;
			        }
			        else
			        {
				        var banAnnouncemens = guild.GetTextChannel(banAnnouncementChannel);
				        var sentMessage = await banAnnouncemens.SendMessageAsync(messageText, embed: embed.Build());
				        return sentMessage.Id;
			        }
		        }
	        }
	        catch (Exception e)
	        {
		        Console.WriteLine($"[{DateTime.Now}] MakeBanMessageFailed: {e.Message}");
	        }

	        return banMessageId;
        }

        public static async Task MakeBanHistoryMessage(IServiceProvider map, SocketGuild guild)
        {
            try
            {
                var signups = map.GetService<RuntimeData>();
                var config = map.GetService<Config>();

                using (var db = new DbBoderator())
                {
	                var message = "";

	                var bans = db.SignupBans.Select(x => x).ToList();

	                var history = bans.GroupBy(
	                    b => b.UserID,
	                    (key, g) => new { UserID = key, BanCount = g.Count(), BanLength = g.Sum(x => (x.End - x.Start).Days)});


	                foreach (var ban in history.OrderByDescending(x => x.BanLength))
	                {
		                if (guild.GetUser(ban.UserID) != null)
			                message += $"{guild.GetUser(ban.UserID).Mention}-{ban.BanCount}-{ban.BanLength}\n";
		                else
			                message += $"<@!{ban.UserID}>-{ban.BanCount}-{ban.BanLength}\n";
                    }

	                var embed = new EmbedBuilder()
		                .WithColor(Color.Green)
		                .WithTitle("`osoba-liczba banów-sumaryczna liczba dni bana`")
		                .WithDescription(message);

	                if (signups.SignupBansHistoryMessage != 0)
	                {
		                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
		                var banMessage = await banAnnouncemens.GetMessageAsync(signups.SignupBansHistoryMessage) as IUserMessage;
		                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
	                }
	                else
	                {
		                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
		                var sentMessage = await banAnnouncemens.SendMessageAsync("Historia banów na zapisy:", embed: embed.Build());
		                signups.SignupBansHistoryMessage = sentMessage.Id;
	                }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] MakeBanHistoryMessageFailed: {e.Message}");
            }
        }

        public static async Task MakeSpamBanHistoryMessage(IServiceProvider map, SocketGuild guild)
        {
            var signups = map.GetService<RuntimeData>();
            var config = map.GetService<Config>();

            var message = "";

            foreach (var ban in signups.SpamBansHistory.OrderByDescending(x=> x.Value.Item1))
            {
                if (guild.GetUser(ban.Key) != null)
                    message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.Item1.ToString()}-{ban.Value.Item2.ToString()}-{ban.Value.Item3.ToString()}\n";
                else
                    message += $"<@!{ban.Key}>-{ban.Value.Item1.ToString()}-{ban.Value.Item2.ToString()}-{ban.Value.Item3.ToString()}\n";
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("`osoba-liczba banów-ostatni ban-typ ostatniego bana`")
                .WithDescription(message);

            if (signups.SpamBansHistoryMessage != 0)
            {
                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
                var banMessage = await banAnnouncemens.GetMessageAsync(signups.SpamBansHistoryMessage) as IUserMessage;
                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
            }
            else
            {
                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
                var sentMessage = await banAnnouncemens.SendMessageAsync("Historia banów za spam reakcjami:", embed: embed.Build());
                signups.SpamBansHistoryMessage = sentMessage.Id;
            }
        }

        public static async Task UnsignUser(IServiceProvider map, SocketGuild guild, SocketUser user)
        {
            var signups = map.GetService<RuntimeData>();
            var config = map.GetService<Config>();

            foreach (var mission in signups.Missions)
            {
                await mission.Access.WaitAsync(-1);
                try
                {
                    if (mission.Date < signups.SignupBans[user.Id] && mission.SignedUsers.Contains(user.Id))
                    {
                        foreach (var team in mission.Teams)
                        {
                            if(team.Slots.Any(x => x.Signed.Contains(user.Id)))
                            {
                                var channel = guild.GetTextChannel(mission.SignupChannel);
                                var message = await channel.GetMessageAsync(team.TeamMsg) as IUserMessage;
                                IEmote reaction;
                                try
                                {
                                    reaction = Emote.Parse(team.Slots.Single(x => x.Signed.Contains(user.Id)).Emoji);
                                }
                                catch (Exception e)
                                {
                                    reaction = new Emoji(team.Slots.Single(x => x.Signed.Contains(user.Id)).Emoji);
                                }
                                await message.RemoveReactionAsync(reaction, user);
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

        public static async Task BanUserSpam(IServiceProvider map, IUser user)
        {
            var signups = map.GetService<RuntimeData>();
            var config = map.GetService<Config>();
            var client = map.GetService<DiscordSocketClient>();

            if (signups.SpamBansHistory.ContainsKey(user.Id) && signups.SpamBansHistory[user.Id].Item2.AddDays(1) > DateTime.Now)
            {
                var banEnd = DateTime.Now;
                switch (signups.SpamBansHistory[user.Id].Item3)
                {
                    case RuntimeData.BanType.Godzina:
                        signups.SpamBans.Add(user.Id, DateTime.Now.AddDays(1));
                        signups.SpamBansHistory[user.Id] = new Tuple<uint, DateTime, RuntimeData.BanType>(
                            signups.SpamBansHistory[user.Id].Item1 + 1,
                            DateTime.Now.AddDays(1),
                            RuntimeData.BanType.Dzień);
                        break;
                    case RuntimeData.BanType.Dzień:
                    case RuntimeData.BanType.Tydzień:
                        signups.SpamBans.Add(user.Id, DateTime.Now.AddDays(7));
                        signups.SpamBansHistory[user.Id] = new Tuple<uint, DateTime, RuntimeData.BanType>(
                            signups.SpamBansHistory[user.Id].Item1 + 1,
                            DateTime.Now.AddDays(7),
                            RuntimeData.BanType.Tydzień);
                        break;
                }
            }
            else
            {
                signups.SpamBans.Add(user.Id, DateTime.Now.AddHours(1));
                if (signups.SpamBansHistory.ContainsKey(user.Id))
                {
                    signups.SpamBansHistory[user.Id] = new Tuple<uint, DateTime, RuntimeData.BanType>(
                                signups.SpamBansHistory[user.Id].Item1 + 1,
                                DateTime.Now.AddHours(1),
                                RuntimeData.BanType.Godzina);
                }
                else
                {
                    signups.SpamBansHistory[user.Id] = new Tuple<uint, DateTime, RuntimeData.BanType>(
                                1,
                                DateTime.Now.AddHours(1),
                                RuntimeData.BanType.Godzina);
                }
            }

            var guild = client.GetGuild(config.AFGuild);
            var contemptChannel = guild.GetTextChannel(config.PublicContemptChannel);
            switch (signups.SpamBansHistory[user.Id].Item3)
            {
                case RuntimeData.BanType.Godzina:
                    await user.SendMessageAsync("Za spamowanie reakcji w zapisach został Ci odebrany dostęp na godzinę.");
                    await contemptChannel.SendMessageAsync($"Ten juj chebany {user.Mention} dostał bana na zapisy na godzine za spam reakcją do zapisów. Wiecie co z nim zrobić.");
                    break;
                case RuntimeData.BanType.Dzień:
                    await user.SendMessageAsync("Pojebało Cie? Ban na zapisy do jutra.");
                    await contemptChannel.SendMessageAsync($"Ten palant {user.Mention} niczego się nie nauczył i dalej spamował, ban na dzień.");
                    break;
                case RuntimeData.BanType.Tydzień:
                    await user.SendMessageAsync("Masz trociny zamiast mózgu. Banik na tydzień.");
                    await contemptChannel.SendMessageAsync($"Ten debil {user.Mention} dalej spamuje pomimo bana na cały dzień, banik na tydzień.");
                    break;
            }

            signups.SpamBansMessage = await Helpers.BanHelper.MakeBanMessage(
                map,
                guild,
                signups.SpamBans,
                signups.SpamBansMessage,
                config.HallOfShameChannel,
                "Bany za spam reakcjami:");

            await Helpers.BanHelper.MakeSpamBanHistoryMessage(map, guild);

            foreach (var mission in signups.Missions)
            {
                var missionChannel = guild.GetTextChannel(mission.SignupChannel);
                try
                {
                    await missionChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
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
                catch(Exception e)
                {
                    Console.WriteLine($"Woops, banning user from channel failed : {e.Message}");
                }
            }
        }
    }
}
