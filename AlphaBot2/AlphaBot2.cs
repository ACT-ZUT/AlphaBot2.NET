using Iot.Device.Adc;
using Iot.Device.CpuTemperature;
using Iot.Device.DCMotor;
using Iot.Device.Hcsr04;
using Iot.Device.Imu;
using Iot.Device.IrReceiver;

//using Iot.Device.Ws28xx;
using Iot.Device.Ws2812b;
using System;
using System.Collections.Generic;
using System.Device;
using System.Device.I2c;
using System.Device.Pwm;
using System.Reflection;
using System.Threading;

namespace AlphaBot2
{
    public class AlphaBot
    {
        private CpuTemperature cpuTemperature;

        private DCMotor motorL; private DCMotor motorR;
        private Camera camera;
        private Mpu6050 imu;
        private Tlc1543 adc;
        private Hcsr04 sonar;
        private Ws2812b led;
        private IrReceiver ir;

        private double delay = 10;
        private List<Tlc1543.Channel> channelList = new List<Tlc1543.Channel> {
                Tlc1543.Channel.A0,
                Tlc1543.Channel.A1,
                Tlc1543.Channel.A2,
                Tlc1543.Channel.A3,
                Tlc1543.Channel.A4
            };
        List<LineSensor> tupleSensors = new List<LineSensor>();

        //Change to normal class
        public AlphaBot()
        {
            
        }

        public void FollowLine()
        {
        }

        public struct LineSensor
        {
            public Tlc1543.Channel Channel { get; set; }
            public int Value { get; set; }
            public bool IsBlack { get; set; }

            public LineSensor(Tlc1543.Channel Channel, int Value, bool IsBlack)
            {
                this.Channel = Channel;
                this.Value = Value;
                this.IsBlack = IsBlack;
            }
        }

