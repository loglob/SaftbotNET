using Discore;
using Saftbot.NET.DBSystem;
using System.Collections.Generic;

// Disable a warning about naming methods lowercase
// (I do this to show they are private members)
#pragma warning disable IDE1006

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

        public override void RunCommand(CommandInformation cmdinfo)
        {
            cmdinfo.Messaging.Send(InternalRunCommand(cmdinfo));
        }

        private string InternalRunCommand(CommandInformation cmdinfo)
        {
            if (cmdinfo.Arguments.Length >= 1)
            {
                string mode = cmdinfo.Arguments[0].ToLower();

                if (mode == "list")
                    return list();

                if (cmdinfo.Arguments.Length >= 2 && (cmdinfo.Message.Mentions.Count > 0))
                {

                    if (TryParsePerm(cmdinfo.Arguments[1], out permission permissionMode))
                    {
                        switch (mode)
                        {
                            case "give":
                                return give(cmdinfo.Message.Mentions, permissionMode, cmdinfo.Guild.GuildID);

                            case "take":
                                return take(cmdinfo.Message.Mentions, permissionMode, cmdinfo.Guild.GuildID);

                            case "view":
                                return view(cmdinfo.Message.Mentions, permissionMode, cmdinfo.Guild.GuildID);
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
            string message = "";

            foreach (permission perm in AllPermissions)
            {
                message += $"__**{perm.Name}**__: {perm.Description}\n";
            }

            return message;
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
            if(user.GetSetting(perm.Setting))
            {
                return $"{user.Mention} is already {perm.FullName}!";
            }
            else
            {
                user.SetSetting(perm.Setting, true);
                return $"{user.Mention} is now {perm.FullName}";
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
            if (user.GetSetting(perm.Setting))
            {
                user.SetSetting(perm.Setting, false);
                return $"{user.Mention} is no longer {perm.FullName}";
            }
            else
            {
                return $"{user.Mention} isn't {perm.FullName}!";
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
            return $"{user.Mention} {((user.GetSetting(perm.Setting)) ? ("is") : ("isn't"))} {perm.FullName}";
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
