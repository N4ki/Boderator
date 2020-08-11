using ArmaforcesMissionBot.Attributes;
using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Modules
{
    [Name("Rangi")]
    public class Ranks : ModuleBase<SocketCommandContext>
    {
        public IServiceProvider _map { get; set; }
        public DiscordSocketClient _client { get; set; }
        public Config _config { get; set; }

        [Command("rekrutuj")]
        [Summary("Przydziela rangę rekrut.")]
        [RequireRank(RanksEnum.Recruiter)]
        public async Task Recruit(IGuildUser user)
        {
            Console.WriteLine($"[{DateTime.Now.ToString()}] {Context.User.Username} called recruit command");
            var signupRole = Context.Guild.GetRole(_config.SignupRole);
            if (user.RoleIds.Contains(_config.RecruitRole))
                await ReplyAsync($"Przecież {user.Mention} już został zrekrutowany.");
            else if (user.RoleIds.Contains(_config.SignupRole))
                await ReplyAsync($"Przecież {user.Mention} jest już w {signupRole.Mention}!");
            else
            {
                await user.AddRoleAsync(Context.Guild.GetRole(_config.RecruitRole));
                var recruitMessageText =
                    $"Gratulujemy przyjęcia {user.Mention} w grono rekrutów! Od teraz masz miesiąc na rozegranie swojej pierwszej misji z nami, wtedy otrzymasz rangę #{signupRole.Name}#! W innym wypadku zostaniesz usunięty z Discorda z możliwością powrotu.\n" +
                    $"Polecamy też sprawdzić kanał {Context.Guild.GetTextChannel(_config.RecruitInfoChannel).Mention}.\n" +
                    $"W razie pytań pisz na {Context.Guild.GetTextChannel(_config.RecruitAskChannel).Mention}.\n" +
                    $"Twoim opiekunem do momentu dołączenia do grupy jest {Context.User.Mention}.";
                var recruitMessage = await ReplyAsync(recruitMessageText);
                // Modify message to include rank mention but without mentioning it
                var replacedMessage = recruitMessage.Content;
                replacedMessage = Regex.Replace(replacedMessage, "#ArmaForces#", $"{signupRole.Mention}");
                await recruitMessage.ModifyAsync(x => x.Content = replacedMessage);
            }

            await Context.Message.DeleteAsync();
        }

        [Command("wyrzuc")]
        [Summary("Wyrzuca rekruta bądź randoma z Discorda.")]
        [RequireRank(RanksEnum.Recruiter)]
        public async Task Kick(IGuildUser user)
        {
            Console.WriteLine($"[{DateTime.Now.ToString()}] {Context.User.Username} called kick command");
            var signupRole = Context.Guild.GetRole(_config.SignupRole);
            var userRoleIds = user.RoleIds;
            if (userRoleIds.All(x => x == _config.RecruitRole || x == _config.AFGuild))
            {
                var embedBuilder = new EmbedBuilder()
                {
                    ImageUrl = _config.KickImageUrl
                };
                var replyMessage =
                    await ReplyAsync(
                        $"{user.Mention} został pomyślnie wykopany z serwera przez {Context.User.Mention}.",
                        embed: embedBuilder.Build());
                await user.KickAsync("AFK");
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await replyMessage.ModifyAsync(x => x.Embed = null);
                });
            }
            else
            {
                await ReplyAsync($"Nie możesz wyrzucić {user.Mention}, nie jest on rekrutem.");
            }

            await Context.Message.DeleteAsync();
        }
    }
}
