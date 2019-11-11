using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArmaforcesMissionBotWeb.HelperClasses
{
    public class UserDatabase
    {
        private class Record
        {
            public DiscordUser  user;
            public bool         canCreateMissions = false;
        }
        private Dictionary<string, Record> _Database = new Dictionary<string, Record>();
        private SemaphoreSlim _Access = new SemaphoreSlim(1);

        public void StoreUser(string token, DiscordUser user)
        {
            _Access.Wait(-1);
            try
            {
                _Database[token] = new Record();
                _Database[token].user = user;
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
                result = _Database[token].user.Copy();
            }
            finally
            {
                _Access.Release();
            }

            return result;
        }

        public bool IsUserRegistered(string token)
        {
            bool result;

            _Access.Wait(-1);
            try
            {
                result = _Database.Keys.Contains(token);
            }
            finally
            {
                _Access.Release();
            }

            return result;
        }

        public void SetUserCanCreateMissions(string token, bool value)
        {
            _Access.Wait(-1);
            try
            {
                _Database[token].canCreateMissions = value;
            }
            finally
            {
                _Access.Release();
            }
        }

        public bool GetUserCanCreateMissions(string token)
        {
            bool result;

            _Access.Wait(-1);
            try
            {
                result = _Database[token].canCreateMissions;
            }
            finally
            {
                _Access.Release();
            }

            return result;
        }
    }
}
