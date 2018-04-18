using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TeleBot.BotClient;
using TeleBot.Classes;

namespace TeleBot
{
    internal class Program
    {
        public static readonly string AppName = "TeleBot";
        public static readonly string AppVersion = GetVersion();
        public static readonly string AppNameWithVersion = string.Format("{0} v{1}", AppName, AppVersion);
        
        public static readonly string WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static readonly string DataDirectory = GetDataDirectory();

        public static readonly DateTime StartTime = DateTime.Now;

        private static void Main(string[] args)
        {
            // set debug level
            foreach(var arg in args)
            {
                if (arg == "--debug")
                    Log.ShowDebug = true;
            }

            // selamat datang
            Console.Title = AppNameWithVersion;
            var log = new Log("Main");
            log.Info(AppNameWithVersion);
            log.Debug("Working Directory: {0}", WorkingDirectory);
            log.Debug("Data Directory: {0}", DataDirectory);
            
            // bot menerima pesan
            while (true)
            {
                try
                {
                    Bot.StartReceivingMessage();
                    break;
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                    Task.Delay(10000).Wait();
                }
            }
            
            // tunggu key ctrl+c untuk keluar dari console
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
            exitEvent.WaitOne();
        }

        private static string GetVersion()
        {
            var attrb = typeof(Program)
                .GetTypeInfo()
                .Assembly
                .CustomAttributes
                .ToList()
                .FirstOrDefault(x => x.AttributeType.Name == "AssemblyFileVersionAttribute");
            var version = attrb?.ConstructorArguments[0].Value.ToString();
            if (string.IsNullOrWhiteSpace(version)) version = "0.0";
            return version;
        }

        private static string GetDataDirectory()
        {
            try
            {
                var linux = Path.Combine("/home/pi/.config", AppName);
                var windows = Path.Combine(WorkingDirectory, "Data");
                var isRunOnLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                var dataDir = isRunOnLinux ? linux : windows;
                
                // bikin directory kalo blm ada
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                return dataDir;
            }
            catch (Exception)
            {
                return WorkingDirectory;
            }
        }

        public static string FilePathInData(string filename)
        {
            return Path.Combine(DataDirectory, filename);
        }

        public static string FilePathInWorkingDir(string filename)
        {
            return Path.Combine(WorkingDirectory, filename);
        }
    }
}
