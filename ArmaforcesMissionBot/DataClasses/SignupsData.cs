using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using ArmaforcesMissionBotSharedClasses;

namespace ArmaforcesMissionBot.DataClasses
{
    public class SignupsData
    {
        public List<Mission> Missions = new List<Mission>();
        public Dictionary<ulong, Mission> BeforeEditMissions = new Dictionary<ulong, Mission>();
        public SemaphoreSlim BanAccess = new SemaphoreSlim(1);
        public Dictionary<ulong, DateTime> SignupBans = new Dictionary<ulong, DateTime>();
        public ulong SignupBansMessage = 0;
        public Dictionary<ulong, DateTime> SpamBans = new Dictionary<ulong, DateTime>();
        public ulong SpamBansMessage = 0;
        public Dictionary<ulong, Queue<DateTime>> ReactionTimes = new Dictionary<ulong, Queue<DateTime>>();
        public ulong HallOfShameMessage = 0;
        public Dictionary<ulong, Tuple<uint, uint>> SignupBansHistory = new Dictionary<ulong, Tuple<uint, uint>>();
        public ulong SignupBansHistoryMessage = 0;
        public enum BanType
        {
            Godzina,
            Dzień,
            Tydzień
        }
        public Dictionary<ulong, Tuple<uint, DateTime, BanType>> SpamBansHistory = new Dictionary<ulong, Tuple<uint, DateTime, BanType>>();
        public ulong SpamBansHistoryMessage = 0;
    }
}
