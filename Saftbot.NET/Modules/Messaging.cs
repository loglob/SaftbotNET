using System;
using Discore;

// Disable a warning about naming methods lowercase
// (I do this to show they are private members)
#pragma warning disable IDE1006

namespace Saftbot.NET.Modules
{
    public class Messaging
    {
        public const string NoPermsMessage = "You do not have the required permissions to run this command";

        private static void send(ITextChannel textChannel, string message)
        {
            if (message == "")
                return;

            try
            {
                //DiscordMessage sentmessage = await textChannel.SendMessage($"{message}");
                textChannel.CreateMessage(message).Wait();
                Program.log.Enter($"Succesfully send message: '{message}'");
            }
            catch (Exception e)
            {
                Program.log.Enter(e, $"trying to send message: '{message}'");
            }
        }

        public Messaging(ITextChannel textChannel)
        {
            this.textChannel = textChannel;
        }

        private ITextChannel textChannel;
        
        public void Send(string message)
        {
            send(textChannel, message);
        }

        public void NoPerms()
        {
            send(textChannel, NoPermsMessage);
        }
    }
}
