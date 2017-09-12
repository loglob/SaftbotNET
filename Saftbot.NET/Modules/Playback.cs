using Discore.Voice;
using System.IO;
using System.Threading;
using Discore.WebSocket;
using System;

namespace Saftbot.NET.Modules
{
    public class Playback
    {
        public DiscordVoiceConnection voice;
        private Stream source;
        private bool doSend;
        private bool isSending;

        private void InternalDoLoop()
        {
            // Ensure voice buffer is empty.
            if (voice.BytesToSend > 0)
                voice.ClearVoiceBuffer();

            // Create a buffer for moving data from the source to the voice connection.
            byte[] transferBuffer = new byte[DiscordVoiceConnection.PCM_BLOCK_SIZE];

            isSending = true;

            while (doSend && source.CanRead && (source.Position < source.Length) && voice.IsValid)
            {
                // Check if there is room in the voice buffer
                if (voice.CanSendVoiceData(transferBuffer.Length))
                {
                    // Read some voice data into our transfer buffer.
                    int read = source.Read(transferBuffer, 0, transferBuffer.Length);
                    // Send the data we read from the source into the voice buffer.
                    voice.SendVoiceData(transferBuffer, 0, read);
                }
                else
                    // Wait for at least 1ms to avoid burning CPU cycles.
                    Thread.Sleep(1);
            }

            isSending = false;
        }

        public Playback(Shard shard,ulong voiceChannelID, ulong guildID)
        {
            voice = shard.Voice.CreateOrGetConnection(guildID);
            voice.ConnectAsync(voiceChannelID);
        }

        public Playback(Shard shard, ulong voiceChannelID, ulong guildID, Stream source) : this(shard, voiceChannelID, guildID)
        {
            Play(source);
        }

        public void Play(Stream source)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            DateTime start = DateTime.Now;

            while (!voice.IsConnected)
            {
                if ((DateTime.Now - start) < timeout)
                    Thread.Sleep(20);
                else
                    throw new TimeoutException();
            }

            this.source = source;
            doSend = true;

            try
            {
                InternalDoLoop();
            }
            catch (Exception e)
            {
                isSending = false;
                throw e;
            }
        }

        public bool IsSending()
        {
            return isSending;
        }
    }

    public class TimeoutException : Exception
    {   }
}
