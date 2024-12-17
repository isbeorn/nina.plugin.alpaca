using ASCOM.Common.Alpaca;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Alpaca.Controllers {

    internal static class AlpacaHelpers {

        public static bool IsDeviceIdenticalWithAlpacaService(IProfileService profileService, object deviceMediator, Guid serviceId) {
            if (Guid.TryParse(profileService.ActiveProfile.SafetyMonitorSettings.Id, out var id)) {
                if (id == serviceId) {
                    return true;
                }
            }
            try {
                var type = deviceMediator.GetType();
                var info = type.GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var handler = info.GetValue(deviceMediator);
                var handlerType = handler.GetType();
                var handlerInfo = handlerType.GetProperty("DeviceChooserVM", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var chooser = (IDeviceChooserVM)handlerInfo.GetValue(handler);
                if (Guid.TryParse(chooser.SelectedDevice.Id, out var selectedId)) {
                    if (selectedId == serviceId) {
                        return true;
                    }
                }
            } catch { }

            return false;
        }

        public static Array ConvertTo2dArray(ushort[] flatArray, int width, int height) {
            // Validate inputs
            if (flatArray == null)
                throw new ArgumentNullException(nameof(flatArray));
            if (flatArray.Length != width * height)
                throw new ArgumentException("The size of the flat array does not match the given width and height.");
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive integers.");

            // Initialize the jagged array
            int[,] imageArray = new int[width, height];

            for (int x = 0; x < width; x++) {
                // Create the column array
                for (int y = 0; y < height; y++) {
                    // Populate the column array with pixel values
                    imageArray[x, y] = flatArray[y * width + x];
                }
            }

            return imageArray;
        }

        public static IResponse NotImplementedResponse(uint clientTransactionId, uint serverTransactionId) {
            return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, "The operation is not implemented");
        }

        public static IResponse HandleEmptyResponse(uint clientTransactionId, uint serverTransactionId, Action action) {
            try {
                action();
            } catch (NotImplementedException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (ASCOM.PropertyNotImplementedException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (InvalidOperationException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidOperationException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidValueException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.InvalidValue, ex.Message);
            } catch (Exception ex) {
                Logger.Error(ex);
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.UnspecifiedError, ex.Message);
            }
            return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.AlpacaNoError, string.Empty);
        }

        public static async Task<IResponse> HandleEmptyResponse(uint clientTransactionId, uint serverTransactionId, Func<Task> action) {
            try {
                await action();
            } catch (NotImplementedException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (ASCOM.PropertyNotImplementedException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (InvalidOperationException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidOperationException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidValueException ex) {
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.InvalidValue, ex.Message);
            } catch (Exception ex) {
                Logger.Error(ex);
                return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.UnspecifiedError, ex.Message);
            }
            return new EmptyResponse(clientTransactionId, serverTransactionId, AlpacaErrors.AlpacaNoError, string.Empty);
        }

        public static IValueResponse<T> HandleValueResponse<T>(uint clientTransactionId, uint serverTransactionId, Func<T> action) {
            T value = default(T);
            try {
                value = action();
            } catch (NotImplementedException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (ASCOM.PropertyNotImplementedException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (InvalidOperationException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidOperationException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidValueException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.InvalidValue, ex.Message);
            } catch (Exception ex) {
                Logger.Error(ex);
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.UnspecifiedError, ex.Message);
            }
            return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.AlpacaNoError, string.Empty);
        }

        public static async Task<IValueResponse<T>> HandleValueResponse<T>(uint clientTransactionId, uint serverTransactionId, Func<Task<T>> action) {
            T value = default(T);
            try {
                value = await action();
            } catch (NotImplementedException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (ASCOM.PropertyNotImplementedException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.NotImplemented, ex.Message);
            } catch (InvalidOperationException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidOperationException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.InvalidOperationException, ex.Message);
            } catch (ASCOM.InvalidValueException ex) {
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.InvalidValue, ex.Message);
            } catch (Exception ex) {
                Logger.Error(ex);
                return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.UnspecifiedError, ex.Message);
            }
            return new ValueResponse<T>(value, clientTransactionId, serverTransactionId, AlpacaErrors.AlpacaNoError, string.Empty);
        }
    }
}