using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using ArmaforcesMissionBot.DataClasses.SQL;
using static ArmaforcesMissionBot.DataClasses.OpenedDialogs;

namespace ArmaforcesMissionBot.Helpers
{
    public class MiscHelper
    {
        public static List<string> BuildTeamSlots(ArmaforcesMissionBotSharedClasses.Mission.Team team)
        {
            List<string> results = new List<string>();
            results.Add("");
            foreach (var slot in team.Slots)
            {
                for (var i = 0; i < slot.Count; i++)
                {
                    string description = $"{HttpUtility.HtmlDecode(slot.Emoji)}";
                    if (slot.Name != "" && i == 0)
                        description += $"({slot.Name})";
                    description += "-";
                    if (i < slot.Signed.Count)
                    {
                        var user = Program.GetGuildUser(slot.Signed.ElementAt(i));
                        if(user != null)
                            description += user.Mention;
                    }
                    description += "\n";
                    
                    if(results.Last().Length + description.Length > 1024)
                        results.Add("");

                    results[results.Count-1] += description;
                }
            }

            return results;
        }

        public static List<string> BuildTeamSlots(ulong teamID)
        {
	        List<string> results = new List<string>();
	        results.Add("");

	        using (var db = new DataClasses.SQL.DbBoderator())
	        {
		        foreach (var slot in db.Slots.Where(q => q.TeamID == teamID))
		        {
			        var signed = new DataClasses.SQL.DbBoderator().Signed.Where(q => q.Emoji == slot.Emoji && q.TeamID == slot.TeamID);
			        for (var i = 0; i < slot.Count; i++)
			        {
				        string description = $"{HttpUtility.HtmlDecode(slot.Emoji)}";
				        if (slot.Name != "" && i == 0)
					        description += $"({slot.Name})";
				        description += "-";
				        if (i < signed.Count())
				        {
					        var user = Program.GetGuildUser(signed.ElementAt(i).UserID);
					        if (user != null)
						        description += user.Mention;
				        }

				        description += "\n";

				        if (results.Last().Length + description.Length > 1024)
					        results.Add("");

				        results[results.Count - 1] += description;
			        }
		        }
	        }

	        return results;
        }

        public static void BuildTeamsEmbed(List<ArmaforcesMissionBotSharedClasses.Mission.Team> teams, EmbedBuilder builder, bool removeSlotNamesFromName = false)
        {
            foreach (var team in teams)
            {
                var slots = Helpers.MiscHelper.BuildTeamSlots(team);

                var teamName = team.Name;
                if (removeSlotNamesFromName)
                {
                    foreach (var slot in team.Slots)
                    {
                        if (teamName.Contains(slot.Emoji))
                            teamName = teamName.Remove(teamName.IndexOf(slot.Emoji));
                    }
                }

                if(slots.Count == 1)
                    builder.AddField(teamName, slots[0], true);
                else if(slots.Count > 1)
                {
                    foreach(var part in slots)
                    {
                        builder.AddField(teamName, part, true);
                    }
                }
            }
        }

        public static void BuildTeamsEmbed(ulong missionID, EmbedBuilder builder)
        {
	        using (var db = new DataClasses.SQL.DbBoderator())
	        {
		        foreach (var team in db.Teams.Where(q => q.MissionID == missionID))
		        {
			        var slots = Helpers.MiscHelper.BuildTeamSlots(team.TeamMsg);

			        var teamName = team.Name;
			        /*if (removeSlotNamesFromName)
			        {
				        foreach (var slot in team.Slots)
				        {
					        if (teamName.Contains(slot.Emoji))
						        teamName = teamName.Remove(teamName.IndexOf(slot.Emoji));
				        }
			        }*/

			        if (slots.Count == 1)
				        builder.AddField(teamName, slots[0], true);
			        else if (slots.Count > 1)
			        {
				        foreach (var part in slots)
				        {
					        builder.AddField(teamName, part, true);
				        }
			        }
		        }
	        }
        }

        public static string BuildEditTeamsPanel(List<ArmaforcesMissionBotSharedClasses.Mission.Team> teams, int highlightIndex)
        {
            string result = "";

            int index = 0;
            foreach (var team in teams)
            {
                if (highlightIndex == index)
                    result += "**";
                result += $"{team.Name}";
                if (highlightIndex == index)
                    result += "**";
                result += "\n";
                index++;
            }

            return result;
        }

        public static int CountFreeSlots(ArmaforcesMissionBotSharedClasses.Mission mission)
        {
            return CountAllSlots(mission) - mission.SignedUsers.Count;
        }

        public static int CountFreeSlots(ulong missionID)
        {
	        return CountAllSlots(missionID) - CountTakenSlots(missionID);
        }

        public static int CountAllSlots(ArmaforcesMissionBotSharedClasses.Mission mission)
        {
            int slots = 0;
            foreach (var team in mission.Teams)
            {
                foreach (var slot in team.Slots)
                {
                    slots += slot.Count;
                }
            }

            return slots;
        }

        public static int CountTakenSlots(ulong missionID)
        {
	        int count = 0;
	        using (var db = new DbBoderator())
	        {
		        var query =
			        from m in db.Missions.Where(q => q.SignupChannel == missionID)
			        join t in db.Teams on m.SignupChannel equals t.MissionID
			        join s in db.Slots on t.TeamMsg equals s.TeamID
                    join u in db.Signed on new {s.Emoji, s.TeamID} equals new {u.Emoji, u.TeamID}
			        select u;

		        count = query.Count();
	        }

	        return count;
        }

        public static int CountAllSlots(ulong missionID)
        {
	        int count = 0;
	        using (var db = new DbBoderator())
	        {
		        var query =
			        from m in db.Missions.Where(q => q.SignupChannel == missionID)
			        join t in db.Teams on m.SignupChannel equals t.MissionID
                    join s in db.Slots on t.TeamMsg equals s.TeamID
                    select s;

		        count = query.Sum(x => x.Count);
	        }

	        return count;
        }

        public static async void CreateConfirmationDialog(SocketCommandContext context, Embed description, Action<Dialog> confirmAction, Action<Dialog> cancelAction)
        {
            var dialog = new OpenedDialogs.Dialog();

            var message = await context.Channel.SendMessageAsync("Zgadza sie?", embed: description);

            dialog.DialogID = message.Id;
            dialog.DialogOwner = context.User.Id;
            dialog.Buttons["✔️"] = confirmAction;
            dialog.Buttons["❌"] = cancelAction;

            var reactions = new List<IEmote>();
            foreach(var key in dialog.Buttons.Keys)
            {
                reactions.Add(new Emoji(key));
            }
            await message.AddReactionsAsync(reactions.ToArray());

            ArmaforcesMissionBot.Program.GetDialogs().Dialogs.Add(dialog);
        }

        public static MatchCollection GetSlotMatchesFromText(string text)
        {
            string unicodeEmoji = @"(?:\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])(?:\ufe0f)?";
            string emote = $@"((?:<?a?:.+?:(?:[0-9]+>)?)|{unicodeEmoji})";
            string slotCount = @"(\[[0-9]+\])";
            string slotName = @"([^\|]*?)?";
            string rolePattern = $@"[ ]*{emote}[ ]*{slotCount}[ ]*{slotName}[ ]*(?:\|)?";

            return Regex.Matches(text, rolePattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        }
    }
}
