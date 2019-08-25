using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArmaforcesMissionBotWeb.Pages
{
    public class MissionModel : PageModel
    {
        public ArmaforcesMissionBotSharedClasses.Mission _Mission = null;
        public JArray _Emotes = null;
        public JArray _Users = null;
        public Dictionary<ulong, string> _TeamSlots = new Dictionary<ulong, string>();
        public void OnGet()
        {
            if(Request.Query.Keys.Contains("id"))
            {
                // Get data from boderator
                {
                    var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/mission?id={Request.Query["id"]}");

                    request.Method = "GET";

                    var response = (HttpWebResponse)request.GetResponse();

                    var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    _Mission = JsonConvert.DeserializeObject<ArmaforcesMissionBotSharedClasses.Mission>(responseString);
                }

                {
                    var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/emotes");

                    request.Method = "GET";

                    var response = (HttpWebResponse)request.GetResponse();

                    var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    _Emotes = JsonConvert.DeserializeObject<JArray>(responseString);
                }

                {
                    var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/users");

                    request.Method = "GET";

                    var response = (HttpWebResponse)request.GetResponse();

                    var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    _Users = JsonConvert.DeserializeObject<JArray>(responseString);
                }

                foreach (var team in _Mission.Teams)
                {
                    var slotsText = "";
                    foreach (var slot in team.Slots)
                    {
                        for (var i = 0; i < slot.Value; i++)
                        {
                            slotsText += "<tr class='text-align-middle'>";
                            if (_Emotes.Any(x => (string)x["id"] == slot.Key))
                            {
                                slotsText += $"<td><img width='16' height='16' src='{_Emotes.Single(x => (string)x["id"] == slot.Key)["url"]}'/></td><td>{team.SlotNames[slot.Key]}</td>";
                            }
                            else
                            {
                                slotsText += $"<td>{slot.Key}</td><td>{team.SlotNames[slot.Key]}</td>";
                            }
                            if(team.Signed.ContainsValue(slot.Key))
                            {
                                var signedUser = team.Signed.First(x => x.Value == slot.Key);
                                team.Signed.Remove(signedUser.Key);
                                slotsText += $"<td class='text-right'>{_Users.Single(x => (string)x["id"] == signedUser.Key)["name"]}</td>";
                                if(signedUser.Key.Contains(Request.Cookies["DiscordID"]))
                                {
                                    slotsText += "<td class='text-right'><a href='signoff' class='btn btn-outline-warning'>Wypisz</button></td>";
                                }
                            }
                            else if(!_Mission.SignedUsers.Contains(ulong.Parse(Request.Cookies["DiscordID"])))
                            {
                                slotsText += "<td class='text-right'><a href='signup' class='btn btn-outline-primary'>Zapisz</button></td>";
                            }
                            slotsText += "</tr>";
                        }
                    }
                    _TeamSlots.Add(team.TeamMsg, slotsText);
                }
            }
        }
    }
}