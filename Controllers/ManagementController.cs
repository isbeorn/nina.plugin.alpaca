using ASCOM.Alpaca.Discovery;
using ASCOM.Common.Alpaca;
using ASCOM.Common.DeviceInterfaces;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NINA.Alpaca.Controllers {

    public class ManagementController : WebApiController {

        public ManagementController() {
        }

        private uint txId = 0;

        [Route(EmbedIO.HttpVerbs.Get, "/management/apiversions")]
        public IntListResponse GetApiVersions(
            [Range(0, 4294967295)] uint ClientID = 0,
            [Range(0, 4294967295)] uint ClientTransactionID = 0) {
            return new IntListResponse(ClientTransactionID, txId++, new List<int> { 1 });
        }

        [Route(EmbedIO.HttpVerbs.Get, "/management/v1/description")]
        public AlpacaDescriptionResponse GetDescription(
            [Range(0, 4294967295)] uint ClientID = 0,
            [Range(0, 4294967295)] uint ClientTransactionID = 0) {
            return new AlpacaDescriptionResponse(ClientTransactionID, txId++, new AlpacaDeviceDescription("N.I.N.A. Alpaca Server", "", "", ""));
        }

        [Route(EmbedIO.HttpVerbs.Get, "/management/v1/configureddevices")]
        public AlpacaConfiguredDevicesResponse GetConfiguredDevices(
            [Range(0, 4294967295)] uint ClientID = 0,
            [Range(0, 4294967295)] uint ClientTransactionID = 0) {
            var devices = new List<AlpacaConfiguredDevice> {
                new AlpacaConfiguredDevice("N.I.N.A. Camera", "Camera", 0, CameraController.Id.ToString()),
                new AlpacaConfiguredDevice("N.I.N.A. Filter Wheel", "FilterWheel", 0, FilterWheelController.Id.ToString()),
                new AlpacaConfiguredDevice("N.I.N.A. Weather Device", "ObservingConditions", 0, WeatherDataController.Id.ToString()),
                new AlpacaConfiguredDevice("N.I.N.A. Safety Monitor", "SafetyMonitor", 0, SafetyMonitorController.Id.ToString())
            };
            return new AlpacaConfiguredDevicesResponse(ClientTransactionID, txId++, devices);
        }
    }
}