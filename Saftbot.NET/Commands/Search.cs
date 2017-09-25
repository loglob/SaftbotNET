using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Saftbot.NET.Commands
{
    class Search : Command
    {
        /*
        private static SearchProvider[] Providers = new SearchProvider[]
        {
            new SearchProvider("ddg",               "DuckDuckGo",               "https://duckduckgo.com/?q="),
            new SearchProvider("g",                 "Google",                   "https://google.com/search?q="),
            new SearchProvider("tpb",               "The Piratebay",            "https://thepiratebay.org/search/", "%20"),
            new SearchProvider("yt",                "YouTube",                  "https://www.youtube.com/results?search_query="),
            new SearchProvider("st",                "Steam Store",              "https://store.steampowered.com/search/?term="),
            new SearchProvider("tw",                "Twtter",                   "https://twitter.com/search?q=", "%20"),
            new SearchProvider("nya",               "Nya.sii Anime Tracker",    "https://nyaa.si/?q="),
            new SearchProvider("r",                 "Reddit",                   "https://www.reddit.com/search?q="),
            new SearchProvider("go",                "GoMovies (.sc)",           "https://gomovies.sc/search-query/"),
            new SearchProvider("r_{x}",             "Given Subreddit",          "https://www.reddit.com/r/", "+", "/search?restrict_sr=on&q="),
            new SearchProvider("go_{x}",            "Given GoMovies Mirror",    "https://gomovies.", "+", "/search-query/"),
            new SearchProvider("4c_{x}",            "Given 4chan board",        "https://4chan.org/", "+", "/catalog#s="),
        };*/

        public override void InitializeVariables()
        {
            Name = "Search";
            Description = "Searches using the given search provider. Use !Search -list for a list of providers.";
            PermsRequired = 0;
            Usage = "[<provider>] <query>";

            ProviderSerializer.Path = Program.AssemblyPath + "SearchProviders.txt";
            ProviderSerializer.Update();
        }
        
        internal override string InternalRunCommand(CommandInformation cmdinfo)
        {
            if (cmdinfo.Arguments[0].StartsWith("-"))
            {
                if (cmdinfo.Arguments[0].ToLower() == "-list")
                    return List();

                string providerShorthand = cmdinfo.Arguments[0].Substring(1);

                foreach (var provider in ProviderSerializer.Providers)
                {
                    if (provider.Matches(providerShorthand))
                    {
                        string[] query = new string[cmdinfo.Arguments.Length - 1];
                        Array.Copy(cmdinfo.Arguments, 1, query, 0, query.Length);

                        return provider.GetLink(providerShorthand, query);
                    }
                }
                    
            }

            if (Program.database.FetchEntry(cmdinfo.Guild.GuildID).FetchSetting(DBSystem.ServerSettings.useGoogle))
                return ProviderSerializer.Providers[1].GetLink("", cmdinfo.Arguments);
            else
                return ProviderSerializer.Providers[0].GetLink("", cmdinfo.Arguments);
                
        }

        private string List()
        {
            string message = "";

            foreach (var provider in ProviderSerializer.Providers)
            {
                message += $"__**{provider.ShortHand}**__: {provider.Description}\n";
            }

            return message;
        }
    }

    struct SearchProvider
    {
        public SearchProvider(string shorthand, string description, string prefix, string spaceEscape = "+", string preSuffix = "")
        {
            ShortHand = shorthand;
            Description = description;
            SpaceEscape = spaceEscape;
            this.prefix = prefix;
            this.preSuffix = preSuffix;
        }

        public string ShortHand;
        public string Description;
        public string SpaceEscape;

        private string prefix;
        private string preSuffix;

        public bool Matches(string providerArgument)
        {
            if (providerArgument.Contains("_") == ShortHand.Contains("_"))
            {
                if (providerArgument.Contains("_"))
                    return providerArgument.Split('_')[0].ToLower() == ShortHand.Split('_')[0].ToLower();
                else
                    return providerArgument.ToLower() == ShortHand.ToLower();
            }
            else
                return false;
        }

        public string GetPrefix(string providerArgument)
        {
            if(providerArgument.Contains("_"))
            {
                string[] specifiedProvider = providerArgument.Split('_');
                string[] cutDown = new string[specifiedProvider.Length - 1];
                Array.Copy(specifiedProvider, 1, cutDown, 0, cutDown.Length);

                return prefix + String.Join('_',cutDown).ToLower() + preSuffix;
            }
            else
                return prefix;
            
        }

        public string GetLink(string providerArgument, string[] query)
        {
            return GetPrefix(providerArgument) + String.Join(SpaceEscape, query);
        }
    }
    
    static class ProviderSerializer
    {
        public static string Path;
        public static TimeSpan UpdateInterval = new TimeSpan(0,5,0);
        public const char SeperatorChar = '|';

        public static SearchProvider[] Providers
        {
            get
            {
                SearchProvider[] CacheCopy = cache;

                if((DateTime.Now - lastUpdate) >= UpdateInterval)
                    (new Thread(() => Update())).Start();

                return CacheCopy;
            }
        }
        
        private static SearchProvider[] cache = new SearchProvider[0];
        private static DateTime lastUpdate = DateTime.MinValue;

        public static void Update()
        {
            string[] lines = new string[0];
            try
            {
                lines = File.ReadAllLines(Path);
            }
            catch(Exception e)
            {
                Program.log.Enter(e, "read providerlist");
                return;
            }

            List<SearchProvider> newCache = new List<SearchProvider>();

            foreach (string line in lines)
            {
                if (!line.StartsWith('#'))
                {
                    string[] splitline = line.Split(SeperatorChar);
                    try
                    {
                        newCache.Add(Parse(splitline));
                    }
                    catch (Exception e)
                    {
                        Program.log.Enter(e, $"parse search provider: '{line}'");
                    }
                }
            }

            lastUpdate = DateTime.Now;
            cache = newCache.ToArray();
        }

        /// <summary>
        /// Builds Search provider form strings that gives data like this:
        /// shorthand, description, prefix, spaceEscape = "+", preSuffix = ""
        /// </summary>
        /// <param name="splitline"></param>
        /// <returns></returns>
        public static SearchProvider Parse(string[] splitline)
        {
            switch(splitline.Length)
            {
                case 3:
                    return new SearchProvider(splitline[0].Trim(), splitline[1].Trim(), splitline[2].Trim());
                case 4:
                    return new SearchProvider(splitline[0].Trim(), splitline[1].Trim(), splitline[2].Trim(), splitline[3].Trim());
                case 5:
                    return new SearchProvider(splitline[0].Trim(), splitline[1].Trim(), splitline[2].Trim(), splitline[3].Trim(), 
                                              splitline[4].Trim());
                default:
                    throw new Exception($"Attempted to build a SearchProvider from array with length {splitline.Length}");
            }
        }
    }
}
