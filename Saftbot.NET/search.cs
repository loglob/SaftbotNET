using System;
using System.Collections.Generic;
using System.Text;

namespace Saftbot.NET
{
    public class Search
    {
        private static StaticSearchProvider[] staticSearchProviders = new StaticSearchProvider[]
        {
            new StaticSearchProvider("ddg", "https://duckduckgo.com/?q=", "Duckduckgo"),
            new StaticSearchProvider("g", "https://google.com/search?q=", "Google"),
            new StaticSearchProvider("tpb", "https://thepiratebay.org/search/", "The Piratebay", "%20"),
            new StaticSearchProvider("yt", "https://www.youtube.com/results?search_query=", "Youtube"),
            new StaticSearchProvider("st", "http://store.steampowered.com/search/?term=", "Steam Store"),
            new StaticSearchProvider("tw", "https://twitter.com/search?q=", "Twitter", "%20"),
            new StaticSearchProvider("nya", "https://nyaa.si/?q=", "Nyaa.si anime tracker"),
            new StaticSearchProvider("r", "https://www.reddit.com/search?q=", "Reddit"),
            new StaticSearchProvider("go", "https://gomovies.sc/search-query/", "GoMovies (.sc)")
        };

        private static DynamicSearchProvider[] dynamicSearchProviders = new DynamicSearchProvider[]
        {
            new DynamicSearchProvider("4c", "https://4chan.org/", "/catalog#s=", "The given 4chan board"),
            new DynamicSearchProvider("r", "https://www.reddit.com/r/", "/search?restrict_sr=on&q=", "The given subreddit"),
            new DynamicSearchProvider("go", "https://gomovies.", "/search-query/", "GoMovies with the given CCTLD" )
        };

        static ISearchProvider standard(ulong guildID)
        {
            bool google = Program.database.FetchEntry(guildID).FetchSetting(ServerSettings.useGoogle);

            return staticSearchProviders[google ? 1 : 0];
        }

        static public string DescribeProviders()
        {
            string msg = "```";

            foreach (StaticSearchProvider ssp in staticSearchProviders)
            {
                msg += $"{ssp.Shorthand.ToUpper()}: {ssp.Description}\n";
            }

            foreach (DynamicSearchProvider dsp in dynamicSearchProviders)
            {
                msg += $"{dsp.Shorthand.ToUpper()}_[x]: {dsp.Description}\n";
            }

            return msg + "```";
        }

        private static ISearchProvider GetProvider(string argument, ulong guildID)
        {
            argument = argument.ToLower();

            if (argument.StartsWith("-"))
            {
                if (argument.Contains("_"))
                {
                    foreach (DynamicSearchProvider dsp in dynamicSearchProviders)
                    {
                        if (argument.Split('_')[0].Substring(1) == dsp.Shorthand)
                            return dsp;
                    }
                }
                else
                {
                    foreach (StaticSearchProvider ssp in staticSearchProviders)
                    {
                        if (argument.Substring(1) == ssp.Shorthand)
                            return ssp;
                    }
                }
            }

            return standard(guildID);
        }

        public static string DoSearchCommand(string[] arguments, ulong guildID)
        {
            string argument = "";

            if (arguments.Length >= 1)
                argument = arguments[0].ToLower();

            if (argument == "-list")
                return DescribeProviders();

            return GetProvider(argument, guildID).BuildSearchLink(arguments);
        }
    }

    public interface ISearchProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arguments">The string[] of arguments, including the provider-indentifying argument, if there is one</param>
        /// <returns></returns>
        string BuildSearchLink(string[] arguments);
    }

    public struct StaticSearchProvider : ISearchProvider
    {
        public StaticSearchProvider(string shorthand, string prefix, string description, string spaceEscape = "+") : this()
        {
            Shorthand = shorthand;
            Prefix = prefix;
            SpaceEscape = spaceEscape;
            Description = description;
        }

        public string Shorthand;
        public string Prefix;
        public string SpaceEscape;
        public string Description;

        public string BuildSearchLink(string[] arguments)
        {
            if ((arguments.Length >= 1) && (arguments[0].ToLower() == $"-{Shorthand}"))
            {
                string[] newArgs = new string[arguments.Length - 1];
                Array.Copy(arguments, 1, newArgs, 0, newArgs.Length);

                arguments = newArgs;
            }

            return Prefix + String.Join(SpaceEscape, arguments);
        }
    }

    public struct DynamicSearchProvider : ISearchProvider
    {
        public DynamicSearchProvider(string shorthand, string prefix, string postfix, string description, string spaceEscape = "+") : this()
        {
            Shorthand = shorthand;
            Prefix = prefix;
            Postfix = postfix;
            Description = description;
            SpaceEscape = spaceEscape;
        }

        public string Shorthand;
        public string Description;
        public string SpaceEscape;

        private string Prefix;
        private string Postfix;

        public string getSearchHeader(string argument)
        {
            return Prefix + argument + Postfix;
        }

        public string BuildSearchLink(string[] arguments)
        {
            if (arguments.Length < 1 || (!arguments[0].Contains("_")))
                throw new Exception("Failed dynamic link building");

            string[] newArgs = new string[arguments.Length - 1];
            Array.Copy(arguments, 1, newArgs, 0, newArgs.Length);

            return getSearchHeader(arguments[0].Split('_')[1]) + String.Join(SpaceEscape, newArgs);
        }
    }
}
