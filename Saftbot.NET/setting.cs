using System;
using System.Collections.Generic;
using System.Text;

namespace Saftbot.NET
{
    internal class Setting
    {
        public static string doSettingsCommand(string[] arguments, ulong guildID)
        {
            if (arguments.Length < 1)
                return "Too few arguments given. Proper usage:\n" + commandUsage;

            string mode = arguments[0].ToLower();

            switch(mode)
            {
                case "list":
                    return listSettings();

                case "view":
                    if (arguments.Length >= 2)
                        return ViewSetting(arguments[1], guildID);
                    else
                        return "Too few arguments given. Proper usage:\n" + commandUsage;

                case "set":
                    if (arguments.Length >= 3)
                    {
                        bool newValue;
                        if(Boolean.TryParse(arguments[2], out newValue))
                            return SetSetting(arguments[1], newValue, guildID);
                        else
                            return "Given value isn't boolean";
                    }
                    else
                        return "Too few arguments given. Proper usage:\n" + commandUsage;

                default:
                    return $"Unknown mode: '{arguments[0]}'. Proper usage:\n" + commandUsage;
            }
        }

        public static ServerSettings ParseSetting(string settingName)
        {
            switch(settingName.ToLower())
            {
                case "usegoogle":
                    return ServerSettings.useGoogle;

                case "plebscandj":
                    return ServerSettings.plebsCanDJ;

                default:
                    throw new Exception("Unknown Setting. Use '!settings list' for a list of settings");
            }
        }

        public static string ViewSetting(string settingName, ulong guildID)
        {
            try
            {
                return Program.database.FetchEntry(guildID).FetchSetting(ParseSetting(settingName)).ToString();
            }
            catch
            {
                return "Unknown Setting. Use '!settings list' for a list of settings.";
            }
        }

        public static string SetSetting(string settingName, bool newValue, ulong guildID)
        {
            try
            {
                Program.database.FetchEntry(guildID).EditSetting(ParseSetting(settingName), newValue);
                return "Setting changed.";
            }
            catch
            {
                return "Unknown Setting. Use '!settings list' for a list of settings.";
            }
        }

        private static string listSettings()
        {
            return "PlebsCanDJ: Determines if users without admin permissions can use playback commands,\n" +
                   "UseGoogle: Determines if !search uses the evil botnet google as default search engine";
        }

        public static string commandUsage = "!settings [view/edit/list] {setting name} {new value (bool)}";
    }
}
