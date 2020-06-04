using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ArmaforcesMissionBot.DataClasses.SQL;
using ArmaforcesMissionBotSharedClasses;
using Discord;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArmaforcesMissionBot.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        [HttpGet("missions")]
        public void Missions(bool includeArchive = false, uint ttl = 0)
        {
            var missions = Program.GetMissions();
            JArray missionArray = new JArray();
            using (var db = new DbBoderator())
            {
	            foreach (var mission in db.Missions.OrderByDescending(q => q.Date))
	            {
		            var objMission = new JObject();
		            objMission.Add("title", mission.Title);
		            objMission.Add("date", mission.Date.ToString("yyyy-MM-ddTHH:mm:ss"));
		            objMission.Add("closeDate", mission.CloseDate.ToString("yyyy-MM-ddTHH:mm:ss"));
		            objMission.Add("image", mission.Attachment);
		            objMission.Add("description", mission.Description);
		            objMission.Add("modlist", mission.Modlist);
		            objMission.Add("id", mission.SignupChannel);
		            objMission.Add("freeSlots", Helpers.MiscHelper.CountFreeSlots(mission.SignupChannel));
		            objMission.Add("allSlots", Helpers.MiscHelper.CountAllSlots(mission.SignupChannel));
		            if (mission.Date < DateTime.Now)
		            {
			            objMission.Add("archive", true);
                        objMission.Add("state", "Archived");
		            }
		            else
			            objMission.Add("state", mission.CloseDate < DateTime.Now ? "Closed" : "Open");

		            missionArray.Add(objMission);
	            }

	            Response.ContentType = "application/json; charset=utf-8";
	            if (ttl != 0)
	            {
		            Response.Headers.Add("Cache-Control", $"public, max-age={ttl}");
	            }

	            Response.WriteAsync($"{missionArray}");
            }
        }

        [HttpGet("emotes")]
        public void Emotes()
        {
            var emotes = Program.GetEmotes();
            JArray emotesArray = new JArray();
            foreach (var emote in emotes)
            {
                var emoteObj = new JObject();
                var animated = emote.Animated ? "a" : "";
                emoteObj.Add("id", $"<{animated}:{emote.Name}:{emote.Id}>");
                emoteObj.Add("url", emote.Url);

                emotesArray.Add(emoteObj);
            }
            Response.WriteAsync($"{emotesArray}");
        }

        [HttpGet("users")]
        public void Users()
        {
            var users = Program.GetUsers();
            var guild = Program.GetClient().GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild")));
            var makerRole = guild.GetRole(ulong.Parse(Environment.GetEnvironmentVariable("AF_MissionMakerRole")));
            JArray usersArray = new JArray();
            foreach (var user in users)
            {
                var userObj = new JObject();
                userObj.Add("id", user.Id);
                userObj.Add("name", user.Username);
                userObj.Add("isMissionMaker", user.Roles.Contains(makerRole));

                usersArray.Add(userObj);
            }
            Response.WriteAsync($"{usersArray.ToString()}");
        }
    }
}
