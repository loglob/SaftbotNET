using Saftbot.NET.DBSystem;

namespace Saftbot.NET.Modules
{
    public struct UserProfile
    {
        public UserProfile(ulong userID, ulong guildID)
        {
            UserID = userID;
            GuildID = guildID;
        }

        private ulong UserID;
        private ulong GuildID;

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
                else
                    return 0;
            }
        }

        public void Set(UserSettings setting, bool newValue)
        {
            Program.database.FetchEntry(GuildID).EditUserSetting(UserID, setting, newValue);
        }

        public bool Is(UserSettings setting)
        {
            return Program.database.FetchEntry(GuildID).FetchUserSetting(UserID, setting);
        }

        public bool IsIgnored
        {
            get
            {
                return Program.database.FetchEntry(GuildID).FetchUserSetting(UserID, UserSettings.isIgnored);
            }
            set
            {
                Program.database.FetchEntry(GuildID).EditUserSetting(UserID, UserSettings.isIgnored, value);
            }
        }

        public bool IsAdmin
        {
            get
            {
                return Program.database.FetchEntry(GuildID).FetchUserSetting(UserID, UserSettings.isAdmin);
            }
            set
            {
                Program.database.FetchEntry(GuildID).EditUserSetting(UserID, UserSettings.isAdmin, value);
            }
        }

        public bool IsDJ
        {
            get
            {
                return Program.database.FetchEntry(GuildID).FetchUserSetting(UserID, UserSettings.isDJ);
            }
            set
            {
                Program.database.FetchEntry(GuildID).EditUserSetting(UserID, UserSettings.isDJ, value);
            }
        }

        public bool IsDev
        {
            get
            {
                return (UserID == 66261079918915584 || UserID == 291958246179078144);
            }
        }

        public bool HasPlaybackPerms
        {
            get
            {
                return IsAdmin || IsDJ || Program.database.FetchEntry(GuildID).FetchSetting(ServerSettings.plebsCanDJ);
            }
        }

        public string GetMention()
        {
            return Utility.Mention(UserID);
        }
    }
}
