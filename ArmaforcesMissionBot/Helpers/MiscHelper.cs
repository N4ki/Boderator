using ArmaforcesMissionBot.DataClasses;
using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ArmaforcesMissionBot.Helpers
{
    public class MiscHelper
    {
        public static string BuildTeamSlots(ArmaforcesMissionBotSharedClasses.Mission.Team team)
        {
            var description = "";
            foreach (var slot in team.Slots)
            {
                for (var i = 0; i < slot.Value; i++)
                    description += HttpUtility.HtmlDecode(slot.Key) + "-\n";
            }

            foreach (var prebeton in team.Signed)
            {
                Console.WriteLine(prebeton.Value + " " + prebeton.Key);
                Console.WriteLine(HttpUtility.HtmlDecode(prebeton.Value) + " " + HttpUtility.HtmlDecode(prebeton.Key));
                var regex = new Regex(Regex.Escape(HttpUtility.HtmlDecode(prebeton.Value)) + @"-(?:$|\n)");
                description = regex.Replace(description, HttpUtility.HtmlDecode(prebeton.Value) + "-" + HttpUtility.HtmlDecode(prebeton.Key) + "\n", 1);
            }

            return description;
        }

        public static void BuildTeamsEmbed(List<ArmaforcesMissionBotSharedClasses.Mission.Team> teams, EmbedBuilder builder)
        {
            foreach (var team in teams)
            {
                var slots = Helpers.MiscHelper.BuildTeamSlots(team);

                builder.AddField(team.Name, slots, true);
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
            int slots = 0;
            foreach(var team in mission.Teams)
            {
                foreach(var slot in team.Slots)
                {
                    slots += slot.Value;
                }
            }

            return slots - mission.SignedUsers.Count;
        }

        public static int CountAllSlots(ArmaforcesMissionBotSharedClasses.Mission mission)
        {
            int slots = 0;
            foreach (var team in mission.Teams)
            {
                foreach (var slot in team.Slots)
                {
                    slots += slot.Value;
                }
            }

            return slots;
        }
    }
}
