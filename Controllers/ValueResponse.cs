using ASCOM.Common.Alpaca;

namespace NINA.Alpaca.Controllers {

    public class ValueResponse<T> : EmptyResponse, IValueResponse<T> {

        public ValueResponse(T value, uint clientTransactionID, uint serverTransactionID, AlpacaErrors errorNumber, string errorMessage) : base(clientTransactionID, serverTransactionID, errorNumber, errorMessage) {
            Value = value;
            ClientTransactionID = clientTransactionID;
            ServerTransactionID = serverTransactionID;
            ErrorNumber = errorNumber;
            ErrorMessage = errorMessage;
        }

        public T Value { get; set; }
    }
}