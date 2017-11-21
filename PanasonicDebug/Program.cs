using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panasonic;
using System.Diagnostics;

namespace PanasonicDebug
{
    class Program
    {
        static void onConnect()
        {
            Console.WriteLine("Connected");
        }

        static void onDisconnect()
        {
            Console.WriteLine("Disconnected");
        }

        static void log(string function, string message)
        {

            Console.WriteLine(function + "\t" + message);
        }





        static void Main(string[] args)
        {
            HE50.debug = true;
            HE50.cameraUpdates = false;
            HE50.expandedDebug = true;
            
            HE50 camera1 = new HE50("192.168.62.155", "admin", "12345");
            camera1.onLog += log;

            camera1.onConnect += onConnect;
            camera1.onDisconnect += onDisconnect;

            


            Console.Clear();
            Console.WriteLine("1. Test Gain up/down.");
            Console.WriteLine("2. Pan & Tilt.");
            Console.WriteLine("3. Chroma");
            Console.WriteLine("");


            ConsoleKeyInfo key;
            key = Console.ReadKey();

            while (key.KeyChar != 'q')
            {

                
                switch (key.KeyChar)
                {
                    case '1':
                        GainStep.TestGainStep(camera1);
                        break;
                    case '2':
                        PanTilt.TestPanTilt(camera1);
                        break;
                    case '3':
                        Chroma.TestChroma(camera1);
                        break;
                }

                if (key.KeyChar == 'q')
                {
                    break;
                }

                Console.Clear();
                Console.WriteLine("1. Test Gain up/down.");
                Console.WriteLine("2. Pan & Tilt.");
                Console.WriteLine("3. Chroma");
                Console.WriteLine("");

                key = Console.ReadKey();
            }


            /*
            Console.WriteLine("Preset = 1");
            Console.ReadKey();

            camera1.Preset = 0;
            Console.WriteLine("Preset = 2");
            Console.ReadKey();

            camera1.Preset = 1;
            Console.WriteLine("Preset = 3");
            Console.ReadKey();

            camera1.Power = 0;
            Console.WriteLine("Start");
            Console.ReadKey();

            camera1.Preset = 1;
            Console.WriteLine("Preset = 1");
            Console.ReadKey();

            camera1.Preset = 0;
            Console.WriteLine("Preset = 2");
            Console.ReadKey();

            camera1.Preset = 1;
            Console.WriteLine("Preset = 3");
            System.Threading.Thread.Sleep(5000);
            /*

            camera1.Power = 1;
            Console.WriteLine("Start");

            camera1.Pan = 60;
            Console.WriteLine("Pan=55");
            System.Threading.Thread.Sleep(3000);


            camera1.Tilt = 60;
            Console.WriteLine("Tilt=55");
            System.Threading.Thread.Sleep(2000);

            camera1.Pan = 40;
            Console.WriteLine("Pan=55");
            System.Threading.Thread.Sleep(2000);

            camera1.Tilt = 40;
            Console.WriteLine("Tilt=45");
            System.Threading.Thread.Sleep(3000);


            camera1.Pan = 50;
            Console.WriteLine("Pan=50");
            camera1.Tilt = 50;
            Console.WriteLine("Tilt=50");

            System.Threading.Thread.Sleep(5000);
            Console.WriteLine("Väntar på Användare....");
            Console.ReadKey();

            camera1.Pan = 30;
            Console.WriteLine("Pan=30");
            System.Threading.Thread.Sleep(3000);

            camera1.Pan = 70;
            Console.WriteLine("Pan=70");
            System.Threading.Thread.Sleep(3000);

            camera1.Pan = 50;

            System.Threading.Thread.Sleep(5000);
            Console.WriteLine("Klar");

             * 
             */


            Console.ReadKey();
            camera1.stop();

        }
    }
}
