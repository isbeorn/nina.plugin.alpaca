using ASCOM.Common.Alpaca;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using EmbedIO;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using static NINA.Equipment.Model.CaptureSequence;
using Accord.Math.Comparers;
using Google.Protobuf.WellKnownTypes;
using NINA.Core.Model;
using ASCOM.Common.DeviceInterfaces;
using NINA.Image.Interfaces;
using ASCOM;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Equipment.Equipment.MyCamera;

namespace NINA.Alpaca.Controllers {

    public class CameraController : WebApiController, ICameraConsumer {
        public static readonly Guid Id = Guid.Parse("04C7F3FA-2100-418E-9253-8E21AF6317E6");
        private static uint txId = 0;
        private static Dictionary<uint, bool> connectionState = new Dictionary<uint, bool>();
        private static Task exposureTask;
        private static bool isExposing;
        private static IExposureData lastExposure;
        private static int? overrideGain;
        private static int? overrideOffset;
        private static int overrideStartX;
        private static int overrideStartY;
        private static int overrideNumX;
        private static int overrideNumY;
        private const string BaseURL = "/api/v1/camera";
        private const int InterfaceVersion = 3; //ASCOM.Common.DeviceInterfaces.ICamera

        public IProfileService ProfileService { get; }
        public ICameraMediator DeviceMediator { get; }

