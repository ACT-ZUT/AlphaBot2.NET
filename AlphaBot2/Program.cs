using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace AlphaBot2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("AlphaBot2.NET");
            Console.WriteLine("ver 0.2.1, ACT Science Club");
            Console.WriteLine("Waiting for remote debugger...");
            while (true)
            {
                if (Debugger.IsAttached) break;
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);

            var Robot = new AlphaBot2();
            if (args.Length != 0)
            {
                int delay = 0;
                if (args.Length > 1) delay = Convert.ToInt32(args[1]);

                Console.WriteLine("Debug");
                Console.WriteLine($"length: {args.Length}");
                foreach (var item in args) Console.WriteLine($"{item}");
                Console.WriteLine($"delay: {delay}");
                Console.WriteLine("");

                switch (args[0])
                {
                    case "camera":
                        //Robot.CameraTest();
                        break;
                    case "imu":
                        if (delay != 0) Robot.ImuTest(delay);
                        else Robot.ImuTest();
                        break;
                    case "motor":
                        if (delay != 0) Robot.MotorTest(delay);
                        else Robot.MotorTest();
                        break;
                    case "adc":
                        if (delay != 0) Robot.AdcTest(delay);
                        else Robot.AdcTest();
                        break;
                }
            }
            else
            {
                Console.WriteLine("Default Case:");
            }

            Console.CancelKeyPress += (o, e) =>
            {
                GC.Collect();
            };
        }
    }
}