using System.IO;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Saftbot.NET.DBSystem
{
    /// <summary>
    /// A custom database system made specifically for this bot
    /// Used for maximum response speed and low RAM/disk usage
    /// </summary>
    public class Database
    {
        /// <summary>
        /// The entire 'database' is read into memory to increase respond speed
        /// </summary>
        private Dictionary<ulong, byte[]> PreloadedFiles;

        /// <summary>
        /// Path to the folder in which the database saves the .sbs files that make it up
        /// (should be {directory of Saftbot.NET.dll}/db/)
        /// </summary>
        private string folderPath;
        
        /// <summary>
        /// The file ending for the database's files
        /// Only used to avoid excessive hardcoding
        /// There is a file created for each server the bot joins
        /// </summary>
        private const string FileEnding = "sbs";

        /// <summary>
        /// The size of the ServerSettings-Header at the start of each .sbs file
        /// </summary>
        public const int HeaderSize = 2;
        
        /// <summary>
        /// Constuct a new database instance
        /// Preloads the .sbs files in given folder
        /// and writes new ones into it
        /// </summary>
        /// <param name="folderPath">Path in which .sbs files are stored</param>
        public Database(string folderPath)
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (isWindows)
            {
                if (! folderPath.EndsWith(@"\"))
                    folderPath += @"\";
            }
            else
            {
                if (!folderPath.EndsWith(@"/"))
                    folderPath += @"/";
            }

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            this.folderPath = folderPath;

            PreloadedFiles = new Dictionary<ulong, byte[]>();

            foreach(string subPath in Directory.EnumerateFiles(folderPath))
            {
                if(subPath.EndsWith("."+FileEnding))
                {

                    if (UInt64.TryParse(Path.GetFileNameWithoutExtension(subPath), out ulong id))
                    {
                        PreloadedFiles.Add(id, File.ReadAllBytes(subPath));
                        
                    }
                }
            }
        }

        /// <summary>
        /// Add an entry to the database or override an existing one
        /// </summary>
        /// <param name="entry"></param>
        public void RegisterEntry(DataEntry entry)
        {
            if (PreloadedFiles.ContainsKey(entry.Id))
                PreloadedFiles[entry.Id] = entry.Raw;
            else
                PreloadedFiles.Add(entry.Id, entry.Raw);

            File.WriteAllBytes($"{folderPath}{entry.Id}.{FileEnding}", entry.Raw);
        }

        /// <summary>
        /// Determines if given entry exists in this database
        /// Please note that files manually added after the database instance was constructed won't be
        /// preloaded and therefore not count
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public bool DoesEntryExist(ulong guildId)
        {
            return PreloadedFiles.ContainsKey(guildId);
        }

        #region FetchEntries
        public  DataEntry FetchEntry(Discore.DiscordChannel channel)
        {
            return FetchEntry(channel.Id);
        }

        public DataEntry FetchEntry(Discore.Snowflake channelSnowflake)
        {
            return FetchEntry(channelSnowflake.Id);
        }

        /// <summary>
        /// Fetch the entry with the given ID
        /// If it doesn't exist, creates a new file with DefaultEntry() values
        /// </summary>
        /// <param name="id">GuildID that should be fetched</param>
        /// <returns></returns>
        public DataEntry FetchEntry(ulong id)
        {
            if(PreloadedFiles.ContainsKey(id))
            {
                return new DataEntry(PreloadedFiles[id], id, this);
            }
            else
            {   //The given channel id does not have an entry yet
                DataEntry entry = DefaultEntry(id);
                RegisterEntry(entry);
                return entry;
            }
        }
        #endregion
        
        /// <summary>
        /// Save all changes made to this Database to disk
        /// </summary>
        public void SaveChanges()
        {
            foreach (var entry in PreloadedFiles)
            {
                File.WriteAllBytes($"{folderPath}{entry.Key}.{FileEnding}", entry.Value);
            }
        }
        
        /// <summary>
        /// Get the standard entry for creating new entries
        /// </summary>
        /// <param name="id">The ID of the guild</param>
        /// <returns></returns>
        public DataEntry DefaultEntry(ulong id)
        {
            return new DataEntry(new byte[2] { 0, 0 }, id, this);
        }
    }
}
