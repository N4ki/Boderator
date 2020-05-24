using LinqToDB.Mapping;
using System;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	[Table(Name = "Teams")]
	public class TeamTbl
	{
		[PrimaryKey, Identity]
		public ulong TeamMsg { get; set; }

		[Column(Name="Name")]
		public string Name { get; set; }

		[Column(Name="Pattern")]
		public string Description { get; set; }

		[Column(Name="Reserve")]
		public ulong Reserve { get; set; }

		[Column(Name="MissionID"), NotNull]
		public ulong MissionID { get; set; }

		[Association(ThisKey="MissionID", OtherKey="SignupChannel", CanBeNull=false)]
		public MissionTbl Mission { get; set; }
	}
}
