using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArmaforcesMissionBotWeb.HelperClasses
{
    public class UserDatabase
    {
        private Dictionary<string, DiscordUser> _Database = new Dictionary<string, DiscordUser>();
        private SemaphoreSlim _Access = new SemaphoreSlim(1);

        public void StoreUser(string token, DiscordUser user)
        {
            _Access.Wait(-1);
            try
            {
                _Database[token] = user;
            }
            finally
            {
                _Access.Release();
            }
        }

        public DiscordUser GetUser(string token)
        {
            DiscordUser result;

            _Access.Wait(-1);
            try
            {
                result = _Database[token].Copy();
            }
            finally
            {
                _Access.Release();
            }

            return result;
        }
    }
}
