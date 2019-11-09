using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ArmaforcesMissionBotSharedClasses;
using ArmaforcesMissionBotWeb.HelperClasses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArmaforcesMissionBotWeb.Pages
{
    public class CreateMissionModel : PageModel
    {
        public List<string> _Modlists = new List<string>();
        public JArray _Users = null;
        public string _SelectOptionsCode = "";
        public JArray _Emotes = null;
        public string _CustomEmojiCode = "";

        private IHostingEnvironment _environment;
        public CreateMissionModel(IHostingEnvironment environment)
        {
            _environment = environment;
        }

        public async Task OnGetAsync()
        {
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync("https://server.armaforces.com:8888//modsets/downloadable.json");
                var pageContents = await response.Content.ReadAsStringAsync();
                _Modlists = JsonConvert.DeserializeObject<List<string>>(pageContents);
            }

            {
                var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/users");

                request.Method = "GET";

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                _Users = JsonConvert.DeserializeObject<JArray>(responseString);

                foreach(var user in _Users)
                {
                    _SelectOptionsCode += $"<option value='{user["id"].ToString().Substring(3, user["id"].ToString().Length-4)}'>{user["name"]}</option>";
                }
            }

            {
                var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/emotes");

                request.Method = "GET";

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                _Emotes = JsonConvert.DeserializeObject<JArray>(responseString);

                foreach(var emote in _Emotes)
                {
                    _CustomEmojiCode += 
                        "<i class='emojibtn' role='button' onclick='AddEmoji(\"${$(element).attr('id')}\", \"" + 
                        emote["id"].ToString().Substring(1, emote["id"].ToString().Length-2) + "\", \"" + emote["url"] + "\")'>" +
                        "<img class='emojioneemoji' src='" + emote["url"] + "'></i>";
                }
            }
        }

        public async Task OnPostAsync(
            string missionName,
            string missionDate,
            string missionDescription,
            IFormFile missionPicture,
            string missionModlist,
            string missionClose,
            Dictionary<int, string> teamName,
            Dictionary<int, Dictionary<int, Dictionary<string, string>>> team,
            Dictionary<int, Dictionary<int, List<string>>> prebetons)
        {
            /*var file = Path.Combine(_environment.ContentRootPath, "uploads", missionPicture.FileName);
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                await missionPicture.CopyToAsync(fileStream);
            }*/

            var mission = new Mission();

            mission.Owner = ulong.Parse(Program.Database.GetUser(Request.Cookies["Token"]).id);
            mission.Title = missionName;
            mission.Date = DateTime.Parse(missionDate);
            mission.Description = missionDescription;
            mission.Modlist = "https://modlist.armaforces.com/#/download/" + missionModlist;
            mission.CloseTime = uint.Parse(missionClose);

            for(int teamID = 0; teamID < teamName.Count; teamID++)
            {
                var missionTeam = new Mission.Team();
                missionTeam.Name = teamName[teamID];
                for(int slotID = 0; slotID < team[teamID].Count; slotID++)
                {
                    var icon = (team[teamID][slotID]["slotIcon"][0] == ':' || team[teamID][slotID]["slotIcon"][0] == 'a') ? $"<{team[teamID][slotID]["slotIcon"]}>" : team[teamID][slotID]["slotIcon"];
                    icon = HttpUtility.HtmlEncode(icon);
                    missionTeam.Slots.Add(icon, int.Parse(team[teamID][slotID]["slotCount"]));
                    missionTeam.SlotNames.Add(icon, team[teamID][slotID]["slotName"]);
                    foreach (var prebeton in prebetons[teamID][slotID])
                    {
                        missionTeam.Signed.Add(HttpUtility.HtmlEncode("<@!"+prebeton+">"), icon);
                        mission.SignedUsers.Add(ulong.Parse(prebeton));
                    }
                }
                mission.Teams.Add(missionTeam);
            }

            {
                var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/createMission");

                request.Method = "POST";
                request.ContentType = "application/json";

                byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mission));
                
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
        }
    }
}