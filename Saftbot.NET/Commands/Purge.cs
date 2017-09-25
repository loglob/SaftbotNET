using System;
using System.Collections.Generic;
using Discore;

namespace Saftbot.NET.Commands
{
    public class Purge : Command
    {
        public override void InitializeVariables()
        {
            Name = "Purge";
            Description = "Bulk deletes messages in this channel. Auto mode deletes a big monologue with a maximum of the given amount";
            PermsRequired = 2;
            Usage = new string[] { "<Amount of messages>", "[<auto>]" };
        }

        internal override string InternalRunCommand(CommandInformation cmdinfo)
        {
            if (Int32.TryParse(cmdinfo.Arguments[0], out int amount))
            {
                if (amount > 0)
                {
                    var selectedMessages = cmdinfo.Messaging.GetTextChannel.GetMessages(cmdinfo.Message.Id, amount).Result;

                    if ((cmdinfo.Arguments.Length >= 2) && (cmdinfo.Arguments[1].ToLower() == "auto"))
                    {
                        List<DiscordMessage> MonologueMessages = new List<DiscordMessage>();

                        bool nonMonologueReached = false;
                        Snowflake? userID = null;

                        foreach (var msg in selectedMessages)
                        {
                            if (!nonMonologueReached)
                            {
                                if (msg.Author.IsBot)
                                {
                                    MonologueMessages.Add(msg);
                                }
                                else
                                {
                                    if (userID.HasValue)
                                    {
                                        if (msg.Author.Id == userID.Value)
                                        {
                                            MonologueMessages.Add(msg);
                                        }
                                        else
                                            nonMonologueReached = true;
                                    }
                                    else
                                    {
                                        userID = (Snowflake?)msg.Author.Id;
                                        MonologueMessages.Add(msg);
                                    }
                                }
                            }
                        }

                        selectedMessages = MonologueMessages;
                    }

                    cmdinfo.Messaging.GetTextChannel.BulkDeleteMessages(selectedMessages);
                    cmdinfo.Message.Delete();

                    return $"Purged {selectedMessages.Count} Messages.";
                }
            }
            return InvalidValue("Amount");
        }
    }
}
