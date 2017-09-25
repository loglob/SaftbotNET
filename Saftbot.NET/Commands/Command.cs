using Discore;
using Discore.WebSocket;
using System.Threading.Tasks;
using System;

namespace Saftbot.NET.Commands
{
    public abstract class Command
    {
        /// <summary>
        /// The string used to call the command (i.e. !Ping)
        /// case insensitive
        /// </summary>
        public string Name;

        /// <summary>
        /// A short description of the command's function
        /// </summary>
        public string Description;
        
        public string[] Usage;

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
        
        public string NoValue(string parameterName)
        {
            return $"No value given for parameter '{parameterName}'";
        }
        
        public string NoValue(int parameterID)
        {
            if (parameterID < Usage.Length)
            {
                string parameterName = Usage[parameterID].Replace('<', ' ').Replace('>', ' ');
                parameterName = parameterName.Replace('[', ' ').Replace(']', ' ');
                parameterName = parameterName.Trim();

                return NoValue(parameterName);
            }
            else
                throw new Exception($"Tried to resolve invalid parameter index (command: {Name}, given index: {parameterID})");
        }

        public string InvalidValue(string parameterName)
        {
            return $"Invalid value given for parameter '{parameterName}'";
        }
        
        public abstract void InitializeVariables();
        

        /// <returns>
        /// Return bool is if the command got executed
        /// </returns>
        public virtual async Task<bool> RunCommand(CommandInformation cmdinfo)
        {
            // Calculate number of required parameters
            string[] required = Modules.Utility.FindNecessaryParameters(this);

            // Check if enough arguments are given
            if (cmdinfo.Arguments.Length < required.Length)
            {
                await cmdinfo.Messaging.Send(NoValue(required[cmdinfo.Arguments.Length]));
                return false;
            }

            var x = new Task<string>(() => InternalRunCommand(cmdinfo));
            x.Start();
            await cmdinfo.Messaging.Send(await x);
            return true;
        }

        /// <summary>
        /// If RunCommand() is not overridden, all arguments described in Usage with as necessary
        /// (<...> and not [<...>])
        /// </summary>
        /// <param name="cmdinfo"></param>
        /// <returns>The response for the user</returns>
        internal virtual string InternalRunCommand(CommandInformation cmdinfo)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A struct of all necessary information to execute commands
    /// </summary>
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
