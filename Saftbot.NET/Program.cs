﻿using System;
using Discore;
using Discore.WebSocket;
using System.Threading;
using System.IO;
using System.Reflection;
using Saftbot.NET.DBSystem;
using Saftbot.NET.Modules;
using Discore.Http;
using System.Threading.Tasks;

namespace Saftbot.NET
{
    //disable a warning about calling an async method without await
    #pragma warning disable CS4014

    class Program
    {
        /// <summary>
        /// The time the bot sleeps in between checking for shard shutdown or manual shutdown.
        /// Shouldn`t be set too low to avoid burning CPU cycles
        /// </summary>
        private const int sleepTime = 3000;

        /// <summary>
        /// The database instance used by the bot
        /// </summary>
        internal static Database database;
        
        /// <summary>
        /// The log that the bot writes status reports, error messages etc. to
        /// </summary>
        internal static Log log;
        
        /// <summary>
        /// All commands registered for the bot
        /// </summary>
        internal static Commands.Command[] AllCommands = new Commands.Command[]
        {
            new Commands.Ping(),        new Commands.Say(),         new Commands.EightBall(),       new Commands.Status(),
            new Commands.MyID(),        new Commands.Laughter(),    new Commands.MakeOwnerAdmin(),  new Commands.WhoIs(),
            new Commands.Repo(),        new Commands.Help(),        new Commands.Square(),          new Commands.Die(),
            new Commands.Search(),
            new Commands.Purge(),
            new Commands.Settings(),
            new Commands.Permissions(),
            new Commands.Crash()
        };

        internal static DiscordHttpClient httpClient;
        
        /// <summary>
        /// A version tag appended to the !status message.
        /// Doesn't serve any real purpose
        /// </summary>
        public const string saftbotVersionTag = "SaftBot v3.5 'Mostly async (again)-Edition'";
        
        #region initializing methods
        public static void Main(string[] args)
        {
            string currentwork = "initializing";
            
            try
            {
                log = new Log();

                currentwork = "initializing database";

                //Initialize new Database
                database = new Database(AssemblyPath + "db" + Path.DirectorySeparatorChar);

                Program program = new Program();
                program.Run(out currentwork);
            }
            catch(Commands.StopNowException)
            {
                log.Enter("The bot shut down normally");
            }
            catch (Exception e)
            {
                log.Enter(e, currentwork);
            }
        }

        public void Run(out string currentWork)
        {
            // Create authenticator using a bot user token.
            currentWork = "initializing (grabbing token)";
            string token = File.ReadAllText(AssemblyPath + "discord_token.txt");

            httpClient = new DiscordHttpClient(token);

            // Create and start a single shard.
            currentWork = "initializing (starting shard)";
            Shard shard = new Shard(token, 0, 1);

            currentWork = "initializing (subscribing methods)";
            // Subscribe to the message creation event.
            shard.Gateway.OnMessageCreated += Gateway_OnMessageCreated;
            // Subscribe to the guild creatino event.
            shard.Gateway.OnGuildCreated += Gateway_OnGuildCreated;


            currentWork = "initializing (Trying to connect)";
            shard.StartAsync().Wait();
            log.Enter("Bot connected!");

            // Wait for the shard to end before closing the program.
            currentWork = "running";
            shard.WaitUntilStoppedAsync().Wait();
        }
        #endregion

        /// <summary>
        /// Called whenever a message is send
        /// </summary>
        private static async void Gateway_OnMessageCreated(object sender, MessageEventArgs e)
        {
            DiscordMessage message = e.Message;

            if (message.Content.StartsWith("!"))
            {
                #region preparing cmdinfo
                // Ignore messages created by this bot or any other bots
                if (message.Author.IsBot)
                    return;
                
                Shard shard = e.Shard;

                // Grab the DM or guild text channel this message was posted in from cache.
                ITextChannel textChannel = (ITextChannel)shard.Cache.GetChannel(message.ChannelId);                

                // Ignore all commands not from servers
                if (textChannel.ChannelType != DiscordChannelType.Guild)
                    return;

                // Visually represent that the bot is working on the command
                await textChannel.TriggerTypingIndicator();
                
                // Split message into command and arguments
                string[] splitmsg = message.Content.Split(' ');
                string command = splitmsg[0].Substring(1).ToLower();
                string[] arguments = new string[splitmsg.Length - 1];
                Array.Copy(splitmsg, 1, arguments, 0, splitmsg.Length - 1);
                
                // Retrieve guild- and authorIDs
                ulong guildID = ((DiscordGuildTextChannel)shard.Cache.GetChannel(message.ChannelId)).GuildId.Id;
                ulong authorID = message.Author.Id.Id;

                // Get a userProfile for the author
                UserProfile authorProfile = new UserProfile(authorID, guildID);
                
                // Ignore messages made by ignored users
                if (authorProfile.IsIgnored)
                    return;
                
                // Build a CommandInformation struct used to call commands
                Commands.CommandInformation cmdinfo = new Commands.CommandInformation()
                {
                    Author = authorProfile,
                    Guild = new GuildProfile(guildID),
                    Arguments = arguments,
                    Message = message,
                    Shard = shard,
                    Messaging = new Messaging(textChannel)
                };
                #endregion

                // Find the command the user requested
                foreach (var cmd in AllCommands)
                {
                    if(command == cmd.Name.ToLower())
                    {
                        // Log the send command asynchronously to keep respond time low
                        log.EnterAsync($"{message.Author.Username} sent command: '{message.Content}'");

                        if (authorProfile.PermissionLevel >= cmd.PermsRequired)
                        {
                            try
                            {
                                await cmd.RunCommand(cmdinfo);
                            }
                            catch (Commands.StopNowException stopNow)
                            {
                                throw stopNow;
                            }
                            catch (Exception exception)
                            {
                                log.Enter(exception, $"processing command '{message.Content}'");
                                await cmdinfo.Messaging.Send("The SaftBot ran into a problem processing your command. If this has happend before, " +
                                                       "please make a bug report here: https://github.com/loglob/SaftbotNET/issues");
                            }
                        }
                        else
                        {
                            await cmdinfo.Messaging.NoPerms();
                        }
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
            // Make a new, empty serverSettings entry
            if (!database.DoesEntryExist(e.Guild.Id.Id))
            {
                database.RegisterEntry(database.DefaultEntry(e.Guild.Id.Id));
                database.SaveChanges();
            }

            // Register the owner as admin of the server
            new UserProfile(e.Guild.Id.Id, e.Guild.OwnerId.Id)
            {
                IsAdmin = true
            };
        }


        /// <summary>
        /// Gets the path to the directory of Saftbot.NET.dll
        /// (ends with direcotry seperator)
        /// </summary>
        public static string AssemblyPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar;
            }
        }
    }
}