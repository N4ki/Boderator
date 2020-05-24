using ArmaforcesMissionBot.DataClasses;
using LinqToDB.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ArmaforcesMissionBot.Database
{
	public class DbBoderatorSettings : ILinqToDBSettings
	{
		private readonly Config _config;
		public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

		public string DefaultConfiguration => "MySql";
		public string DefaultDataProvider => "MySql";

		public IEnumerable<IConnectionStringSettings> ConnectionStrings
		{
			get
			{
				yield return
					new ConnectionStringSettings
					{
						Name = "BoderatorConnection",
						ProviderName = "MySql",
						Server = "ilddor.com",
						Database = "Boderator",
						User = _config.DatabaseUser,
						Password = _config.DatabasePassword
					};
			}
		}

		public DbBoderatorSettings(Config config)
		{
			_config = config;
		}
	}
}
