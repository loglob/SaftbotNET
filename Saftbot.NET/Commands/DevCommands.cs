using System;
using System.Threading;
using System.Threading.Tasks;

namespace Saftbot.NET.Commands
{
    /// <summary>
    /// An exception that tells the bot to shut down immidiatly. If cought anywhere, it should be thrown again. Should only be resolved in Main()
    /// </summary>
    public class StopNowException : Exception
    { }

    public class Crash : Command
    {
        public override void InitializeVariables()
        {
            Name = "Crash";
            Description = "Shuts the bot down";
            PermsRequired = 3;
            Usage = new string[0];
        }

        public override async Task<bool> RunCommand(CommandInformation cmdinfo)
        {
            Program.log.Enter($"{cmdinfo.Message.Author.Username} ({cmdinfo.Author.UserID}) has shut the bot down.");

            if (cmdinfo.Guild.CoolReference)
            {
                await cmdinfo.Messaging.Send("L");
                Thread.Sleep(1500);
                await cmdinfo.Messaging.Send("O");
            }
            else
                await cmdinfo.Messaging.Send("Shutting down...");

            throw new StopNowException();
        }
    }
}
