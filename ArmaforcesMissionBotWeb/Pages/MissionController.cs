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
        public ActionResult Signup()
        {
            var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/" +
                $"signup?missionID={Request.Query["missionID"]}&teamID={Request.Query["teamID"]}&userID={Request.Query["userID"]}&slotID={Request.Query["slotID"]}");

            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return Redirect($"/Mission?id={Request.Query["missionID"]}");
        }

        [HttpGet("signoff")]
        public ActionResult Signoff()
        {
            var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/" +
                $"signoff?missionID={Request.Query["missionID"]}&teamID={Request.Query["teamID"]}&userID={Request.Query["userID"]}&slotID={Request.Query["slotID"]}");

            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return Redirect($"/Mission?id={Request.Query["missionID"]}");
        }
    }
}