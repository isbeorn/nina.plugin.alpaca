using ASCOM.Common.Alpaca;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using EmbedIO;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyDome;
using NINA.WPF.Base.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Astrometry;

namespace NINA.Alpaca.Controllers {

    internal class DomeController : WebApiController {
        public static readonly Guid Id = Guid.Parse("E9EE4B14-0933-44FF-9E87-4412D850AD76");
        private static uint txId = 0;
        private static Dictionary<uint, bool> connectionState = new Dictionary<uint, bool>();
        private static bool isMovingViaController;
        private const string BaseURL = "/api/v1/dome";
        private const int InterfaceVersion = 2; //ASCOM.Common.DeviceInterfaces.IDomeV2

        public IProfileService ProfileService { get; }
        public IDomeMediator DeviceMediator { get; }

        public DomeController(IProfileService profileService, IDomeMediator deviceMediator) {
            ProfileService = profileService;
            DeviceMediator = deviceMediator;
        }

        #region General_Ascom_Device

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/action")]
        public IResponse PutCommandAction(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Required] string Action,
            [FormField][Required] string Parameters,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.Action(Action, Parameters));
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/commandblind")]
        public IResponse PutCommandBlind(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Required] string Command,
            [FormField][Required] string Raw,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.SendCommandBlind(Command, Raw?.ToLower() == "true"));
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/commandbool")]
        public IResponse PutCommandBool(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Required] string Command,
            [FormField][Required] string Raw,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.SendCommandBool(Command, Raw?.ToLower() == "true"));
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/commandstring")]
        public IResponse PutCommandString(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Required] string Command,
            [FormField][Required] string Raw,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.SendCommandString(Command, Raw?.ToLower() == "true"));
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/connected")]
        public Task<IResponse> PutConnected(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Required] bool Connected,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (AlpacaHelpers.IsDeviceIdenticalWithAlpacaService(ProfileService, DeviceMediator, CameraController.Id)) {
                    throw new ASCOM.InvalidOperationException("The application cannot connect to its own hosted Alpaca device. Please ensure the host is accessed by other applications only.");
                }

                if (Connected && !DeviceMediator.GetInfo().Connected) {
                    try {
                        await DeviceMediator.Connect();
                    } catch (Exception) {
                        throw;
                    }
                }

                if (connectionState.ContainsKey(ClientID)) {
                    connectionState[ClientID] = Connected;
                } else {
                    connectionState.Add(ClientID, Connected);
                }
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/connected")]
        public IValueResponse<bool> GetConnected(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            connectionState.TryGetValue(ClientID, out var value2);
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => connectionState.TryGetValue(ClientID, out var value) ? value : false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/description")]
        public IValueResponse<string> GetDescription(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Description);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/driverinfo")]
        public IValueResponse<string> GetDriverInfo(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().DriverInfo);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/driverversion")]
        public IValueResponse<string> GetDriverVersion(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().DriverVersion);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/interfaceversion")]
        public IValueResponse<int> GetInterfaceVersion(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => InterfaceVersion);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/name")]
        public IValueResponse<string> GetName(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Name);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/supportedactions")]
        public IValueResponse<IList<string>> GetSupportedActions(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().SupportedActions);
        }

        #endregion General_Ascom_Device

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/altitude")]
        public IResponse GetAltitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/athome")]
        public IValueResponse<bool> GetAtHome(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().AtHome);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/atpark")]
        public IValueResponse<bool> GetAtPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().AtPark);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/azimuth")]
        public IValueResponse<double> GetAzimuth(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Azimuth);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canfindhome")]
        public IValueResponse<bool> GetCanFindHome(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanFindHome);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canpark")]
        public IValueResponse<bool> GetCanPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanPark);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetaltitude")]
        public IValueResponse<bool> GetCanSetAltitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetazimuth")]
        public IValueResponse<bool> GetCanSetAzimuth(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetAzimuth);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetpark")]
        public IValueResponse<bool> GetCanSetPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetPark);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetshutter")]
        public IValueResponse<bool> GetCanSetShutter(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetShutter);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canslave")]
        public IValueResponse<bool> GetCanSlave(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().DriverCanFollow);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansyncazimuth")]
        public IValueResponse<bool> GetCanSyncAzimuth(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSyncAzimuth);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/shutterstatus")]
        public IValueResponse<int> GetShutterStatus(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)DeviceMediator.GetInfo().ShutterStatus.ToASCOM());
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/slaved")]
        public IResponse GetSlaved(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (DeviceMediator.GetDevice() as IDome).DriverFollowing);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slaved")]
        public IResponse PutSlaved(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] bool Slaved,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (!DeviceMediator.GetInfo().DriverCanFollow) { throw new ASCOM.PropertyNotImplementedException("Slaved", true); }
                (DeviceMediator.GetDevice() as IDome).DriverFollowing = Slaved;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/slewing")]
        public IValueResponse<bool> GetSlewing(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Slewing || isMovingViaController);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/abortslew")]
        public IResponse PutAbortSlew(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                (DeviceMediator.GetDevice() as IDome).StopAll();
                isMovingViaController = false;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/closeshutter")]
        public IResponse PutCloseShutter(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                _ = DeviceMediator.CloseShutter(default).ContinueWith(t => isMovingViaController = false);
                isMovingViaController = true;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/findhome")]
        public IResponse PutFindHome(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                _ = DeviceMediator.FindHome(default).ContinueWith(t => isMovingViaController = false);
                isMovingViaController = true;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/openshutter")]
        public IResponse PutOpenShutter(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                _ = DeviceMediator.OpenShutter(default).ContinueWith(t => isMovingViaController = false);
                isMovingViaController = true;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/park")]
        public IResponse PutPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                _ = DeviceMediator.Park(default).ContinueWith(t => isMovingViaController = false);
                isMovingViaController = true;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/setpark")]
        public IResponse PutSetPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => (DeviceMediator.GetDevice() as IDome).SetPark());
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtoaltitude")]
        public IResponse PutSlewToAltitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtoazimuth")]
        public IResponse PutSlewToAzimuth(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, double.MaxValue)] double Azimuth,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (Azimuth < 0 || Azimuth > 360) { throw new ASCOM.InvalidValueException(); }

                _ = DeviceMediator.SlewToAzimuth(AstroUtil.EuclidianModulus(Azimuth, 360), default).ContinueWith(t => isMovingViaController = false);
                isMovingViaController = true;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/synctoazimuth")]
        public Task<IResponse> PutSyncToAzimuth(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, double.MaxValue)] double Azimuth,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                (DeviceMediator.GetDevice() as IDome).SyncToAzimuth(Azimuth);
                await Task.Delay(TimeSpan.FromSeconds(ProfileService.ActiveProfile.ApplicationSettings.DevicePollingInterval));
            });
        }
    }
}