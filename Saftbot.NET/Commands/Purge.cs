using System;

namespace Saftbot.NET.Commands
{
    public class Purge : Command
    {
        public override void InitializeVariables()
        {
            Name = "Purge";
            Description = "Bulk deletes messages in this channel";
            PermsRequired = 2;
            Usage = "<Amount of messages>";
        }

        public override void RunCommand(CommandInformation cmdinfo)
        {
            if (cmdinfo.Arguments.Length >= 1)
            {
                if (Int32.TryParse(cmdinfo.Arguments[0], out int amount))
                {
                    if (amount > 0)
                    {
                        var messagesToDelete = cmdinfo.Messaging.GetTextChannel.GetMessages(cmdinfo.Message.Id, amount);
                        cmdinfo.Messaging.GetTextChannel.BulkDeleteMessages(messagesToDelete.Result);
                        cmdinfo.Message.Delete();
                        cmdinfo.Messaging.Send($"Purged {cmdinfo} Messages.");
                        return;
                    }
                }

                cmdinfo.Messaging.Send(InvalidValue("Amount"));
            }
            else
                cmdinfo.Messaging.Send(NoValue("Amount"));
        }
    }
}
