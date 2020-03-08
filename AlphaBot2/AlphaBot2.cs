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

        public AlphaBot2()
        {
            motorL = DCMotor.Create(6, 12, 13);
            motorR = DCMotor.Create(new SoftwarePwmChannel(26, 400, usePrecisionTimer: true), 20, 21);
            sonar = new Hcsr04(22, 27);
            imu = new Mpu6050(I2cDevice.Create(new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
        }

        public void Test()
        {
            const double Period = 20.0;

            Console.WriteLine($"Motor Test");
            Stopwatch sw = Stopwatch.StartNew();
            string lastSpeedDisp = null;
            while (sw.ElapsedMilliseconds < (Math.PI*2000))
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

                Thread.Sleep(1);
            }

        }

        public void ImuTest()
        {
            var gyro = imu.GetGyroscopeReading();
            Console.WriteLine($"Gyro X = {gyro.X, 15}");
            Console.WriteLine($"Gyro Y = {gyro.Y, 15}");
            Console.WriteLine($"Gyro Z = {gyro.Z, 15}");
            var acc = imu.GetAccelerometer();
            Console.WriteLine($"Acc X = {acc.X, 15}");
            Console.WriteLine($"Acc Y = {acc.Y, 15}");
            Console.WriteLine($"Acc Z = {acc.Z, 15}");
            var temp = imu.GetTemperature();
            Console.WriteLine($"Temp = {temp.Celsius.ToString("0.00")} °C");
        }

        ~AlphaBot2()
        {
            motorL.Dispose();
            motorR.Dispose();
        }
    }
}
