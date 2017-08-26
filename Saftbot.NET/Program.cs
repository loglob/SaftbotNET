using System;
using Discore;
using System.Threading.Tasks;
using Discore.WebSocket;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Saftbot.NET
{
    //disable a warning about calling an async method without await
    #pragma warning disable CS4014

    class Program
    {
        /// <summary>
        /// The database instance used by this bot instance
        /// </summary>
        private static Database database;

        /// <summary>
        /// The path into which log files are written
        /// </summary>
        private static string logFilePath;

        /// <summary>
        /// A version tag appended to the !status message.
        /// Doesn't serve any real purpose
        /// </summary>
        public const string saftbotVersionTag = "SaftBot™ Experimental v2.0.2 Cutting Edge NonDB-Edition";
        
        #region loggingMethods
        private static void startLog()
        {
            //Get absolute path to the bot (the directory the Saftbot.NET.dll file is in)
            string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            
            //check if / or \ is regquired to build path
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string logPath;

            if (isWindows)
                logPath = @"\logs\";
            else
                logPath = @"/logs/";

            //create a logs folder
            Directory.CreateDirectory(assemblyPath + logPath);

            //figure out relative path for new file
            DateTime startTime = DateTime.Now;
            
            //Build Path ti store current logfile in
            logPath += startTime.Year.ToString() + "-" + startTime.Month.ToString() + "-" +
                                        startTime.Day.ToString() + "_" + startTime.Hour.ToString() + "-" + 
                                        startTime.Minute.ToString()+ "-" + startTime.Second.ToString() + ".txt";

            logFilePath = assemblyPath + logPath;
            
            //create empty log file
            FileStream FS = File.Create(logFilePath);
            FS.Flush();
            FS.Dispose();
        }

        //Please use this method instead of Console.WriteLine for making console entries
        private static void log(string entry)
        {
            StreamWriter SW = File.AppendText(logFilePath);
            SW.WriteLine(entry);
            SW.Flush();
            SW.Dispose();

            Console.WriteLine(entry);
        }
        
        private static void logError(Exception e, string source)
        {
            log($"Encountered {e.GetType().ToString()} while {source}");
            log($"{e.Message} \n at: {e.Source} \n data: {e.Data} \n help at: {e.HelpLink}");
        }
        #endregion

        #region helper methods
        private static string systemSummary()
        {
            int CoreCount = Environment.ProcessorCount;
            string timezone = TimeZoneInfo.Local.DisplayName;
            string UTCoffset = TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString();
            string machineName = Environment.MachineName;
            string OSarch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
            string OSdesc = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            string cpuarch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
            string fwInfo = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            string uptime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).ToString();

            return ($"I am running on {machineName}, specs:\nOS:{OSdesc}({OSarch})\nCPU:{CoreCount} " +
                $"Core {cpuarch} CPU\nTimezone: {timezone}(UTC+{UTCoffset})\nFramework: {fwInfo}\nUptime: {uptime}\n" +
                $"Saftbot-Versiontag™: {saftbotVersionTag}");
        }
        
        private static string mention(DiscordUser user) { return mention(user.Id.Id); }
        private static string mention(Snowflake userID) { return mention(userID.Id); }
        /// <summary>
        /// Generate the string that will show up as a mention (like @username) after being send
        /// </summary>
        private static string mention(ulong userID) {   return $"<@{userID}>";  }

        private static string DescribeAllSettings()
        {
            return "plebscandj: Determines if users without admin permissions can use playback commands,\n" +
                   "usegoogle: Determines if !search uses the evil botnet google";
        }

        private static ServerSettings? parseToSetting(string settingName)
        {
            switch(settingName.ToLower())
            {
                case "plebscandj":
                    return ServerSettings.plebsCanDJ;

                case "usegoogle":
                    return ServerSettings.useGoogle;

                default:
                    byte settingNr;
                    if (Byte.TryParse(settingName, out settingNr))
                        if (settingNr < 16)
                            return (ServerSettings)settingNr;
                return null;
            }
        }

        /// <summary>
        /// when using !search, this method determines the search prefix used
        /// </summary>
        /// <param name="shorthand">arguments[0] minus the - at begining</param>
        /// <returns></returns>
        private static string searchPrefixByShorthand(string shorthand, ulong guildID)
        {
            shorthand = shorthand.ToLower();
            
            //search 4chan/g/ and possibly a given board
            if(shorthand.StartsWith("4c"))
            {
                if (shorthand.StartsWith("4c_") && shorthand.Length > 3)
                    return $"https://boards.4chan.org/{ shorthand.Substring(3) }/";
                else
                    return "https://boards.4chan.org/g/";
            }

            //search reddit and possibly a given subreddit
            if(shorthand.StartsWith("r"))
            {
                if(shorthand.StartsWith("r_") && shorthand.Length > 2)
                    return $"https://www.reddit.com/r/{ shorthand.Substring(2) }/search?restrict_sr=on&q=";
                else
                    return "https://www.reddit.com/search?q=";
            }

            //search gomovies under possibly given domain
            if(shorthand.StartsWith("go"))
            {
                if (shorthand.StartsWith("go_") && shorthand.Length > 3)
                    return $"https://gomovies.{ shorthand.Substring(3) }/search-query/";
                else
                    return "https://gomovies.sc/search-query/";
            }

            switch(shorthand)
            {
                case "g":
                    return "https://google.com/search?q=";

                case "ddg":
                    return "https://duckduckgo.com/?q=";

                case "tpb":
                    return "https://thepiratebay.org/search/";

                case "yt":
                    return "https://www.youtube.com/results?search_query=";

                case "st":
                    return "http://store.steampowered.com/search/?term=";

                case "tw":
                    return "https://twitter.com/search?q=";

                case "nya":
                    return "https://nyaa.si/?q=";
                    
                default:
                    if (database.FetchEntry(guildID).FetchSetting(ServerSettings.useGoogle))
                        return "https://google.com/search?q=";
                    else
                        return "https://duckduckgo.com/?q=";
            }
        }

        private static string searchServiceShorthands()
        {
            return "\nG: Google             \nDDG: Duckduckgo           \nTPB: The Piratebay            " +
                   "\nYT: Youtube           \n4C: 4chan                 \n4C_{x}: The /{x}/ board       " +
                   "\nST: Steam Store       \nTW: Twitter               \nNYA: nyaa.si anime tracker    " +
                   "\nR: Reddit             \nR_{x}: {x}-subreddit      \nGO: GoMovies streaming site   " +
                   "\nGO_{x} GOMovies.{x}   ";
        }
        #endregion

        #region initializing methods
        public static void Main(string[] args)
        {
            Program program = new Program();

            //Get absolute path to the bot (the directory the Saftbot.NET.dll file is in)
            string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            //check if / or \ is regquired to build path
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string databaseFolder;

            if (isWindows)
                databaseFolder = @"\db\";
            else
                databaseFolder = @"/db/";

            database = new Database(assemblyPath + databaseFolder);
            program.Run().Wait();
        }

        public async Task Run()
        {
            // Create authenticator using a bot user token.
            DiscordBotUserToken token = new DiscordBotUserToken(System.IO.File.ReadAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/discord_token.txt"));

            //Create a new log file for the current session.
            startLog();
            log($"Created Log file at: {logFilePath}");
            
            // Create a WebSocket application.
            DiscordWebSocketApplication app = new DiscordWebSocketApplication(token);

            // Create and start a single shard.
            Shard shard = app.ShardManager.CreateSingleShard();
            await shard.StartAsync(CancellationToken.None);

            log("Bot connected!");

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
                log($"Succesfully send message: {message}");
            }
            catch (Exception e)
            {
                logError(e, $"trying to send message: {message}");
            }
        }

        //Generic no permisisons message
        private static void noPermsMessage(ITextChannel textChannel)
        {
            sendMessage(textChannel, "You do not have the required permissions to run this command");
        }
        #endregion

        private static void Gateway_OnGuildCreated(object sender, GuildEventArgs e)
        {
            //Make a new, empty serverSettings entry
            if (!database.DoesEntryExist(e.Guild.Id.Id))
            {
                database.RegisterEntry(database.DefaultEntry(e.Guild.Id.Id));
                database.SaveChanges();
            }

            //register the owner as admin of the server
            addAdmin(e.Guild.Id.Id, e.Guild.OwnerId.Id);
        }
        
        #region Admin Methods
        private static bool isAdmin(ulong guildid, ulong userid)
        {
            DataEntry entry = database.FetchEntry(guildid);
            bool[] userSettings = entry.FetchUserSettings(userid);
            return userSettings[(int)UserSettings.isAdmin];
        }

        private static void addAdmin(ulong guildid, ulong userid)
        {
            if (!isAdmin(guildid, userid))
            {
                DataEntry entry = database.FetchEntry(guildid);
                entry.EditUserSetting(userid, UserSettings.isAdmin, true);
                entry.SaveChanges();
            }
        }

        private static void removeAdmin(ulong guildid, ulong userid)
        {
            if (isAdmin(guildid, userid))
            {
                DataEntry entry = database.FetchEntry(guildid);
                entry.EditUserSetting(userid, UserSettings.isAdmin, false);
                entry.SaveChanges();
            }
        }
        #endregion

        /// <summary>
        /// Check the supplied userid against the hardcoded developer IDs
        /// Allows access to commands that influence the bot as a whole like !crash
        /// </summary>
        private static bool isDeveloper(ulong userid)
        {
            return (userid == 66261079918915584 || userid == 291958246179078144);
        }
     
        /// <summary>
        /// Determines if a given user can control playback commands under the current servers settings
        /// </summary>
        private static bool UserCanDJ(ulong userId, ulong guildId)
        {
            DataEntry GuildEntry = database.FetchEntry(guildId);
            return (GuildEntry.FetchSetting(ServerSettings.plebsCanDJ) || GuildEntry.FetchUserSetting(userId, UserSettings.isDJ));
        }
        
        /// <summary>
        /// Performs all actions required to edit / read settings
        /// </summary>
        /// <param name="arguments">The arugments supplied to the settings command</param>
        /// <returns>the response to the user</returns>
        private static string doSettingsCommand(string[] arguments, ulong guildID)
        {
            string usage = "!settings [view/edit/list] {setting name / number} {new value}";

            if (arguments.Length >= 1)
            {
                if (arguments[0].ToLower() == "view" || arguments[0].ToLower() == "edit")
                {
                    if (arguments.Length >= 2)
                    {
                        if (parseToSetting(arguments[1]).HasValue)
                        {
                            ServerSettings setting = parseToSetting(arguments[1]).Value;

                            if(arguments[0].ToLower() == "view")
                            {
                                return database.FetchEntry(guildID).FetchSetting(setting).ToString();
                            }
                            else
                            {
                                if (arguments.Length >= 3)
                                {
                                    bool value;
                                    if (Boolean.TryParse(arguments[2], out value))
                                    {
                                        database.FetchEntry(guildID).EditSetting(setting, value);
                                        return "Setting edited";
                                    }    
                                    else
                                        return "No parsable boolean given";
                                    
                                }
                                else
                                    return "Requires additional arguments.\nUsage: " + usage;
                            }
                        }
                        else
                            return "Unknown setting. Use '!settings list' for a list of settings";
                    }
                    else
                        return $"Command {arguments[0]} required additional arguments";
                }
                else if (arguments[0].ToLower() == "list")
                    return "Possible settings are:\n" + DescribeAllSettings();
                else
                    return $"Unknown command '{arguments[0]}'.\nUsage: "+ usage;
            }
            else
                return "Insufficient arguments supplied.\nUsage: " + usage;
        }

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
                Thread loggingThread = new Thread(() => log($"{message.Author.Username} sent command: {message.Content}"));
                loggingThread.Start();

                // Grab the DM or guild text channel this message was posted in from cache.
                ITextChannel textChannel = (ITextChannel)shard.Cache.Channels.Get(message.ChannelId);

                //Visually represent that the bot is working on the command
                textChannel.TriggerTypingIndicator();

                //Split message into command and arguments
                string[] splitmsg = message.Content.Split(' ');
                string command = splitmsg[0].Substring(1).ToLower();
                string[] arguments = new string[splitmsg.Length - 1];

                Array.Copy(splitmsg, 1, arguments, 0, splitmsg.Length - 1);
                
                ulong guildID = ((DiscordGuildTextChannel)shard.Cache.Channels.Get(message.ChannelId)).GuildId.Id;
                ulong userID = message.Author.Id.Id;
                
                switch (command)
                {
                    #region No required Perms
                    //
                    case ("ping"):
                        TimeSpan timeSincePost = DateTime.Now.Subtract(message.Timestamp);
                        sendMessage(textChannel, $"{mention(message.Author)} Pong! Took {timeSincePost.TotalMilliseconds} ms");
                    break;

                    //Deletes a message and sends is af it it was by the bot
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


                    case ("status"):
                        sendMessage(textChannel, systemSummary());
                    break;

                    case ("myid"):
                        sendMessage(textChannel, message.Author.Id.ToString());
                    break;

                    case ("listadmins"):
                        DataEntry guildEntry = database.FetchEntry(guildID);
                        List<string> adminMentions = new List<string>();

                        foreach (var userEntry in guildEntry.FetchParsedUsersettings())
                        {
                            if(userEntry.Value[(int)UserSettings.isAdmin])
                            {
                                adminMentions.Add($"{mention(userEntry.Key)}");
                            }
                        }

                        if(adminMentions.Count == 0)
                        {
                            sendMessage(textChannel, "There are no Admins on this server.");
                        }
                        else
                        {
                            string fullAdminMessage = String.Join(", ", adminMentions.ToArray());
                            
                            sendMessage(textChannel, "Admins on this server are:\n" +
                                                    $"{fullAdminMessage}");
                        }
                    break;
                        
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
                        

                    case ("search"):
                        string searchPrefix = searchPrefixByShorthand("", guildID);

                        if (arguments.Length >= 1)
                            if (arguments[0].StartsWith("-"))
                                if(arguments[0].ToLower() == "list")
                                {
                                    sendMessage(textChannel, "Possible search service shorthands are:" + searchServiceShorthands());
                                    return;
                                }
                                else
                                    searchPrefix = searchPrefixByShorthand(arguments[0].Substring(1), guildID);
                        
                        sendMessage(textChannel, $"{searchPrefix}{String.Join("+", arguments)}");
                    break;

                    case ("makeowneradmin"):
                        //grab guilds owner from cache
                        ulong ownerid = (shard.Cache.Guilds.Get(new Discore.Snowflake(guildID))).Value.OwnerId.Id;

                        if (isAdmin(guildID, ownerid))
                            sendMessage(textChannel, $"{mention(ownerid)} is already an admin!");
                        else
                        {
                            addAdmin(guildID, ownerid);
                            sendMessage(textChannel, $"{mention(ownerid)} is now an admin!");
                        }

                        break;
                    #endregion

                    #region Playback commands, may require permissions
                    case ("musicpermtest"):
                        sendMessage(textChannel, (UserCanDJ(userID, guildID) ? "You're a DJ!" : "You can't DJ!"));
                    break;
                        
                    #endregion

                    #region Requires Admin Perms
                    case ("addadmin"):
                        if (isAdmin(guildID, userID))
                        {
                            DiscordUser[] allUsersToBeAdded = message.Mentions.ToArray();
                            foreach (DiscordUser userToBeAdded in allUsersToBeAdded)
                            {

                                if (isAdmin(guildID, userToBeAdded.Id.Id))
                                    sendMessage(textChannel, $"{mention(userToBeAdded)} is already an admin!");
                                else
                                    sendMessage(textChannel, $"{mention(userToBeAdded)} is now an admin!");

                                addAdmin(guildID, userToBeAdded.Id.Id);
                            }
                        }
                        else
                            noPermsMessage(textChannel);
                    break;

                    case ("removeadmin"):
                        if (isAdmin(guildID, userID))
                        {
                            DiscordUser[] allUsersToBeRemoved = message.Mentions.ToArray();
                            foreach (DiscordUser userToBeRemoved in allUsersToBeRemoved)
                            {
                                if (isAdmin(guildID, userToBeRemoved.Id.Id))
                                    sendMessage(textChannel, $"{mention(userToBeRemoved)} is no longer an admin!");
                                else
                                    sendMessage(textChannel, $"{mention(userToBeRemoved)} isn't an admin!");

                                removeAdmin(guildID, userToBeRemoved.Id.Id);
                            }
                        }
                        else
                            noPermsMessage(textChannel);
                    break;
                        
                    case ("purge"):
                        if (isAdmin(guildID, userID))
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
                                    await message.Delete();
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

                    case ("settings"):
                        sendMessage(textChannel, doSettingsCommand(arguments, guildID));
                    break;

                    #endregion

                    #region Requires Developer Perms
                    case ("crash"):
                        if (isDeveloper(message.Author.Id.Id))
                        {   //User has the required Permissions to shut the bot down
                            sendMessage(textChannel, "L");
                            Thread.Sleep(1000);
                            sendMessage(textChannel, "O");
                            Thread.Sleep(1200);
                            shard.Application.ShardManager.StopShardsAsync(CancellationToken.None);
                        }
                        else
                            noPermsMessage(textChannel);
                    break;
                    #endregion
                }
                
            }
        }
    }
}