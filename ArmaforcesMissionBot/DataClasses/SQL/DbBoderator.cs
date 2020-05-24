using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.DataClasses.SQL
{
	public class DbBoderator : LinqToDB.Data.DataConnection
	{
		public DbBoderator() : base("BoderatorConnection")
		{
		}

		public ITable<MissionTbl> Missions => GetTable<MissionTbl>();
		public ITable<TeamTbl> Teams => GetTable<TeamTbl>();
	}
}
