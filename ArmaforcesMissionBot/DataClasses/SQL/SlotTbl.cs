using LinqToDB.Mapping;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Slots")]
	public class SlotTbl
	{
		[PrimaryKey, Identity]
		public int ID { get; set; }

		[Column(Name = "Name")]
		public string Name { get; set; }

		[Column(Name = "Emoji"), NotNull]
		public string Emoji { get; set; }

		[Column(Name = "Count")]
		public int Count { get; set; }

		[Column(Name = "IsReserve")]
		public bool IsReserve { get; set; }

		[Column(Name = "TeamID"), NotNull]
		public ulong TeamID { get; set; }

		[Association(ThisKey = "TeamID", OtherKey = "TeamMsg", CanBeNull = false)]
		public TeamTbl Team { get; set; }
	}
}
