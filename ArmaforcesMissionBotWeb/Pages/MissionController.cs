using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArmaforcesMissionBotWeb.Pages
{
    [Route("api/")]
    [ApiController]
    public class MissionController : ControllerBase
    {
        [HttpGet("signup")]
        public ActionResult Signup(ulong missionID, ulong teamID, string slotID)
        {
            var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/" +
                $"signup?missionID={missionID}" +
                $"&teamID={teamID}" +
                $"&userID={Program.Database.GetUser(Request.Cookies["Token"]).id}" +
                $"&slotID={slotID}");

            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return Redirect($"/Mission?id={Request.Query["missionID"]}");
        }

        [HttpGet("signoff")]
        public ActionResult Signoff(ulong missionID, ulong teamID, string slotID)
        {
            var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/" +
                $"signoff?missionID={missionID}" +
                $"&teamID={teamID}" +
                $"&userID={Program.Database.GetUser(Request.Cookies["Token"]).id}" +
                $"&slotID={slotID}");

            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return Redirect($"/Mission?id={Request.Query["missionID"]}");
        }
    }
}