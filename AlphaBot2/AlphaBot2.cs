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
    public class AlphaBot2
    {
        private CpuTemperature cpuTemperature;
        private Logger logger;

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
        public AlphaBot2(List<string> argsList)
        {
            if (argsList.Count > 0 && typeof(AlphaBot2).Assembly.GetName().ProcessorArchitecture == ProcessorArchitecture.Arm)
            {
                switch (argsList[0])
                {
                    case "camera":
                        Enable(camera);
                        Testing.CameraTest(argsList, camera);
                        break;

                    case "imu":
                        Enable(imu);
                        Testing.ImuTest(argsList, imu);
                        break;

                    case "motor":
                        Enable(motorL, motorR);
                        Testing.MotorTest(argsList, motorL, motorR);
                        break;

                    case "adc":
                        Enable(adc);
                        Testing.AdcTest(argsList, adc);
                        break;

                    case "adc1":
                        Enable(adc);
                        Testing.AdcTest1(argsList, adc);
                        break;

                    case "ir":
                        Enable(ir);
                        Testing.IrTest(argsList, ir);
                        break;

                    case "ir1":
                        Enable(ir);
                        Testing.IrTest1(argsList, ir);
                        break;

                    case "sonar":
                        Enable(sonar);
                        //Testing.Sonar(argsList, sonar);
                        break;

                    case "led":
                        Enable(led);
                        Testing.LedTest(argsList, led);
                        break;

                    case "line":
                        Enable(adc);
                        FindLine(argsList);
                        //FollowLine();
                        //Testing.LedTest(argsList, led);
                        break;

                    case "timing":
                        Testing.Timing(argsList);
                        break;

                    default:
                        Console.WriteLine("Default case");
                        break;
                }
            }
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
        public void FindLine(List<string> argsList)
        {

            Console.WriteLine($"FindLine");
            double delay;
            if (argsList.Count > 1) delay = Convert.ToDouble(argsList[1]);
            else delay = 100;
            int line = 0;
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

            Enable(motorL, motorR);


            while (true)
            {
                List<int> values = adc.ReadChannels(channelList); //read data
                line = 0;
                for (int i = 0; i < values.Count; i++)
                {
                    line <<= 1;
                    if (values[i] < 300)
                    {
                        line++;
                        tuple[i] = new LineSensor(channelList[i], values[i], true);
                    }
                    else
                    {
                        tuple[i] = new LineSensor(channelList[i], values[i], false);
                    }
                    line <<= 1;
                    //Console.Write($"{i}: {values[i],4} ");
                }
                if(line > 0 & line < 32)
                {
                    Console.WriteLine($"line: {Convert.ToString(line, toBase: 2)} (right)");
                    motorL.Speed = 0.25;
                    motorR.Speed = 0.0;
                }
                else if(line >= 32 & line <= 168)
                {
                    Console.WriteLine($"line: {Convert.ToString(line, toBase: 2)} (center)");
                    motorL.Speed = 0.25;
                    motorR.Speed = 0.25;
                }
                else if(line > 168 & line <= 672)
                {
                    Console.WriteLine($"line: {Convert.ToString(line, toBase: 2)} (left)");
                    motorL.Speed = 0.0;
                    motorR.Speed = 0.25;
                }
                else if(line > 672 & line <= 682)
                {
                    Console.WriteLine($"line: {Convert.ToString(line, toBase: 2)} (split)");
                    motorL.Speed = 0.0;
                    motorR.Speed = 0.0;
                }
                else
                {
                    Console.WriteLine($"line: {Convert.ToString(line, toBase: 2)} (not_found)");
                    //motorL.Speed = 0.0;
                    //motorR.Speed = 0.0;
                }
                Thread.Sleep((int)delay);
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

        public void Enable(Camera camera)
        {
            if (camera is null) this.camera = new Camera();
            else { Disable(camera); this.camera = new Camera(); }
        }

        public void Enable(Mpu6050 imu)
        {
            if (imu is null)
            {
                this.imu = new Mpu6050(I2cDevice.Create(
                            new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
            }
            else 
            {
                Disable(imu);
                this.imu = new Mpu6050(I2cDevice.Create(
                            new I2cConnectionSettings(1, Mpu6050.DefaultI2cAddress)));
            }
        }

        public void Enable(Tlc1543 adc)
        {
            if (adc is null) this.adc = new Tlc1543(24, 5, 23, 25);
            else { Disable(adc); this.adc = new Tlc1543(24, 5, 23, 25); }
        }

        public void Enable(IrReceiver ir)
        {
            if (ir is null) this.ir = new IrReceiver(17);
            else { Disable(ir); this.ir = new IrReceiver(17); }
        }

        public void Enable(Hcsr04 sonar)
        {
            if (sonar is null) this.sonar = new Hcsr04(22, 27);
            else { Disable(sonar); this.sonar = new Hcsr04(22, 27); }
        }

        public void Enable(Ws2812b led)
        {
            if (led is null) this.led = new Ws2812b(18, 4);
            else { Disable(led); this.led = new Ws2812b(18, 4); }
        }

        public void Enable(DCMotor motorL, DCMotor motorR)
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
            //motorL = DCMotor.Create(6, 12, 13);
            //motorR = DCMotor.Create(new SoftwarePwmChannel(26, 400, usePrecisionTimer: true), 20, 21);

            if (motorL is null) this.motorL = DCMotor.Create(6, 12, 13);
            else { Disable(motorL); this.motorL = DCMotor.Create(6, 12, 13); }

            if (motorR is null) this.motorR = DCMotor.Create(26, 20, 21);
            else { Disable(motorR); this.motorR = DCMotor.Create(26, 20, 21); }

            //if (motorL is null) this.motorL = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 500));
            //else { Disable(motorL); this.motorL = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 500)); }

            //if (motorR is null) this.motorR = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 500));
            //else { Disable(motorR); this.motorR = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 500)); }
        }

        public void Enable(CpuTemperature cpuTemperature)
        {
            if (cpuTemperature is null) this.cpuTemperature = new CpuTemperature();
        }

        public void Enable(Logger logger)
        {
            if (logger is null) this.logger = new Logger();
            else { Disable(logger); this.logger = new Logger(); }
        }

        #endregion

        #region Disabling Modules

        public void Disable(Camera camera)
        {
            camera.Dispose();
        }

        public void Disable(Mpu6050 imu)
        {
            imu.Dispose();
        }

        public void Disable(Tlc1543 adc)
        {
            //adc = null;
            adc.Dispose();
        }

        public void Disable(IrReceiver ir)
        {
            ir.Dispose();
        }

        public void Disable(Hcsr04 sonar)
        {
            sonar.Dispose();
        }

        public void Disable(Ws2812b led)
        {
            led.Dispose();
        }

        public void Disable(DCMotor motor)
        {
            if (motor != null)
            {
                motor.Speed = 0;
            }
            motor.Dispose();
        }

        public void Disable(CpuTemperature cpuTemperature)
        {
            if (cpuTemperature != null)
            {
                //implement cleaning procedure (probably saving file with cpuTemperature data)
            }
        }

        public void Disable(Logger logger)
        {
            logger.Dispose();
        }

        #endregion

        #region Dispose

        ~AlphaBot2()
        {
            Disable(camera);
            Disable(imu);
            Disable(adc);
            Disable(ir);
            Disable(motorL);
            Disable(motorR);
            Disable(cpuTemperature);
            Disable(logger);
        }

        #endregion

        public event EventHandler valueChanged;
    }
}