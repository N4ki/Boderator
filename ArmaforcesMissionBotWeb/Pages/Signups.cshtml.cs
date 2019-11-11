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

            Program.UpdateDatabase(token);

            // Get data from boderator
            {
                var request = (HttpWebRequest)WebRequest.Create($"{Program.BoderatorAddress}/api/missions");

                request.Method = "GET";

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                _MissionMeta = JsonConvert.DeserializeObject<JArray>(responseString);
            }
        }
    }
}