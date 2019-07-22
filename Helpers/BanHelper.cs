using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Helpers
{
    public class BanHelper
    {
        public static async Task MakeBanMessage(IServiceProvider map, SocketGuild guild)
        {
            var signups = map.GetService<SignupsData>();
            var config = map.GetService<Config>();

            var message = "";

            var list = signups.SignupBans.ToList();

            list.Sort((x, y) => x.Value.CompareTo(y.Value));

            foreach (var ban in list)
            {
                message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.ToString()}\n";
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription(message);

            if (signups.SignupBansMessage != 0)
            {
                var banAnnouncemens = guild.GetTextChannel(config.BanAnnouncementChannel);
                var banMessage = await banAnnouncemens.GetMessageAsync(signups.SignupBansMessage) as IUserMessage;
                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
            }
            else
            {
                var banAnnouncemens = guild.GetTextChannel(config.BanAnnouncementChannel);
                var sentMessage = await banAnnouncemens.SendMessageAsync("Bany na zapisy:", embed: embed.Build());
                signups.SignupBansMessage = sentMessage.Id;
            }
        }

        public static async Task MakeSpamBanMessage(IServiceProvider map, SocketGuild guild)
        {
            var signups = map.GetService<SignupsData>();
            var config = map.GetService<Config>();

            var message = "";

            var list = signups.SpamBans.ToList();

            list.Sort((x, y) => x.Value.CompareTo(y.Value));

            foreach (var ban in list)
            {
                message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.ToString()}\n";
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription(message);

            if (signups.SpamBansMessage != 0)
            {
                var banAnnouncemens = guild.GetTextChannel(config.BanAnnouncementChannel);
                var banMessage = await banAnnouncemens.GetMessageAsync(signups.SpamBansMessage) as IUserMessage;
                await banMessage.ModifyAsync(x => x.Embed = embed.Build());
            }
            else
            {
                var banAnnouncemens = guild.GetTextChannel(config.BanAnnouncementChannel);
                var sentMessage = await banAnnouncemens.SendMessageAsync("Bany za spam reakcjami:", embed: embed.Build());
                signups.SpamBansMessage = sentMessage.Id;
            }
        }

        public static async Task UnsignUser(IServiceProvider map, SocketGuild guild, SocketUser user)
        {
            var signups = map.GetService<SignupsData>();
            var config = map.GetService<Config>();

            foreach (var mission in signups.Missions)
            {
                await mission.Access.WaitAsync();
                try
                {
                    if (mission.Date < signups.SignupBans[user.Id] && mission.SignedUsers.Contains(user.Id))
                    {
                        foreach (var team in mission.Teams)
                        {
                            if (team.Signed.ContainsKey(user.Mention))
                            {
                                var channel = guild.GetTextChannel(mission.SignupChannel);
                                var message = await channel.GetMessageAsync(team.TeamMsg) as IUserMessage;
                                IEmote reaction;
                                try
                                {
                                    reaction = Emote.Parse(team.Signed[user.Mention]);
                                }
                                catch (Exception e)
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