        public CameraController(IProfileService profileService, ICameraMediator deviceMediator) {
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
                if (AlpacaHelpers.IsDeviceIdenticalWithAlpacaService(ProfileService, DeviceMediator)) {
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

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/bayeroffsetx")]
        public IValueResponse<short> GetBayerOffsetX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (DeviceMediator.GetInfo().SensorType == Core.Enum.SensorType.Monochrome) {
                    throw new ASCOM.NotImplementedException();
                }
                return DeviceMediator.GetInfo().BayerOffsetX;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/bayeroffsety")]
        public IValueResponse<short> GetBayerOffsetY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (DeviceMediator.GetInfo().SensorType == Core.Enum.SensorType.Monochrome) {
                    throw new ASCOM.NotImplementedException();
                }
                return DeviceMediator.GetInfo().BayerOffsetY;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/binx")]
        public IValueResponse<short> GetBinX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().BinX);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/binx")]
        public IResponse PutBinX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, short.MaxValue)] short BinX = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (BinX < 1) { throw new ASCOM.InvalidValueException("BinX", BinX.ToString(), "> 0"); }
                var maxBin = DeviceMediator.GetInfo().BinningModes.MaxBy(x => x.X).X;
                if (BinX > maxBin) { throw new ASCOM.InvalidValueException("BinX", BinX.ToString(), $"<= {maxBin}"); }
                DeviceMediator.SetBinning(BinX, DeviceMediator.GetInfo().BinX);
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/biny")]
        public IValueResponse<short> GetBinY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().BinY);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/biny")]
        public IResponse PutBinY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, short.MaxValue)] short BinY = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (BinY < 1) { throw new ASCOM.InvalidValueException("BinY", BinY.ToString(), $"> 0"); }
                var maxBin = DeviceMediator.GetInfo().BinningModes.MaxBy(x => x.Y).Y;
                if (BinY > maxBin) { throw new ASCOM.InvalidValueException("BinY", BinY.ToString(), $"<= {maxBin}"); }
                DeviceMediator.SetBinning(DeviceMediator.GetInfo().BinX, BinY);
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/camerastate")]
        public IValueResponse<int> GetCameraState(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientID, txId++, () => {
                if (!DeviceMediator.IsFreeToCapture(this)) {
                    return (int)CameraState.Exposing;
                }
                return isExposing ? (int)CameraState.Exposing : (int)CameraState.Idle;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cameraxsize")]
        public IResponse GetXSize(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().XSize);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cameraysize")]
        public IResponse GetYSize(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().YSize);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canabortexposure")]
        public IValueResponse<bool> GetCanAbort(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canasymmetricbin")]
        public IValueResponse<bool> GetCanAsymmetricBin(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canfastreadout")]
        public IValueResponse<bool> GetCanFastReadout(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cangetcoolerpower")]
        public IValueResponse<bool> GetCanGetCoolerPower(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => !double.IsNaN(DeviceMediator.GetInfo().CoolerPower));
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canpulseguide")]
        public IValueResponse<bool> GetCanPulseGuide(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cansetccdtemperature")]
        public IValueResponse<bool> GetCanSetCCDTemperature(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CanSetTemperature);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/canstopexposure")]
        public IValueResponse<bool> GetCanStopExposure(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/ccdtemperature")]
        public IValueResponse<double> GetTemperature(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Temperature);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/cooleron")]
        public IValueResponse<bool> GetCoolerOn(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().CoolerOn);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/cooleron")]
        public IValueResponse<bool> PutCoolerOn(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] bool CoolerOn,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (DeviceMediator.GetDevice() as ICamera).CoolerOn = CoolerOn);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/coolerpower")]
        public IValueResponse<double> GetCoolerPower(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (double.IsNaN(DeviceMediator.GetInfo().CoolerPower)) {
                    throw new ASCOM.PropertyNotImplementedException();
                }
                return DeviceMediator.GetInfo().CoolerPower;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/electronsperadu")]
        public IValueResponse<double> GetElectronsPerADU(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (double.IsNaN(DeviceMediator.GetInfo().ElectronsPerADU)) {
                    throw new ASCOM.PropertyNotImplementedException();
                }
                return DeviceMediator.GetInfo().ElectronsPerADU;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/exposuremax")]
        public IValueResponse<double> GetExposureMax(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().ExposureMax);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/exposuremin")]
        public IValueResponse<double> GetExposureMin(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().ExposureMin);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/exposureresolution")]
        public IValueResponse<double> GetExposureResolution(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().ExposureMin);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/fastreadout")]
        public Task<IResponse> GetFastReadout(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                throw new ASCOM.PropertyNotImplementedException();
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/fastreadout")]
        public IResponse PutFastReadout(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField] bool FastReadout,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/fullwellcapacity")]
        public IResponse GetFullWellCapacity(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/gain")]
        public IValueResponse<int> GetGain(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => overrideGain.HasValue ? overrideGain.Value : DeviceMediator.GetInfo().Gain);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/gain")]
        public IResponse PutGain(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(-1, int.MaxValue)] int Gain,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (Gain < DeviceMediator.GetInfo().GainMin) { throw new ASCOM.InvalidValueException(); }
                if (Gain > DeviceMediator.GetInfo().GainMax) { throw new ASCOM.InvalidValueException(); }
                overrideGain = Gain;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/gainmax")]
        public IValueResponse<int> GetMaxGain(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().GainMax);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/gainmin")]
        public IValueResponse<int> GetMinGain(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().GainMin);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/gains")]
        public IResponse GetGains(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
            //return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Gains.Select(x => x.ToString()).ToList());
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/hasshutter")]
        public IValueResponse<bool> GetHasShutter(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().HasShutter);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/heatsinktemperature")]
        public IValueResponse<double> GetHeatSinkTemperature(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().Temperature);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/imagearray")]
        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/imagearrayvariant")]
        public async Task<IResponse> GetImageArray(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            if (exposureTask is null && lastExposure is null) { return new EmptyResponse(ClientTransactionID, txId++, AlpacaErrors.InvalidOperationException, "Image is not ready"); }
            if (exposureTask is not null) {
                exposureTask = null;
                isExposing = false;
                lastExposure = await DeviceMediator.Download(default);
            }
            var imageData = await lastExposure.ToImageData();
            return new ImageResponse(imageData.Data.FlatArray, imageData.Properties.Width, imageData.Properties.Height, ClientTransactionID, txId++, AlpacaErrors.AlpacaNoError, string.Empty);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/imageready")]
        public IValueResponse<bool> GetImageReady(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => exposureTask?.IsCompleted ?? false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/ispulseguiding")]
        public IValueResponse<bool> GetIsPulseGuiding(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => false);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/lastexposureduration")]
        public IResponse GetLastExposureDuration(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            // TODO!
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/lastexposurestarttime")]
        public IResponse GetLastExposureStartTime(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            // TODO!
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/maxadu")]
        public IResponse GetMaxADU(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/maxbinx")]
        public IValueResponse<short> GetMaxBinX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().BinningModes.MaxBy(x => x.X).X);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/maxbiny")]
        public IValueResponse<short> GetMaxBinY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().BinningModes.MaxBy(x => x.Y).Y);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/numx")]
        public IValueResponse<int> GetNumX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().XSize);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/numx")]
        public IResponse PutNumX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, int.MaxValue)] int NumX = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                overrideNumX = NumX;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/numy")]
        public IValueResponse<int> GetNumY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().YSize);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/numy")]
        public IResponse PutNumY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, int.MaxValue)] int NumY = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                overrideNumY = NumY;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/offset")]
        public IValueResponse<int> GetOffset(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => overrideOffset.HasValue ? overrideOffset.Value : DeviceMediator.GetInfo().Offset);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/offset")]
        public IResponse PutOffset(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(-1, int.MaxValue)] int Offset,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (Offset < DeviceMediator.GetInfo().OffsetMin) { throw new ASCOM.InvalidValueException(); }
                if (Offset > DeviceMediator.GetInfo().OffsetMax) { throw new ASCOM.InvalidValueException(); }
                overrideOffset = Offset;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/offsetmax")]
        public IValueResponse<int> GetOffsetMax(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().OffsetMax);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/offsetmin")]
        public IValueResponse<int> GetOffsetMin(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().OffsetMin);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/offsets")]
        public IResponse GetOffsets(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/percentcompleted")]
        public IResponse GetPercentCompleted(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/pixelsizex")]
        public IValueResponse<double> GetPixelSizeX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().PixelSize);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/pixelsizey")]
        public IValueResponse<double> GetPixelSizeY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().PixelSize);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/readoutmode")]
        public IValueResponse<int> GetReadoutMode(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)DeviceMediator.GetInfo().ReadoutMode);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/readoutmode")]
        public IResponse GetReadoutMode(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, int.MaxValue)] int ReadoutMode = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => DeviceMediator.SetReadoutMode((short)ReadoutMode));
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/readoutmodes")]
        public IValueResponse<List<string>> GetReadoutModes(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => DeviceMediator.GetInfo().ReadoutModes.ToList());
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/sensorname")]
        public IValueResponse<string> GetSensorName(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => string.Empty);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/sensortype")]
        public IValueResponse<int> GetSensorType(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => (int)DeviceMediator.GetInfo().SensorType);
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/setccdtemperature")]
        public IValueResponse<double> GetSetCCDTemperature(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => {
                if (double.IsNaN(DeviceMediator.GetInfo().TemperatureSetPoint)) {
                    throw new ASCOM.PropertyNotImplementedException();
                }
                return DeviceMediator.GetInfo().TemperatureSetPoint;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/setccdtemperature")]
        public IResponse PutSetCCDTemperature(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(double.MinValue, double.MaxValue)] double SetCCDTemperature,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                if (SetCCDTemperature < -273) { throw new ASCOM.InvalidValueException(); }
                if (SetCCDTemperature >= 100) { throw new ASCOM.InvalidValueException(); }
                (DeviceMediator.GetDevice() as ICamera).TemperatureSetPoint = SetCCDTemperature;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/startx")]
        public IValueResponse<int> GetStartX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => 0);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/startx")]
        public IResponse PutStartX(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, int.MaxValue)] int StartX,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                overrideStartX = StartX;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/starty")]
        public IValueResponse<int> GetStartY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleValueResponse(ClientTransactionID, txId++, () => 0);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/starty")]
        public IResponse PutStartY(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, int.MaxValue)] int StartY,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                overrideStartY = StartY;
            });
        }

        [Route(HttpVerbs.Get, BaseURL + "/{DeviceNumber}/subexposureduration")]
        public IResponse GetSubExposureDuration(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [QueryField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [QueryField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/subexposureduration")]
        public IResponse PutSubExposureDuration(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, int.MaxValue)] int SubExopsureDuration,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/abortexposure")]
        public IResponse PutAbortExposure(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/pulseguide")]
        public IResponse PutPulseGuide(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/startexposure")]
        public IResponse PutStartExposure(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [Required][FormField][Range(0, double.MaxValue)] double Duration,
            [Required][FormField] bool Light,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.HandleEmptyResponse(ClientTransactionID, txId++, () => {
                var info = DeviceMediator.GetInfo();
                if (Duration < info.ExposureMin) { throw new ASCOM.InvalidValueException("StartExposure", nameof(Duration), ">= 0.0"); }
                if (Duration > info.ExposureMax) { throw new ASCOM.InvalidValueException("StartExposure", nameof(Duration), $"<= {info.ExposureMax}"); }
                if (overrideStartX < 0) { throw new ASCOM.InvalidValueException("StartExposure", "StartX", $">= 0"); }
                if (overrideStartY < 0) { throw new ASCOM.InvalidValueException("StartExposure", "StartY", $">= 0"); }
                if (overrideStartX > info.XSize) { throw new ASCOM.InvalidValueException("StartExposure", "StartX", $"< {info.XSize}"); }
                if (overrideStartY > info.YSize) { throw new ASCOM.InvalidValueException("StartExposure", "StartY", $"< {info.YSize}"); }
                if (overrideNumX > info.XSize / info.BinX) { throw new ASCOM.InvalidValueException("StartExposure", "NumX", $"<= {info.XSize / info.BinX}"); }
                if (overrideNumY > info.YSize / info.BinY) { throw new ASCOM.InvalidValueException("StartExposure", "NumY", $"<= {info.YSize / info.BinY}"); }
                if (info.BinX != info.BinY) { throw new InvalidValueException("StartExposure, BinX != BinY"); }

                var seq = new CaptureSequence(Duration, Light ? ImageTypes.LIGHT : ImageTypes.DARK, default, new Core.Model.Equipment.BinningMode(info.BinX, info.BinY), 1);
                seq.Offset = overrideOffset.HasValue ? overrideOffset.Value : info.Offset;
                seq.Gain = overrideGain.HasValue ? overrideGain.Value : info.Gain;

                if (overrideStartX > 0 || overrideStartY > 0 || overrideNumX < info.XSize / info.BinX || overrideNumY < info.YSize / info.BinY) {
                    if (overrideStartX * info.BinX + overrideNumX * info.BinX > info.XSize || overrideStartY * info.BinY + overrideNumY * info.BinY > info.YSize) {
                        throw new InvalidValueException("StartExposure, ROI is incorrect, size too big!");
                    }

                    seq.EnableSubSample = true;
                    seq.SubSambleRectangle = new Core.Utility.ObservableRectangle(overrideStartX, overrideStartY, overrideNumX, overrideNumY);
                } else {
                    seq.EnableSubSample = false;
                }

                seq.Dither = false;
                exposureTask = DeviceMediator.Capture(seq, default, new Progress<ApplicationStatus>());
                exposureTask.ContinueWith(t => isExposing = false);
                isExposing = true;
            });
        }

        [Route(HttpVerbs.Put, BaseURL + "/{DeviceNumber}/stopexposure")]
        public IResponse PutStopExposure(
            [Required][Range(0, uint.MaxValue)] uint DeviceNumber,
            [FormField][Range(0, uint.MaxValue)] uint ClientID = 0,
            [FormField][Range(0, uint.MaxValue)] uint ClientTransactionID = 0) {
            return AlpacaHelpers.NotImplementedResponse(ClientTransactionID, txId++);
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
        }

        public void Dispose() {
        }
    }
}