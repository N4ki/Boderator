using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.DataClasses
{
    public class OpenedDialogs
    {
        public class Dialog
        {
            public ulong DialogID = 0;
            public ulong DialogOwner = 0;
            public Dictionary<string, Action<Dialog>> Buttons = new Dictionary<string, Action<Dialog>>();
        }

        public List<Dialog> Dialogs = new List<Dialog>();
    }
}
