using System;
using System.Collections.Generic;

namespace Saftbot.NET.DBSystem
{
    /// <summary>
    /// An entry for a single server and it's users
    /// </summary>
    public class DataEntry
    {
        /// <summary>
        /// The raw bytes making up the file for this entry
        /// </summary>
        public byte[] Raw;
        
        /// <summary>
        /// The guildID of this entries channel 
        /// </summary>
        public ulong Id;

        /// <summary>
        /// The Database instance this entry is from
        /// </summary>
        private Database from;

        public DataEntry(byte[] raw, ulong id, Database sourceDatabase)
        {
            Raw = raw;
            Id = id;
            from = sourceDatabase;
        }

        #region working with server settings
        /// <summary>
        /// Fetch the raw 2-byte settings header for this entry
        /// </summary>
        /// <returns></returns>
        public byte[] FetchSettingsHeader()
        {
            //The first 2 bytes in the raw data are meant for various server specific settings
            byte[] toFill = new byte[Database.HeaderSize];
            Array.Copy(Raw, toFill, Database.HeaderSize);
            return toFill;
        }

        /// <summary>
        /// Fetch the Settings header digested into a length 16 bool array
        /// </summary>
        /// <returns></returns>
        public bool[] FetchParsedSettingsHeader()
        {
            byte[] settingsHeader = FetchSettingsHeader();
            bool[] parsedSettingsHeader = new bool[settingsHeader.Length * 8];

            for (int i = 0; i < settingsHeader.Length; i++)
            {
                Array.Copy(TurnIntoBools(settingsHeader[i]), 0, parsedSettingsHeader, i * 8, 8);
            }

            return parsedSettingsHeader;
        }

        /// <summary>
        /// Fetch the given settings value for this entry
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public bool FetchSetting(ServerSettings setting)
        {
            return FetchParsedSettingsHeader()[(int)setting];
        }

        /// <summary>
        /// Set the given setting to the given value
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="value"></param>
        public void EditSetting(ServerSettings setting, bool value)
        {
            byte[] settingsHeader = FetchSettingsHeader();
            bool[] boolifiedSettingsHeader = TurnIntoBools(settingsHeader);

            boolifiedSettingsHeader[(int)setting] = value;
            EditSettingsHeader(TurnIntoBytes(boolifiedSettingsHeader));
        }

        public void EditSettingsHeader(bool[] newValues)
        {
            for (int i = 0; i < 16; i++)
            {
                EditSetting((ServerSettings)i, newValues[i]);
            }
        }

        /// <summary>
        /// Overwrite the raw settings header with new raw data
        /// </summary>
        /// <param name="newHeader"></param>
        public void EditSettingsHeader(byte[] newHeader)
        {
            if(newHeader.Length >= 2)
            {
                Array.Copy(newHeader, Raw, 2);
                SaveChanges();
            }
        }
        #endregion

        #region Working with userdata
        /// <summary>
        /// Get the userdata saved in this instance. Each user has 1 byte for saving information about them
        /// </summary>
        /// <returns></returns>
        public Dictionary<ulong, byte> FetchParsedUserdata()
        {
            Dictionary<ulong, byte> dict = new Dictionary<ulong, byte>();
            int i = Database.HeaderSize;

            while (i < Raw.Length)
            {
                ulong idCOmponent = BitConverter.ToUInt64(Raw, i);
                i += 8;
                dict.Add(idCOmponent, Raw[i]);
                i++;
            }

            return dict;
        }


        /// <summary>
        /// Get the user's information parsed into bool arrays
        /// </summary>
        /// <returns></returns>
        public Dictionary<ulong, bool[]> FetchParsedUsersettings()
        {
            Dictionary<ulong, bool[]> userSettings = new Dictionary<ulong, bool[]>();

            foreach(var x in FetchParsedUserdata())
            {
                userSettings.Add(x.Key, TurnIntoBools(x.Value));
            }

            return userSettings;
        }

        #region FetchUserSetting
        public bool FetchUserSetting(Discore.DiscordUser user, UserSettings setting)
        {
            return FetchUserSetting(user.Id, setting);
        }

        public bool FetchUserSetting(Discore.Snowflake userSnowflake, UserSettings setting)
        {
            return FetchUserSetting(userSnowflake.Id, setting);
        }

        /// <summary>
        /// Get a specific users specific setting
        /// If the user doesn'T have an entry yet, creates one
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public bool FetchUserSetting(ulong userId, UserSettings setting)
        {
            if(IsUserRegistered(userId))
                return FetchUserSettings(userId)[(int)setting];
            else
            {
                EditUserdata(userId, DefaultUserdata());

                return TurnIntoBools(DefaultUserdata())[(int)setting];
            }
        }

        #endregion

        #region is user registered
        public bool IsUserRegistered(Discore.DiscordUser user)
        {
            return IsUserRegistered(user.Id);
        }

        public bool IsUserRegistered(Discore.Snowflake userSnowflake)
        {
            return IsUserRegistered(userSnowflake.Id);
        }

        /// <summary>
        /// Determines if the given userID is already registered in this entry
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool IsUserRegistered(ulong userId)
        {
            return FetchParsedUserdata().ContainsKey(userId);
        }
        #endregion

