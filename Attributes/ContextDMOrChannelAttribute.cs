using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ContextDMOrChannelAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var config = services.GetService<Config>();
            if (context.Channel is IDMChannel || context.Channel.Id == config.CreateMissionChannel)
                return PreconditionResult.FromSuccess();
            else
                return PreconditionResult.FromError("Nie ten kanał");
        }
    }
}
