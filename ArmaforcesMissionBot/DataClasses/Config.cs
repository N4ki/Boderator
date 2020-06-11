using dotenv.net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ArmaforcesMissionBot.DataClasses
{
    public class Config
    {
        public string DiscordToken { get; set; }
        public ulong SignupsCategory { get; set; }
        public ulong SignupsArchive { get; set; }
        public ulong AFGuild { get; set; }
        public ulong MissionMakerRole { get; set; }
        public ulong SignupRole { get; set; }
        public ulong BotRole { get; set; }
        public ulong RecruiterRole { get; set; }
        public ulong RecruitRole { get; set; }
        public ulong CreateMissionChannel { get; set; }
        public ulong PublicContemptChannel { get; set; }
        public ulong HallOfShameChannel { get; set; }
        public ulong RecruitInfoChannel { get; set; }
        public ulong RecruitAskChannel { get; set; }

        public void Load()
        {
            DotEnv.Config(false);

            PropertyInfo[] properties = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if(prop.PropertyType == typeof(string))
                    prop.SetValue(this, Environment.GetEnvironmentVariable("AF_" + prop.Name));
                if (prop.PropertyType == typeof(ulong))
                    prop.SetValue(this, ulong.Parse(Environment.GetEnvironmentVariable("AF_" + prop.Name)));
            }
        }
    }
}
