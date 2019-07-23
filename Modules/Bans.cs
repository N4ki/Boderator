using ArmaforcesMissionBot.Attributes;
using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
            var signups = _map.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync(-1);

            try
            {
                var banEnd = DateTime.Now.AddDays(days);
                banEnd = banEnd.AddHours(23 - banEnd.Hour);
                banEnd = banEnd.AddMinutes(59 - banEnd.Minute);
                banEnd = banEnd.AddSeconds(59 - banEnd.Second);
                signups.SignupBans.Add(user.Id, banEnd);
                if (signups.SignupBansHistory.ContainsKey(user.Id))
                {
                    signups.SignupBansHistory[user.Id] = new Tuple<uint, uint>(
                        signups.SignupBansHistory[user.Id].Item1 + 1,
                        signups.SignupBansHistory[user.Id].Item2 + days);
                }
                else
                    signups.SignupBansHistory[user.Id] = new Tuple<uint, uint>(1, days);

                signups.SignupBansMessage = await Helpers.BanHelper.MakeBanMessage(
                    _map, 
                    Context.Guild, 
                    signups.SignupBans, 
                    signups.SignupBansMessage, 
                    _config.BanAnnouncementChannel, 
                    "Bany na zapisy:");

                await Helpers.BanHelper.MakeBanHistoryMessage(_map, Context.Guild);

                await ReplyAsync("Niech ginie.");
                await Helpers.BanHelper.UnsignUser(_map, Context.Guild, user);
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        [Command("unban")]
        [Summary("Odbanowuje podaną osobę.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UnbanSignups(SocketUser user)
        {
            var signups = _map.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync(-1);

            try
            {
                if (signups.SignupBans.ContainsKey(user.Id))
                {
                    signups.SignupBans.Remove(user.Id);
                    signups.SignupBansMessage = await Helpers.BanHelper.MakeBanMessage(
                        _map, 
                        Context.Guild, 
                        signups.SignupBans, 
                        signups.SignupBansMessage, 
                        _config.BanAnnouncementChannel, 
                        "Bany na zapisy:");
                    await ReplyAsync("Jesteś zbyt pobłażliwy...");
                }
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
            var signups = _map.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync(-1);

            try
            {
                if (signups.SpamBans.ContainsKey(user.Id))
                {
                    signups.SpamBans.Remove(user.Id);
                    signups.SpamBansMessage = await Helpers.BanHelper.MakeBanMessage(
                        _map,
                        Context.Guild,
                        signups.SpamBans,
                        signups.SpamBansMessage,
                        _config.BanAnnouncementChannel,
                        "Bany za spam reakcjami:");

                    // Remove permissions override from channels
                    if (signups.Missions.Count > 0)
                    {
                        foreach (var mission in signups.Missions)
                        {
                            var channel = Context.Guild.GetTextChannel(mission.SignupChannel);
                            await channel.RemovePermissionOverwriteAsync(user);
                        }
                    }
                    await ReplyAsync("Tylko nie marudź na lagi...");
                }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        [Command("printBans")]
        [Summary("Wypisuje aktualne bany do historii.")]
        [RequireOwner]
        public async Task PrintBans()
        {
            var signups = _map.GetService<SignupsData>();

            if (signups.SignupBansHistoryMessage == 0)
            {
                foreach(var ban in signups.SignupBans)
                    signups.SignupBansHistory[ban.Key] = new Tuple<uint, uint>(1, 7);
            }

            await Helpers.BanHelper.MakeBanHistoryMessage(_map, Context.Guild);
        }
    }
}
