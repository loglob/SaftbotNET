using System;
using System.Collections.Generic;
using System.Linq;
using Discore;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Saftbot.NET.Modules
{
    public static class Utility
    {
        public static string SpecialAndJoin(IEnumerable<string> list)  {   return SpecialAndJoin(list.ToArray());  }

        public static string SpecialAndJoin(string[] array)
        {
            string joined = "";

            for (int i = 0; i < array.Length; i++)
            {
                joined += array[i].ToString();

                if (i < array.Length - 1)
                {
                    if (i < array.Length - 2)
                        joined += ", ";
                    else
                        joined += " and ";
                }
            }

            return joined;
        }

        public static string Mention(DiscordUser user) { return Mention(user.Id.Id); }
        public static string Mention(Snowflake userID) { return Mention(userID.Id); }
        public static string Mention(ulong userID) { return $"<@{userID}>"; }

        public static int Count(string text, char tocount)
        {
            int count = 0;

            foreach (char chr in text)
            {
                if (chr == tocount)
                    count++;
            }

            return count;
        }

        public static string[] FindNecessaryParameters(Commands.Command cmd)
        {
            List<string> parameters = new List<string>();

            foreach (string parameter in cmd.Usage)
            {
                if (!(parameter.Contains("[")))
                {
                    parameters.Add(parameter.Substring(1, parameter.Length - 2));
                }
            }

            return parameters.ToArray();
        }

        public static string SystemSummary()
        {
            int CoreCount = Environment.ProcessorCount;
            string timezone = TimeZoneInfo.Local.DisplayName;
            string UTCoffset = TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString();
            string machineName = Environment.MachineName;
            string OSarch = RuntimeInformation.OSArchitecture.ToString();
            string OSdesc = RuntimeInformation.OSDescription;
            string cpuarch = RuntimeInformation.ProcessArchitecture.ToString();
            string fwInfo = RuntimeInformation.FrameworkDescription;
            string uptime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).ToString();

            return ($"I am running on {machineName}, specs:\nOS:{OSdesc}({OSarch})\nCPU:{CoreCount} " +
                $"Core {cpuarch} CPU\nTimezone: {timezone}(UTC+{UTCoffset})\nFramework: {fwInfo}\nUptime: {uptime}\n" +
                $"Saftbot-Versiontag: {Program.saftbotVersionTag}");
        }

    }
}
