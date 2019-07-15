using ArmaforcesMissionBot.DataClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ArmaforcesMissionBot.Helpers
{
    public class MiscHelper
    {
        public static string BuildTeamSlots(SignupsData.SignupsInstance.Team team)
        {
            var description = "";
            foreach (var slot in team.Slots)
            {
                for (var i = 0; i < slot.Value; i++)
                    description += slot.Key + "-\n";
            }

            foreach (var prebeton in team.Signed)
            {
                var regex = new Regex(Regex.Escape(prebeton.Value) + @"-(?:$|\n)");
                description = regex.Replace(description, prebeton.Value + "-" + prebeton.Key + "\n", 1);
            }

            return description;
        }
    }
}
