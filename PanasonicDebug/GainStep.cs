using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panasonic;

namespace PanasonicDebug
{
    public class GainStep
    {
        public static void TestGainStep(HE50 camera)
        {
            /* Test Gainstep up down */
            ConsoleKeyInfo key;
            key = Console.ReadKey();

            Console.Clear();
            Console.WriteLine("Increase/Decrease cameras gain by using buttons below");
            Console.WriteLine("U. Up");
            Console.WriteLine("D. Down");
            Console.WriteLine("Q. Quit");

            Console.WriteLine("");

            while (key.KeyChar != 'q')
            {
                switch (key.KeyChar)
                {
                    case 'u':
                        camera.GainStepUp();
                        break;
                    case 'd':
                        camera.GainStepDown();
                        break;
                }

                if (key.KeyChar == 'q')
                {
                    break;
                }

                key = Console.ReadKey();
            }
        }
    }
}
