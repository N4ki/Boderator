using LinqToDB.Mapping;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Signed")]
	public class SignedTbl
	{
		[PrimaryKey, Identity]
		public int ID { get; set; }

		[Column(Name = "UserID")]
		public ulong UserID { get; set; }

		[Column(Name = "SlotID"), NotNull]
		public int SlotID { get; set; }

		[Association(ThisKey = "SlotID", OtherKey = "ID", CanBeNull = false)]
		public SlotTbl Slot { get; set; }
	}
}