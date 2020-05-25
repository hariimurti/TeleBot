using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeleBot.BotClass;
using TeleBot.Classes;
using TeleBot.Plugins;

namespace TeleBot
{
    internal class Program
    {
        public static readonly string AppName = "TeleBot";
        public static readonly string AppVersion = "v2.0";
        public static readonly string AppBuildVersion = GetBuildVersion();
        public static readonly string AppNameWithVersion =
            string.Format("{0} {1} build {2}", AppName, AppVersion, AppBuildVersion);

        private static readonly string WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private static readonly string DataDirectory = GetDataDirectory();

        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        public static readonly DateTime StartTime = DateTime.Now;

        private static async Task Main(string[] args)
        {
            // set debug level
            foreach (var arg in args)
                if (arg == "--debug")
                    Log.ShowDebug = true;

            // selamat datang
            Console.Title = AppNameWithVersion;
            var log = new Log("Main");
            log.Info(AppNameWithVersion);
            log.Debug("Working Directory: {0}", WorkingDirectory);
            log.Debug("Data Directory: {0}", DataDirectory);

            // buka konfigurasi
            var isBotConfigLoaded = Bot.Loaded();
            var isBotResponseLoaded = BotResponse.Loaded();
            if (!isBotConfigLoaded || !isBotResponseLoaded) Terminate(1);

            // bot menerima pesan
            while (true)
                try
                {
                    log.Debug("Mengakses akun bot...");

                    var receive = await BotClient.StartReceivingMessage();

                    if (receive) break;
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                    Task.Delay(10000).Wait();
                }

            // reSchedule
            Schedule.ReSchedule();

            // chatbot load library
            await ChatBot.LoadLibraryAsync();

            // tunggu key ctrl+c untuk keluar dari aplikasi
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
            exitEvent.WaitOne();

            // keluar dari aplikasi
            Terminate();
        }

        private static string GetBuildVersion()
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
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

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

        public static void Terminate(int code = 0)
        {
            new Log("Main").Info("Keluar dari aplikasi...");
            Environment.Exit(code);
        }
    }
}