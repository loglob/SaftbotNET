using Saftbot.NET.DBSystem;

namespace Saftbot.NET.Modules
{
    public struct GuildProfile
    {
        private ulong guildID;

        public ulong GuildID
        {
            get
            {
                return guildID;
            }
        }
        
        public GuildProfile(ulong guildID)
        {
            this.guildID = guildID;
        }

        public DataEntry Entry
        {
            get
            {
                return Program.database.FetchEntry(guildID);
            }
        }

        public bool PlebsCanDJ
        {
            get
            {
                return Entry.FetchSetting(ServerSettings.plebsCanDJ);
            }
            set
            {
                Entry.EditSetting(ServerSettings.plebsCanDJ, true);
            }
        }

        public bool UseGoogle
        {
            get
            {
                return Entry.FetchSetting(ServerSettings.useGoogle);
            }
            set
            {
                Entry.EditSetting(ServerSettings.useGoogle, true);
            }
        }

        public bool CoolReference
        {
            get
            {
                return Entry.FetchSetting(ServerSettings.coolReference);
            }
            set
            {
                Entry.EditSetting(ServerSettings.coolReference, true);
            }
        }
    }
}
