using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace HappyHelper
{
    class PassthroughTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("    HappyHelper Passthrough Fusion Live Monitor   ");
            Console.WriteLine("==================================================");
            Console.WriteLine("Starting GamepadPassthrough engine...");

            GamepadPassthrough.Start();
            Thread.Sleep(300);

            Console.WriteLine("Virtual Slot assigned: " + GamepadPassthrough.VirtualSlot);
            Console.WriteLine("Now polling for 5 seconds. Move your Physical Gamepad L-Stick or press buttons...");

            for (int i = 0; i < 50; i++)
            {
                // Inject auto skill RB every 1 second
                if (i % 10 == 0)
                {
                    Console.WriteLine(">>> PULSING AUTO SKILL RB (2006) <<<");
                    GamepadPassthrough.PulseAction(2006, 150);
                }
                Thread.Sleep(100);
            }

            GamepadPassthrough.Stop();
            Console.WriteLine("Test completed.");
        }
    }
}
