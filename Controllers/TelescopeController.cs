using ASCOM.Common.Alpaca;
using ASCOM.Common.DeviceInterfaces;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using EmbedIO;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Astrometry;
using NINA.Core.Utility.Converters;
using NINA.Core.Enum;
using System.Globalization;
using static Google.Protobuf.Reflection.ExtensionRangeOptions.Types;
using ASCOM;
using NINA.Core.Utility;
using System.Threading;

namespace NINA.Alpaca.Controllers {

    internal class TelescopeController : WebApiController {
        public static readonly Guid Id = Guid.Parse("34BF2E73-6BA3-4E2E-ABB9-FFEB8808C870");
        private static uint txId = 0;
        private static CoverState coverState = CoverState.Unknown;
        private static Dictionary<uint, bool> connectionState = new Dictionary<uint, bool>();
        private static bool targetSet;
        private const string BaseURL = "/api/v1/telescope";
        private const int InterfaceVersion = 3; //ASCOM.Common.DeviceInterfaces.ITelescopeV3

        public IProfileService ProfileService { get; }
        public ITelescopeMediator DeviceMediator { get; }

        public TelescopeController(IProfileService profileService, ITelescopeMediator deviceMediator) {
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
                var syncDirection = ProfileService.ActiveProfile.TelescopeSettings.TelescopeLocationSyncDirection;

                ProfileService.ActiveProfile.TelescopeSettings.TelescopeLocationSyncDirection = TelescopeLocationSyncDirection.NOSYNC;
                try {
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
                } finally {
                    ProfileService.ActiveProfile.TelescopeSettings.TelescopeLocationSyncDirection = syncDirection;
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

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/alignmentmode")]
        public IValueResponse<int> GetAlignmentmode(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)DeviceMediator.GetInfo().AlignmentMode);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/altitude")]
        public IValueResponse<double> GetAltitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Altitude);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/aperturearea")]
        public IResponse GetApertureArea(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/aperturediameter")]
        public IResponse GetApertureDiameter(
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

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canpulseguide")]
        public IValueResponse<bool> GetCanPulseGuide(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanPulseGuide);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetdeclinationrate")]
        public IValueResponse<bool> GetCanSetDeclinationRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetDeclinationRate);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetguiderates")]
        public IValueResponse<bool> GetCanSetGuideRates(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                return false; // the setter is not exposed
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetpark")]
        public IValueResponse<bool> GetCanSetPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false); // the setter is not exposed
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetpierside")]
        public IValueResponse<bool> GetCanSetPierSide(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false); // the setter is not exposed
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetrightascensionrate")]
        public IValueResponse<bool> GetCanSetRightAscensionRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetRightAscensionRate);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansettracking")]
        public IValueResponse<bool> GetCanSetTracking(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetTrackingEnabled);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canslew")]
        public IValueResponse<bool> GetCanSlew(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSlew);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canslewaltaz")]
        public IValueResponse<bool> GetCanSlewAltAz(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false); // no good alt/az slew available at the moment
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canslewaltazasync")]
        public IValueResponse<bool> GetCanSlewAltAzAsync(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false); // no good alt/az slew available at the moment
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canslewasync")]
        public IValueResponse<bool> GetCanSlewAsync(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSlew);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansync")]
        public IValueResponse<bool> GetCanSync(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => true);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansyncaltaz")]
        public IValueResponse<bool> GetCanSyncAltAz(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canunpark")]
        public IValueResponse<bool> GetCanUnpark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                // this is currently not exposed - hence the reflection - but fallback to true, as most mounts should be able to do it
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("CanUnpark");
                if (prop != null) {
                    var val = prop.GetValue(mount);
                    return (bool)(val ?? true);
                }
                return true;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/declination")]
        public IValueResponse<double> GetDeclination(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Declination);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/declinationrate")]
        public IValueResponse<double> GetDeclinationRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("DeclinationRate");
                if (prop != null) {
                    return (double)prop.GetValue(mount);
                }
                return 0;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/declinationrate")]
        public Task<IResponse> PutDeclinationRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(double.MinValue, double.MaxValue)] double DeclinationRate,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("DeclinationRate");
                if (prop != null) {
                    prop.SetValue(mount, DeclinationRate);
                }
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/doesrefraction")]
        public IValueResponse<bool> GetDoesRefraction(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("DoesRefraction");
                if (prop != null) {
                    var val = prop.GetValue(mount);
                    return (bool)(val ?? false);
                }
                return false;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/doesrefraction")]
        public IResponse PutDoesRefraction(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] bool DoesRefraction,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/equatorialsystem")]
        public IValueResponse<int> GetEquatorialSystem(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)ToASCOMEquatorialSystem(DeviceMediator.GetInfo().EquatorialSystem));
        }

