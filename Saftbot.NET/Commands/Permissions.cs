using Discore;
using Saftbot.NET.DBSystem;
using System.Collections.Generic;

namespace Saftbot.NET.Commands
{
    public class Permissions : Command
    {
        private permission[] AllPermissions = new permission[]
        {
            new permission("Admin", "able to run all bot commands", UserSettings.isAdmin, "an admin"),
            new permission("DJ", "able to run audio playback commands", UserSettings.isDJ, "a DJ"),
            new permission("Ignored", "Not able to run any commands", UserSettings.isIgnored, "ignored")
        };

        public override void InitializeVariables()
        {
            Name = "Permissions";
            Description = "Changes the given user(s) permissions";
            PermsRequired = 3;
            Usage = "<list/give/take/view> [<Permission name>] [<user mention(s)>]";
        }

        public override string RunCommand(CommandInformation cmdinfo)
        {
            if (cmdinfo.Arguments.Length >= 1)
            {
                string mode = cmdinfo.Arguments[0].ToLower();

                if (mode == "list")
                    return list();

                if (cmdinfo.Arguments.Length >= 2 && (cmdinfo.Message.Mentions.Count > 0))
                {
                    permission permissionMode;

                    if (TryParsePerm(cmdinfo.Arguments[1], out permissionMode))
                    {
                        switch(mode)
                        {
                            case "give":
                                return give(cmdinfo.Message.Mentions, permissionMode, cmdinfo.GuildID);
                                
                            case "take":
                                return take(cmdinfo.Message.Mentions, permissionMode, cmdinfo.GuildID);

                            case "view":
                                return view(cmdinfo.Message.Mentions, permissionMode, cmdinfo.GuildID);
                            default:
                                return "Unknown mode. Use !help permissions for proper usage.";
                        }
                    }
                    else
                        return "Unknown permission. Use list for a list of permissions.";
                }
            }

            return "Insufficient arguments supplied";
        }

        private bool TryParsePerm(string value, out permission result)
        {
            foreach (permission perm in AllPermissions)
            {
                if (perm.Name.ToLower() == value.ToLower())
                {
                    result = perm;
                    return true;
                }
            }

            result = new permission();
            return false;
        }
    
        #region modes
        private string list()
        {
            string message = "```";

            foreach (permission perm in AllPermissions)
            {
                message += $"{perm.Name}: {perm.Description}\n";
            }

            return message + "```";
        }

        private string give(IEnumerable<DiscordUser> users, permission perm, ulong guildID)
        {
            string message = "";

            foreach (DiscordUser user in users)
            {
                message += give(new Modules.UserProfile(user.Id.Id, guildID), perm);
                message += "\n";
            }

            return message;
        }

        private string give(Modules.UserProfile user, permission perm)
        {
            if(user.Is(perm.Setting))
            {
                return $"{user.GetMention()} is already {perm.FullName}!";
            }
            else
            {
                user.Set(perm.Setting, true);
                return $"{user.GetMention()} is now {perm.FullName}";
            }
        }

        private string take(IEnumerable<DiscordUser> users, permission perm, ulong guildID)
        {
            string message = "";

            foreach (DiscordUser user in users)
            {
                message += take(new Modules.UserProfile(user.Id.Id, guildID), perm);
                message += "\n";
            }

            return message;
        }

        private string take(Modules.UserProfile user, permission perm)
        {
            if (user.Is(perm.Setting))
            {
                user.Set(perm.Setting, false);
                return $"{user.GetMention()} is no longer {perm.FullName}";
            }
            else
            {
                return $"{user.GetMention()} isn't {perm.FullName}!";
            }
        }

        private string view(IEnumerable<DiscordUser> users, permission perm, ulong guildID)
        {
            string message = "";

            foreach (DiscordUser user in users)
            {
                message += view(new Modules.UserProfile(user.Id.Id, guildID), perm);
                message += "\n";
            }

            return message;
        }

        private string view(Modules.UserProfile user, permission perm)
        {
            return $"{user.GetMention()} {((user.Is(perm.Setting)) ? ("is") : ("isn't"))} {perm.FullName}";
        }
#endregion
    }

    struct permission
    {
        public permission(string name, string description, UserSettings setting, string fullname)
        {
            Name = name;
            FullName = fullname;
            Description = description;
            Setting = setting;
        }

        public string Name;
        public string FullName;
        public string Description;
        public UserSettings Setting;
    }
}
