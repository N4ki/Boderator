using LinqToDB.Configuration;

namespace ArmaforcesMissionBot.Database
{
	public class ConnectionStringSettings : IConnectionStringSettings
	{
		public string Name { get; set; }
		public string ProviderName { get; set; }
		public string Server { get; set; }
		public string Database { get; set; }
		public string User { get; set; }
		public string Password { get; set; }
		public bool IsGlobal => false;
		public string ConnectionString => $"Server={Server};Database={Database};User Id={User};Password={Password};";
	}
}
