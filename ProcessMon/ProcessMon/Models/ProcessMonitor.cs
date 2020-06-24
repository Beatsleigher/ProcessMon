using System;

namespace ProcessMon.Models {

    using Newtonsoft.Json;
    using NLog;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A monitor object for each enabled process to be monitored by the application.
    /// </summary>
    public class ProcessMonitor: IDisposable {

        #region Instance Variables
        public int ChildPid => ChildProcess?.Id ?? -1;

        public bool IsMonitoring { get; private set; }

        public Process ChildProcess { get; private set; }

        public bool IsProcessRunning => (ChildPid >= 0 || IsProcessRunningExternally());

        private object lockObj = "u iz lawked :3";
        private bool _restartProcess = true;
        private TextWriter _outputStream;
        private TextWriter _errorStream;
        private Logger _logger;
        #endregion

        #region JSON Properties
        [JsonProperty("Name")]
        public string MonitorName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Executable")]
        public string Executable { get; set; }

        [JsonProperty("ProcessArgs")]
        public List<string> ProcessArgs { get; set; }

        [JsonProperty("StdoutFile")]
        public string StdOutFile { get; set; }

        [JsonProperty("StderrFile")]
        public string StdErrFile { get; set; }

        [JsonProperty("WorkingDirectory")]
        public string WorkingDirectory { get; set; }

        [JsonProperty("Priority")]
        public ProcessPriorityClass ProcessPriority { get; set; }

        [JsonProperty("Niceness")]
        public int ProcessNicesness { get; set; }

        [JsonProperty("RunAsUser")]
        public string DesiredUser { get; set; }

        [JsonProperty("RunOnce")]
        public bool RunOnce { get; set; }

        [JsonProperty("NoRestartAfterSignal")]
        public List<int> IgnoreSignals { get; set; }

        [JsonProperty("EnvironmentArgs")]
        public Dictionary<string, string> EnvironmentArgs { get; set; }
        #endregion

        #region Process Event Handlers
        private void ChildProcess_ProcessExited(object sender, EventArgs e) {
            using (_outputStream)
            using (_errorStream) { } // Make sure to dispose of the streams
            if (_restartProcess) StartProcess();
        }

        private void ChildProcess_ProcessOutputReceived(object sender, DataReceivedEventArgs e) {
            _outputStream?.WriteLine(e.Data);
        }

        private void ChildProcess_ProcessErrorReceived(object sender, DataReceivedEventArgs e) {
            _errorStream?.WriteLine(e.Data);
        }
        #endregion

        public ProcessMonitor() {
        }

        protected ProcessStartInfo GetProcessStartInfo() {
            var procStartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = Executable,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UserName = DesiredUser ?? null,
                UseShellExecute = false,
                WorkingDirectory = WorkingDirectory
            };

            // Add arguments
            ProcessArgs.ForEach(arg => procStartInfo.ArgumentList.Add(arg));

            return procStartInfo;
        }

        protected int StartProcess() {
            ChildProcess = new Process {
                StartInfo = GetProcessStartInfo(),
                EnableRaisingEvents = true,
                PriorityClass = ProcessPriority
            };

            // Open streams to respective output files
            if (!string.IsNullOrEmpty(StdOutFile))
                _outputStream = new StreamWriter(File.OpenWrite(StdOutFile));
            if (!string.IsNullOrEmpty(StdErrFile))
                _errorStream = new StreamWriter(File.OpenWrite(StdErrFile));

            if (ChildProcess.Start()) {
                ChildProcess.Exited += ChildProcess_ProcessExited;
                ChildProcess.ErrorDataReceived += ChildProcess_ProcessErrorReceived;
                ChildProcess.OutputDataReceived += ChildProcess_ProcessOutputReceived;
                ChildProcess.BeginErrorReadLine();
                ChildProcess.BeginOutputReadLine();
            }

            // after restart
            return ChildPid;
        }

        protected void StopProcess(bool restart = false) {
            lock (lockObj) { 
                _restartProcess = restart;

                ChildProcess.Kill(true);
            }
        }

        public void RestartProcess() => StopProcess(true);

        public async Task StartMonitoringAsync() {
            IsMonitoring = true;
            _logger = LogManager.GetLogger($"ProcessMonitor:{ MonitorName }");

            _logger.Info("Beginning monitoring of {procname}...", MonitorName);
            while (IsMonitoring) {

                if (!IsProcessRunning && !BindToExternalProcess()) {
                    _logger.Info("Starting {procName}...", MonitorName);
                    if (StartProcess() > 0) {
                        _logger.Info("Process started with PID {pid}", ChildPid);
                    } else _logger.Error("Process was not started!");
                }

                // Loop and sleep while the process is running
                while (!ChildProcess.HasExited) { Thread.Sleep(50); /* Sleep 50 millis */ }
                _logger.Warn("{procname} has exited.", MonitorName);

                if (RunOnce || !_restartProcess) break;

            }

            _logger.Info("Ending monitoring.");
        }

        public void EndMonitoring() {
            IsMonitoring = false;
        }

        public bool IsProcessRunningExternally() => Process.GetProcesses().Count(x => x.ProcessName.Equals(Executable, StringComparison.InvariantCultureIgnoreCase)) >= 1;

        public bool BindToExternalProcess() {
            if (!IsProcessRunningExternally()) return false;

            return (ChildProcess = Process.GetProcessesByName(Executable).FirstOrDefault()) is default(Process);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    ChildProcess.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ProcessMonitor()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
