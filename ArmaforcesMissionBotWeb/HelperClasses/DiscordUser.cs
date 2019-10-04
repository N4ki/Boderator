using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBotWeb.HelperClasses
{
    public class DiscordUser
    {
        public string id;
        public string username;
        public string discriminator;
        public string avatar;
        public bool bot;
        public bool mfa_enabled;
        public string locale;
        public bool verified;
        public string email;
        public int flags;
        public int premium_type;

        public DiscordUser Copy()
        {
            DiscordUser copy = new DiscordUser();
            copy.id = String.Copy(id);
            copy.username = String.Copy(username);
            copy.discriminator = String.Copy(discriminator);
            copy.avatar = String.Copy(avatar);
            copy.bot = bot;
            copy.mfa_enabled = mfa_enabled;
            copy.locale = String.Copy(locale);
            copy.verified = verified;
            // email is noll for token requests
            copy.flags = flags;
            copy.premium_type = premium_type;

            return copy;
        }
    }
}
