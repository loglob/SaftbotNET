using System;
using Saftbot.NET.DBSystem;

namespace Saftbot.NET.Modules
{
    internal class Setting
    {
        private static setting[] availableSettings = new setting[] 
        {
            new setting("PlebsCanDJ", "Determines if users without admin permissions can use playback commands", 
                        ServerSettings.plebsCanDJ),
            new setting("UseGoogle", "Determines if !search uses the evil botnet google as default search engine",
                        ServerSettings.useGoogle)
        };

        private static string commandUsage = "!settings [view/edit/list] {setting name} {new value (bool)}";

        private static ServerSettings ParseSetting(string settingName)
        {
            foreach (var item in availableSettings)
            {
                if (settingName.ToLower() == item.Name.ToLower())
                    return item.Setting;
            }

            throw new Exception("Internal Setting.cs exception. Check for bugs in the code / contact a dev pls");
        }

        private static string ViewSetting(string settingName, ulong guildID)
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

        private static string SetSetting(string settingName, bool newValue, ulong guildID)
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
            string result = "";

            for (int i = 0; i < availableSettings.Length; i++)
            {
                setting s = availableSettings[i];
                result += $"{s.Name}: {s.Description}{(i < (availableSettings.Length - 1)?",":"")}\n";
            }

            return result;
        }
        

        public static string doSettingsCommand(string[] arguments, ulong guildID)
        {
            if (arguments.Length < 1)
                return "Too few arguments given. Proper usage:\n" + commandUsage;

            string mode = arguments[0].ToLower();

            switch (mode)
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
                        if (Boolean.TryParse(arguments[2], out newValue))
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
    }

    struct setting
    {
        public setting(string name, string description, ServerSettings setting)
        {
            Name = name;
            Description = description;
            Setting = setting;
        }

        public string Name;
        public string Description;
        public ServerSettings Setting;
    }
}
