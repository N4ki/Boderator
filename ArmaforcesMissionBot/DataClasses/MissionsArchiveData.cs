using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.DataClasses
{
    public class MissionsArchiveData
    {
        public class Mission
        {
            public string Title;
            public DateTime Date;
            public DateTime? CloseTime = null;
            public string Description;
            public string Modlist;
            public string Attachment;
            public ulong FreeSlots;
            public ulong AllSlots;
        }

        public List<Mission> ArchiveMissions = new List<Mission>();
    }
}
