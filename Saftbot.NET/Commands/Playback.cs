using Discore.Voice;
using System;
using Saftbot.NET.Modules;
using Discore;

namespace Saftbot.NET.Commands
{
    class MusicPermTest : Command
    {
        public override void InitializeVariables()
        {
            Name = "MusicPermTest";
            Description = "Checks if you have playback permissions";
            PermsRequired = 0;
            Usage = "";
        }

        public override void RunCommand(CommandInformation cmdinfo)
        {
            cmdinfo.Messaging.Send(new UserProfile(cmdinfo.AuthorID, cmdinfo.GuildID).HasPlaybackPerms.ToString());
        }
    }
    
}
