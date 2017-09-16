using Discore;
using Discore.WebSocket;

namespace Saftbot.NET.Commands
{
    public abstract class Command
    {
        public string Name;
        public string Description;

        public string Usage;

        /// <summary>
        /// 0: No Perms needed
        /// 1: Playback Perms
        /// 2: Admin Perms
        /// 3: Developer Perms
        /// </summary>
        public int PermsRequired;

        public Command()
        {
            InitializeVariables();
        }

        public abstract void InitializeVariables();

        public abstract void RunCommand(CommandInformation cmdinfo);
    }

    public struct CommandInformation
    {
        public Modules.UserProfile Author;
        public Modules.GuildProfile Guild;
        public string[] Arguments;
        public DiscordMessage Message;
        public Shard Shard;
        public Modules.Messaging Messaging;
        

        public string MentionAuthor
        {
            get
            {
                return Author.Mention;
            }
        }
    }
}
