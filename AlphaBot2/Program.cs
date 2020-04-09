using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace AlphaBot2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            Debug debug = new Debug();
            debug.WriteInfo();
            AlphaBot2 Robot;

            
            if (debug.Architecture == ProcessorArchitecture.Arm)
            {
                Robot = new AlphaBot2();
            }
            else
            {
                //make constructor for a PC testing version
                Robot = new AlphaBot2();
            }

            if (argsList.Count > 0)
            {
                switch (argsList[0])
                {
                    case "camera":
                        Robot.CameraTest(argsList);
                        break;
                    case "imu":
                        Robot.ImuTest(argsList);
                        break;
                    case "motor":
                        Robot.MotorTest(argsList);
                        break;
                    case "adc":
                        Robot.AdcTest(argsList);
                        break;
                    case "adc1":
                        Robot.AdcTest1(argsList);
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