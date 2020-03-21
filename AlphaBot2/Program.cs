using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Globalization;

namespace AlphaBot2
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);

            Console.WriteLine($"{typeof(AlphaBot2).Assembly.GetName().Name}");
            Console.WriteLine($"ACT Science Club");
            Console.WriteLine($"{typeof(AlphaBot2).Assembly.GetName().ProcessorArchitecture}");
            Console.WriteLine($"Version: {version}");
            Console.WriteLine($"Build Date: {buildDate}");

#if DEBUG
            Console.WriteLine("Configuration: Debug");
            Console.WriteLine("Waiting for remote debugger...");
            while (true)
            {
                if (Debugger.IsAttached) break;
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
#else
            Console.WriteLine("Configuration: Release");
#endif

            var Robot = new AlphaBot2();
            if (args.Length != 0)
            {
                double delay = 0;
                if (args.Length > 1) delay = Convert.ToDouble(args[1]);

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