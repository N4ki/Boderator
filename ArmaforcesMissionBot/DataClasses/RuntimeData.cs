using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using ArmaforcesMissionBotSharedClasses;

namespace ArmaforcesMissionBot.DataClasses
{
    public class RuntimeData
    {
	    private readonly Dictionary<ulong, Mission> _editedMissions = new Dictionary<ulong, Mission>();
        private readonly Dictionary<ulong, SemaphoreSlim> _teamsAccesses = new Dictionary<ulong, SemaphoreSlim>();

        public Mission GetEditedMission(ulong ownerID, bool create = false)
        {
	        if (!_editedMissions.ContainsKey(ownerID) && create)
		        _editedMissions[ownerID] = new Mission();

            return _editedMissions[ownerID];
        }

        public SemaphoreSlim GetTeamSemaphore(ulong teamID)
        {
	        if (!_teamsAccesses.ContainsKey(teamID))
		        _teamsAccesses[teamID] = new SemaphoreSlim(1);

	        return _teamsAccesses[teamID];
        }

        public HashSet<ulong> OpenedMissions { get; } = new HashSet<ulong>();
        public SemaphoreSlim BanAccess { get; } = new SemaphoreSlim(1);
        public ulong SignupBansMessage { get; set; } = 0;
        public ulong SignupBansHistoryMessage { get; set; } = 0;
        public ulong SpamBansMessage { get; set; } = 0;
        public ulong SpamBansHistoryMessage { get; set; } = 0;
        public Dictionary<ulong, Queue<DateTime>> ReactionTimes { get; set; } = new Dictionary<ulong, Queue<DateTime>>();

        [Obsolete("This should not be used at all, everything should use either SQl database or GetEditedMission.")]
        public List<Mission> Missions = new List<Mission>();
        [Obsolete("This should not be used at all, everything should use either SQl database or GetEditedMission.")]
        public Dictionary<ulong, Mission> BeforeEditMissions = new Dictionary<ulong, Mission>();
        [Obsolete("This should not be used at all, everything should use SQl database.")]
        public Dictionary<ulong, DateTime> SignupBans = new Dictionary<ulong, DateTime>();
        [Obsolete("This should not be used at all, everything should use SQl database.")]
        public Dictionary<ulong, DateTime> SpamBans = new Dictionary<ulong, DateTime>();
        [Obsolete("This should not be used at all, everything should use SQl database.")]
        public Dictionary<ulong, Tuple<uint, uint>> SignupBansHistory = new Dictionary<ulong, Tuple<uint, uint>>();
        [Obsolete("This should not be used at all, everything should use SQl database.")]
        public enum BanType
        {
            Godzina,
            Dzień,
            Tydzień
        }
        [Obsolete("This should not be used at all, everything should use SQl database.")]
        public Dictionary<ulong, Tuple<uint, DateTime, BanType>> SpamBansHistory = new Dictionary<ulong, Tuple<uint, DateTime, BanType>>();
        
    }
}
