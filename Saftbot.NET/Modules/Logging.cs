using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Saftbot.NET.Modules
{
    internal class Log
    {
        /// <summary>
        /// The path to the .txt into which the log entries are written
        /// </summary>
        private string logFilePath;

        public Log(bool doInitMessage = true)
        {
            // Gets path to Saftbot.NET.dll and adds a subdirectory calles "logs"
            string directoryPath = Program.AssemblyPath + "logs";

            //create the logs folder if it doesn't exist
            Directory.CreateDirectory(directoryPath);
            
            //Build Path to store current logfile in
            logFilePath = directoryPath + Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+".txt";

            //create empty log file
            FileStream FS = File.Create(logFilePath);
            FS.Flush();
            FS.Dispose();

            if(doInitMessage)
            {
                Enter($"Created log file at {logFilePath}");
            }
        }

        public Task EnterAsync(string entry, bool addTimeStamp = true)
        {
            var task = new Task(() => Enter(entry, addTimeStamp));
            task.Start();
            return task;
        }

        public Task EnterAsync(Exception e)
        {
            return EnterAsync($"Encountered {e.GetType().ToString()} at {e.Source} \n Message: {e.Message} \n Data: {e.Data} \n ");
        }

        public Task EnterAsync(Exception e, string source)
        {
            return EnterAsync($"Encountered {e.GetType().ToString()} while {source} \n {e.Message} \n at: {e.Source} \n data: {e.Data}");
        }

        public void Enter(string entry, bool addTimeStamp = true)
        {
            StreamWriter SW = File.AppendText(logFilePath);

            if (addTimeStamp)
                SW.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]: {entry}");
            else
                SW.WriteLine(entry);

            SW.Flush();
            SW.Dispose();

            Console.WriteLine(entry);
        }
        
        public void Enter(Exception e)
        {
            Enter($"Encountered {e.GetType().ToString()} at {e.Source} \n Message: {e.Message} \n Data: {e.Data} \n ");
        }

        public void Enter(Exception e, string source)
        {
            Enter($"Encountered {e.GetType().ToString()} while {source} \n {e.Message} \n at: {e.Source} \n data: {e.Data}");
        }
        
    }
}
