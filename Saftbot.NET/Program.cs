using System;
using Discore;
using System.Threading.Tasks;
using Discore.WebSocket;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Saftbot.NET.DBSystem;
using Saftbot.NET.Modules;

namespace Saftbot.NET
{
    //disable a warning about calling an async method without await
    #pragma warning disable CS4014

    class Program
    {
        /// <summary>
        /// The database instance used by the bot
        /// </summary>
        internal static Database database;
        
        /// <summary>
        /// The Log this bot writes entires into
        /// </summary>
        internal static Log log;

        internal static Commands.Command[] AllCommands = new Commands.Command[]
        {
            new Commands.Ping(),        new Commands.Say(),         new Commands.EightBall(),       new Commands.Status(),
            new Commands.MyID(),        new Commands.Laughter(),    new Commands.MakeOwnerAdmin(),  new Commands.WhoIs(),
            new Commands.Repo(),        new Commands.Help(),
            new Commands.Search(),
            new Commands.Settings(),
            new Commands.Permissions(),
            new Commands.Crash()
        };

        /// <summary>
        /// A version tag appended to the !status message.
        /// Doesn't serve any real purpose
        /// </summary>
        public const string saftbotVersionTag = "SaftBot™ v2.1.5 'Rewriting things is fun'-Edition";
        
        #region initializing methods
        public static void Main(string[] args)
        {
            Program program = new Program();
           
            //Get absolute path to the bot (the directory the Saftbot.NET.dll file is in)
            string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            //Initialize new Database
            database = new Database(assemblyPath + Path.DirectorySeparatorChar + "db" + Path.DirectorySeparatorChar);
            //Initialize new Log
            log = new Log();

            try
            {
                program.Run().Wait();
            }
            catch(Exception e)
            {
                log.Enter(e);
            }
        }

        public async Task Run()
        {
            // Create authenticator using a bot user token.
            DiscordBotUserToken token = new DiscordBotUserToken(System.IO.File.ReadAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/discord_token.txt"));

            //Create a new log file for the current session.
            
            // Create a WebSocket application.
            DiscordWebSocketApplication app = new DiscordWebSocketApplication(token);

            // Create and start a single shard.
            Shard shard = app.ShardManager.CreateSingleShard();
            await shard.StartAsync(CancellationToken.None);

            log.Enter("Bot connected!");

            // Subscribe to the message creation event.
            shard.Gateway.OnMessageCreated += Gateway_OnMessageCreated;
            shard.Gateway.OnGuildCreated += Gateway_OnGuildCreated;

            // Wait for the shard to end before closing the program.
            while (shard.IsRunning)
                await Task.Delay(1000);
        }
        #endregion

        #region message sending
        private static async void sendMessage(ITextChannel textChannel, string message)
        {
            try
            {
                DiscordMessage sentmessage = await textChannel.SendMessage($"{message}");
                log.Enter($"Succesfully send message: '{message}'");
            }
            catch (Exception e)
            {
                log.Enter(e, $"trying to send message: '{message}'");
            }
        }

        //Generic no permisisons message
        private static void noPermsMessage(ITextChannel textChannel)
        {
            sendMessage(textChannel, "You do not have the required permissions to run this command");
        }
        #endregion
        
        
        
        /// <summary>
        /// Called whenever a message is send
        /// </summary>
        private static async void Gateway_OnMessageCreated(object sender, MessageEventArgs e)
        {
            Shard shard = e.Shard;
            DiscordMessage message = e.Message;

            // Ignore messages created by this bot or any other bots
            if (message.Author.IsBot)
                return;

            if (message.Content.StartsWith("!"))
            {
                //Log the send command asynchronously to keep respond time low
                Thread loggingThread = new Thread(() => log.Enter($"{message.Author.Username} sent command: {message.Content}"));
                loggingThread.Start();

                // Grab the DM or guild text channel this message was posted in from cache.
                ITextChannel textChannel = (ITextChannel)shard.Cache.Channels.Get(message.ChannelId);

                // Ignore all commands not from servers
                if (textChannel.ChannelType != DiscordChannelType.Guild)
                    return;

                //Visually represent that the bot is working on the command
                // (waits for execution because the bot may respond too fast causing the typing indicator to stay active
                await textChannel.TriggerTypingIndicator();

                #region Split message into command and arguments
                    string[] splitmsg = message.Content.Split(' ');
                    string command = splitmsg[0].Substring(1).ToLower();
                    string[] arguments = new string[splitmsg.Length - 1];
                    Array.Copy(splitmsg, 1, arguments, 0, splitmsg.Length - 1);
                #endregion

                // Retrieve guild- and authorIDs
                ulong guildID = ((DiscordGuildTextChannel)shard.Cache.Channels.Get(message.ChannelId)).GuildId.Id;
                ulong authorID = message.Author.Id.Id;

                // Get a userProfile for the author
                UserProfile authorProfile = new UserProfile(authorID, guildID);
                
                // Ignore messages made by ignored users
                if (authorProfile.IsIgnored)
                    return;

                var cmdinfo = new Commands.CommandInformation()
                {
                    AuthorID = authorID,
                    GuildID = guildID,
                    Arguments = arguments,
                    Message = message,
                    Shard = shard
                };

                foreach (var cmd in AllCommands)
                {
                    if(command == cmd.Name.ToLower())
                    {
                        if(authorProfile.PermissionLevel >= cmd.PermsRequired)
                            sendMessage(textChannel, cmd.RunCommand(cmdinfo));
                        else
                            noPermsMessage(textChannel);
                    }
                }
                
            }
        }

        /// <summary>
        /// Called when a new server is added
        /// Adds a new DB entry and adds the owner as admin
        /// </summary>
        private static void Gateway_OnGuildCreated(object sender, GuildEventArgs e)
        {
            //Make a new, empty serverSettings entry
            if (!database.DoesEntryExist(e.Guild.Id.Id))
            {
                database.RegisterEntry(database.DefaultEntry(e.Guild.Id.Id));
                database.SaveChanges();
            }

            //register the owner as admin of the server
            UserProfile owner = new UserProfile(e.Guild.Id.Id, e.Guild.OwnerId.Id);
            owner.IsAdmin = true;
        }
    }
}