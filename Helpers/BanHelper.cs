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
        public static async Task<ulong> MakeBanMessage(IServiceProvider map, SocketGuild guild, Dictionary<ulong, DateTime> bans, ulong banMessageId, ulong banAnnouncementChannel, string messageText)
        {
            var message = "";

            var list = bans.ToList();

            list.Sort((x, y) => x.Value.CompareTo(y.Value));

            foreach (var ban in list)
            {
                message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.ToString()}\n";
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

        public static async Task MakeBanHistoryMessage(IServiceProvider map, SocketGuild guild)
        {
            var signups = map.GetService<SignupsData>();
            var config = map.GetService<Config>();

            var message = "";;

            foreach (var ban in signups.SignupBansHistory.OrderByDescending(x => x.Value.Item2))
            {
                message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.Item1.ToString()}-{ban.Value.Item2.ToString()}\n";
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

        public static async Task MakeSpamBanHistoryMessage(IServiceProvider map, SocketGuild guild)
        {
            var signups = map.GetService<SignupsData>();
            var config = map.GetService<Config>();

            var message = "";

            foreach (var ban in signups.SpamBansHistory.OrderByDescending(x=> x.Value.Item1))
            {
                message += $"{guild.GetUser(ban.Key).Mention}-{ban.Value.Item1.ToString()}-{ban.Value.Item2.ToString()}-{ban.Value.Item3.ToString()}\n";
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
            var signups = map.GetService<SignupsData>();
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