        private EquatorialCoordinateType ToASCOMEquatorialSystem(Epoch epoch) {
            return epoch switch {
                Epoch.JNOW => EquatorialCoordinateType.Topocentric,
                Epoch.B1950 => EquatorialCoordinateType.B1950,
                Epoch.J2000 => EquatorialCoordinateType.J2000,
                Epoch.J2050 => EquatorialCoordinateType.J2050,
                _ => EquatorialCoordinateType.J2000
            };
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/focallength")]
        public IValueResponse<double> GetFocalLength(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => ProfileService.ActiveProfile.TelescopeSettings.FocalLength);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/guideratedeclination")]
        public IValueResponse<double> GetGuideRateDeclination(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().GuideRateDeclinationArcsecPerSec / 3600.0);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/guideratedeclination")]
        public IResponse PutGuideRateDeclination(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/guideraterightascension")]
        public IValueResponse<double> GetGuideRateRightAscensionArcsecPerSec(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().GuideRateRightAscensionArcsecPerSec / 3600.0);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/guideraterightascension")]
        public IResponse PutGuideRateRightAscensionArcsecPerSec(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/ispulseguiding")]
        public IValueResponse<bool> GetIsPulseGuiding(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().IsPulseGuiding);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/rightascension")]
        public IValueResponse<double> GetRightAscension(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().RightAscension);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/rightascensionrate")]
        public IValueResponse<double> GetRightAscensionRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("RightAscensionRate");
                if (prop != null) {
                    return (double)prop.GetValue(mount);
                }
                return 0;
            });
        }

