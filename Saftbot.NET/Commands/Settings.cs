using System;
using Saftbot.NET.DBSystem;

namespace Saftbot.NET.Commands
{
    public class Settings : Command
    {
        private setting[] allSettings = new setting[]
        {
            new setting("UseGoogle",    "Determines if !Search uses the evil botnet Google as default",     ServerSettings.useGoogle),
            new setting("PlebsCanDJ",   "Determines if users without any perms are allowed to DJ",          ServerSettings.plebsCanDJ),
            new setting("CoolReference","Determines if the bot does the cool 'LO' reference when shut down",ServerSettings.coolReference)
        };

        public override void InitializeVariables()
        {
            Name = "Settings";
            Description = "Changes server settings";
            PermsRequired = 3;
            Usage = "<list/set/view> [<setting name>] [<new value>]";
        }

        public override string RunCommand(CommandInformation cmdinfo)
        {
            if (cmdinfo.Arguments.Length >= 1)
            {
                switch(cmdinfo.Arguments[0].ToLower())
                {
                    case ("set"):
                        if (cmdinfo.Arguments.Length >= 3)
                            return set(cmdinfo.Arguments[1], cmdinfo.Arguments[2], cmdinfo.GuildID);
                        break;

                    case ("view"):
                        if (cmdinfo.Arguments.Length >= 2)
                            return view(cmdinfo.Arguments[1], cmdinfo.GuildID);
                        break;

                    case ("list"):
                        return list();

                    default:
                        return "Unknown mode. Use !help settings for proper usage";
                }
            }

            return "Insufficient arguments supplied";
        }

        private string list()
        {
            string message = "```";

            foreach (setting possibleSetting in allSettings)
            {
                message += $"{possibleSetting.Name}: {possibleSetting.Description}\n";
            }

            return message + "```";
        }

        private string view(string settingName, ulong guildID)
        {
            setting toView;

            if(tryParseSetting(settingName, out toView))
            {
                return Program.database.FetchEntry(guildID).FetchSetting(toView.Setting).ToString();
            }
            else
                return "Unknown setting. Use list for a list of possible settings.";
        }

        private string set(string settingName, string newValue, ulong guildID)
        {
            setting toChange;
            bool newVal;

            if (tryParseSetting(settingName, out toChange))
            {
                if (Boolean.TryParse(newValue, out newVal))
                {
                    Program.database.FetchEntry(guildID).EditSetting(toChange.Setting, newVal);
                    return "Settings updated";
                }
                else
                    return "No readable boolean was supplied";
            }
            else
                return "Unknown setting. Use list for a list of possible settings.";
        }

        private bool tryParseSetting(string value, out setting result)
        {
            foreach (setting possibleSetting in allSettings)
            {
                if(possibleSetting.Name.ToLower() == value.ToLower())
                {
                    result = possibleSetting;
                    return true;
                }
            }

            result = new setting();
            return false;
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
