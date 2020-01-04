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

        public class Mission
        {
            public class Team
            {
                public class Slot
                {
                    public string Icon { get; set; }
                    public string Name { get; set; }
                    public int Count { get; set; }
                    public List<ulong> Signed { get; set; }
                }
                public string Name { get; set; }
                public Slot[] Slots { get; set; }
            }
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; }
            public IFormFile Picture { get; set; }
            public string Modlist { get; set; }
            public DateTime Close { get; set; }
            public Team[] Teams { get; set; }

            public ArmaforcesMissionBotSharedClasses.Mission ToShared(ulong owner)
            {
                var shared = new ArmaforcesMissionBotSharedClasses.Mission();

                shared.Title = Name;
                shared.Date = Date;
                shared.CloseTime = Close;
                shared.Description = Description;
                //Attachment handle
                shared.Modlist = "https://modlist.armaforces.com/#/download/" + Modlist;
                shared.Owner = owner;

                foreach(var team in Teams)
                {
                    var outTeam = new ArmaforcesMissionBotSharedClasses.Mission.Team();
                    outTeam.Name = team.Name;

                    foreach(var slot in team.Slots)
                    {
                        var outSlot = new ArmaforcesMissionBotSharedClasses.Mission.Team.Slot();
                        outSlot.Name = slot.Name;
                        outSlot.Emoji = (slot.Icon[0] == ':' || slot.Icon[0] == 'a') ? $"<{slot.Icon}>" : slot.Icon;
                        outSlot.Count = slot.Count;
                        if (slot.Signed != null)
                        {
                            foreach (var prebeton in slot.Signed)
                            {
                                outSlot.Signed.Add(prebeton);
                                shared.SignedUsers.Add(prebeton);
                            }
                        }

                        outTeam.Name += $" | {outSlot.Emoji} [{outSlot.Count}] {outSlot.Name}";

                        outTeam.Slots.Add(outSlot);
                    }

                    shared.Teams.Add(outTeam);
                }

                shared.AttachmentBytes = new byte[Picture.Length];
                using (MemoryStream ms = new MemoryStream())
                {
                    Picture.CopyTo(ms);
                    shared.AttachmentBytes = ms.ToArray();
                    shared.FileName = Picture.FileName;
                }

                return shared;
            }
        }

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
            Mission Mission)
        {
            /*var file = Path.Combine(_environment.ContentRootPath, "uploads", Mission.Picture.FileName);
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                await Mission.Picture.CopyToAsync(fileStream);
            }*/


            var missionOut = Mission.ToShared(ulong.Parse(Program.Database.GetUser(Request.Cookies["Token"]).id));
            
            {
                var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/createMission");

                request.Method = "POST";
                request.ContentType = "application/json";

                string serialized = JsonConvert.SerializeObject(missionOut);

                byte[] byteArray = Encoding.UTF8.GetBytes(serialized);
                
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