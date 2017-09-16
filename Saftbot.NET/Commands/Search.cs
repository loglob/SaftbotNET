using System;

namespace Saftbot.NET.Commands
{
    class Search : Command
    {
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
        };

        public override void InitializeVariables()
        {
            Name = "Search";
            Description = "Searches using the given search provider. Use !Search -list for a list of providers.";
            PermsRequired = 0;
            Usage = "[<provider>] <query>";
        }
        
        public override void RunCommand(CommandInformation cmdinfo)
        {
            cmdinfo.Messaging.Send(InternalRunCommand(cmdinfo));
        }

        private string InternalRunCommand(CommandInformation cmdinfo)
        {
            if(cmdinfo.Arguments.Length > 0)
            {
                if (cmdinfo.Arguments[0].StartsWith("-"))
                {
                    if (cmdinfo.Arguments[0].ToLower() == "-list")
                        return List();

                    string providerShorthand = cmdinfo.Arguments[0].Substring(1);

                    foreach (var provider in Providers)
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
                    return Providers[1].GetLink("", cmdinfo.Arguments);
                else
                    return Providers[0].GetLink("", cmdinfo.Arguments);

            }
            return "No arguments given...";
        }

        private string List()
        {
            string message = "";

            foreach (var provider in Providers)
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
                return prefix + providerArgument.Split('_')[1].ToLower() + preSuffix;
            }
            else
                return prefix;
            
        }

        public string GetLink(string providerArgument, string[] query)
        {
            return GetPrefix(providerArgument) + String.Join(SpaceEscape, query);
        }
    }
}
