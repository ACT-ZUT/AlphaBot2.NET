using System;
using System.Collections.Generic;
using System.Text;
using System.Device.Pwm.Drivers;
using System.Threading;
using System.Diagnostics;


using System.Device.I2c;

using Iot.Device.DCMotor;
using Iot.Device.Hcsr04;
using Iot.Device.Graphics;
using Iot.Device.Ws28xx;
using Iot.Device.CpuTemperature;
using Iot.Device.Imu;
using Iot.Device.Adc;
using System.Device.Gpio;

using Filters;
using System.IO;
using System.Reflection;

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
        StringBuilder csv = new StringBuilder();

        public AlphaBot2()
        {
            //motorL = DCMotor.Create(6, 12, 13);
            //motorR = DCMotor.Create(new SoftwarePwmChannel(26, 400, usePrecisionTimer: true), 20, 21);
            //sonar = new Hcsr04(22, 27);
            //imu = new Mpu6050(I2cDevice.Create(new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
        }

        public void MotorTest(double delay = 1)
        {
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

                Thread.Sleep((int)delay);
            }

        }

        public void ImuTest(double delay = 100)
        {
            Console.WriteLine($"IMU Test");
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
            csv.CSV_Write("testdata", line);
            csv_filtered.CSV_Write("testdata_filtered", line_filtered);

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
                    csv.CSV_Write("testdata", line);
                    csv_filtered.CSV_Write("testdata_filtered", line_filtered);
                }

                //Thread.Sleep(delay);
            }

        }


        public void AdcTest(double delay = 100)
        {
            adc = new Tlc1543((byte)5, (byte)23, (byte)24, (byte)25, (byte)5);
            while (true)
            {
                var TRvalues = adc.AnalogRead();
                foreach (var sensor in TRvalues)
                {
                    Console.Write($"{sensor.Key}: {sensor.Value,4} ");
                }

                Console.WriteLine();
                Thread.Sleep((int)delay);
            }
        }

        public void AdcTest1()
        {
            Console.WriteLine($"ADC Test1");
            byte CS = 5;
            byte DOUT = 23;
            byte ADDR = 24;
            byte IOCLK = 25;
            GpioController controller = new GpioController(PinNumberingScheme.Logical);
            controller.OpenPin(CS, PinMode.Output);
            controller.OpenPin(DOUT, PinMode.InputPullUp);
            controller.OpenPin(ADDR, PinMode.Output);
            controller.OpenPin(IOCLK, PinMode.Output);
            while (true)
            {
                for (int channel = 0; channel < 12; channel++)
                {
                    int value = 0;
                    controller.Write(CS, 0);
                    for (int i = 0; i < 4; i++)
                    {
                        if ((channel >> (3 - i) & 0x01) != 0)
                        {
                            controller.Write(ADDR, 1);
                        }
                        else
                        {
                            controller.Write(ADDR, 0);
                        }
                        controller.Write(IOCLK, 1);
                        controller.Write(IOCLK, 0);
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        controller.Write(IOCLK, 1);
                        controller.Write(IOCLK, 0);
                    }
                    controller.Write(CS, 1);
                    Thread.Sleep(1);

                    controller.Write(CS, 0);
                    for (int i = 0; i < 10; i++)
                    {
                        controller.Write(IOCLK, 1);
                        value <<= 1;
                        if (controller.Read(DOUT) == PinValue.High)
                        {
                            value |= 0x01;
                        }
                        controller.Write(IOCLK, 0);
                    }
                    controller.Write(IOCLK, 1);

                    Console.Write($"{channel}: {value,4} ");
                }
                Thread.Sleep(100);
                Console.WriteLine("");
            }
        }

        public void AdcTest2()
        {
            Console.WriteLine($"ADC Test2");
            byte CS = 5;
            byte DOUT = 23;
            byte ADDR = 24;
            byte IOCLK = 25;
            GpioController controller = new GpioController(PinNumberingScheme.Logical);
            controller.OpenPin(CS, PinMode.Output);
            controller.OpenPin(DOUT, PinMode.InputPullUp);
            controller.OpenPin(ADDR, PinMode.Output);
            controller.OpenPin(IOCLK, PinMode.Output);


            while (true)
            {
                
                Thread.Sleep(100);
                Console.WriteLine();
            }
        }

        ~AlphaBot2()
        {
            motorL.Dispose();
            motorR.Dispose();
        }
    }
}
