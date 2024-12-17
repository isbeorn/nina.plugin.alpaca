using ASCOM.Common.Alpaca;
using System;

namespace NINA.Alpaca.Controllers {

    public class ImageResponse : EmptyResponse {

        public ImageResponse(ushort[] value, int width, int height, uint clientTransactionID, uint serverTransactionID, AlpacaErrors errorNumber, string errorMessage) : base(clientTransactionID, serverTransactionID, errorNumber, errorMessage) {
            Value = ConvertToMonochromeArray(value, width, height);
            ClientTransactionID = clientTransactionID;
            ServerTransactionID = serverTransactionID;
            ErrorNumber = errorNumber;
            ErrorMessage = errorMessage;
        }

        public int Rank => 2;

        public int Type => 2;

        public int[][] Value { get; }

        public static int[][] ConvertToMonochromeArray(ushort[] flatArray, int width, int height) {
            // Validate inputs
            if (flatArray == null)
                throw new ArgumentNullException(nameof(flatArray));
            if (flatArray.Length != width * height)
                throw new ArgumentException("The size of the flat array does not match the given width and height.");
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive integers.");

            // Initialize the jagged array
            int[][] imageArray = new int[width][];

            for (int x = 0; x < width; x++) {
                // Create the column array
                imageArray[x] = new int[height];
                for (int y = 0; y < height; y++) {
                    // Populate the column array with pixel values
                    imageArray[x][y] = flatArray[y * width + x];
                }
            }

            return imageArray;
        }
    }
}