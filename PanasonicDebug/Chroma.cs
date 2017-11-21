using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panasonic;

namespace PanasonicDebug
{
    class Chroma
    {
        public static void TestChroma(HE50 camera)
        {
            ConsoleKeyInfo key;
            key = Console.ReadKey();


            Console.Clear();
            Console.WriteLine("Test chroma on all Cameras except HE-130, by pressing vkeys 0-6");
            Console.WriteLine("Test chroma on HE-130, by the following commands.");
            Console.WriteLine("W. Set chroma to 120/168");
            Console.WriteLine("S. set chroma to 49/168");
            Console.WriteLine("Q. Quit");
            Console.WriteLine("E. Turn chroma off");


            Console.WriteLine("");

            while (key.KeyChar != 'q')
            {
                switch (key.KeyChar)
                {
                    case '0':
                        camera.Chroma = 0;
                        break;
                    case '1':
                        camera.Chroma = 1;
                        break;
                    case '2':
                        camera.Chroma = 2;
                        break;
                    case '3':
                        camera.Chroma = 3;
                        break;
                    case '4':
                        camera.Chroma = 4;
                        break;
                    case '5':
                        camera.Chroma = 5;
                        break;
                    case '6':
                        camera.Chroma = 6;
                        break;
                    case 'w':
                        camera.Chroma130 = 120;
                        break;
                    case 's':
                        camera.Chroma130 = 49;
                        break;
                    case 'e':
                        camera.Chroma130 = 0;
                        break;
                }

                key = Console.ReadKey();
            }
        }
    }
}
