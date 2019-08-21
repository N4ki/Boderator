using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Handlers
{
    interface IInstallable
    {
        Task Install(IServiceProvider map);
    }
}
