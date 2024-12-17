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
using NINA.Equipment.Interfaces;

namespace NINA.Alpaca.Controllers {

    public class SwitchController : WebApiController {
        public static readonly Guid Id = Guid.Parse("24B74FE8-F32E-4B05-A340-CDE69C92439D");
        private static uint txId = 0;
        private static Dictionary<uint, bool> connectionState = new Dictionary<uint, bool>();
        private const string BaseURL = "/api/v1/switch";
        private const int InterfaceVersion = 2; //ASCOM.Common.DeviceInterfaces.ISwitchV2

        public IProfileService ProfileService { get; }
        public ISwitchMediator DeviceMediator { get; }

        public SwitchController(IProfileService profileService, ISwitchMediator deviceMediator) {
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

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/maxswitch")]
        public IValueResponse<int> GetTempComp(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (DeviceMediator.GetDevice() as ISwitchHub).Switches.Count);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canwrite")]
        public IValueResponse<bool> GetCanWrite(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return (DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id) is IWritableSwitch;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/getswitch")]
        public IValueResponse<bool> GetGetSwitch(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return (DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id).Value > 0 ? true : false;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/getswitchdescription")]
        public IValueResponse<string> GetGetSwitchDescription(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return (DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id).Description;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/getswitchname")]
        public IValueResponse<string> GetGetSwitchName(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return (DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id).Name;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/getswitchvalue")]
        public IValueResponse<double> GetGetSwitchValue(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return (DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id).Value;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/minswitchvalue")]
        public IValueResponse<double> GetMinSwitchValue(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return ((DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id) as IWritableSwitch)?.Minimum ?? 0;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/maxswitchvalue")]
        public IValueResponse<double> GetMaxSwitchValue(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return ((DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id) as IWritableSwitch)?.Maximum ?? 0;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/switchstep")]
        public IValueResponse<double> GetMaxSwitchStep(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, int.MaxValue)] int Id,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                return ((DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id) as IWritableSwitch)?.StepSize ?? 0;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/setswitch")]
        public Task<IResponse> PutSetSwitch(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, int.MaxValue)] int Id,
            [Required][FormField] bool State,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }
                var theSwitch = ((DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id) as IWritableSwitch) ?? throw new ASCOM.NotImplementedException("SetSwitchValue");
                theSwitch.TargetValue = State ? 1 : 0;
                try {
                    await theSwitch.SetValue();
                } catch { }
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/setswitchvalue")]
        public Task<IResponse> PutSetSwitchValue(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, int.MaxValue)] int Id,
            [Required][FormField][Range(double.MinValue, double.MaxValue)] double Value,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                var hub = DeviceMediator.GetDevice() as ISwitchHub;
                if (Id < 0) { throw new ASCOM.InvalidValueException(); }
                if (Id >= hub.Switches.Count) { throw new ASCOM.InvalidValueException(); }

                var theSwitch = ((DeviceMediator.GetDevice() as ISwitchHub).Switches.ElementAt(Id) as IWritableSwitch) ?? throw new ASCOM.NotImplementedException("SetSwitchValue");
                if (Value > theSwitch.Maximum) { throw new ASCOM.InvalidValueException(); }
                if (Value < theSwitch.Minimum) { throw new ASCOM.InvalidValueException(); }

                theSwitch.TargetValue = Value;
                try {
                    await theSwitch.SetValue();
                } catch { }
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/setswitchname")]
        public IResponse PutSetSwitchName(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, int.MaxValue)] string Name,
            [Required][FormField][Range(double.MinValue, double.MaxValue)] double value,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, (Action)(() => { throw new ASCOM.NotImplementedException($"SetSwitchName ({Id})"); }));
        }
    }
}