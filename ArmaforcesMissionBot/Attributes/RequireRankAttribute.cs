using ArmaforcesMissionBot.DataClasses;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Attributes
{
    public enum RanksEnum
    {
        Recruiter
    }

    static class RanksEnumMethods
    {
        public static ulong GetID(this RanksEnum role)
        {
            Config config = Program.GetConfig();
            switch(role)
            {
                case RanksEnum.Recruiter:
                    return config.RecruiterRole;
                default:
                    return 0;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RequireRankAttribute : PreconditionAttribute
    {
        RanksEnum _role;

        public RequireRankAttribute(RanksEnum role)
        {
            _role = role;
        }

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var client = services.GetService<DiscordSocketClient>();
            var config = services.GetService<Config>();

            if((context.User as SocketGuildUser).Roles.Any(x => x.Id == _role.GetID()))
                return PreconditionResult.FromSuccess();
            else
                return PreconditionResult.FromError("Co ty próbujesz osiągnąć?");
        }
    }
}