        #region FetchUserData
        public byte FetchUserData(Discore.DiscordUser user)
        {
            return FetchUserData(user.Id);
        }

        public byte FetchUserData(Discore.Snowflake userSnowflake)
        {
            return FetchUserData(userSnowflake.Id);
        }

        /// <summary>
        /// Fetch a given users informatino byte.
        /// Creates a new entry is one doesn't exist.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public byte FetchUserData(ulong userId)
        {
            var parsedUD = FetchParsedUserdata();

            if(parsedUD.ContainsKey(userId))
                return FetchParsedUserdata()[userId];
            else
            {
                EditUserdata(userId, DefaultUserdata());
                return DefaultUserdata();
            }
        }

        public bool[] FetchUserSettings(Discore.DiscordUser user)
        {
            return TurnIntoBools(FetchUserData(user));
        }

        public bool[] FetchUserSettings(Discore.Snowflake userSnowflake)
        {
            return TurnIntoBools(FetchUserData(userSnowflake));
        }

        /// <summary>
        /// Fetch a given users information parsed into a bool array
        /// Creates a new userentry is none exists
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool[] FetchUserSettings(ulong userId)
        {
            return TurnIntoBools(FetchUserData(userId));
        }
        #endregion

        #region edit Userdata
        public void EditUserdata(Discore.DiscordUser user, byte userdata)
        {
            EditUserdata(user.Id, userdata);
        }

        public void EditUserdata(Discore.Snowflake userSnowflake, byte userdata)
        {
            EditUserdata(userSnowflake.Id, userdata);
        }

        /// <summary>
        /// Edits a users information.
        /// If the user doesn't have an entry, creates one.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userdata"></param>
        public void EditUserdata(ulong userId, byte userdata)
        {
            //determine if the user is registered
            if(IsUserRegistered(userId))
            {
                int i = Database.HeaderSize;
                byte[] target = BitConverter.GetBytes(userId);

                while(i < Raw.Length)
                {
                    if(Raw[i] == target[0])
                    {
                        ulong IdAtPosition = BitConverter.ToUInt64(Raw, i);

                        if(IdAtPosition == userId)
                        {
                            Raw[i + 8] = userdata;
                            return;
                        }
                    }

                    i += 9;
                }
            }
            else
            {
                byte[] newRaw = new byte[Raw.Length + 9];
                Array.Copy(Raw, newRaw, Raw.Length);
                Array.Copy(BitConverter.GetBytes(userId), 0, newRaw, Raw.Length, 8);
                newRaw[newRaw.Length - 1] = userdata;

                Raw = newRaw;
            }

            SaveChanges();
        }
        #endregion

        #region edit userSettings
        public void EditUserSetting(Discore.DiscordUser user, UserSettings setting, bool newValue)
        {
            EditUserSetting(user.Id, setting, newValue);
        }
        public void EditUserSetting(Discore.Snowflake userSnowflake, UserSettings setting, bool newValue)
        {
            EditUserSetting(userSnowflake.Id, setting, newValue);
        }

        public void EditUserSetting(ulong userId, UserSettings setting, bool newValue)
        {
            if (IsUserRegistered(userId))
            {
                bool[] userSettings = FetchUserSettings(userId);
                userSettings[(int)setting] = newValue;

                EditUserdata(userId, TurnIntoByte(userSettings));
            }
            else
            {
                bool[] defaultSettings = TurnIntoBools(DefaultUserdata());
                defaultSettings[(int)setting] = newValue;

                EditUserdata(userId, TurnIntoByte(defaultSettings));
            }
        }
        #endregion
        #endregion  

        /// <summary>
        /// Save all changes made to this entry to the actual database.
        /// </summary>
        public void SaveChanges()
        {
            from.RegisterEntry(this);
            from.SaveChanges();
        }

        #region helper methods
        private bool[] TurnIntoBools(byte rawUserdata)
        {
            bool[] settings = new bool[8];

            for (int i = 0; i < 8; i++)
            {
                byte temp = (byte)(rawUserdata << i);
                temp = (byte)(temp >> 7);
                settings[i] = temp > 0;
            }

            return settings;
        }

        private bool[] TurnIntoBools(byte[] bytes)
        {
            bool[] bools = new bool[bytes.Length * 8];

            for (int i = 0; i < bytes.Length; i++)
            {
                Array.Copy(TurnIntoBools(bytes[i]), 0, bools, i * 8, 8);
            }

            return bools;
        }

        private byte[] TurnIntoBytes(bool[] headerData)
        {
            byte[] bytes = new byte[headerData.Length / 8];

            for (int i = 0; i < bytes.Length; i++)
            {
                bool[] singleByte = new bool[8];

                Array.Copy(headerData, i * 8, singleByte, 0, 8);

                bytes[i] = TurnIntoByte(singleByte);
            }

            return bytes;
        }

        private byte TurnIntoByte(bool[] userSettings)
        {
            byte result = 0;

            for (int i = 0; i < userSettings.Length; i++)
            {
                if(userSettings[i])
                    result += (byte)(1 << (7 - i));
            }

            return result;
        }
        #endregion

        public byte DefaultUserdata()
        {
            return 0;
        }
    }
}
