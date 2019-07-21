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
        [ContextDMOrChannel]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task BanSignups(SocketUser user, uint days = 7)
        {
            var signups = _map.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync();

            try
            {
                var banEnd = DateTime.Now.AddDays(days);
                signups.SignupBans.Add(user.Id, banEnd);
                await MakeBanMessage(Context.Guild);
                await ReplyAsync("Niech ginie.");
                await UnsignUser(Context.Guild, user);
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        [Command("unban")]
        [Summary("Odbanowuje podaną osobę.")]
        [ContextDMOrChannel]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UnbanSignups(SocketUser user)
        {
            var signups = _map.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync();

            try
            {
                if (signups.SignupBans.ContainsKey(user.Id))
                {
                    signups.SignupBans.Remove(user.Id);
                    await MakeBanMessage(Context.Guild);
                    await ReplyAsync("Jesteś zbyt pobłażliwy...");
                }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }

        private async Task MakeBanMessage(SocketGuild guild)
        {
            var signups = _map.GetService<SignupsData>();

            var message = "";

            foreach (var ban in signups.SignupBans)
            {
                message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.ToString()}\n";
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription(message);

            if(signups.SignupBansMessage != 0)
            {
                var banAnnouncemens = guild.GetTextChannel(_config.BanAnnouncementChannel);
                var banMessage = await banAnnouncemens.GetMessageAsync(signups.SignupBansMessage) as IUserMessage;
                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
            }
            else
            {
                var banAnnouncemens = guild.GetTextChannel(_config.BanAnnouncementChannel);
                var sentMessage = await banAnnouncemens.SendMessageAsync("Bany na zapisy:", embed: embed.Build());
                signups.SignupBansMessage = sentMessage.Id;
            }
        }

        private async Task UnsignUser(SocketGuild guild, SocketUser user)
        {
            var signups = _map.GetService<SignupsData>();

            foreach(var mission in signups.Missions)
            {
                await mission.Access.WaitAsync();
                try
                {
                    if(mission.Date < signups.SignupBans[user.Id] && mission.SignedUsers.Contains(user.Id))
                    {
                        foreach(var team in mission.Teams)
                        {
                            if(team.Signed.ContainsKey(user.Mention))
                            {
                                var channel = guild.GetTextChannel(mission.SignupChannel);
                                var message = await channel.GetMessageAsync(team.TeamMsg) as IUserMessage;
                                IEmote reaction;
                                try
                                {
                                    reaction = Emote.Parse(team.Signed[user.Mention]);
                                }
                                catch(Exception e)
                                {
                                    reaction = new Emoji(team.Signed[user.Mention]);
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
    }
}
