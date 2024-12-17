using ASCOM.Alpaca.Discovery;
using ASCOM.Common.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.Alpaca {

    public class AscomLogger : ILogger {
        public LogLevel LoggingLevel { get; set; }

        public void Log(LogLevel level, string message) {
            switch (level) {
                case LogLevel.Fatal:
                    Logger.Debug(message);
                    break;

                case LogLevel.Error:
                    Logger.Debug(message);
                    break;

                case LogLevel.Debug:
                    Logger.Debug(message);
                    break;

                case LogLevel.Verbose:
                    Logger.Trace(message);
                    break;

                case LogLevel.Warning:
                    Logger.Debug(message);
                    break;

                case LogLevel.Information:
                    Logger.Debug(message);
                    break;

                default:
                    Logger.Trace(message);
                    break;
            }
        }

        public void SetMinimumLoggingLevel(LogLevel level) {
            LoggingLevel = level;
        }
    }

    public static class DiscoveryManager {

        public static Responder DiscoveryResponder {
            get;
            private set;
        }

        public static bool IsRunning => !DiscoveryResponder?.Disposed ?? false;

        public static void Start(int alpacaPort) {
            Logger.Debug("Starting discovery responder from defaults");
            var ipv6 = true;
            if (!Dns.GetHostAddresses(Dns.GetHostName()).Any(o => o.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)) {
                ipv6 = false;
            }

            DiscoveryResponder = new Responder(AlpacaPort: alpacaPort, IPv4: true, IPv6: ipv6, Logger: new AscomLogger()) {
                AllowRemoteAccess = true,
                LocalRespondOnlyToLocalHost = true
            };
        }

        public static void Stop() {
            DiscoveryResponder.Dispose();
        }
    }
}