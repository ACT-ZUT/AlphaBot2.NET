using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using System.Device.I2c;
using System.Device.Gpio;
using System.Device.Pwm.Drivers;

using Iot.Device.Imu;
using Iot.Device.Adc;
using Iot.Device.Ws28xx;
using Iot.Device.Hcsr04;
using Iot.Device.DCMotor;
using Iot.Device.Graphics;
using Iot.Device.IrReceiver;
using Iot.Device.CpuTemperature;

using Emgu.CV;

using Filters;

namespace AlphaBot2
{

    class AlphaBot2
    {
        // 1 pin mode
        // using (DCMotor motor = DCMotor.Create(6))
        // using (DCMotor motor = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 50)))
        // 2 pin mode
        // using (DCMotor motor = DCMotor.Create(27, 22))
        // using (DCMotor motor = DCMotor.Create(new SoftwarePwmChannel(27, frequency: 50), 22))
        // 3 pin mode
        // using (DCMotor motor = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 50), 23, 24))
        //DCMotor motor2 = DCMotor.Create(26, 20, 21);
        DCMotor motorL;
        DCMotor motorR;
        Hcsr04 sonar;
        CpuTemperature cpuTemperature = new CpuTemperature();
        Mpu6050 imu;
        Tlc1543 adc;
        Kalman filter_imu;
        Debug csv = new Debug();
        IrReceiver IR;
        
        VideoCapture camera;
        VideoWriter VideoW;
        Mat frame;

        public AlphaBot2()
        {
            //motorL = DCMotor.Create(6, 12, 13);
            //motorR = DCMotor.Create(new SoftwarePwmChannel(26, 400, usePrecisionTimer: true), 20, 21);
            //sonar = new Hcsr04(22, 27);
            //imu = new Mpu6050(I2cDevice.Create(new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
        }

        public void MotorTest(List<string> argsList)
        {
            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            const double Period = 20.0;

            Console.WriteLine($"Motor Test");
            Stopwatch sw = Stopwatch.StartNew();
            string lastSpeedDisp = null;

            motorL = DCMotor.Create(6, 12, 13);
            motorR = DCMotor.Create(26, 20, 21);


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

                Thread.Sleep((int)delay);
            }

        }

        public void ImuTest(List<string> argsList)
        {
            Console.WriteLine($"IMU Test");
            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;


            imu = new Mpu6050(I2cDevice.Create(new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
            Kalman gx = new Kalman(), gy = new Kalman(), gz = new Kalman();
            Kalman gx1 = new Kalman(), gy1 = new Kalman(), gz1 = new Kalman();
            Kalman ax = new Kalman(), ay = new Kalman(), az = new Kalman();
            Kalman ax1 = new Kalman(), ay1 = new Kalman(), az1 = new Kalman();
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

                    Console.Write($"{temp.Celsius.ToString("0.00"), 8:F2}°C");
                    line += $"{temp.Celsius.ToString("0.00"),8:F2}";
                    line_filtered += $"{temp.Celsius.ToString("0.00"),8:F2}";

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

                //Thread.Sleep(delay);
            }

        }

        public void AdcTest(List<string> argsList)
        {
            double delay;
            byte sensorNumber;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;
            if (argsList.Count > 2) sensorNumber = Convert.ToByte(argsList[2]);
            else sensorNumber = 11;

            adc = new Tlc1543(24, 5, 23, 25);
            while (true)
            {
                for (int i = 0; i < sensorNumber; i++)
                {
                    Console.Write($"{i}: {adc.ReadChannel((Tlc1543.Channel)i), 4} ");
                    Thread.Sleep((int)delay);
                }
                Console.WriteLine();
            }
        }

        public void AdcTest1(List<string> argsList)
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

            adc = new Tlc1543(24, 5, 23, 25);
            while (true)
            {
                List<int> values = adc.ReadChannel(channelList);
                for (int i = 0; i < values.Count; i++)
                {
                    Console.Write($"{i}: {values[i],4} ");
                }
                Thread.Sleep((int)delay);
                Console.WriteLine();
            }
        }

        public void CameraTest(List<string> argsList)
        {
            int frameRate = 30;
            camera = new VideoCapture(0);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Autofocus, 0);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoWb, 0);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps, frameRate);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, 1920);
            camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, 1080);

            int fourcc = VideoWriter.Fourcc('I', 'Y', 'U', 'V');

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
           
            

            //VideoW = new VideoWriter(@"/home/pi/test.avi", 30, fourcc, new System.Drawing.Size(1280, 720), true);


            frame = new Mat();
            camera.ImageGrabbed += ProcessFrame;
            if (camera != null)
            {
                try
                {
                    camera.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            this.AdcTest1(argsList);
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            DateTime date = DateTime.Now;
            if (camera != null && camera.Ptr != IntPtr.Zero)
            {
                camera.Retrieve(frame, 0);
                frame.Save($@"/home/pi/dataset/test_{date.Year:D4}{date.Month:D2}{date.Day:D2}_{date.Hour:D2}{date.Minute:D2}{date.Millisecond:D3}.jpg");
                System.Device.DelayHelper.DelayMilliseconds(1000, true);
                //VideoW.Write(frame);
                //rest of processing 
                Console.Write(" frame");
            }
        }

        public void IrTest(List<string> argsList)
        {
            Console.WriteLine($"Ir Test");
            IR = new IrReceiver(17);

            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 10;

            while (true)
            {
                int data = IR.GetKey();
                if(data != 0 )
                {
                    Console.WriteLine($"data: {data} ");
                }
                else if(data == 999)
                {
                    Console.WriteLine($"data: repeated last");
                }
                Thread.Sleep((int)delay);
                Console.WriteLine();
            }
        }

        ~AlphaBot2()
        {
            motorL.Dispose();
            motorR.Dispose();
            camera.Stop();
            camera.Dispose();
        }
    }
}
