using Saftbot.NET.DBSystem;

namespace Saftbot.NET.Modules
{
    public struct UserProfile
    {
        public UserProfile(ulong userID, ulong guildID)
        {
            this.userID = userID;
            this.guildID = guildID;
        }

        private ulong userID;
        private ulong guildID;

        public ulong UserID
        {
            get
            {
                return userID;
            }
        }
        public ulong GuildID
        {
            get
            {
                return guildID;
            }
        }

        public int PermissionLevel
        {
            get
            {
                if (IsDev)
                    return 3;
                if (IsAdmin)
                    return 2;
                if (HasPlaybackPerms)
                    return 1;

                return 0;
            }
        }

        public void SetSetting(UserSettings setting, bool newValue)
        {
            Program.database.FetchEntry(guildID).EditUserSetting(userID, setting, newValue);
        }

        public bool GetSetting(UserSettings setting)
        {
            return Program.database.FetchEntry(guildID).FetchUserSetting(userID, setting);
        }

        public bool IsIgnored
        {
            get
            {
                return GetSetting(UserSettings.isIgnored);
            }
            set
            {
                SetSetting(UserSettings.isIgnored, value);
            }
        }

        public bool IsAdmin
        {
            get
            {
                return GetSetting(UserSettings.isAdmin);
            }
            set
            {
                SetSetting(UserSettings.isAdmin, value);
            }
        }

        public bool IsDJ
        {
            get
            {
                return GetSetting(UserSettings.isDJ);
            }
            set
            {
                SetSetting(UserSettings.isDJ, value);
            }
        }

        public bool IsDev
        {
            get
            {
                return (userID == 66261079918915584 || userID == 291958246179078144);
            }
        }

        public bool HasPlaybackPerms
        {
            get
            {
                return (PermissionLevel >= 1) || Program.database.FetchEntry(guildID).FetchSetting(ServerSettings.plebsCanDJ);
            }
        }

        public string Mention
        {
            get
            {
                return Utility.Mention(userID);
            }
        }
    }
}
