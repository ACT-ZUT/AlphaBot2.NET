using Iot.Device.Adc;
using Iot.Device.CpuTemperature;
using Iot.Device.DCMotor;
using Iot.Device.Hcsr04;
using Iot.Device.Imu;
using Iot.Device.IrReceiver;

using Iot.Device.Ws2812b;
using System;
using System.Collections.Generic;
using System.Device;
using System.Device.I2c;
using System.Device.Pwm;
using System.Diagnostics;
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
        private static double mainSpeed = 10;
        private static double delay = 10;
        private List<Tlc1543.Channel> channelList = new List<Tlc1543.Channel> {
                Tlc1543.Channel.A0,
                Tlc1543.Channel.A1,
                Tlc1543.Channel.A2,
                Tlc1543.Channel.A3,
                Tlc1543.Channel.A4
            };

        public AlphaBot()
        {
            
        }

        /// <summary>
        /// Calculates and sets the speed of the motors depending on <paramref name="MainSpeed"/> parameter.
        /// <br>Motor speeds are calculated from <paramref name="MainSpeed"/> parameter and <paramref name="speed"/> value </br>
        /// <br>by adding to one motor and substracting from the other to create differential steering.</br>
        /// <para>Below 0 = turn left; Above 0 = turn right.</para>
        /// </summary>
        /// <param name="speed">Value offsetting mainSpeed variable clamped at -100 to 100</param>
        /// <returns>Calculated and clamped from -100 to 100 values of <paramref name="speedL"/> and <paramref name="speedR"/></returns>
        public (double, double) SetSpeed(double speed)
        {
            if (motorL == null || motorR == null)
            {
                Enable(Accessories.Motors);

            }
            double speedL = 0.0;
            double speedR = 0.0;
            if (speed > 0.0)
            {
                speedL = mainSpeed + Math.Abs(speed);
                speedR = mainSpeed - Math.Abs(speed);
            }
            if (speed < 0.0)
            {
                speedL = mainSpeed - Math.Abs(speed);
                speedR = mainSpeed + Math.Abs(speed);
            }
            if (speed == 0.0)
            {
                speedL = mainSpeed;
                speedR = mainSpeed;
            }

            speedL = Clamp(speedL, -100, 100);
            speedR = Clamp(speedR, -100, 100);

            try
            {
                motorL.Speed = speedL / 100;
                motorR.Speed = speedR / 100;
            }
            catch (Exception) { }

            return (speedL, speedR);
        }

        /// <summary>
        /// Sets the speed of the motors. 
        /// <br>Values can range from -100 to 100 where 100 is max speed, 0 equals no movement and -100 is movement in opposite direction</br>
        /// </summary>
        /// <param name="speedL">Value to set to left motor clamped at -100 to 100</param>
        /// <param name="speedR">Value to set to right motor clamped at -100 to 100></param>
        /// <returns>Clamped <paramref name="speedL"/> and <paramref name="speedR"/> values</returns>
        public (double, double) SetSpeed(double speedL, double speedR)
        {
            if (motorL == null || motorR == null)
            {
                Enable(Accessories.Motors);
            }

            speedL = Clamp(speedL, -100, 100);
            speedR = Clamp(speedR, -100, 100);

            try
            {
                motorL.Speed = speedL / 100;
                motorR.Speed = speedR / 100;
            }
            catch (Exception) { }
            
            return (speedL, speedR);
        }

        /// <summary>
        /// Function providing sensors readout
        /// and calculating position of 
        /// black line underneath the robot
        /// </summary>
        /// <returns>
        /// Decimal value ranging from -100 to 100 (left to right)
        /// <br>NaN means line was not found</br>
        /// </returns>
        public double? GetLineValue()
        {
            return GetLineValue(adc.ReadChannels(channelList));
        }
        /// <summary>
        /// Function calculating position of 
        /// black line underneath the robot
        /// </summary>
        /// <param name="values">List of values ranging from 0 to 1023</param>
        /// <returns>
        /// Decimal value ranging from -100 to 100 (left to right)
        /// <br>NaN means line was not found</br>
        /// </returns>
        public double? GetLineValue(List<int> values)
        {
            double? ADCLineValue = null;
            var lineAverage = 0;
            int foundBlack = 0;

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 300) // Change it into settable parameter
                {
                    lineAverage += (i - 2);
                    foundBlack++;
                }
            }

            ADCLineValue = ((double)lineAverage / (double)foundBlack) * 50;
            return ADCLineValue;
        }

        /// <summary>
        /// Limit a variable to the set OutputMax and OutputMin properties
        /// </summary>
        /// <param name="variableToClamp"></param>
        /// <param name="outputMin"></param>
        /// <param name="outputMax"></param>
        /// <returns>A value that is between the OutputMax and OutputMin properties</returns>
        private double Clamp(double variableToClamp, double outputMin, double outputMax)
        {
            if (variableToClamp <= outputMin) { return outputMin; }
            if (variableToClamp >= outputMax) { return outputMax; }
            return variableToClamp;
        }

        /// <summary>
        /// Change parameters
        /// </summary>
        /// <returns></returns>
        public bool SetParameter(Parameters parameter, object value)
        {
            bool result;
            object valueBefore;
            switch (parameter)
            {
                case Parameters.Delay:
                    valueBefore = delay;
                    result = Double.TryParse(value.ToString(), out delay);
                    break;
                case Parameters.MainSpeed:
                    valueBefore = mainSpeed;
                    result = Double.TryParse(value.ToString(), out mainSpeed);
                    break;
                default:
                    valueBefore = Double.NaN;
                    result = false;
                    break;
            }

#if DEBUG
            if (result)
            {
                Debug.WriteLine($"Parameter {Enum.GetName(typeof(Parameters), parameter)} set to: {value}, was: {valueBefore}");
                Console.WriteLine($"Parameter {Enum.GetName(typeof(Parameters), parameter)} set to: {value}, was: {valueBefore}");
            }
            else
            {
                Debug.WriteLine($"Could not set Parameter {Enum.GetName(typeof(Parameters), parameter)} to: {value}, still is: {valueBefore}");
                Console.WriteLine($"Could not set Parameter {Enum.GetName(typeof(Parameters), parameter)} to: {value}, still is: {valueBefore}");
            }
#endif
            return result;
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

        /// <summary>
        /// Available parameters to change from outside of this class
        /// </summary>
        public enum Parameters
        {
            /// <summary>
            /// Delay
            /// </summary>
            Delay = 0,

            /// <summary>
            /// Main Speed
            /// </summary>
            MainSpeed = 1
        }
        #endregion
    }
}