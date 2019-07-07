using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ArmaforcesMissionBot.DataClasses
{
    public class SignupsData
    {
        public class SignupsInstance
        {
            public class Team
            {
                public string                   Name;
                public Dictionary<string, int>  Slots = new Dictionary<string, int>();
                public ulong                    TeamMsg;
            }
            public string       Title;
            public DateTime     Date;
            public string       Description;
            public string       Attachment;
            public List<Team>   Teams = new List<Team>();
            public ulong        Owner;
            public bool         Editing;
            public ulong        SignupChannel;
        }

        public List<SignupsInstance> Missions = new List<SignupsInstance>();
    }
}
