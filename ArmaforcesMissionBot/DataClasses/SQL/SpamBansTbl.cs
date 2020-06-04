using System;
using LinqToDB.Mapping;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "SpamBans")]
	public class SpamBansTbl
	{
		[Column(Name = "ID"), PrimaryKey, NotNull, Identity]
		public int ID { get; set; }

		[Column(Name = "UserID"), NotNull]
		public ulong UserID { get; set; }

		[Column(Name = "Start"), NotNull]
		public DateTime Start { get; set; }

		[Column(Name = "End"), NotNull]
		public DateTime End { get; set; }
	}
}