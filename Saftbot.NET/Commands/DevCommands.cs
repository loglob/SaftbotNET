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

        public override string RunCommand(CommandInformation cmdinfo)
        {
            Program.log.Enter($"{cmdinfo.Message.Author.Username} ({cmdinfo.Message.Author.Id.Id}) has shut the bot down.");
            cmdinfo.Shard.Application.ShardManager.StopShardsAsync(CancellationToken.None);

            if (Program.database.FetchEntry(cmdinfo.GuildID).FetchSetting(DBSystem.ServerSettings.coolReference))
                return "L\nO";
            else
                return "Shutting down...";
        }
    }
}
