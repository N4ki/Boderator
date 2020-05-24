using LinqToDB.Mapping;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Slots")]
	public class SlotTbl
	{
		[Column(Name = "Name")]
		public string Name { get; set; }

		[Column(Name = "Emoji"), PrimaryKey, NotNull]
		public string Emoji { get; set; }

		[Column(Name = "Count")]
		public int Count { get; set; }

		[Column(Name = "IsReserve")]
		public bool IsReserve { get; set; }

		[Column(Name = "TeamID"), PrimaryKey, NotNull]
		public ulong TeamID { get; set; }

		private TeamTbl _team;

		[Association(ThisKey = "TeamID", OtherKey = "TeamMsg", CanBeNull = false)]
		public TeamTbl Team
		{
			get => _team;
			set
			{
				_team = value;
				TeamID = _team.TeamMsg;
			}
		}

		public SlotTbl()
		{
		}

		public SlotTbl(string name, string emoji, int count, bool isReserve, ulong teamID)
		{
			Name = name;
			Emoji = emoji;
			Count = count;
			IsReserve = isReserve;
			TeamID = teamID;
		}

		public static SlotTbl Build(SlotTbl slot, TeamTbl team)
		{
			if (slot != null)
				slot.Team = team;
			return slot;
		}
	}
}
