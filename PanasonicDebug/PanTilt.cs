using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panasonic;

namespace PanasonicDebug
{
    class PanTilt
    {
        public static void TestPanTilt(HE50 camera)
        {
            /* Test Gainstep up down */
            ConsoleKeyInfo key;
            key = Console.ReadKey();

            Console.Clear();
            Console.WriteLine("Test your cameras Pan/Tilt by using the keyboard.");
            Console.WriteLine("W. Up");
            Console.WriteLine("S. Down");
            Console.WriteLine("A. Left");
            Console.WriteLine("D. Right");
            Console.WriteLine("Z. Zoom in");
            Console.WriteLine("X. Zoom out");
            Console.WriteLine("E. Stop");
            Console.WriteLine("Q. Quit");
            Console.WriteLine("");

            while (key.KeyChar != 'q')
            {
                switch (key.KeyChar)
                {
                    case 'w':
                        camera.Tilt = 70;
                        break;
                    case 's':
                        camera.Tilt = 30;
                        break;
                    case 'a':
                        camera.Pan = 30;
                        break;
                    case 'd':
                        camera.Pan = 70;
                        break;
                    case 'z':
                        camera.Zoom = 70;
                        break;
                    case 'x':
                        camera.Zoom = 30;
                        break;
                    case 'e':
                        camera.Pan = 50;
                        camera.Tilt = 50;
                        camera.Zoom = 50;
                        break;
                }

                key = Console.ReadKey();
            }

            camera.Pan = 50;
            camera.Tilt = 50;
            camera.Zoom = 50;
        }
    }
}
