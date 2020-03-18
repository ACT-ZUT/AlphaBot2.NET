﻿using System;
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

        public AlphaBot2()
        {
            //motorL = DCMotor.Create(6, 12, 13);
            //motorR = DCMotor.Create(new SoftwarePwmChannel(26, 400, usePrecisionTimer: true), 20, 21);
            //sonar = new Hcsr04(22, 27);
            //imu = new Mpu6050(I2cDevice.Create(new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
        }

        public void MotorTest(int delay = 1)
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

                Thread.Sleep(delay);
            }

        }

        public void ImuTest(int delay = 100)
        {
            Console.WriteLine($"IMU Test");
            imu = new Mpu6050(I2cDevice.Create(new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
            Kalman gx = new Kalman(), gy = new Kalman(), gz = new Kalman();
            Kalman gx1 = new Kalman(), gy1 = new Kalman(), gz1 = new Kalman();
            Kalman ax = new Kalman(), ay = new Kalman(), az = new Kalman();
            Kalman ax1 = new Kalman(), ay1 = new Kalman(), az1 = new Kalman();
            Stopwatch sw = Stopwatch.StartNew();

            
            Console.WriteLine($"Bias = {imu.RunGyroscopeAccelerometerSelfTest()}");
            Console.WriteLine($"Bias = {imu.CalibrateGyroscopeAccelerometer()}");
            float time;
            float last_time = 0;
            
            while (false)
            {
                //var gyro = imu.GetGyroscopeReading();
                //Console.Write($"Gyro ");
                //Console.Write($"X: {gyro.X, 6:N3} ");
                //Console.Write($"Y: {gyro.Y, 6:N3} ");
                //Console.Write($"Z: {gyro.Z, 6:N3} ");
                //Console.Write($"Gyro filtered ");
                //Console.Write($"X: {gx.getFilteredValue(gyro.X), 6:N3} ");
                //Console.Write($"Y: {gy.getFilteredValue(gyro.Y), 6:N3} ");
                //Console.Write($"Z: {gz.getFilteredValue(gyro.Z), 6:N3} ");

                time = sw.ElapsedMilliseconds;
                if (time - last_time >= delay)
                {
                    Console.Write($"T: {time - last_time} ");
                    System.Numerics.Vector3 acc = imu.GetAccelerometer();

                    Console.Write($" Acc ");

                    Console.Write($"  X: ");
                    Console.Write($"{acc.X,6:N2} ");
                    Console.Write($"{ax.getFilteredValue(acc.X),6:N2} ");
                    Console.Write($"{ax1.getFilteredValue(Math.Round(acc.X, 3)),6:N2} ");

                    Console.Write($"  Y: ");
                    Console.Write($"{acc.Y,6:N2} ");
                    Console.Write($"{ay.getFilteredValue(acc.Y),6:N2} ");
                    Console.Write($"{ay1.getFilteredValue(Math.Round(acc.Y, 3)),6:N2} ");

                    Console.Write($"  Z: ");
                    Console.Write($"{acc.Z,6:N2} ");
                    Console.Write($"{az.getFilteredValue(acc.Z),6:N2} ");
                    Console.Write($"{az1.getFilteredValue(Math.Round(acc.Z, 3)),6:N2} ");

                    System.Numerics.Vector3 gyro = imu.GetGyroscopeReading();
                    Console.Write($" Gyro ");

                    Console.Write($"  X: ");
                    Console.Write($"{gyro.X,7:N2} ");
                    Console.Write($"{gx.getFilteredValue(gyro.X),7:N2} ");
                    Console.Write($"{gx1.getFilteredValue(Math.Round(gyro.X, 3)),7:N2} ");

                    Console.Write($"  Y: ");
                    Console.Write($"{gyro.Y,7:N2} ");
                    Console.Write($"{gy.getFilteredValue(gyro.Y),7:N2} ");
                    Console.Write($"{gy1.getFilteredValue(Math.Round(gyro.Y, 3)),7:N2} ");

                    Console.Write($"  Z: ");
                    Console.Write($"{gyro.Z,7:N2} ");
                    Console.Write($"{gz.getFilteredValue(gyro.Z),7:N2} ");
                    Console.Write($"{gz1.getFilteredValue(Math.Round(gyro.Z, 3)),7:N2} ");

                    //var temp = imu.GetTemperature();
                    //Console.Write($"Temp = {temp.Celsius.ToString("0.00")} °C");
                    last_time = time;
                    Console.WriteLine();
                }

                //Thread.Sleep(delay);
            }

        }


        public void AdcTest(int delay = 100)
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
                Thread.Sleep(delay);
            }
        }

        public void AdcTest1()
        {
            Console.WriteLine($"IMU Test");
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

        ~AlphaBot2()
        {
            motorL.Dispose();
            motorR.Dispose();
        }
    }
}
