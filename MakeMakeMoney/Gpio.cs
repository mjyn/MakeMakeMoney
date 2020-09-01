using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;

namespace MakeMakeMoney
{
    public static class Gpio
    {
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
            var blinkingPin = Pi.Gpio[BcmPin.Gpio05];

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
    }
}
