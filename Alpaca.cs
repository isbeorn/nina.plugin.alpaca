using ASCOM.Com.DriverAccess;
using NINA.Alpaca.Properties;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Settings = NINA.Alpaca.Properties.Settings;

namespace NINA.Alpaca {

    /// <summary>
    /// This class exports the IPluginManifest interface and will be used for the general plugin information and options
    /// The base class "PluginBase" will populate all the necessary Manifest Meta Data out of the AssemblyInfo attributes. Please fill these accoringly
    ///
    /// An instance of this class will be created and set as datacontext on the plugin options tab in N.I.N.A. to be able to configure global plugin settings
    /// The user interface for the settings will be defined by a DataTemplate with the key having the naming convention "Alpaca_Options" where Alpaca corresponds to the AssemblyTitle - In this template example it is found in the Options.xaml
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class Alpaca : PluginBase, INotifyPropertyChanged {
        private readonly IPluginOptionsAccessor pluginSettings;
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly ISwitchMediator switchMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IWeatherDataMediator weatherMonitor;
        private readonly IDomeMediator domeMediator;
        private readonly ISafetyMonitorMediator safetyMonitor;
        private IServiceHost serviceHost;

        // Implementing a file pattern

        [ImportingConstructor]
        public Alpaca(IProfileService profileService,
                      IOptionsVM options,
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
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            // This helper class can be used to store plugin settings that are dependent on the current profile
            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.rotatorMediator = rotatorMediator;
            this.telescopeMediator = telescopeMediator;
            this.switchMediator = switchMediator;
            this.flatDeviceMediator = flatDeviceMediator;
            this.weatherMonitor = weatherMonitor;
            this.domeMediator = domeMediator;
            this.safetyMonitor = safetyMonitor;
            // React on a changed profile
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            serviceHost = new ServiceHost();
            RestartService();
        }

        public override Task Teardown() {
            // Make sure to unregister an event when the object is no longer in use. Otherwise garbage collection will be prevented.
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            DiscoveryManager.Stop();
            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RestartService();
        }

        public int AlpacaDevicePort {
            get {
                return pluginSettings.GetValueInt32(nameof(AlpacaDevicePort), 32323);
            }
            set {
                pluginSettings.SetValueInt32(nameof(AlpacaDevicePort), value);
                RaisePropertyChanged();

                RestartService();
            }
        }

        private void RestartService() {
            if (DiscoveryManager.IsRunning) {
                DiscoveryManager.Stop();
            }

            if (serviceHost.IsRunning) {
                serviceHost.Stop();
            }
            serviceHost.RunService(AlpacaDevicePort,
                                   profileService,
                                   cameraMediator,
                                   focuserMediator,
                                   filterWheelMediator,
                                   rotatorMediator,
                                   telescopeMediator,
                                   switchMediator,
                                   flatDeviceMediator,
                                   weatherMonitor,
                                   domeMediator,
                                   safetyMonitor);
            DiscoveryManager.Start(AlpacaDevicePort);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}