using System;
using System.Collections.Generic;
using System.Text;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;

namespace PushStartToRich
{
    public static class Gpio
    {
        const BcmPin CoinPin = BcmPin.Gpio05;
        const BcmPin ConnectionPin = BcmPin.Gpio06;
        static Gpio()
        {

            Pi.Init<BootstrapWiringPi>();
            InProgress = false;
        }
        static bool InProgress;
        public static int Coin(int count)
        {
            if (InProgress) return -1;
            else InProgress = true;
            var blinkingPin = Pi.Gpio[CoinPin];

            blinkingPin.PinMode = GpioPinDriveMode.Output;
            blinkingPin.Write(false);

            for (var i = 0; i < count; i++)
            {
                Console.WriteLine(1);
                blinkingPin.Write(true);
                System.Threading.Thread.Sleep(100);

                Console.WriteLine(0);
                blinkingPin.Write(false);
                System.Threading.Thread.Sleep(300);
            }
            InProgress = false;
            return 0;
        }

        public static void SetConnectionStatus(bool isOnline)
        {
            var blinkingPin = Pi.Gpio[ConnectionPin];
            blinkingPin.PinMode = GpioPinDriveMode.Output;
            blinkingPin.Write(isOnline);
        }
    }
}
