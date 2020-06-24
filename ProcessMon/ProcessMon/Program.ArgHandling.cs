using System;

namespace ProcessMon {

    using ProcessMon.Models;

    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;

    partial class Program {

        public const string ApplicationName = "ProcessMonitor (c) Simon Cahill";

        static List<AppArgument> Arguments { get; } = new List<AppArgument> {
            new AppArgument {
                ArgumentHandler = HandleHelp,
                AuxillaryArgument = "-h",
                IsCaseSensitive = false,
                IsSwitch = true,
                MainArgument = "--help"
            },
            new AppArgument {
                ArgumentHandler = HandleListMonitors,
                AuxillaryArgument = "-lm",
                MainArgument = "--list-monitors"
            },
            new AppArgument {
                ArgumentHandler = HandleStartAllMonitors,
                AuxillaryArgument = "-sa",
                MainArgument = "--start-all"
            },
            new AppArgument {
                ArgumentHandler = HandleStopAllMonitors,
                AuxillaryArgument = "-sta",
                MainArgument = "--stop-all"
            },
            new AppArgument {
                ArgumentHandler = HandleRestartAllMonitors,
                AuxillaryArgument = "-rsa",
                MainArgument = "--restart-all"
            },
            new AppArgument {
                ArgumentHandler = HandleAddMonitor,
                AuxillaryArgument = "-am",
                MainArgument = "add-monitor",
                IsSwitch = false
            },
            new AppArgument {
                ArgumentHandler = HandleRemoveMonitor,
                AuxillaryArgument = "-rm",
                MainArgument = "remove-monitor",
                IsSwitch = false
            },
            new AppArgument {
                ArgumentHandler = HandleKillMonitor,
                AuxillaryArgument = "-km",
                MainArgument = "kill-monitor",
                IsSwitch = false
            },
            new AppArgument {
                ArgumentHandler = HandleStopMonitor,
                MainArgument = "stop",
                IsSwitch = false
            },
            new AppArgument {
                ArgumentHandler = HandleStartMonitor,
                MainArgument = "start",
                IsSwitch = false
            },
            new AppArgument {
                ArgumentHandler = HandleProcessStatistics,
                MainArgument = "--get-stats",
                AuxillaryArgument = "-gs"
            }
        };

        static void HandleArguments(params string[] args) {
            foreach (var arg in args.Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x))) {
                Arguments.FirstOrDefault(x => x == arg)?.ArgumentHandler(arg);
            }
        }

        static void HandleHelp(string arg) {
            Console.WriteLine(ApplicationName);
            Console.WriteLine("{0,20}\t{1,-120}", "Argument", "Description");

            Console.WriteLine("Switches:");
            foreach (var appArg in Arguments.Where(x => x.IsSwitch)) {
                Console.WriteLine("{0,20}\t{1,-120}", appArg.MainArgument, appArg.HelpText ?? "No description available");
                if (!string.IsNullOrEmpty(appArg.AuxillaryArgument))
                    Console.WriteLine("{0,20}", appArg.AuxillaryArgument);
            }

            Console.WriteLine("Arguments:");
            foreach (var appArg in Arguments.Where(x => !x.IsSwitch)) {
                Console.WriteLine("{0,20}\t{1,-120}", $"{ appArg.MainArgument }=<val>", appArg.HelpText ?? "No description available");
                if (!string.IsNullOrEmpty(appArg.AuxillaryArgument))
                    Console.WriteLine("{0,20}", $"{ appArg.AuxillaryArgument }=<val>");
            }

            // Terminate application here!
            Environment.Exit(0);
        }

        static void HandleListMonitors(string arg) { }

        static void HandleStartAllMonitors(string arg) { }

        static void HandleStopAllMonitors(string arg) { }

        static void HandleRestartAllMonitors(string arg) { }

        static void HandleAddMonitor(string arg) { }

        static void HandleRemoveMonitor(string arg) { }

        static void HandleKillMonitor(string arg) { }

        static void HandleStopMonitor(string arg) { }

        static void HandleStartMonitor(string arg) { }

        static void HandleProcessStatistics(string arg) { }

    }
}
