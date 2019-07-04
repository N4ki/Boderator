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
        public ulong SignupRank { get; set; }

        public void Save()
        {
            try
            {
                using (FileStream stream = File.Open("config.json", FileMode.Create))
                {
                    var streamWriter = new StreamWriter(stream);
                    streamWriter.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
                    streamWriter.Flush();
                    stream.Flush();
                }
            }
            catch (Exception) { }
        }

        public void Load()
        {
            try
            {
                using (Stream stream = File.Open("config.json", FileMode.Open))
                {
                    var streamReader = new StreamReader(stream);
                    var tmp = JsonConvert.DeserializeObject<Config>(streamReader.ReadToEnd());
                    // Is here a better way to do this?
                    Type t = this.GetType();
                    PropertyInfo[] properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in properties)
                    {
                        prop.SetValue(this, prop.GetValue(tmp));
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
