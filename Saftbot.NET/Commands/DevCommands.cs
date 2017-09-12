using System.Threading;

namespace Saftbot.NET.Commands
{
    public class Crash : Command
    {
        public override void InitializeVariables()
        {
            Name = "Crash";
            Description = "Shuts the bot down";
            PermsRequired = 3;
            Usage = "";
        }

        public override void RunCommand(CommandInformation cmdinfo)
        {
            Program.log.Enter($"{cmdinfo.Message.Author.Username} ({cmdinfo.Message.Author.Id.Id}) has shut the bot down.");

            if (Program.database.FetchEntry(cmdinfo.GuildID).FetchSetting(DBSystem.ServerSettings.coolReference))
            {
                cmdinfo.Messaging.Send("L");
                Thread.Sleep(1500);
                cmdinfo.Messaging.Send("O");
            }
            else
                cmdinfo.Messaging.Send("Shutting down...");

            cmdinfo.Shard.StopAsync().Wait();
        }
    }
}
