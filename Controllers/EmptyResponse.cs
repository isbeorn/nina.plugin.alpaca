using ASCOM.Common.Alpaca;

namespace NINA.Alpaca.Controllers {

    public class EmptyResponse : IResponse {

        public EmptyResponse(uint clientTransactionID, uint serverTransactionID, AlpacaErrors errorNumber, string errorMessage) {
            ClientTransactionID = clientTransactionID;
            ServerTransactionID = serverTransactionID;
            ErrorNumber = errorNumber;
            ErrorMessage = errorMessage;
        }

        public uint ClientTransactionID { get; set; }
        public uint ServerTransactionID { get; set; }
        public AlpacaErrors ErrorNumber { get; set; }
        public string ErrorMessage { get; set; }
    }
}