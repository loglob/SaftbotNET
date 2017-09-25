using System;
using Discore;
using System.Threading.Tasks;

// Disable a warning about naming methods lowercase
// (I do this to show they are private members)
#pragma warning disable IDE1006

namespace Saftbot.NET.Modules
{
    public class Messaging
    {
        public const string NoPermsMessage = "You do not have the required permissions to run this command";

        private static async Task<bool> send(ITextChannel textChannel, string message)
        {
            if (message == "")
                return false;

            try
            {
                await textChannel.CreateMessage(message);
                Program.log.Enter($"Succesfully send message: '{message}'");
                return true;
            }
            catch (Exception e)
            {
                Program.log.Enter(e, $"trying to send message: '{message}'");
                return false;
            }
        }

        public Messaging(ITextChannel textChannel)
        {
            this.textChannel = textChannel;
        }

        private ITextChannel textChannel;
        
        public ITextChannel GetTextChannel
        {
            get
            {
                return textChannel;
            }
        }

        public async Task<bool> Send(string message)
        {
            return await send(textChannel, message);
        }

        public async Task<bool> NoPerms()
        {
            return await send(textChannel, NoPermsMessage);
        }
    }
}
