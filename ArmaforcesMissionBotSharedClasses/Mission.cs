using ProtoBuf;
using System;
using System.Collections.Generic;

namespace ArmaforcesMissionBotSharedClasses
{
    [ProtoContract]
    public class Mission
    {
        public class Team
        {
            public string Name;
            public Dictionary<string, int> Slots = new Dictionary<string, int>();
            public Dictionary<string, string> Signed = new Dictionary<string, string>(); // user, emoji
            public ulong TeamMsg;
        }

        [ProtoMember(0)]
        public string Title;
        [ProtoMember(1)]
        public DateTime Date;
        [ProtoMember(2)]
        public uint CloseTime = 60;
        [ProtoMember(3)]
        public string Description;
        [ProtoMember(4)]
        public string Attachment;
        [ProtoMember(5)]
        public string Modlist;
        [ProtoMember(6)]
        public List<Team> Teams = new List<Team>();
        [ProtoMember(7)]
        public ulong Owner;
        [ProtoMember(8)]
        public bool Editing = false;
        [ProtoMember(9)]
        public ulong SignupChannel;
        [ProtoMember(10)]
        public List<ulong> SignedUsers = new List<ulong>();
        [ProtoMember(11)]
        public ulong EditTeamsMessage = 0;
        [ProtoMember(12)]
        public int HighlightedTeam = 0;
        [ProtoMember(13)]
        public bool IsMoving = false;
    }
}
