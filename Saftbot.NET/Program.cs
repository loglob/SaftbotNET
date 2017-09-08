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

        /// <summary>
        /// A version tag appended to the !status message.
        /// Doesn't serve any real purpose
        /// </summary>
        public const string saftbotVersionTag = "SaftBot™ Alpha v2.1.1 'It's modular™!'-Edition";
        
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
        
        #region generic userperms methods
        /// <summary>
        /// Perform a generic attempt to make a given user a given perm on a given server
        /// </summary>
        /// <param name="userID">The userID of the user</param>
        /// <param name="guildID">The guildID of the server</param>
        /// <param name="perm">The setting to edit</param>
        /// <param name="newStatus">New status of that setting</param>
        /// <param name="permdescription">A description of the permission</param>
        /// <returns>A message to respond to user</returns>
        private static string MakeUser(ulong userID, ulong guildID, UserSettings perm, bool newStatus, string permdescription)
        {
            bool currentSetting = database.FetchEntry(guildID).FetchUserSetting(userID, perm);
            database.FetchEntry(guildID).EditUserSetting(userID, perm, newStatus);

            if (newStatus)
            {
                if (currentSetting)
                    return $"{Utility.Mention(userID)} is already {permdescription}!";
                else
                    return $"{Utility.Mention(userID)} is now {permdescription}!";
            }
            else
            {
                if (currentSetting)
                    return $"{Utility.Mention(userID)} is no longer a {permdescription}";
                else
                    return $"{Utility.Mention(userID)} isn't a {permdescription}";
            }
        }
        
        /// <summary>
        /// Attempt to give a list of users a given permission
        /// </summary>
        /// <param name="allUsers">All users to be given permission</param>
        /// <param name="guildID">The guild this is doen in</param>
        /// <param name="perm">the permission in question</param>
        /// <param name="newStatus">The new status of the setting</param>
        /// <param name="permdescription">A description of the permission</param>
        /// <param name="respondChannel">A channel into which responds are given</param>
        private static void MakeUsers(IEnumerable<DiscordUser> allUsers, ulong guildID, UserSettings perm, bool newStatus, 
                                      string permdescription, ITextChannel respondChannel)
        {
            foreach (DiscordUser user in allUsers)
            {
                string response = MakeUser(user.Id.Id, guildID, perm, newStatus, permdescription);
                sendMessage(respondChannel, response);
            }
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
                textChannel.TriggerTypingIndicator();

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

                switch (command)
                {
                    #region No required Perms
                    //Test if the bot is online and how fast it responds
                    case ("ping"):
                        TimeSpan timeSincePost = DateTime.Now.Subtract(message.Timestamp);
                        sendMessage(textChannel, $"{Utility.Mention(message.Author)} Pong! Took {timeSincePost.TotalMilliseconds} ms");
                    break;

                    //Deletes and resends a given message
                    case ("say"):
                        message.Delete();
                        sendMessage(textChannel, $"{String.Join(" ",arguments)}");
                    break;

                    //A 8ball roll adjusted to be fair
                    case ("8ball"):
                        string[] answers = {"It is certain", "It is decidedly so", "Without a doubt", "Yes, definitely",
                                            "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", "Yes",
                                            "Signs point to yes", "Reply hazy try again", "Ask again later", "Better not tell you now",
                                            "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no",
                                            "My sources say no", "Outlook not so good", "Very doubtful", "Nah m8", "That's retarded",
                                            "Are you stupid?", "Literally kill yourself", "Negative"};
                        Random random = new Random();
                        string answer = answers[(random.Next(00, answers.Length))];
                        
                        sendMessage(textChannel, answer);
                        break;

                    //Information about the system the bot is hosted on
                    //ALso gives saftbot-versiontag info
                    case ("status"):
                        sendMessage(textChannel, Utility.SystemSummary());
                    break;

                    //Tells the user their discord-userID
                    case ("myid"):
                        sendMessage(textChannel, $"{Utility.Mention(message.Author)}, your ID is {message.Author.Id.ToString()}");
                    break;

                    //Lists all users with admin permissions on this server
                    case ("listadmins"):
                        List<string> adminMentions = new List<string>();
                        
                        foreach (var userEntry in database.FetchEntry(guildID).FetchParsedUsersettings())
                        {
                            if(userEntry.Value[(int)UserSettings.isAdmin])
                                adminMentions.Add($"{Utility.Mention(userEntry.Key)}");
                            
                        }

                        if(adminMentions.Count == 0)
                            sendMessage(textChannel, "There are no Admins on this server.");
                        else
                            sendMessage(textChannel, $"Admins on this server are:\n{Utility.SpecialAndJoin(adminMentions)}");
                        
                    break;
                        
                    //Make the bot laugh
                    case ("laughter"):
                        string vocals = "aeiou";
                        string laughter = "";
                        Random rand = new Random();

                        for (int i = 0; i < rand.Next(5,10); i++)
                        {
                            laughter += "h";
                            laughter += vocals[rand.Next(0, 4)];
                        }

                        sendMessage(textChannel, laughter);
                    break;
                        
                    //Quickly generate a link to search different websites
                    case ("search"):
                        sendMessage(textChannel, Search.DoSearchCommand(arguments, guildID));
                    break;

                    // Make the owner of this server a bot admin.
                    // Should be done automatically when the bot joins a server
                    case ("makeowneradmin"):
                        //grab guilds owner from cache
                        ulong ownerid = (shard.Cache.Guilds.Get(new Discore.Snowflake(guildID))).Value.OwnerId.Id;

                        MakeUser(ownerid, guildID, UserSettings.isAdmin, true, "an admin");
                    break;

                    // Report the permissions of all mentioned users
                    case ("whois"):
                        foreach(DiscordUser mentionedUser in message.Mentions)
                        {
                            List<string> descriptions = new List<string>();
                            UserProfile mentionedUserProfile = new UserProfile(mentionedUser.Id.Id, guildID);

                            if (mentionedUserProfile.IsAdmin)
                                descriptions.Add("an admin");
                            if (mentionedUserProfile.IsIgnored)
                                descriptions.Add("ignored");
                            if (mentionedUserProfile.IsDJ)
                                descriptions.Add("a DJ");
                            if (mentionedUserProfile.IsDev)
                                descriptions.Add("a dev");

                            if (descriptions.Count > 0)
                                sendMessage(textChannel, $"{Utility.Mention(mentionedUser)} is {Utility.SpecialAndJoin(descriptions)}");
                            else
                                sendMessage(textChannel, $"{Utility.Mention(mentionedUser)} is a nobody");
                        }
                    break;

                    case ("source"):
                    case ("repo"):
                        sendMessage(textChannel, "My source can be found at https://github.com/LordGruem/SaftbotNET");
                    break;
                    #endregion

                    #region Playback commands, may require permissions
                    // Check wether or not the sender has access to playback commands
                    case ("musicpermtest"):
                        sendMessage(textChannel, (authorProfile.HasPlaybackPerms ? "You can DJ!" : "You can't DJ!"));
                    break;

                    case ("play"):
                        if (authorProfile.HasPlaybackPerms)
                        {
                            sendMessage(textChannel, "This command is not implemented yet, sorry :c");
                        }
                        else
                            noPermsMessage(textChannel);
                        break;
                    #endregion

                    #region Requires Admin Perms

                    case ("makedj"):
                        if (authorProfile.IsAdmin)
                        {
                            MakeUsers(message.Mentions, guildID, UserSettings.isDJ, true, "a DJ", textChannel);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;

                    case ("undj"):
                        if (authorProfile.IsAdmin)
                        {
                            MakeUsers(message.Mentions, guildID, UserSettings.isDJ, false, "a DJ", textChannel);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;

                    //ignore a user (they can no longer execute commands)
                    case ("ignore"):
                        if (authorProfile.IsAdmin)
                        {
                            MakeUsers(message.Mentions, guildID, UserSettings.isIgnored, true, "ignored", textChannel);
                        }
                        else
                            noPermsMessage(textChannel);
                        break;

                    //reacknowledge a user 
                    case ("unignore"):
                        if (authorProfile.IsAdmin)
                        {
                            MakeUsers(message.Mentions, guildID, UserSettings.isDJ, false, "ignored", textChannel);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;

                    // give on or more users admin permissions
                    case ("addadmin"):
                        if (authorProfile.IsAdmin)
                        {
                            MakeUsers(message.Mentions, guildID, UserSettings.isAdmin, true, "an admin", textChannel);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;

                    // take admin permissions from one or more users
                    case ("removeadmin"):
                        if (authorProfile.IsAdmin)
                        {
                            MakeUsers(message.Mentions, guildID, UserSettings.isAdmin, false, "an admin", textChannel);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;
                        
                    // Bulk delete messages
                    case ("purge"):
                        if (authorProfile.IsAdmin)
                        {
                            int deleteMessageCount;
                            if (Int32.TryParse(arguments[0], out deleteMessageCount))
                            {
                                if (deleteMessageCount < 0)
                                {   //No acceptable number was given
                                    sendMessage(textChannel, "The given number was invalid");
                                }
                                else
                                {
                                    Snowflake baseID = await textChannel.GetLastMessageId();
                                    System.Collections.Generic.IReadOnlyList<DiscordMessage> messagesToDelete = await textChannel.GetMessages(baseID, deleteMessageCount);
                                    await textChannel.BulkDeleteMessages(messagesToDelete);
                                    message.Delete();
                                    sendMessage(textChannel, $"{deleteMessageCount} Messages have been deleted!");
                                }
                            }
                            else
                            {   //Couldn't Parse the given string
                                sendMessage(textChannel, "The given argument wasn't an acceptable number");
                            }
                        }
                        else
                            noPermsMessage(textChannel);
                    break;

                    // Edit, view or list this servers settings
                    case ("settings"):
                        if (authorProfile.IsAdmin)
                            sendMessage(textChannel, Setting.doSettingsCommand(arguments, guildID));
                        else
                            noPermsMessage(textChannel);
                    break;

                    #endregion

                    #region Requires Developer Perms

                    //Shut the bot down and make a niche reference
                    case ("crash"):
                        if (authorProfile.IsDev)
                        {
                            log.Enter($"{message.Author.Username} ({message.Author.Id.Id}) has shut the bot down.");
                            /*
                            sendMessage(textChannel, "L");
                            Thread.Sleep(1000);
                            sendMessage(textChannel, "O");
                            Thread.Sleep(10);*/
                            shard.Application.ShardManager.StopShardsAsync(CancellationToken.None);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;
                    #endregion
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