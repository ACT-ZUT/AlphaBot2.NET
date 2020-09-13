using Filters;
using UnitsNet;
using Iot.Device.Adc;
using Iot.Device.DCMotor;
using Iot.Device.Graphics;
using Iot.Device.Imu;
using Iot.Device.IrReceiver;
using Iot.Device.Ws2812b;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Device;
using System.Device.Gpio;
using System.Device.Pwm.Drivers;
using System.Device.Pwm;

namespace AlphaBot2
{
    internal class Debug
    {
        /// <summary>
        /// Class used to record, display and export debug data
        /// </summary>
        private string line_temp;

        private int i = 0;
        public ProcessorArchitecture Architecture;
        public string AssemblyName;
        public DateTime buildDate;
        public Version version;

        public Debug()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Architecture = typeof(AlphaBot).Assembly.GetName().ProcessorArchitecture;
            AssemblyName = typeof(AlphaBot).Assembly.GetName().Name;
            version = Assembly.GetExecutingAssembly().GetName().Version;
            buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
        }

        public void WriteInfo()
        {
            Console.WriteLine($"{AssemblyName}");
            Console.WriteLine($"ACT Science Club");
            Console.WriteLine($"{Architecture}");
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
        }

        public void CSV_Write(string filename, string line)
        {
            i++;
            line_temp += line;
            if (i % 1000 == 0)
            {
                File.AppendAllText($@"/home/pi/{filename}.csv", line_temp);
                line_temp = "";
                i = 0;
            }
        }
    }

    public static class Testing
    {
        public static void MotorTest(List<string> argsList, DCMotor motorL, DCMotor motorR)
        {
            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            const double Period = 20.0;

            Console.WriteLine($"Motor Test");
            Stopwatch sw = Stopwatch.StartNew();
            string lastSpeedDisp = null;

            while (sw.ElapsedMilliseconds < (Math.PI * 2000))
            {
                double time = sw.ElapsedMilliseconds / 1000.0;

                // Note: range is from -1 .. 1 (for 1 pin setup 0 .. 1)
                motorL.Speed = Math.Sin(2.0 * Math.PI * time / Period);
                motorR.Speed = Math.Sin(2.0 * Math.PI * time / Period);
                string disp = $"Speed[L, R] = [{motorL.Speed:0.00}, {motorR.Speed:0.00}]";
                if (disp != lastSpeedDisp)
                {
                    lastSpeedDisp = disp;
                    Console.WriteLine(disp);
                }
                DelayHelper.DelayMilliseconds((int)delay, true);
            }
        }

        public static void ImuTest(List<string> argsList, Mpu6050 imu)
        {
            Console.WriteLine($"IMU Test");
            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            Kalman gx = new Kalman(), gy = new Kalman(), gz = new Kalman();
            Kalman ax = new Kalman(), ay = new Kalman(), az = new Kalman();
            Stopwatch sw = Stopwatch.StartNew();
            var freq = (double)Stopwatch.Frequency;
            Debug csv = new Debug();
            Debug csv_filtered = new Debug();

            string separator = ",";
            string line = "T, Acc X, Acc Y, Acc Z, Gyro X, Gyro Y, Gyro Z, Temp" + Environment.NewLine;
            string line_filtered = "T, Acc X, Acc Y, Acc Z, Gyro X, Gyro Y, Gyro Z, Temp" + Environment.NewLine;
            if (argsList[2] == "csv")
            {
                csv.CSV_Write("testdata", line);
                csv_filtered.CSV_Write("testdata_filtered", line_filtered);
            }

            Console.WriteLine("Run Gyroscope and Accelerometer Self Test:");
            Console.WriteLine($"{imu.RunGyroscopeAccelerometerSelfTest()}");

            //Console.WriteLine("Calibrate Gyroscope and Accelerometer:");
            //Console.WriteLine($"{imu.CalibrateGyroscopeAccelerometer()}");
            double time;
            double last_time = 0;

            while (true)
            {
                time = ((double)sw.ElapsedTicks / freq) * 1000;
                if (time - last_time >= delay)
                {
                    System.Numerics.Vector3 acc = imu.GetAccelerometer();
                    System.Numerics.Vector3 gyro = imu.GetGyroscopeReading();
                    var temp = imu.GetTemperature();

                    Console.Write($"T: {(time - last_time):F12} ");
                    line = $"{time:F12}" + separator;
                    line_filtered = $"{time:F12}" + separator;

                    Console.Write($"Acc");

                    Console.Write($" X: ");
                    Console.Write($"{acc.X,6:F2} ");
                    Console.Write($"{ax.getFilteredValue(acc.X),6:F2} ");
                    line += $"{acc.X,6:F2}" + separator;
                    line_filtered += $"{ax.getFilteredValue(acc.X),6:F2}" + separator;

                    Console.Write($" Y: ");
                    Console.Write($"{acc.Y,6:F2} ");
                    Console.Write($"{ay.getFilteredValue(acc.Y),6:F2} ");
                    line += $"{acc.Y,6:F2}" + separator;
                    line_filtered += $"{ay.getFilteredValue(acc.Y),6:F2}" + separator;

                    Console.Write($" Z: ");
                    Console.Write($"{acc.Z,6:F2} ");
                    Console.Write($"{az.getFilteredValue(acc.Z),6:F2} ");
                    line += $"{acc.Z,6:F2}" + separator;
                    line_filtered += $"{az.getFilteredValue(acc.Z),6:F2}" + separator;

                    Console.Write($" Gyro ");

                    Console.Write($" X: ");
                    Console.Write($"{gyro.X,8:F2} ");
                    Console.Write($"{gx.getFilteredValue(gyro.X),8:F2} ");
                    line += $"{gyro.X,8:F2}" + separator;
                    line_filtered += $"{gx.getFilteredValue(gyro.X),8:F2}" + separator;

                    Console.Write($" Y: ");
                    Console.Write($"{gyro.Y,8:F2} ");
                    Console.Write($"{gy.getFilteredValue(gyro.Y),8:F2} ");
                    line += $"{gyro.Y,8:F2}" + separator;
                    line_filtered += $"{gy.getFilteredValue(gyro.Y),8:F2}" + separator;

                    Console.Write($" Z: ");
                    Console.Write($"{gyro.Z,8:F2} ");
                    Console.Write($"{gz.getFilteredValue(gyro.Z),8:F2} ");
                    line += $"{gyro.Z,8:F2}" + separator;
                    line_filtered += $"{gz.getFilteredValue(gyro.Z),8:F2}" + separator;

                    Console.Write($"{temp.DegreesCelsius.ToString("0.00"),8:F2}°C");
                    line += $"{temp.DegreesCelsius.ToString("0.00"),8:F2}";
                    line_filtered += $"{temp.DegreesCelsius.ToString("0.00"),8:F2}";

                    Console.Write(Environment.NewLine);
                    line += Environment.NewLine;
                    line_filtered += Environment.NewLine;

                    last_time = time;
                    if (argsList[2] == "csv")
                    {
                        csv.CSV_Write("testdata", line);
                        csv_filtered.CSV_Write("testdata_filtered", line_filtered);
                    }
                }
            }
        }

        public static void AdcTest(List<string> argsList, Tlc1543 adc)
        {
            Console.WriteLine($"ADC Test");
            double delay;
            byte sensorNumber;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;
            if (argsList.Count > 2) sensorNumber = Convert.ToByte(argsList[2]);
            else sensorNumber = 11;

            while (true)
            {
                for (int i = 0; i < sensorNumber; i++)
                {
                    Console.Write($"{i}: {adc.ReadChannel((Tlc1543.Channel)i),4} ");
                    DelayHelper.DelayMilliseconds((int)delay, true);
                }
                Console.WriteLine();
            }
        }

        public static void AdcTest1(List<string> argsList, Tlc1543 adc)
        {
            Console.WriteLine($"ADC Test1");
            List<Tlc1543.Channel> channelList = new List<Tlc1543.Channel> {
                Tlc1543.Channel.A0,
                Tlc1543.Channel.A1,
                Tlc1543.Channel.A2,
                Tlc1543.Channel.A3,
                Tlc1543.Channel.A4,
                Tlc1543.Channel.A10
            };

            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            while (true)
            {
                List<int> values = adc.ReadChannels(channelList); //read data

                for (int i = 0; i < values.Count; i++)
                {
                    Console.Write($"{i}: {values[i],4} ");
                }
                Thread.Sleep((int)delay);
                Console.WriteLine();
            }
        }

        public static void IrTest(List<string> argsList, IrReceiver ir)
        {
            Console.WriteLine($"Ir Test");

            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            while (true)
            {
                int data = ir.GetKey();
                if (data == 0 & data != 999)
                {
                    Console.Write($"_");
                }
                else if (data == 999)
                {
                    Console.WriteLine($"data: repeated last");
                }
                else
                {
                    Console.WriteLine($"data: {data} ");
                }
                DelayHelper.DelayMilliseconds((int)delay, true);
            }
        }

        public static void IrTest1(List<string> argsList, IrReceiver ir)
        {
            Console.WriteLine($"Ir Test1");

            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            while (true)
            {
                Console.WriteLine($"{ir.GetKeyTemp()}");
                DelayHelper.DelayMilliseconds((int)delay, true);
            }
        }

        public static void LedTest(List<string> argsList, Ws2812b led)
        {
            Console.WriteLine($"Led Test");

            //double delay;
            //if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            //else delay = 10;

            //BitmapImage img = led.Image;
            //img.Clear();
            //img.SetPixel(0, 0, Color.White);
            //img.SetPixel(1, 0, Color.Red);
            //img.SetPixel(2, 0, Color.Green);
            //img.SetPixel(3, 0, Color.Blue);
            //img.Clear();
            //while (true)
            //{
            //    for (byte b = 0; b < 255; b++)
            //    {
            //        img.SetPixel(0, 0, Color.FromArgb(0xff, b, 0, 0));
            //        img.SetPixel(1, 0, Color.FromArgb(0xff, 0, b, 0));
            //        img.SetPixel(2, 0, Color.FromArgb(0xff, 0, 0, b));
            //        img.SetPixel(3, 0, Color.FromArgb(0xff, b, 0, b));
            //        led.Update();
            //        Console.WriteLine($"{b}");
            //        DelayHelper.DelayMilliseconds((int)delay, true);
            //    }
            //}
        }

        public static void CameraTest(List<string> argsList, Camera camera)
        {
            Console.WriteLine($"Camera Test");
            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 100;

            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps, 30);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, 1920);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, 1080);

            //int fourcc = VideoWriter.Fourcc('I', 'Y', 'U', 'V');
            //
            //try
            //{
            //    VideoW = new VideoWriter(@"/home/pi/test_%02d.avi",
            //        frameRate, fourcc,
            //        new System.Drawing.Size(
            //            (int)camera.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth),
            //            (int)camera.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight)),
            //        true);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}
            //
            //VideoW = new VideoWriter(@"/home/pi/test.avi", 30, fourcc, new System.Drawing.Size(1280, 720), true);

            camera.Start();

            while (true)
            {
                DelayHelper.DelayMilliseconds((int)delay, true);
            }
        }

        public static void Timing(List<string> argsList)
        {
            
            Console.WriteLine($"Timing");
            Timing timing_test = new Timing(12, 21);
            int delay;
            if (argsList.Count > 1) delay = Convert.ToInt32(argsList[1]);
            else delay = 1;
            timing_test.Test(delay);
        }
    }

    public class Timing : IDisposable
    {
        private readonly int _ch1;
        private readonly int _ch2;
        private bool _disposedValue;
        private GpioController _digital = new GpioController(PinNumberingScheme.Logical);
        //DCMotor motor = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 50));

        public Timing(int CH1, int CH2)
        {
            //_ch1 = CH1;
            _ch2 = CH2;

            //_digital.OpenPin(_ch1, PinMode.Output);
            _digital.OpenPin(_ch2, PinMode.Output);

        }

        public void Test(int delay)
        {
            var pwm = PwmChannel.Create(0, 0, delay);
            //var softwarePwmChannel = new SoftwarePwmChannel(21, delay, 0.5, true);
            pwm.DutyCycle = 0.5;
            pwm.Start();
            // SpinWait spinWait = new SpinWait();
            while (true)
            {
                

                //_digital.Write(_ch1, 1);
                // _digital.Write(_ch1, 0);

                /*
                _digital.Write(_ch1, 1); // IOCLK CH1

                _digital.Write(_ch2, 1); // CS CH2
                for (int i = 0; i < delay; i++)
                {

                }
                //DelayHelper.DelayMicroseconds(delay, false);

                _digital.Write(_ch2, 0);
                _digital.Write(_ch1, 0);

                for (int i = 0; i < 10000; i++)
                {

                }
                */


                /*
                _digital.Write(_chipSelect, 1);
                DelayHelper.DelayMicroseconds(0, false);
                _digital.Write(_chipSelect, 0);

                _digital.Write(_chipSelect, 1);
                DelayHelper.DelayMicroseconds(1, false);
                _digital.Write(_chipSelect, 0);

                _digital.Write(_chipSelect, 1);
                DelayHelper.DelayMicroseconds(2, false);
                _digital.Write(_chipSelect, 0);
                */
            }

            // DelayHelper.DelayMicroseconds(5, false); // t(PZH/PZL)
        }

        public void Dispose()
        {
            ((IDisposable)_digital).Dispose();
        }
    }

}