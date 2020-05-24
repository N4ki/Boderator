using LinqToDB.Mapping;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Signed")]
	public class SignedTbl
	{
		[Column(Name = "UserID"), PrimaryKey, NotNull]
		public ulong UserID { get; set; }

		[Column(Name = "Emoji"), PrimaryKey, NotNull]
		public string Emoji { get; set; }

		[Column(Name = "TeamID"), PrimaryKey, NotNull]
		public ulong TeamID { get; set; }

		private SlotTbl _slot;

		[Association(ThisKey = "Emoji, TeamID", OtherKey = "Emoji, TeamID", CanBeNull = false)]
		public SlotTbl Slot
		{
			get => _slot;
			set
			{
				_slot = value;
				Emoji = _slot.Emoji;
				TeamID = _slot.TeamID;
			}
		}

		public SignedTbl()
		{
		}

		public SignedTbl(ulong userID, string emoji, ulong teamID)
		{
			UserID = userID;
			Emoji = emoji;
			TeamID = teamID;
		}
	}
}