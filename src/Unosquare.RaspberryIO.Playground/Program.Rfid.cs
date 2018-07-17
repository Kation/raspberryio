namespace Unosquare.RaspberryIO.Playground
{
    using Peripherals;
    using Swan;
    using System;
    using System.Linq;

    public partial class Program
    {
        public static readonly byte[] CustomKey = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };

        private static void TestRfidController()
        {
            "Testing RFID".Info();
            try
            {
                var device = new RFIDControllerMfrc522(Pi.Spi.Channel1, 500000, Pi.Gpio[18]);

                while (true)
                {
                    // If a card is found
                    if (device.DetectCard() == RFIDControllerMfrc522.Status.AllOk)
                    {
                        "Card detected".Info();

                        // Get the UID of the card
                        var uidResponse = device.ReadCardUniqueId();

                        // If we have the UID, continue
                        if (uidResponse.Status == RFIDControllerMfrc522.Status.AllOk)
                        {
                            var cardUid = uidResponse.Data;

                            // Print UID
                            $"Card UID: {cardUid[0]},{cardUid[1]},{cardUid[2]},{cardUid[3]}".Info();

                            // Select the scanned tag
                            device.SelectCardUniqueId(cardUid);

                            ////// Change KeyA
                            //if (device.AuthenticateCard1A(RFIDControllerMfrc522.DefaultAuthKey, cardUid, 7) == RFIDControllerMfrc522.Status.AllOk)
                            //{
                            //    var data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0xFF, 0x07, 0x80, 0x69, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                            //    var changeAuthResult = device.CardWriteData(7, data);
                            //    $"Change KeyA Result: {changeAuthResult}".Info();
                            //}
                            //else
                            //{
                            //    "Error while changing KeyA".Error();
                            //}

                            // Writing data to sector 1 blocks
                            // Authenticate sector
                            if (device.AuthenticateCard1A(RFIDControllerMfrc522.DefaultAuthKey, cardUid, 7) == RFIDControllerMfrc522.Status.AllOk)
                            {
                                var data = new byte[16 * 3];
                                for (var x = 0; x < data.Length; x++)
                                {
                                    data[x] = (byte)(112 - x);
                                }

                                for (int b = 0; b < 3; b++)
                                {
                                    device.CardWriteData((byte)(4 + b), data.Skip(b * 16).Take(16).ToArray());
                                }
                            }
                            else
                            {
                                "Authentication error for write".Error();
                            }

                            // Reading data
                            var continueReading = true;
                            for (int s = 0; s < 16 && continueReading; s++)
                            {
                                // Authenticate sector
                                var authResult = device.AuthenticateCard1A(RFIDControllerMfrc522.DefaultAuthKey, cardUid, (byte)((4 * s) + 3));

                                //if (authResult != RFIDControllerMfrc522.Status.AllOk)
                                //{
                                //    "Authenticating with custom key...".Info();
                                //    authResult = device.AuthenticateCard1A(CustomKey, cardUid, (byte)((4 * s) + 3));
                                //}

                                if (authResult == RFIDControllerMfrc522.Status.AllOk)
                                {
                                    $"Sector {s}".Info();
                                    for (int b = 0; b < 4 && continueReading; b++)
                                    {
                                        var data = device.CardReadData((byte)((4 * s) + b));
                                        if (data.Status != RFIDControllerMfrc522.Status.AllOk)
                                        {
                                            continueReading = false;
                                            break;
                                        }

                                        $"  Block {b} ({data.Data.Length} bytes): {string.Join(" ", data.Data.Select(x => x.ToString("X2")))}".Info();
                                    }
                                }
                                else
                                {
                                    "Authentication error".Error();
                                    break;
                                }
                            }

                            device.ClearCardSelection();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log(nameof(TestRfidController));
            }
        }
    }
}
