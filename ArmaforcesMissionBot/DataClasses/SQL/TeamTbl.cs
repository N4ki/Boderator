using LinqToDB.Mapping;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Teams")]
	public class TeamTbl
	{
		[Column(Name = "TeamMsg"), PrimaryKey, NotNull]
		public ulong TeamMsg { get; set; }

		[Column(Name = "Name")]
		public string Name { get; set; }

		[Column(Name = "Pattern")]
		public string Pattern { get; set; }

		[Column(Name = "Reserve")]
		public ulong Reserve { get; set; }

		[Column(Name = "MissionID"), NotNull]
		public ulong MissionID { get; set; }

		private MissionTbl _mission;
		[Association(ThisKey = "MissionID", OtherKey = "SignupChannel", CanBeNull = false)]
		public MissionTbl Mission 
		{ 
			get => _mission;
			set
			{
				_mission = value;
				MissionID = _mission.SignupChannel;
			}
		}
	}
}
