using System;

namespace ProcessMon {

    using Models;

    using Newtonsoft.Json;

    using NLog;

    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using NetMQ;
    using NetMQ.Sockets;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using NLog.Config;
    using NLog.Targets;
    using System.Threading;

    public partial class Program {

        #region Variables
        static Logger _logger;
        static ResponseSocket _ipcServer;

        volatile static bool _keepAlive;

        const string _lock = "u iz lawked";
        #endregion

        #region Consts
        public const string LinuxDefaultConfigDir = "/etc/procmon_net";
        public static readonly string WindowsDefaultConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "procmon_net");

        public const string ConfigFileResourceUri = "ProcessMon.Resources.ConfigFiles";
        public const string AppMonConfigTemplateName = "appmonitor.template.json";
        public const string AppConfigTemplateName = "procmon.json";
        public static readonly string DefaultAppMonConfigUri = $"{ ConfigFileResourceUri }.{ AppMonConfigTemplateName }";
        public static readonly string DefaultApplicationConfigUri = $"{ ConfigFileResourceUri }.{ AppConfigTemplateName }";

        public const string AppMonitorDir = "monitored_apps";
        public const string ProcMonConfigFile = ".procmon.json";

        public const ushort ApplicationPort = 56_587;
        #endregion

        #region Application Settings
        public static DirectoryInfo ConfigDirectory { get; private set; } = 
            Environment.OSVersion.Platform == PlatformID.Win32NT ? new DirectoryInfo(WindowsDefaultConfigDir) : new DirectoryInfo(LinuxDefaultConfigDir);

        public static DirectoryInfo ProcessMonitorDirectory { get; private set; }

        public static List<ProcessMonitor> MonitoredProcesses { get; private set; }

        public static RequestSocket IpcSocket { get; private set; }

        public static string BindAddress { get; set; } = "*";
        #endregion

        static void Main(string[] args) {
            var logConfig = new LoggingConfiguration();
            var fileLog = new FileTarget("Log file") { FileName = "procmon.log" };
            var consoleLog = new ConsoleTarget("Console log");

            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, fileLog);
            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, consoleLog);
            LogManager.Configuration = logConfig;

            HandleArguments(args);

            // Set up logger
            _logger = LogManager.GetCurrentClassLogger();

            _logger.Info("Checking for other instances...");
            if (IsInstanceRunning()) {
                _logger.Error("Another instance of ProcessMon is already running. Will exit.");
                return;
            }

            _logger.Info("Checking config directories...");
            // Set up directories
            CheckConfigDirectories();

            // Load process monitor files
            _logger.Info("Loading processes to monitor...");
            LoadProcesses();

            _logger.Info("Starting socket server...");
            _ipcServer = new ResponseSocket();
            _ipcServer.Bind($"tcp://{ BindAddress }:{ ApplicationPort }");
            Task.Run(MonitorIpcServer);

            _logger.Info("Beginning monitoring of {tasks} tasks", MonitoredProcesses.Count);
            StartMonitoringProcesses();

            _keepAlive = true;
            // Keep app alive
            while (_keepAlive) {

                Thread.Sleep(300);
            }

        }

        static void CheckConfigDirectories() {
            if (!ConfigDirectory.Exists) {
                ConfigDirectory.Create();
            }

            if (!ConfigDirectory.EnumerateDirectories().Any(x => x.Equals(AppMonitorDir) && x.Exists)) {
                ProcessMonitorDirectory = ConfigDirectory.CreateSubdirectory(AppMonitorDir);
                DumpDefaultConfigs(); // Will also re-create the application's main config file!
            } else ProcessMonitorDirectory = new DirectoryInfo(AppMonitorDir);
        }

        /// <summary>
        /// Dumps the application's default configs to their respective locations.
        /// </summary>
        static void DumpDefaultConfigs() {
            try {
                using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultAppMonConfigUri))
                using (var fileStream = ConfigDirectory.GetSubdirectory(AppMonitorDir).CreateFile(AppMonConfigTemplateName).OpenWrite()) {
                    resStream.CopyTo(fileStream);
                }
            } catch (IOException ex) {
                _logger.Error(ex.Message);
            }

            try {
                using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultApplicationConfigUri))
                using (var fileStream = ConfigDirectory.CreateFile(AppConfigTemplateName).OpenWrite()) {
                    resStream.CopyTo(fileStream);
                }
            } catch (IOException ex) {
                _logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// Attempts to load all configured processes in to memory.
        /// </summary>
        static void LoadProcesses() {
            if (MonitoredProcesses is default(List<ProcessMonitor>))
                MonitoredProcesses = new List<ProcessMonitor>();

            foreach (var file in ProcessMonitorDirectory.EnumerateFiles("appmonitor.*.json", SearchOption.AllDirectories)) {
                if (file.Name.Equals(AppMonConfigTemplateName, StringComparison.InvariantCultureIgnoreCase)) continue;
                _logger.Info("Found app {file}", file.Name);

                try {
                    MonitoredProcesses.Add(JsonConvert.DeserializeObject<ProcessMonitor>(File.ReadAllText(file.FullName)));
                } catch (JsonException ex) {
                    _logger.Error(ex, "An error occurred while parsing the config file for {file}", file.Name);
                    _logger.Error("Error: {err}", ex.Message);
                } catch (IOException ioEx) {
                    _logger.Error(ioEx, "Failed to read {file}", file.Name);
                }
            }
        }

        /// <summary>
        /// Checks whether an instance of this application is already running.
        /// </summary>
        static bool IsInstanceRunning() {
            try {
                using (var tcpPort = new TcpClient()) {
                    var result = tcpPort.BeginConnect("localhost", ApplicationPort, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(500); // Wait 500ms for a connection; abort otherwise.
                    tcpPort.EndConnect(result);

                    return success;
                }
            } catch (Exception ex) {
                return false;
            }

        }

        static void MonitorIpcServer() {
            using (_ipcServer) {
                while (_keepAlive) {
                    Msg receivedMsg = new Msg();
                    // Wait 500ms before timing out. Give the processor some time to do other things
                    try { if (!_ipcServer.TryReceive(ref receivedMsg, new TimeSpan(0, 0, 0, 0, 500 /*millis*/))) continue; }
                    catch { Thread.Sleep(100); continue; }

                    // TODO
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void StartMonitoringProcesses() {
            foreach (var proc in MonitoredProcesses) {
                _logger.Info("Found process {proc}", proc.MonitorName);
                Task.Run(async () => await proc.StartMonitoringAsync());
            }
        }

    }
}
