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
                return IsAdmin || IsDJ;
            }
        }
    }
}
