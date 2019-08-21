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
    }
}
