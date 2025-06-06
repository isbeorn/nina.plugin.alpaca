﻿using EmbedIO;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using NINA.Alpaca.Controllers;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Alpaca {

    public class SwanLogger : Swan.Logging.ILogger {
        public Swan.Logging.LogLevel LogLevel { get; set; }

        public void Dispose() {
        }

        public void Log(Swan.Logging.LogMessageReceivedEventArgs logEvent) {
            switch (logEvent.MessageType) {
                case Swan.Logging.LogLevel.Fatal:
                case Swan.Logging.LogLevel.Error:
                    Logger.Error(logEvent.Message);
                    break;

                case Swan.Logging.LogLevel.Warning:
                    Logger.Debug(logEvent.Message);
                    break;

                case Swan.Logging.LogLevel.Info:
                    Logger.Debug(logEvent.Message);
                    break;

                case Swan.Logging.LogLevel.Debug:
                    Logger.Debug(logEvent.Message);
                    break;

                case Swan.Logging.LogLevel.Trace:
                    Logger.Trace(logEvent.Message);
                    break;
            }
        }
    }

    public interface IServiceHost {
        public bool IsRunning { get; }

        void RunService(int alpacaPort,
                        IProfileService profileService,
                        ICameraMediator cameraMediator,
                        IFocuserMediator focuserMediator,
                        IFilterWheelMediator filterWheelMediator,
                        IRotatorMediator rotatorMediator,
                        ITelescopeMediator telescopeMediator,
                        ISwitchMediator switchMediator,
                        IFlatDeviceMediator flatDeviceMediator,
                        IWeatherDataMediator weatherMonitor,
                        IDomeMediator domeMediator,
                        ISafetyMonitorMediator safetyMonitor);

        void Stop();
    }

    public interface IServiceBackend {
    }

    public class ServiceHost : IServiceHost {
        private WebServer webServer;
        private CancellationTokenSource serviceToken;

        public bool IsRunning { get; private set; }

        public ServiceHost() {
            serviceToken = null;
        }

        private WebServer CreateWebServer(int alpacaPort,
                                          IProfileService profileService,
                                          ICameraMediator cameraMediator,
                                          IFocuserMediator focuserMediator,
                                          IFilterWheelMediator filterWheelMediator,
                                          IRotatorMediator rotatorMediator,
                                          ITelescopeMediator telescopeMediator,
                                          ISwitchMediator switchMediator,
                                          IFlatDeviceMediator flatDeviceMediator,
                                          IWeatherDataMediator weatherMonitor,
                                          IDomeMediator domeMediator,
                                          ISafetyMonitorMediator safetyMonitor) {
            Swan.Logging.Logger.RegisterLogger(new SwanLogger());

            return new WebServer(o => o
                .WithUrlPrefix($"http://*:{alpacaPort}/")
                .WithMode(HttpListenerMode.EmbedIO))
                .WithWebApi("/", MyResponseSerializerCallback, m => m
                    .WithController(() => new ManagementController())
                    .WithController(() => new CameraController(profileService, cameraMediator))
                    .WithController(() => new FocuserController(profileService, focuserMediator))
                    .WithController(() => new FilterWheelController(profileService, filterWheelMediator))
                    .WithController(() => new RotatorController(profileService, rotatorMediator))
                    .WithController(() => new TelescopeController(profileService, telescopeMediator))
                    .WithController(() => new SwitchController(profileService, switchMediator))
                    .WithController(() => new CoverCalibratorController(profileService, flatDeviceMediator))
                    .WithController(() => new WeatherDataController(profileService, weatherMonitor))
                    .WithController(() => new DomeController(profileService, domeMediator))
                    .WithController(() => new SafetyMonitorController(profileService, safetyMonitor))
                );
        }

        private async Task MyResponseSerializerCallback(IHttpContext context, object data) {
            var settings = new JsonSerializerSettings {
            };
            string jsonResponse = JsonConvert.SerializeObject(data, settings);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);

            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            await context.Response.OutputStream.FlushAsync();
        }

        public void RunService(int alpacaPort,
                               IProfileService profileService,
                               ICameraMediator cameraMediator,
                               IFocuserMediator focuserMediator,
                               IFilterWheelMediator filterWheelMediator,
                               IRotatorMediator rotatorMediator,
                               ITelescopeMediator telescopeMediator,
                               ISwitchMediator switchMediator,
                               IFlatDeviceMediator flatDeviceMediator,
                               IWeatherDataMediator weatherMonitor,
                               IDomeMediator domeMediator,
                               ISafetyMonitorMediator safetyMonitor) {
            if (IsRunning) {
                Logger.Trace("Alpaca Service already running during start attempt");
                return;
            }

            try {
                webServer = CreateWebServer(alpacaPort, profileService, cameraMediator, focuserMediator, filterWheelMediator, rotatorMediator, telescopeMediator, switchMediator, flatDeviceMediator, weatherMonitor, domeMediator, safetyMonitor);
                serviceToken = new CancellationTokenSource();
                var token = serviceToken.Token;
                IsRunning = true;
                webServer.RunAsync(token).ContinueWith(async task => {
                    if (task.Exception != null) {
                        Logger.Warning("Failed to start Alpaca Server - retry in 5 seconds");
                        try {
                            await Task.Delay(5000, token);
                        } catch (OperationCanceledException) {
                            return;
                        }

                        _ = webServer.RunAsync(token).ContinueWith(task => {
                            if (task.Exception != null) {
                                IsRunning = false;
                                if (task.Exception is AggregateException aggregateException && aggregateException.InnerException != null) {
                                    Logger.Error("Failed to start Alpaca Server", aggregateException.InnerException);
                                    Notification.ShowError("Failed to start Alpaca Server: " + aggregateException.InnerException.Message);
                                } else {
                                    Logger.Error("Failed to start Alpaca Server", task.Exception);
                                    Notification.ShowError("Failed to start Alpaca Server: " + task.Exception.ToString());
                                }
                            }
                        });
                    }
                });
            } catch (Exception ex) {
                Logger.Error("Failed to start Alpaca Server", ex);
                Notification.ShowError(string.Format(Loc.Instance["LblServerFailed"], ex.Message));
                IsRunning = false;
                throw;
            }
        }

        public void Stop() {
            if (webServer != null) {
                Logger.Info("Stopping Alpaca Service");
                try {
                    serviceToken?.Cancel();
                    Logger.Info("Alpaca Service stopped");
                } catch (Exception ex) {
                    Logger.Error("Failed to stop Alpaca Server", ex);
                } finally {
                    IsRunning = false;
                    try {
                        webServer?.Dispose();
                        serviceToken?.Dispose();
                    } catch { }
                    webServer = null;
                    serviceToken = null;
                }
            }
        }
    }
}