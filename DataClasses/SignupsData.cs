using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ArmaforcesMissionBot.DataClasses
{
    public class SignupsData
    {
        public class SignupsInstance
        {
            public class Team
            {
                public string                       Name;
                public Dictionary<string, int>      Slots = new Dictionary<string, int>();
                public Dictionary<string, string>   Signed = new Dictionary<string, string>(); // user, emoji
                public ulong                        TeamMsg;
            }
            public string           Title;
            public DateTime         Date;
            public string           Description;
            public string           Attachment;
            public string           Modlist;
            public List<Team>       Teams = new List<Team>();
            public ulong            Owner;
            public bool             Editing = false;
            public ulong            SignupChannel;
            public List<ulong>      SignedUsers = new List<ulong>();
            public SemaphoreSlim    Access = new SemaphoreSlim(1); 
            public ulong            EditTeamsMessage = 0;
            public int              HighlightedTeam = 0;
            public bool             IsMoving = false;
        }

        public List<SignupsInstance> Missions = new List<SignupsInstance>();
        public SemaphoreSlim BanAccess = new SemaphoreSlim(1);
        public Dictionary<ulong, DateTime> SignupBans = new Dictionary<ulong, DateTime>();
        public ulong SignupBansMessage = 0;
        public Dictionary<ulong, DateTime> SpamBans = new Dictionary<ulong, DateTime>();
        public Dictionary<ulong, Queue<DateTime>> ReactionTimes = new Dictionary<ulong, Queue<DateTime>>();
    }
}
