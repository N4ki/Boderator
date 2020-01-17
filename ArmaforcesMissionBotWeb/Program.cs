using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ArmaforcesMissionBotWeb.HelperClasses;
using dotenv.net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ArmaforcesMissionBotWeb
{
    public class Program
    {
#if DEBUG
        public const string SelfAddress = "https://localhost:52294";
        public const string BoderatorAddress = "http://localhost:5555";
        //public const string SelfAddress = "https://localhost:5000";
        //public const string BoderatorAddress = "http://localhost:59286";
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
                //.UseUrls(new string[]{"http://*:5000"})
                .UseStartup<Startup>();

        public static void UpdateDatabase(string token)
        {
            DiscordUser user = null;
            bool canCreateMissions = false;
            List<DiscordPartialGuild> guilds = null;
            {
                var request = (HttpWebRequest)WebRequest.Create($"https://discordapp.com/api/users/@me");

                var postData = "";
                var data = Encoding.ASCII.GetBytes(postData);

                byte[] bytes = Encoding.GetEncoding(28591).GetBytes(token);

                request.Method = "GET";
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                user = JsonConvert.DeserializeObject<DiscordUser>(responseString);
            }

            {
                var request = (HttpWebRequest)WebRequest.Create($"https://discordapp.com/api/users/@me/guilds");

                var postData = "";
                var data = Encoding.ASCII.GetBytes(postData);

                byte[] bytes = Encoding.GetEncoding(28591).GetBytes(token);

                request.Method = "GET";
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                guilds = JsonConvert.DeserializeObject<List<DiscordPartialGuild>>(responseString);
            }

            {
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create($"{Program.BoderatorAddress}/api/users");

                request.Method = "GET";

                var response = (System.Net.HttpWebResponse)request.GetResponse();

                var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

                var users = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(responseString);

                canCreateMissions = (bool)(users.Single(x => x["id"].ToString() == user.id)["isMissionMaker"]);
            }

            var AFGuild = guilds.Single(x => x.id == Environment.GetEnvironmentVariable("AF_GUILDID"));

            if (AFGuild != null)
            {
                Program.Database.StoreUser(token, user);
                Program.Database.SetUserCanCreateMissions(token, canCreateMissions);
            }
        }
    }
}