        /// <summary>
        /// Function providing sensors readout
        /// showing if there is a black line visible 
        /// underneat the robot
        /// </summary>
        /// <returns>Decimal value ranging from -100 to 100 (left to right)</returns>
        public double? FindLine(List<int> values)
        {
            double? ADCLineValue = null;

            var lineAverage = 0;
            int foundBlack = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 300)
                {
                    lineAverage += (i - 2);
                    foundBlack++;
                }
            }
            ADCLineValue = ((double)lineAverage / (double)foundBlack)*50;
            Console.WriteLine(ADCLineValue);
            return ADCLineValue;
        }

        /// <summary>
        /// to fix/change
        /// </summary>
        /// <param name="argsList"></param>
        public (double, double) FindLine()
        {
            List<Tlc1543.Channel> channelList = new List<Tlc1543.Channel> {
                Tlc1543.Channel.A0,
                Tlc1543.Channel.A1,
                Tlc1543.Channel.A2,
                Tlc1543.Channel.A3,
                Tlc1543.Channel.A4
            };
            List<LineSensor> tuple = new List<LineSensor>();

            foreach (var channel in channelList)
            {
                tuple.Add(new LineSensor(channel, 0, false));
            }

            Enable(Accessories.Motors);

            while (true)
            {
                List<int> values = adc.ReadChannels(channelList); //read data
                var line = FindLine(values);
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] < 300)
                    {
                        tuple[i] = new LineSensor(channelList[i], values[i], true);
                    }
                    else
                    {
                        tuple[i] = new LineSensor(channelList[i], values[i], false);
                    }
                    Console.Write($"{i}: {values[i],4} ");
                }

                if(line < 0)
                {
                    Console.WriteLine($"line: {line} (left)");
                    motorL.Speed = 0.0;
                    motorR.Speed = (double)line / 400;
                }
                else if(line > 0)
                {
                    Console.WriteLine($"line: {line} (right)");
                    motorL.Speed = (double)line / 400;
                    motorR.Speed = 0.0;
                }
                else
                {
                    Console.WriteLine($"line: {line} (split)");
                    motorL.Speed = 0.0;
                    motorR.Speed = 0.0;
                }
                Thread.Sleep(10);
                //Console.WriteLine();
            }
        }

        /// <summary>
        /// Change parameters
        /// </summary>
        /// <returns></returns>
        public bool SetParameter()
        {
            return false;
        }

        #region Enabling modules

        public void Enable(Accessories accessory)
        {
            switch (accessory)
            {
                case Accessories.Camera:
                    if (camera is null) this.camera = new Camera();
                    else { Disable(Accessories.Camera); this.camera = new Camera(); }
                    break;

                case Accessories.IMU:
                    if (imu is null)
                    {
                        this.imu = new Mpu6050(I2cDevice.Create(
                                    new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
                    }
                    else
                    {
                        Disable(Accessories.IMU);
                        this.imu = new Mpu6050(I2cDevice.Create(
                                    new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
                    }
                    break;

                case Accessories.MotorL:
                    if (motorL is null) this.motorL = DCMotor.Create(6, 12, 13);
                    else { Disable(Accessories.MotorL); this.motorL = DCMotor.Create(6, 12, 13); }
                    break;

                case Accessories.MotorR:
                    if (motorR is null) this.motorR = DCMotor.Create(26, 20, 21);
                    else { Disable(Accessories.MotorR); this.motorR = DCMotor.Create(26, 20, 21); }
                    break;

                case Accessories.Motors:
                    Enable(Accessories.MotorL);
                    Enable(Accessories.MotorR);
                    break;

                case Accessories.ADC:
                    if (adc is null) this.adc = new Tlc1543(24, 5, 23, 25);
                    else { Disable(Accessories.ADC); this.adc = new Tlc1543(24, 5, 23, 25); }
                    break;

                case Accessories.IR:
                    if (ir is null) this.ir = new IrReceiver(17);
                    else { Disable(Accessories.IR); this.ir = new IrReceiver(17); }
                    break;

                case Accessories.Sonar:
                    if (sonar is null) this.sonar = new Hcsr04(22, 27);
                    else { Disable(Accessories.Sonar); this.sonar = new Hcsr04(22, 27); }
                    break;

                case Accessories.LED:
                    if (led is null) this.led = new Ws2812b(18, 4);
                    else { Disable(Accessories.LED); this.led = new Ws2812b(18, 4); }
                    break;

                case Accessories.CPUTemp:
                    if (cpuTemperature is null) this.cpuTemperature = new CpuTemperature();
                    //else { Disable(led); this.led = new Ws2812b(18, 4); }
                    break;

                case Accessories.All:
                    foreach (var item in Enum.GetValues(typeof(Accessories)))
                    {
                        Enable((Accessories)item);
                    }
                    break;

                default:
                    Console.WriteLine("Default case");
                    break;
            }
            
        }

        #endregion

        #region Disabling Modules
        public void Disable(Accessories accessory)
        {
            switch (accessory)
            {
                case Accessories.Camera:
                    camera.Dispose();
                    break;

                case Accessories.IMU:
                    imu.Dispose();
                    break;

                case Accessories.MotorL:
                    if (motorL != null)
                    {
                        motorL.Speed = 0;
                    }
                    motorL.Dispose();
                    break;

                case Accessories.MotorR:
                    if (motorR != null)
                    {
                        motorR.Speed = 0;
                    }
                    motorR.Dispose();
                    break;

                case Accessories.Motors:
                    Disable(Accessories.MotorL);
                    Disable(Accessories.MotorR);
                    break;

                case Accessories.ADC:
                    //adc = null;
                    adc.Dispose();
                    break;

                case Accessories.IR:
                    ir.Dispose();
                    break;

                case Accessories.Sonar:
                    sonar.Dispose();
                    break;

                case Accessories.LED:
                    led.Dispose();
                    break;

                case Accessories.CPUTemp:
                    cpuTemperature = null;
                    break;

                case Accessories.All:
                    foreach (var item in Enum.GetValues(typeof(Accessories)))
                    {
                        if((Accessories)item != Accessories.All)
                        {
                            Disable((Accessories)item);
                        }
                    }
                    break;

                default:
                    Console.WriteLine("Default case");
                    break;
            }

        }

        #endregion

        #region Dispose

        ~AlphaBot()
        {
            Disable(Accessories.All);
        }

        #endregion

        public event EventHandler valueChanged;

        #region Enums

        /// <summary>
        /// Available Accessories on AlphaBot2
        /// </summary>
        public enum Accessories
        {
            /// <summary>
            /// ADC
            /// </summary>
            ADC = 0,

            /// <summary>
            /// Camera
            /// </summary>
            Camera = 1,

            /// <summary>
            /// IR
            /// </summary>
            IR = 2,

            /// <summary>
            /// IMU
            /// </summary>
            IMU = 3,

            /// <summary>
            /// Sonar
            /// </summary>
            Sonar = 4,

            /// <summary>
            /// MotorL
            /// </summary>
            MotorL = 5,

            /// <summary>
            /// MotorR
            /// </summary>
            MotorR = 6,

            /// <summary>
            /// Both motors
            /// </summary>
            Motors = 7,

            /// <summary>
            /// LED
            /// </summary>
            LED = 8,

            /// <summary>
            /// CPUTemp
            /// </summary>
            CPUTemp = 9,

            /// <summary>
            /// All
            /// </summary>
            All = 100
        }

        #endregion
    }
}