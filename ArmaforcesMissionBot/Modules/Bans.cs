using ArmaforcesMissionBot.Attributes;
using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.DataClasses.SQL;
using ArmaforcesMissionBot.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ArmaforcesMissionBot.Modules
{
    [Name("Bany")]
    public class Bans : ModuleBase<SocketCommandContext>
    {
        public IServiceProvider _map { get; set; }
        public DiscordSocketClient _client { get; set; }
        public Config _config { get; set; }
        public CommandService _commands { get; set; }

        public Bans()
        {
        }

        [Command("ban")]
        [Summary("Banuje daną osobę z zapisów do podanego terminu. Jako drugi argument można podać liczbę dni bana, domyślnie jest to 7.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task BanSignups(SocketUser user, uint days = 7)
        {
            var runtimeData = _map.GetService<RuntimeData>();

            await runtimeData.BanAccess.WaitAsync(-1);
            try
            {
	            using (var db = new DbBoderator())
	            {
		            var ban = new SignupBansTbl
		            {
			            UserID = user.Id,
			            Start = DateTime.Now,
			            End = DateTime.Now.AddDays(days).MakeEndOfDay()
		            };

		            db.Insert(ban);

		            runtimeData.SignupBansMessage = await Helpers.BanHelper.MakeBanMessage<SignupBansTbl>(
			            Context.Guild,
			            runtimeData.SignupBansMessage,
			            _config.HallOfShameChannel,
			            "Bany na zapisy:");

		            await Helpers.BanHelper.MakeBanHistoryMessage(_map, Context.Guild);

		            await ReplyAsync("Niech ginie.");
		            await Helpers.BanHelper.UnsignUser(Context.Guild, user);
	            }
            }
            finally
            {
	            runtimeData.BanAccess.Release();
            }
        }

        [Command("unban")]
        [Summary("Odbanowuje podaną osobę.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UnbanSignups(SocketUser user)
        {
            var signups = _map.GetService<RuntimeData>();

            await signups.BanAccess.WaitAsync(-1);

            try
            {
	            using (var db = new DbBoderator())
	            {
		            var ban = db.SignupBans.Single(q => q.UserID == user.Id && q.End > DateTime.Now);
		            if (ban != null)
		            {
			            db.Delete(ban);
			            signups.SignupBansMessage = await Helpers.BanHelper.MakeBanMessage<SignupBansTbl>(
				            Context.Guild,
				            signups.SignupBansMessage,
				            _config.HallOfShameChannel,
				            "Bany na zapisy:");

			            await Helpers.BanHelper.MakeBanHistoryMessage(_map, Context.Guild);

						await ReplyAsync("Jesteś zbyt pobłażliwy...");
					}
	            }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        [Command("banSpam")]
        [Summary("Ban za spam reakcjami.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task BanSpam(SocketUser user)
        {
            var signups = _map.GetService<RuntimeData>();

            await signups.BanAccess.WaitAsync(-1);

            try
            {
                await Helpers.BanHelper.BanUserSpam(_map, user);
                await ReplyAsync("A to śmierdziel jeden");
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        [Command("unbanSpam")]
        [Summary("Zdejmuje ban za spam reakcjami.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UnbanSpam(SocketUser user)
        {
            var signups = _map.GetService<RuntimeData>();

            await signups.BanAccess.WaitAsync(-1);

            using (var db = new DbBoderator())
            {
	            try
	            {
		            var ban = db.SignupBans.Single(q => q.UserID == user.Id && q.End > DateTime.Now);
		            if (ban != null)
		            {
			            await db.DeleteAsync(ban);
			            signups.SpamBansMessage = await Helpers.BanHelper.MakeBanMessage<SpamBansTbl>(
				            Context.Guild,
				            signups.SpamBansMessage,
				            _config.HallOfShameChannel,
				            "Bany za spam reakcjami:");

			            foreach (var mission in db.Missions.Where(q => q.CloseDate > DateTime.Now))
			            {
				            var channel = Context.Guild.GetTextChannel(mission.SignupChannel);
				            await channel.RemovePermissionOverwriteAsync(user);
			            }

			            await ReplyAsync("Tylko nie marudź na lagi...");
					}
	            }
	            finally
	            {
		            signups.BanAccess.Release();
	            }
            }
        }

        [Command("unsign")]
        [Summary("Wypisuje gracza z podanej misji.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Unsign(ulong userID, IMessageChannel channel)
        {
            var signups = _map.GetService<RuntimeData>();
            //var user = _client.GetUser(userID);

            using (var db = new DbBoderator())
            {
	            var query =
		            (from u in db.Signed.Where(q => q.UserID == userID)
		            join s in db.Slots on new { u.Emoji, u.TeamID } equals new { s.Emoji, s.TeamID }
		            join t in db.Teams on s.TeamID equals t.TeamMsg
		            where t.MissionID == channel.Id
		            select new {u, t}).Single();

                if (query != null)
                {
	                await _map.GetService<RuntimeData>().GetTeamSemaphore(query.t.TeamMsg).WaitAsync(-1);
	                try
	                {
		                db.Delete(query.u);

		                var teamMsg = await channel.GetMessageAsync(query.t.TeamMsg) as IUserMessage;
		                var embed = teamMsg.Embeds.Single();

		                var newDescription = Helpers.MiscHelper.BuildTeamSlots(query.t.TeamMsg);

		                var newEmbed = new EmbedBuilder
		                {
			                Title = embed.Title,
			                Color = embed.Color
		                };

		                if (newDescription.Count == 2)
			                newEmbed.WithDescription(newDescription[0] + newDescription[1]);
		                else if (newDescription.Count == 1)
			                newEmbed.WithDescription(newDescription[0]);

		                if (embed.Footer.HasValue)
			                newEmbed.WithFooter(embed.Footer.Value.Text);

		                await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                    }
	                finally
	                {
		                _map.GetService<RuntimeData>().GetTeamSemaphore(query.t.TeamMsg).Release();
	                }
                }
            }
        }
    }
}
