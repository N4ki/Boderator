using LinqToDB.Mapping;
using System;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Missions")]
	public class MissionTbl
	{
		[Column(Name = "SignupChannel"), PrimaryKey, NotNull]
		public ulong SignupChannel { get; set; }

		[Column(Name = "Title")]
		public string Title { get; set; }

		[Column(Name = "Date")]
		public DateTime Date { get; set; }

		[Column(Name = "CloseDate")]
		public DateTime CloseDate { get; set; }

		[Column(Name = "Description")]
		public string Description { get; set; }

		[Column(Name = "Attachment")]
		public string Attachment { get; set; }

		[Column(Name = "Filename")]
		public string Filename { get; set; }

		[Column(Name = "Modlist")]
		public string Modlist { get; set; }

		[Column(Name = "Owner")]
		public ulong Owner { get; set; }

		public MissionTbl()
		{
			SignupChannel = 0;
		}
	}
}
