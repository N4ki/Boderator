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
                var runtimeData = map.GetService<RuntimeData>();
                var config = map.GetService<Config>();

                using (var db = new DbBoderator())
                {
	                var message = "";

	                var bans = db.SignupBans.Select(x => x).ToList();

	                var history = bans.GroupBy(
	                    b => b.UserID,
	                    (key, g) => new { UserID = key, BanCount = g.Count(), BanLength = g.Sum(x => (x.End - x.Start).TotalDays)});


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

	                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
                    if (runtimeData.SignupBansHistoryMessage != 0)
	                {
		                var banMessage = await banAnnouncemens.GetMessageAsync(runtimeData.SignupBansHistoryMessage) as IUserMessage;
		                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
	                }
	                else
	                {
		                var sentMessage = await banAnnouncemens.SendMessageAsync("Historia banów na zapisy:", embed: embed.Build());
		                runtimeData.SignupBansHistoryMessage = sentMessage.Id;
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
	        try
	        {
                var runtimeData = map.GetService<RuntimeData>();
	            var config = map.GetService<Config>();

	            using (var db = new DbBoderator())
	            {
		            var message = "";

		            var bans = db.SignupBans.Select(x => x).ToList();

	                var history = bans.GroupBy(
		                b => b.UserID,
		                (key, g) => new { UserID = key, BanCount = g.Count(), BanLength = g.Sum(x => (x.End - x.Start).Ticks) });

	                foreach (var ban in history.OrderByDescending(x => x.BanLength))
	                {
		                var span = new TimeSpan(ban.BanLength);
		                if (guild.GetUser(ban.UserID) != null)
			                message += $"{guild.GetUser(ban.UserID).Mention}-{ban.BanCount}-{span.TotalDays}:{span.Hours}:{span.Minutes}:{span.Seconds}\n";
		                else
			                message += $"<@!{ban.UserID}>-{ban.BanCount}-{ban.BanLength}\n";
	                }

	                var embed = new EmbedBuilder()
		                .WithColor(Color.Green)
		                .WithTitle("`osoba-liczba banów-sumaryczna długość bana`")
		                .WithDescription(message);

	                if (runtimeData.SpamBansHistoryMessage != 0)
	                {
		                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
		                var banMessage = await banAnnouncemens.GetMessageAsync(runtimeData.SpamBansHistoryMessage) as IUserMessage;
		                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
	                }
	                else
	                {
		                var banAnnouncemens = guild.GetTextChannel(config.HallOfShameChannel);
		                var sentMessage = await banAnnouncemens.SendMessageAsync("Historia banów za spam reakcjami:", embed: embed.Build());
		                runtimeData.SpamBansHistoryMessage = sentMessage.Id;
	                }
                }
	        }
	        catch (Exception e)
	        {
		        Console.WriteLine($"[{DateTime.Now}] MakeSpamBanHistoryMessageFailed: {e.Message}");
	        }
        }

        public static async Task UnsignUser(SocketGuild guild, SocketUser user)
        {
            using (var db = new DbBoderator())
            {
	            var query =
		            from u in db.Signed.Where(q => q.UserID == user.Id)
		            join t in db.Teams on u.TeamID equals t.TeamMsg
		            join m in db.Missions.Where(q => q.CloseDate > DateTime.Now) on t.MissionID equals m.SignupChannel
		            select new { Signup = u, m.SignupChannel, t.TeamMsg};

	            foreach (var entry in query)
	            {
		            var channel = guild.GetTextChannel(entry.SignupChannel);
		            var message = await channel.GetMessageAsync(entry.TeamMsg) as IUserMessage;
		            IEmote reaction;
		            try
		            {
			            reaction = Emote.Parse(entry.Signup.Emoji);
		            }
		            catch
		            {
			            reaction = new Emoji(entry.Signup.Emoji);
		            }
		            await message.RemoveReactionAsync(reaction, user);
		            await db.DeleteAsync(entry.Signup);
	            }
            }
        }

        public static async Task BanUserSpam(IServiceProvider map, IUser user)
        {
            var runtimeData = map.GetService<RuntimeData>();
            var config = map.GetService<Config>();
            var client = map.GetService<DiscordSocketClient>();

            RuntimeData.BanType banType = RuntimeData.BanType.Hour;

			using (var db = new DbBoderator())
            {
	            var last = db.SpamBans.Last(x => x.UserID == user.Id);

	            var ban = new SpamBansTbl();
	            ban.UserID = user.Id;
	            ban.Start = DateTime.Now;
	            ban.End = ban.Start.AddHours(1);
				if (last != null && last.End.AddDays(1) > DateTime.Now)
				{
					if ((last.End - last.Start).TotalHours == 1)
					{
						ban.End = ban.Start.AddDays(1);
						banType = RuntimeData.BanType.Day;
					}
					else
					{
						ban.End = ban.Start.AddDays(7);
						banType = RuntimeData.BanType.Week;
					}
				}

				_ = db.InsertAsync(ban);
            }

            var guild = client.GetGuild(config.AFGuild);
            var contemptChannel = guild.GetTextChannel(config.PublicContemptChannel);
            switch (banType)
            {
                case RuntimeData.BanType.Hour:
                    await user.SendMessageAsync("Za spamowanie reakcji w zapisach został Ci odebrany dostęp na godzinę.");
                    await contemptChannel.SendMessageAsync($"Ten juj chebany {user.Mention} dostał bana na zapisy na godzine za spam reakcją do zapisów. Wiecie co z nim zrobić.");
                    break;
                case RuntimeData.BanType.Day:
                    await user.SendMessageAsync("Pojebało Cie? Ban na zapisy do jutra.");
                    await contemptChannel.SendMessageAsync($"Ten palant {user.Mention} niczego się nie nauczył i dalej spamował, ban na dzień.");
                    break;
                case RuntimeData.BanType.Week:
                    await user.SendMessageAsync("Masz trociny zamiast mózgu. Banik na tydzień.");
                    await contemptChannel.SendMessageAsync($"Ten debil {user.Mention} dalej spamuje pomimo bana na cały dzień, banik na tydzień.");
                    break;
            }

            runtimeData.SpamBansMessage = await Helpers.BanHelper.MakeBanMessage<SpamBansTbl>(
	            guild,
	            runtimeData.SpamBansMessage,
                config.HallOfShameChannel,
                "Bany za spam reakcjami:");

            await Helpers.BanHelper.MakeSpamBanHistoryMessage(map, guild);

            foreach (var missionChannelId in runtimeData.OpenedMissions)
            {
                var missionChannel = guild.GetTextChannel(missionChannelId);
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