        public const double SIDEREAL_SEC_PER_SI_SEC = 1.00273791552838d;
        public const double SIDEREAL_RATE = 15.0d * SIDEREAL_SEC_PER_SI_SEC / 3600.0;

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/rightascensionrate")]
        public IResponse PutRightAscensionRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(double.MinValue, double.MaxValue)] double RightAscensionRate,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("RightAscensionRate");
                if (prop != null) {
                    prop.SetValue(mount, RightAscensionRate);
                }
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/sideofpier")]
        public IValueResponse<int> GetSideOfPier(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)ToASCOMSideOfPier(DeviceMediator.GetInfo().SideOfPier));
        }

        private PointingState ToASCOMSideOfPier(PierSide sideOfPier) {
            return sideOfPier switch {
                PierSide.pierEast => PointingState.Normal,
                PierSide.pierUnknown => PointingState.Unknown,
                PierSide.pierWest => PointingState.ThroughThePole,
                _ => PointingState.Unknown
            };
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/sideofpier")]
        public IResponse PutSideOfPier(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/siderealtime")]
        public IValueResponse<double> GetSiderealTime(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().SiderealTime);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/siteelevation")]
        public IValueResponse<double> GetSiteElevation(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().SiteElevation);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/siteelevation")]
        public IResponse PutSiteElevation(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/sitelatitude")]
        public IValueResponse<double> GetSiteLatitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().SiteLatitude);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/sitelatitude")]
        public IResponse PutSiteLatitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/sitelongitude")]
        public IValueResponse<double> GetSiteLongitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().SiteLongitude);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/sitelongitude")]
        public IResponse PutSiteLongitude(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/slewing")]
        public IValueResponse<bool> GetSlewing(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Slewing);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/slewsettletime")]
        public IValueResponse<int> GetSlewSettleTime(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => ProfileService.ActiveProfile.TelescopeSettings.SettleTime);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewsettletime")]
        public IResponse PutSlewSettleTime(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] int SlewSettleTime,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (SlewSettleTime < 0) { throw new InvalidValueException(); }
                ProfileService.ActiveProfile.TelescopeSettings.SettleTime = SlewSettleTime;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/targetdeclination")]
        public IValueResponse<double> GetTargetDeclination(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (!targetSet) { throw new ASCOM.PropertyNotImplementedException(); }
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("TargetDeclination");
                var dec = (double)prop.GetValue(mount);
                return dec;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/targetdeclination")]
        public Task<IResponse> PutTargetDeclination(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double TargetDeclination,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (TargetDeclination < -90 || TargetDeclination > 90) { throw new InvalidValueException("TargetDeclination", TargetDeclination.ToString(CultureInfo.InvariantCulture), "-90", "90"); }
                targetSet = true;
                Logger.Info($"Setting TargetDeclination to target {TargetDeclination}");
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("TargetDeclination");
                if (prop != null) {
                    prop.SetValue(mount, TargetDeclination);
                }
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/targetrightascension")]
        public IValueResponse<double> GetTargetRightAscension(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (!targetSet) { throw new ASCOM.PropertyNotImplementedException(); }
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("TargetRightAscension");
                var ra = (double)prop.GetValue(mount);
                return ra;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/targetrightascension")]
        public Task<IResponse> PutTargetRightAscension(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double TargetRightAscension,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (TargetRightAscension < 0 || TargetRightAscension > 24) { throw new InvalidValueException("TargetRightAscension", TargetRightAscension.ToString(CultureInfo.InvariantCulture), "0", "24"); }
                targetSet = true;
                Logger.Info($"Setting TargetRightAscension to target {TargetRightAscension}");
                // this is currently not exposed - hence the reflection
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                var t = mount.GetType();
                var prop = t.GetProperty("TargetRightAscension");
                if (prop != null) {
                    prop.SetValue(mount, TargetRightAscension);
                }
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/tracking")]
        public IValueResponse<bool> GetTracking(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().TrackingEnabled);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/tracking")]
        public IResponse PutTargetRightAscension(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] bool Tracking,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.SetTrackingEnabled(Tracking));
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/trackingrate")]
        public IValueResponse<int> GetTrackingRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)ToASCOMDriveRate(DeviceMediator.GetInfo().TrackingRate.TrackingMode));
        }

        private DriveRate ToASCOMDriveRate(TrackingMode trackingMode) {
            return trackingMode switch {
                TrackingMode.Sidereal => DriveRate.Sidereal,
                TrackingMode.Lunar => DriveRate.Lunar,
                TrackingMode.Solar => DriveRate.Solar,
                TrackingMode.King => DriveRate.King,
                TrackingMode.Custom => DriveRate.Sidereal,
                TrackingMode.Stopped => DriveRate.Sidereal,
                _ => DriveRate.Sidereal,
            };
        }

        private TrackingMode ToTrackingMode(DriveRate driveRate) {
            return driveRate switch {
                DriveRate.Sidereal => TrackingMode.Sidereal,
                DriveRate.Lunar => TrackingMode.Lunar,
                DriveRate.Solar => TrackingMode.Solar,
                DriveRate.King => TrackingMode.King,
                _ => throw new ASCOM.InvalidValueException(),
            };
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/trackingrate")]
        public IResponse PutTrackingRate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] int TrackingRate,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if ((DriveRate)TrackingRate != DriveRate.Sidereal) { throw new InvalidValueException(); }
                DeviceMediator.SetTrackingMode(ToTrackingMode((DriveRate)TrackingRate));
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/trackingrates")]
        public IValueResponse<DriveRate[]> GetTrackingRates(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => new DriveRate[] { DriveRate.Sidereal });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/utcdate")]
        public IValueResponse<string> GetUTCDate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().UTCDate.ToString("o", CultureInfo.InvariantCulture));
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/utcdate")]
        public IResponse PutUTCDate(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/abortslew")]
        public Task<IResponse> PutAbortSlew(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                throw new ASCOM.NotImplementedException();
                // if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                // DeviceMediator.StopSlew();
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/axisrates")]
        public IValueResponse<AxisRate[]> GetAxisRates(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField] int Axis,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (Axis < 0) { throw new ASCOM.InvalidValueException(); }
                if (Axis > 2) { throw new ASCOM.InvalidValueException(); }
                var rates = Axis == 0 ? DeviceMediator.GetInfo().PrimaryAxisRates : DeviceMediator.GetInfo().SecondaryAxisRates;
                return rates.Select(x => new AxisRate(x.Item1, x.Item2)).ToArray();
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canmoveaxis")]
        public IValueResponse<bool> GetCanMoveAxis(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField] int Axis,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (Axis < 0) { throw new ASCOM.InvalidValueException(); }
                if (Axis > 2) { throw new ASCOM.InvalidValueException(); }
                if (Axis == 2) { return false; }
                return Axis == 0 ? DeviceMediator.GetInfo().CanMovePrimaryAxis : DeviceMediator.GetInfo().CanMoveSecondaryAxis;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/destinationsideofpier")]
        public IValueResponse<int> GetDestinationSideOfPier(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][QueryField] double RightAscension,
            [Required][QueryField] double Declination,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                var mount = (DeviceMediator.GetDevice() as ITelescope);
                return (int)ToASCOMSideOfPier(mount.DestinationSideOfPier(new Coordinates(Angle.ByHours(RightAscension), Angle.ByDegree(Declination), mount.EquatorialSystem)));
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/destinationsideofpier")]
        public IResponse PutDestinationSideOfPier(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double RightAscension,
            [Required][FormField] double Declination,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/findhome")]
        public Task<IResponse> PutFindHome(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                await DeviceMediator.FindHome(default, default);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/moveaxis")]
        public Task<IResponse> PutMoveAxis(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] int Axis,
            [Required][FormField] double Rate,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                if (Axis < 0) { throw new ASCOM.InvalidValueException(); }
                if (Axis > 1) { throw new ASCOM.InvalidValueException(); }
                var rates = Axis == 0 ? DeviceMediator.GetInfo().PrimaryAxisRates : DeviceMediator.GetInfo().SecondaryAxisRates;
                var maxPrimary = rates.MaxBy(x => x.Item2);
                var minPrimary = rates.MinBy(x => x.Item1);
                if (Math.Abs(Rate) < minPrimary.Item1) { throw new InvalidValueException(); }
                if (Math.Abs(Rate) > maxPrimary.Item2) { throw new InvalidValueException(); }
                DeviceMediator.MoveAxis(Axis == 0 ? TelescopeAxes.Primary : TelescopeAxes.Secondary, Rate);
                if (Math.Abs(Rate) > 0) {
                    using (var ct = new CancellationTokenSource(2000)) {
                        try {
                            while (!DeviceMediator.GetInfo().Slewing) {
                                await Task.Delay(10, ct.Token);
                            }
                        } catch { }
                    }
                }
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/park")]
        public Task<IResponse> PutPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.ParkTelescope(default, default));
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/pulseguide")]
        public Task<IResponse> PutPulseGuide(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] int Direction,
            [Required][FormField] int Duration,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                var guideDireciton = Direction switch {
                    0 => GuideDirections.guideNorth,
                    1 => GuideDirections.guideSouth,
                    2 => GuideDirections.guideEast,
                    3 => GuideDirections.guideWest,
                    _ => throw new ASCOM.InvalidValueException()
                };
                DeviceMediator.PulseGuide(guideDireciton, Duration);
                using (var ct = new CancellationTokenSource(2000)) {
                    try {
                        while (!DeviceMediator.GetInfo().IsPulseGuiding) {
                            await Task.Delay(100, ct.Token);
                        }
                    } catch { }
                }
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/setpark")]
        public IResponse PutSetPark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtoaltaz")]
        public Task<IResponse> PutSlewToAltAz(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double Azimuth,
            [Required][FormField] double Altitude,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                throw new ASCOM.NotImplementedException();
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                if (Azimuth < 0 || Azimuth > 360) { throw new InvalidValueException("Azimuth", Azimuth.ToString(CultureInfo.InvariantCulture), "0", "360"); }
                if (Altitude < -90 || Altitude > 90) { throw new InvalidValueException("Altitude", Altitude.ToString(CultureInfo.InvariantCulture), "-90", "90"); }

                await DeviceMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(
                    Angle.ByDegree(Azimuth),
                    Angle.ByDegree(Altitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Latitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Longitude)), default);
                await DeviceMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(
                    Angle.ByDegree(Azimuth),
                    Angle.ByDegree(Altitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Latitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Longitude)), default);
                DeviceMediator.SetTrackingEnabled(false);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtoaltazasync")]
        public Task<IResponse> PutSlewToAltAzAsync(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double Azimuth,
            [Required][FormField] double Altitude,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                throw new ASCOM.NotImplementedException();
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                if (Azimuth < 0 || Azimuth > 360) { throw new InvalidValueException("Azimuth", Azimuth.ToString(CultureInfo.InvariantCulture), "0", "360"); }
                if (Altitude < -90 || Altitude > 90) { throw new InvalidValueException("Altitude", Altitude.ToString(CultureInfo.InvariantCulture), "-90", "90"); }

                await DeviceMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(
                    Angle.ByDegree(Azimuth),
                    Angle.ByDegree(Altitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Latitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Longitude)), default);
                await DeviceMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(
                    Angle.ByDegree(Azimuth),
                    Angle.ByDegree(Altitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Latitude),
                    Angle.ByDegree(ProfileService.ActiveProfile.AstrometrySettings.Longitude)), default);
                DeviceMediator.SetTrackingEnabled(false);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtocoordinates")]
        public Task<IResponse> PutSlewToCoordinates(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double RightAscension,
            [Required][FormField] double Declination,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                if (RightAscension < 0 || RightAscension > 24) { throw new InvalidValueException("RightAscension", RightAscension.ToString(CultureInfo.InvariantCulture), "0", "24"); }
                if (Declination < -90 || Declination > 90) { throw new InvalidValueException("Declination", Declination.ToString(CultureInfo.InvariantCulture), "-90", "90"); }
                await DeviceMediator.SlewToCoordinatesAsync(new Coordinates(Angle.ByHours(RightAscension), Angle.ByDegree(Declination), DeviceMediator.GetInfo().EquatorialSystem), default);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtocoordinatesasync")]
        public Task<IResponse> PutSlewToCoordinatesAsync(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double RightAscension,
            [Required][FormField] double Declination,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                if (RightAscension < 0 || RightAscension > 24) { throw new InvalidValueException("RightAscension", RightAscension.ToString(CultureInfo.InvariantCulture), "0", "24"); }
                if (Declination < -90 || Declination > 90) { throw new InvalidValueException("Declination", Declination.ToString(CultureInfo.InvariantCulture), "-90", "90"); }
                await DeviceMediator.SlewToCoordinatesAsync(new Coordinates(Angle.ByHours(RightAscension), Angle.ByDegree(Declination), DeviceMediator.GetInfo().EquatorialSystem), default);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtotarget")]
        public Task<IResponse> PutSlewToTarget(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                var ra = GetTargetRightAscension(DeviceNumber).Value;
                var dec = GetTargetDeclination(DeviceNumber).Value;
                if (ra < 0 || ra > 24) { throw new InvalidValueException("RightAscension", ra.ToString(CultureInfo.InvariantCulture), "0", "24"); }
                if (dec < -90 || dec > 90) { throw new InvalidValueException("Declination", dec.ToString(CultureInfo.InvariantCulture), "-90", "90"); }
                await DeviceMediator.SlewToCoordinatesAsync(new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), DeviceMediator.GetInfo().EquatorialSystem), default);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/slewtotargetasync")]
        public Task<IResponse> PutSlewToTargetAsync(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                var ra = GetTargetRightAscension(DeviceNumber).Value;
                var dec = GetTargetDeclination(DeviceNumber).Value;
                if (ra < 0 || ra > 24) { throw new InvalidValueException("RightAscension", ra.ToString(CultureInfo.InvariantCulture), "0", "24"); }
                if (dec < -90 || dec > 90) { throw new InvalidValueException("Declination", dec.ToString(CultureInfo.InvariantCulture), "-90", "90"); }
                await DeviceMediator.SlewToCoordinatesAsync(new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), DeviceMediator.GetInfo().EquatorialSystem), default);
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/synctoaltaz")]
        public IResponse PutSyncToAltAz(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/synctocoordinates")]
        public Task<IResponse> PutSyncToCoordinates(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] double RightAscension,
            [Required][FormField] double Declination,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                if (RightAscension < 0 || RightAscension > 24) { throw new InvalidValueException("RightAscension", RightAscension.ToString(CultureInfo.InvariantCulture), "0", "24"); }
                if (Declination < -90 || Declination > 90) { throw new InvalidValueException("Declination", Declination.ToString(CultureInfo.InvariantCulture), "-90", "90"); }
                await DeviceMediator.Sync(new Coordinates(Angle.ByHours(RightAscension), Angle.ByDegree(Declination), DeviceMediator.GetInfo().EquatorialSystem));
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/synctotarget")]
        public Task<IResponse> PutSyncToTarget(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, async () => {
                if (DeviceMediator.GetInfo().AtPark) { throw new ASCOM.InvalidOperationException(); }
                var ra = GetTargetRightAscension(DeviceNumber);
                var dec = GetTargetDeclination(DeviceNumber);
                Logger.Info($"Syncing to target {ra} - {dec}");
                await DeviceMediator.Sync(new Coordinates(Angle.ByHours(ra.Value), Angle.ByDegree(dec.Value), DeviceMediator.GetInfo().EquatorialSystem));
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/unpark")]
        public Task<IResponse> PutUnpark(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.UnparkTelescope(default, default));
        }
    }
}