using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
            => Missions(DateTime.MinValue, DateTime.MaxValue, includeArchive);
        
        [HttpGet("missions")]
        public void Missions(DateTime earliestDateTime, bool includeArchive = false, uint ttl = 0)
            => Missions(earliestDateTime, DateTime.MaxValue, includeArchive, ttl);
        
        [HttpGet("missions")]
        public void Missions(DateTime earliestDateTime, DateTime latestDateTime, bool includeArchive = false, uint ttl = 0)
        {
            var missions = Program.GetMissions();
            JArray missionArray = new JArray();
            foreach (var mission in missions.Missions
                .Where(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing)
                .Where(x => x.Date >= earliestDateTime)
                .Where(x => x.Date <= latestDateTime)
                .Reverse())
            {
                var objMission = new JObject();
                objMission.Add("title", mission.Title);
                objMission.Add("date", mission.Date.ToString("yyyy-MM-ddTHH:mm:ss"));
                objMission.Add("closeDate", mission.CloseTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                objMission.Add("image", mission.Attachment);
                objMission.Add("description", mission.Description);
                objMission.Add("modlist", mission.Modlist);
                objMission.Add("id", mission.SignupChannel);
                objMission.Add("freeSlots", Helpers.MiscHelper.CountFreeSlots(mission));
                objMission.Add("allSlots", Helpers.MiscHelper.CountAllSlots(mission));
                objMission.Add("state", "Open");

                missionArray.Add(objMission);
            }

            if(includeArchive)
            {
                var archiveMissions = Program.GetArchiveMissions();
                foreach (var mission in archiveMissions.ArchiveMissions.AsEnumerable()
                    .Where(x => x.Date >= earliestDateTime)
                    .Where(x => x.Date <= latestDateTime)
                    .Reverse())
                {
                    var objMission = new JObject();
                    objMission.Add("title", mission.Title);
                    objMission.Add("date", mission.Date.ToString("yyyy-MM-ddTHH:mm:ss"));
                    objMission.Add("closeDate", mission.CloseTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                    objMission.Add("image", mission.Attachment);
                    objMission.Add("description", mission.Description);
                    objMission.Add("modlist", mission.Modlist);
                    objMission.Add("archive", true);
                    objMission.Add("freeSlots", mission.FreeSlots);
                    objMission.Add("allSlots", mission.AllSlots);
                    objMission.Add("state", mission.Date < DateTime.Now ? "Archived" : "Closed");

                    missionArray.Add(objMission);
                }
            }

            Response.ContentType = "application/json; charset=utf-8";
            if (ttl != 0)
            {
                Response.Headers.Add("Cache-Control", $"public, max-age={ttl}");
            }

            Response.WriteAsync($"{missionArray.ToString()}");
        }

        [HttpGet("mission")]
        public void Mission(ulong id, ulong userID)
        {
            if (!Program.IsUserSpamBanned(userID) && Program.ShowMissionToUser(userID, id))
            {
                var missions = Program.GetMissions();

                var mission = missions.Missions.Single(x => x.SignupChannel == id);

                var serialized = JsonConvert.SerializeObject(mission);
                Response.WriteAsync($"{serialized}");
            }
            else
            {
                Response.StatusCode = 503;
                Response.WriteAsync("Banned");
            }
        }

        [HttpGet("signup")]
        public async Task Signup(ulong missionID, ulong teamID, ulong userID, string slotID)
        {
            var missions = Program.GetMissions();

            missions.BanAccess.Wait(-1);
            try
            {
                if (missions.SignupBans.ContainsKey(userID) ||
                    missions.SpamBans.ContainsKey(userID))
                {
                    Response.StatusCode = 503;
                    await Response.WriteAsync("Banned");
                    return;
                }
            }
            finally
            {
                missions.BanAccess.Release();
            }

            if (missions.Missions.Any(x => x.SignupChannel == missionID))
            {
                var mission = missions.Missions.Single(x => x.SignupChannel == missionID);

                mission.Access.Wait(-1);
                try
                {
                    if (!mission.SignedUsers.Contains(userID))
                    {
                        if (mission.Teams.Any(x => x.TeamMsg == teamID))
                        {
                            var team = mission.Teams.Single(x => x.TeamMsg == teamID);

                            if (team.Slots.Any(x => x.Emoji == slotID && x.Count > x.Signed.Count()))
                            {
                                var channel = Program.GetChannel(missionID);
                                var teamMsg = await channel.GetMessageAsync(teamID) as IUserMessage;

                                var embed = teamMsg.Embeds.Single();

                                if (!mission.SignedUsers.Contains(userID))
                                {
                                    var slot = team.Slots.Single(x => x.Emoji == slotID);
                                    slot.Signed.Add(userID);
                                    mission.SignedUsers.Add(userID);

                                    var newDescription = Helpers.MiscHelper.BuildTeamSlots(team);

                                    var newEmbed = new EmbedBuilder
                                    {
                                        Title = embed.Title,
                                        Color = embed.Color
                                    };

                                    if (newDescription.Count == 2)
                                        newEmbed.WithDescription(newDescription[0] + newDescription[1]);
                                    else if (newDescription.Count == 1)
                                        newEmbed.WithDescription(newDescription[0]);

                                    if (embed.Footer.HasValue)
                                        newEmbed.WithFooter(embed.Footer.Value.Text);

                                    await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                                    await Response.WriteAsync("Success");
                                    return;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    mission.Access.Release();
                }
            }

            Response.StatusCode = 400;
            await Response.WriteAsync("Data invalid");
        }

        [HttpGet("signoff")]
        public async Task Signoff(ulong missionID, ulong teamID, ulong userID, string slotID)
        {
            var missions = Program.GetMissions();

            missions.BanAccess.Wait(-1);
            try
            {
                if (missions.SignupBans.ContainsKey(userID) ||
                    missions.SpamBans.ContainsKey(userID))
                {
                    Response.StatusCode = 503;
                    await Response.WriteAsync("Banned");
                    return;
                }
            }
            finally
            {
                missions.BanAccess.Release();
            }

            if (missions.Missions.Any(x => x.SignupChannel == missionID))
            {
                var mission = missions.Missions.Single(x => x.SignupChannel == missionID);

                mission.Access.Wait(-1);
                try
                {
                    if (mission.SignedUsers.Contains(userID))
                    {
                        if (mission.Teams.Any(x => x.TeamMsg == teamID))
                        {
                            var team = mission.Teams.Single(x => x.TeamMsg == teamID);

                            if (team.Slots.Any(x => x.Emoji == slotID))
                            {
                                var channel = Program.GetChannel(missionID);
                                var teamMsg = await channel.GetMessageAsync(teamID) as IUserMessage;

                                var embed = teamMsg.Embeds.Single();

                                if (mission.SignedUsers.Contains(userID))
                                {
                                    var slot = team.Slots.Single(x => x.Emoji == slotID);
                                    slot.Signed.Remove(userID);
                                    mission.SignedUsers.Remove(userID);

                                    var newDescription = Helpers.MiscHelper.BuildTeamSlots(team);

                                    var newEmbed = new EmbedBuilder
                                    {
                                        Title = embed.Title,
                                        Color = embed.Color
                                    };

                                    if (newDescription.Count == 2)
                                        newEmbed.WithDescription(newDescription[0] + newDescription[1]);
                                    else if (newDescription.Count == 1)
                                        newEmbed.WithDescription(newDescription[0]);

                                    if (embed.Footer.HasValue)
                                        newEmbed.WithFooter(embed.Footer.Value.Text);

                                    await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                                    await Response.WriteAsync("Success");
                                    return;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    mission.Access.Release();
                }
            }

            Response.StatusCode = 400;
            await Response.WriteAsync("Data invalid");
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
            Response.WriteAsync($"{emotesArray.ToString()}");
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

        [HttpPost("createMission")]
        public async Task CreateMissionAsync(Mission mission)
        {
            Console.WriteLine(JsonConvert.SerializeObject(mission));
            var signups = Program.GetMissions();

            mission.Editing = ArmaforcesMissionBotSharedClasses.Mission.EditEnum.New;
            signups.Missions.Add(mission);

            if (Helpers.SignupHelper.CheckMissionComplete(mission))
            {
                var guild = Program.GetClient().GetGuild(Program.GetConfig().AFGuild);

                var signupChannel = await Helpers.SignupHelper.CreateChannelForMission(guild, mission, signups);
                mission.SignupChannel = signupChannel.Id;

                await Helpers.SignupHelper.CreateMissionMessagesOnChannel(guild, mission, signupChannel);
            }
            else
            {
                await Response.WriteAsync($"Incorrect data");
            }
        }
    }
}
