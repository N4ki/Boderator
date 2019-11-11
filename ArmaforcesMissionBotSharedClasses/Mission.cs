using System;
using System.Collections.Generic;
using System.Threading;

namespace ArmaforcesMissionBotSharedClasses
{
    [Serializable]
    public class Mission
    {
        [Serializable]
        public class Team
        {
            public class Slot
            {
                public string Name;
                public string Emoji; // slotID
                public int Count;
                public List<string> Signed = new List<string>();

                public Slot()
                {
                }

                public Slot(string emoji, int count)
                {
                    Name = "";
                    Emoji = emoji;
                    Count = count;
                }

                public Slot(string name, string emoji, int count)
                {
                    Name = name;
                    Emoji = emoji;
                    Count = count;
                }
            }
            public string Name;
            //public Dictionary<string, int> Slots = new Dictionary<string, int>();
            //public Dictionary<string, string> SlotNames = new Dictionary<string, string>(); // id, name
            //public Dictionary<string, string> Signed = new Dictionary<string, string>(); // user, emoji
            public List<Slot> Slots = new List<Slot>();
            public ulong TeamMsg;
        }
        public string Title;
        public DateTime Date;
        public uint CloseTime = 60;
        public string Description;
        public string Attachment;
        public string Modlist;
        public List<Team> Teams = new List<Team>();
        public ulong Owner;
        public ulong SignupChannel;
        public List<ulong> SignedUsers = new List<ulong>();
        [NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public bool Editing = false;
        [NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public ulong EditTeamsMessage = 0;
        [NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public int HighlightedTeam = 0;
        [NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsMoving = false;
        [NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public SemaphoreSlim Access = new SemaphoreSlim(1);
    }
}
