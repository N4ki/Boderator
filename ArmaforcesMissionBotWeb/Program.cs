using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArmaforcesMissionBotWeb.HelperClasses;
using dotenv.net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmaforcesMissionBotWeb
{
    public class Program
    {
#if DEBUG
        public const string SelfAddress = "https://localhost:44348";
        public const string BoderatorAddress = "http://localhost:5555";
#else
        public const string SelfAddress = "https://boderator.ilddor.com";
        public const string BoderatorAddress = "http://localhost:5555";
#endif

        public static UserDatabase Database = new UserDatabase();

        public static void Main(string[] args)
        {
            DotEnv.Config();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://*:5000")
                .UseStartup<Startup>();
    }
}
