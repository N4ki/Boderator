using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ArmaforcesMissionBotWeb.HelperClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArmaforcesMissionBotWeb.Pages
{
    public class SignupsModel : PageModel
    {
        public JArray _MissionMeta { get; set; }
        public void OnGet()
        {
            // Get discord data
            var token = Request.Cookies["Token"];

            DiscordUser user = null;
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

            var AFGuild = guilds.Single(x => x.id == Environment.GetEnvironmentVariable("AF_GUILDID"));

            if(AFGuild != null)
                Response.Cookies.Append("DiscordID", user.id.ToString());

            // Get data from boderator
            {
                var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/missions");

                request.Method = "GET";

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                _MissionMeta = JsonConvert.DeserializeObject<JArray>(responseString);
            }
        }
    }
}