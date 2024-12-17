using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Net.Mime;
using ASCOM.Common.Alpaca;
using System.Net;
using ASCOM.Common.DeviceInterfaces;
using EmbedIO.WebApi;
using System;
using EmbedIO;
using EmbedIO.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Alpaca.Controllers {

    public class SafetyMonitorController : WebApiController {
        public static readonly Guid Id = Guid.Parse("85EA7C77-F77A-4460-A345-F72A831F3750");
        private static uint txId = 0;
        private static Dictionary<uint, bool> connectionState = new Dictionary<uint, bool>();

        private const string BaseURL = "/api/v1/safetymonitor";
        private const int InterfaceVersion = 1; //ASCOM.Common.DeviceInterfaces.ISafetyMonitor

        public IProfileService ProfileService { get; }
        public ISafetyMonitorMediator DeviceMediator { get; }

        public SafetyMonitorController(IProfileService profileService, ISafetyMonitorMediator safetyMonitor) {
            ProfileService = profileService;
            DeviceMediator = safetyMonitor;
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
                if (AlpacaHelpers.IsDeviceIdenticalWithAlpacaService(ProfileService, DeviceMediator, SafetyMonitorController.Id)) {
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

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/issafe")]
        public IValueResponse<bool> GetIsSafe(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().IsSafe);
        }
    }
}