// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Device;
using System.Device.I2c;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using Iot.Units;

namespace Iot.Device.Imu
{
    /// <summary>
    ///  MPU6050 class. MPU6050 has an embedded gyroscope, accelerometer and temperature.
    /// </summary>
    public class Mpu6050 : IDisposable
    {
        /// <summary>
        /// Default address for MPU6050
        /// </summary>
        public const byte DefaultI2cAddress = 0x68;

        /// <summary>
        /// Second address for MPU6050
        /// </summary>
        public const byte SecondI2cAddress = 0x69;

        private const float Adc = 0x8000;
        private const float Gravity = 9.807f;
        internal I2cDevice _i2cDevice;
        private Vector3 _accelerometerBias = new Vector3();
        private Vector3 _gyroscopeBias = new Vector3();
        private AccelerometerRange _accelerometerRange;
        private GyroscopeRange _gyroscopeRange;
        private AccelerometerBandwidth _accelerometerBandwidth;
        private GyroscopeBandwidth _gyroscopeBandwidth;
        internal bool _wakeOnMotion;

        /// <summary>
        /// Initialize the MPU6050
        /// </summary>
        /// <param name="i2cDevice">The I2C device</param>
        public Mpu6050(I2cDevice i2cDevice)
        {
            _i2cDevice = i2cDevice;
            Reset();
            PowerOn();
            if (!CheckVersion())
            {
                throw new IOException($"This device does not contain the correct signature 0x68 for a MPU6050");
            }

            GyroscopeBandwidth = GyroscopeBandwidth.Bandwidth0250Hz;
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth1130Hz;
            AccelerometerRange = AccelerometerRange.Range02G;
        }

        /// <summary>
        /// Used to create the class for the MPU6050. Initialization is a bit different than for the MPU6050
        /// </summary>
        internal Mpu6050()
        {
        }

        #region Accelerometer

        /// <summary>
        /// Accelerometer bias data
        /// </summary>
        public Vector3 AccelerometerBias => _accelerometerBias;

        /// <summary>
        /// Get or set the accelerometer range
        /// </summary>
        public AccelerometerRange AccelerometerRange
        {
            get => _accelerometerRange;

            set
            {
                WriteRegister(Register.ACCEL_CONFIG, (byte)((byte)value << 3));
                // We will cache the range to avoid i2c access every time this data is requested
                // This allow as well to make sure the stored data is the same as the one read
                _accelerometerRange = (AccelerometerRange)(ReadByte(Register.ACCEL_CONFIG) >> 3);
                if (_accelerometerRange != value)
                {
                    throw new IOException($"Can set {nameof(AccelerometerRange)}, desired value {value}, stored value {_accelerometerRange}");
                }
            }
        }

        /// <summary>
        /// Get or set the accelerometer bandwidth
        /// </summary>
        public AccelerometerBandwidth AccelerometerBandwidth
        {
            get => _accelerometerBandwidth;

            set
            {
                WriteRegister(Register.ACCEL_CONFIG_2, (byte)value);
                _accelerometerBandwidth = (AccelerometerBandwidth)ReadByte(Register.ACCEL_CONFIG_2);
                if (_accelerometerBandwidth != value)
                {
                    throw new IOException($"Can set {nameof(AccelerometerBandwidth)}, desired value {value}, stored value {_accelerometerBandwidth}");
                }
            }
        }

        /// <summary>
        /// Get the real accelerometer bandwidth. This allows to calculate the real
        /// degree per second
        /// </summary>
        public float AccelerationScale
        {
            get
            {
                float val = 0;
                switch (AccelerometerRange)
                {
                    case AccelerometerRange.Range02G:
                        val = 2.0f;
                        break;
                    case AccelerometerRange.Range04G:
                        val = 4.0f;
                        break;
                    case AccelerometerRange.Range08G:
                        val = 8.0f;
                        break;
                    case AccelerometerRange.Range16G:
                        val = 16.0f;
                        break;
                    default:
                        break;
                }

                val = (val * Gravity) / Adc;
                return val / (1 + SampleRateDivider);
            }
        }

        /// <summary>
        /// Get the accelerometer in G
        /// </summary>
        /// <remarks>
        /// Vector axes are the following:
        ///    +Z   +Y
        ///  \  |  /
        ///   \ | /
        ///    \|/
        ///    /|\
        ///   / | \
        ///  /  |  \
        ///         +X
        /// </remarks>
        public Vector3 GetAccelerometer() => GetRawAccelerometer() * AccelerationScale;

        private Vector3 GetRawAccelerometer()
        {
            Span<byte> rawData = stackalloc byte[6]
            {
                0, 0, 0, 0, 0, 0
            };
            Vector3 ace = new Vector3();
            ReadBytes(Register.ACCEL_XOUT_H, rawData);
            ace.X = BinaryPrimitives.ReadInt16BigEndian(rawData);
            ace.Y = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(2));
            ace.Z = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(4));
            return ace;
        }

        /// <summary>
        /// Set or get the accelerometer low power mode
        /// </summary>
        public AccelerometerLowPowerFrequency AccelerometerLowPowerFrequency
        {
            get { return (AccelerometerLowPowerFrequency)ReadByte(Register.LP_ACCEL_ODR); }
            set { WriteRegister(Register.LP_ACCEL_ODR, (byte)value); }
        }

        #endregion

        #region Gyroscope

        /// <summary>
        /// Gyroscope bias data
        /// </summary>
        public Vector3 GyroscopeBias => _gyroscopeBias;

        /// <summary>
        /// Get or set the gyroscope range
        /// </summary>
        public GyroscopeRange GyroscopeRange
        {
            get => _gyroscopeRange;

            set
            {
                WriteRegister(Register.GYRO_CONFIG, (byte)((byte)value << 3));
                _gyroscopeRange = (GyroscopeRange)(ReadByte(Register.GYRO_CONFIG) >> 3);
                if (_gyroscopeRange != value)
                {
                    throw new IOException($"Can set {nameof(GyroscopeRange)}, desired value {value}, stored value {_gyroscopeRange}");
                }
            }
        }

        /// <summary>
        /// Get or set the gyroscope bandwidth
        /// </summary>
        public GyroscopeBandwidth GyroscopeBandwidth
        {
            get => _gyroscopeBandwidth;

            set
            {
                if (value == GyroscopeBandwidth.Bandwidth8800HzFS32)
                {
                    WriteRegister(Register.GYRO_CONFIG, (byte)((byte)GyroscopeRange | 0x01));
                }
                else if (value == GyroscopeBandwidth.Bandwidth3600HzFS32)
                {
                    WriteRegister(Register.GYRO_CONFIG, (byte)((byte)GyroscopeRange | 0x02));
                }
                else
                {
                    WriteRegister(Register.GYRO_CONFIG, (byte)GyroscopeRange);
                    WriteRegister(Register.CONFIG, (byte)value);
                }

                var retConf = ReadByte(Register.GYRO_CONFIG);
                if ((retConf & 0x01) == 0x01)
                {
                    _gyroscopeBandwidth = GyroscopeBandwidth.Bandwidth8800HzFS32;
                }
                else if ((retConf & 0x03) == 0x00)
                {
                    _gyroscopeBandwidth = (GyroscopeBandwidth)ReadByte(Register.CONFIG);
                }
                else
                {
                    _gyroscopeBandwidth = GyroscopeBandwidth.Bandwidth3600HzFS32;
                }

                if (_gyroscopeBandwidth != value)
                {
                    throw new IOException($"Can set {nameof(GyroscopeBandwidth)}, desired value {value}, stored value {_gyroscopeBandwidth}");
                }
            }
        }

        /// <summary>
        /// Get the real gyroscope bandwidth. This allows to calculate the real
        /// angular rate in degree per second
        /// </summary>
        public float GyroscopeScale
        {
            get
            {
                float val = 0;
                switch (GyroscopeRange)
                {
                    case GyroscopeRange.Range0250Dps:
                        val = 250.0f;
                        break;
                    case GyroscopeRange.Range0500Dps:
                        val = 500.0f;
                        break;
                    case GyroscopeRange.Range1000Dps:
                        val = 1000.0f;
                        break;
                    case GyroscopeRange.Range2000Dps:
                        val = 2000.0f;
                        break;
                    default:
                        break;
                }

                val /= Adc;
                // the sample rate diver only apply for the non FS modes
                if ((GyroscopeBandwidth != GyroscopeBandwidth.Bandwidth3600HzFS32) &&
                    (GyroscopeBandwidth != GyroscopeBandwidth.Bandwidth8800HzFS32))
                {
                    return val / (1 + SampleRateDivider);
                }

                return val;
            }
        }

        /// <summary>
        /// Get the gyroscope in degrees per seconds
        /// </summary>
        /// <remarks>
        /// Vector axes are the following:
        ///    +Z   +Y
        ///  \  |  /
        ///   \ | /
        ///    \|/
        ///    /|\
        ///   / | \
        ///  /  |  \
        ///         +X
        /// </remarks>
        public Vector3 GetGyroscopeReading() => GetRawGyroscope() * GyroscopeScale;

        private Vector3 GetRawGyroscope()
        {
            Span<byte> rawData = stackalloc byte[6]
            {
                0, 0, 0, 0, 0, 0
            };
            Vector3 gyro = new Vector3();
            ReadBytes(Register.GYRO_XOUT_H, rawData);
            gyro.X = BinaryPrimitives.ReadInt16BigEndian(rawData);
            gyro.Y = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(2));
            gyro.Z = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(4));
            return gyro;
        }

        #endregion

        #region Temperature

        /// <summary>
        /// Get the temperature
        /// </summary>
        public Temperature GetTemperature()
        {
            Span<byte> rawData = stackalloc byte[2]
            {
                0, 0
            };
            ReadBytes(Register.TEMP_OUT_H, rawData);
            // formula from the documentation
            // return Temperature.FromCelsius((BinaryPrimitives.ReadInt16BigEndian(rawData) - 21) / 333.87 + 21);
            return Temperature.FromCelsius(BinaryPrimitives.ReadInt16BigEndian(rawData)/ 340 + 36.53);
        }

        #endregion

        #region Modes, constructor, Dispose

        /// <summary>
        /// Setup the Wake On Motion. This mode generate a rising signal on pin INT
        /// You can catch it with a normal GPIO and place an interruption on it if supported
        /// Reading the sensor won't give any value until it wakes up periodically
        /// Only Accelerator data is available in this mode
        /// </summary>
        /// <param name="accelerometerThreshold">Threshold of magnetometer x/y/z axes. LSB = 4mg. Range is 0mg to 1020mg</param>
        /// <param name="acceleratorLowPower">Frequency used to measure data for the low power consumption mode</param>
        public void SetWakeOnMotion(uint accelerometerThreshold, AccelerometerLowPowerFrequency acceleratorLowPower)
        {
            // Using documentation page 31 of Product Specification to setup
            _wakeOnMotion = true;
            if (accelerometerThreshold > 1020)
            {
                throw new ArgumentException($"{nameof(accelerometerThreshold)} has to be between 0mg and 1020mg");
            }

            // LSB = 4mg
            accelerometerThreshold /= 4;
            // Make sure we start from a clean soft reset
            PowerOn();
            // PWR_MGMT_1 (0x6B) make CYCLE =0, SLEEP = 0  and STANDBY = 0
            WriteRegister(Register.PWR_MGMT_1, (byte)ClockSource.Internal20MHz);
            // PWR_MGMT_2 (0x6C) set DIS_XA, DIS_YA, DIS_ZA = 0 and DIS_XG, DIS_YG, DIS_ZG = 1
            // Remove the Gyroscope
            WriteRegister(Register.PWR_MGMT_2, (byte)(DisableModes.DisableGyroscopeX | DisableModes.DisableGyroscopeY | DisableModes.DisableGyroscopeZ));
            // ACCEL_CONFIG 2 (0x1D) set ACCEL_FCHOICE_B = 0 and A_DLPFCFG[2:0]=1(b001)
            // Bandwidth for Accelerator to 184Hz
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth0184Hz;
            // Enable Motion Interrupt
            //  In INT_ENABLE (0x38), set the whole register to 0x40 to enable motion interrupt only
            WriteRegister(Register.INT_ENABLE, 0x40);
            // Enable AccelHardware Intelligence:
            // In MOT_DETECT_CTRL (0x69), set ACCEL_INTEL_EN = 1 and ACCEL_INTEL_MODE  = 1
            WriteRegister(Register.MOT_DETECT_CTRL, 0b1100_0000);
            // Set Motion Threshold:
            // In WOM_THR (0x1F), set the WOM_Threshold[7:0] to 1~255 LSBs (0~1020mg)
            WriteRegister(Register.WOM_THR, (byte)accelerometerThreshold);
            // Set Frequency of Wake-up:
            // In LP_ACCEL_ODR (0x1E), set Lposc_clksel[3:0] = 0.24Hz ~ 500Hz
            WriteRegister(Register.LP_ACCEL_ODR, (byte)acceleratorLowPower);
            // Enable Cycle Mode (AccelLow Power Mode):
            // In PWR_MGMT_1 (0x6B) make CYCLE =1
            WriteRegister(Register.PWR_MGMT_1, 0b0010_0000);
            // Motion Interrupt Configuration Completed
        }

        internal void Reset()
        {
            WriteRegister(Register.PWR_MGMT_1, 0x80);
            // http://www.invensense.com/wp-content/uploads/2015/02/PS-MPU-9250A-01-v1.1.pdf, section 4.23.
            // Maximum risen time is 100 ms after VDD
            Thread.Sleep(100);
            _wakeOnMotion = false;
        }

        internal void PowerOn()
        {
            // this should be a soft reset
            WriteRegister(Register.PWR_MGMT_1, 0x01);
            // http://www.invensense.com/wp-content/uploads/2015/02/PS-MPU-9250A-01-v1.1.pdf, section 4.23.
            // Maximum risen time is 100 ms after VDD
            Thread.Sleep(100);
            _wakeOnMotion = false;
        }

        /// <summary>
        /// Return true if the version of MPU6050 is the correct one
        /// </summary>
        /// <returns>True if success</returns>
        internal bool CheckVersion()
        {
            // Check if the version is thee correct one
            return ReadByte(Register.WHO_AM_I) == 0x68;
        }

        /// <summary>
        /// Get or set the sample diver mode
        /// </summary>
        public byte SampleRateDivider
        {
            get { return ReadByte(Register.SMPLRT_DIV); }
            set { WriteRegister(Register.SMPLRT_DIV, value); }
        }

        /// <summary>
        /// Get or set the elements to disable.
        /// It can be any axes of the accelerometer and or the gyroscope
        /// </summary>
        public DisableModes DisableModes
        {
            get { return (DisableModes)ReadByte(Register.PWR_MGMT_2); }
            set { WriteRegister(Register.PWR_MGMT_2, (byte)value); }
        }

        #endregion

        #region FIFO

        /// <summary>
        /// Get the number of elements to read from the FIFO (First In First Out) buffer
        /// </summary>
        public uint FifoCount
        {
            get
            {
                Span<byte> rawData = stackalloc byte[2]
                {
                    0, 0
                };
                ReadBytes(Register.FIFO_COUNTH, rawData);
                return BinaryPrimitives.ReadUInt16BigEndian(rawData);
            }
        }

        /// <summary>
        /// Get or set the FIFO (First In First Out) modes
        /// </summary>
        public FifoModes FifoModes
        {
            get
            {
                return (FifoModes)(ReadByte(Register.FIFO_EN));
            }
            set
            {
                if (value != FifoModes.None)
                {
                    // Make sure the FIFO is enabled
                    var usrCtl = (UserControls)ReadByte(Register.USER_CTRL);
                    usrCtl |= UserControls.FIFO_RST;
                    WriteRegister(Register.USER_CTRL, (byte)usrCtl);
                }
                else
                {
                    // Deactivate FIFO
                    var usrCtl = (UserControls)ReadByte(Register.USER_CTRL);
                    usrCtl &= ~UserControls.FIFO_RST;
                    WriteRegister(Register.USER_CTRL, (byte)usrCtl);
                }

                WriteRegister(Register.FIFO_EN, (byte)value);
            }
        }

        /// <summary>
        /// Read data in the FIFO (First In First Out) buffer, read as many data as the size of readData byte span
        /// You should read the number of data available in the FifoCount property then
        /// read them here.
        /// You will read only data you have selected in FifoModes.
        /// Data are in the order of the Register from 0x3B to 0x60.
        /// ACCEL_XOUT_H and ACCEL_XOUT_L
        /// ACCEL_YOUT_H and ACCEL_YOUT_L
        /// ACCEL_ZOUT_H and ACCEL_ZOUT_L
        /// TEMP_OUT_H and TEMP_OUT_L
        /// GYRO_XOUT_H and GYRO_XOUT_L
        /// GYRO_YOUT_H and GYRO_YOUT_L
        /// GYRO_ZOUT_H and GYRO_ZOUT_L
        /// EXT_SENS_DATA_00 to EXT_SENS_DATA_24
        /// </summary>
        /// <param name="readData">Data which will be read</param>
        public void ReadFifo(Span<byte> readData)
        {
            ReadBytes(Register.FIFO_R_W, readData);
        }

        #endregion

        #region Calibration and tests

        /// <summary>
        /// Perform full calibration the gyroscope and the accelerometer
        /// It will automatically adjust as well the offset stored in the device
        /// The result bias will be stored in the AcceloremeterBias and GyroscopeBias
        /// </summary>
        /// <returns>Gyroscope and accelerometer bias</returns>
        public (Vector3 gyroscopeBias, Vector3 accelerometerBias) CalibrateGyroscopeAccelerometer()
        {
            // = 131 LSB/degrees/sec
            const int GyroSensitivity = 131;
            // = 16384 LSB/g
            const int AccSensitivity = 16384;
            byte i2cMaster;
            byte userControls;

            Span<byte> rawData = stackalloc byte[12];

            Vector3 gyroBias = new Vector3();
            Vector3 acceBias = new Vector3();

            Reset();
            // Enable accelerator and gyroscope
            DisableModes = DisableModes.DisableNone;
            Thread.Sleep(200);
            // Disable all interrupts
            WriteRegister(Register.INT_ENABLE, 0x00);
            // Disable FIFO
            FifoModes = FifoModes.None;
            // Disable I2C master
            i2cMaster = ReadByte(Register.I2C_MST_CTRL);
            WriteRegister(Register.I2C_MST_CTRL, 0x00);
            // Disable FIFO and I2C master modes
            userControls = ReadByte(Register.USER_CTRL);
            WriteRegister(Register.USER_CTRL, (byte)UserControls.None);
            // Reset FIFO and DMP
            WriteRegister(Register.USER_CTRL, (byte)UserControls.FIFO_RST);
            ///DelayHelper.DelayMilliseconds(15, false);

            // Configure MPU6050 gyro and accelerometer for bias calculation
            // Set low-pass filter to 184 Hz
            GyroscopeBandwidth = GyroscopeBandwidth.Bandwidth0184Hz;
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth0184Hz;
            // Set sample rate to 1 kHz
            SampleRateDivider = 0;
            // Set gyro to maximum sensitivity
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            AccelerometerRange = AccelerometerRange.Range02G;

            // Configure FIFO will be needed for bias calculation
            FifoModes = FifoModes.GyroscopeX | FifoModes.GyroscopeY | FifoModes.GyroscopeZ | FifoModes.Accelerometer;
            // accumulate 40 samples in 40 milliseconds = 480 bytes
            // Do not exceed 512 bytes max buffer
            ///DelayHelper.DelayMilliseconds(40, false);
            // We have our data, deactivate FIFO
            FifoModes = FifoModes.None;

            // How many sets of full gyro and accelerometer data for averaging
            var packetCount = FifoCount / 12;

            for (uint reading = 0; reading < packetCount; reading++)
            {
                Vector3 accel_temp = new Vector3();
                Vector3 gyro_temp = new Vector3();

                // Read data
                ReadBytes(Register.FIFO_R_W, rawData);

                // Form signed 16-bit integer for each sample in FIFO
                accel_temp.X = BinaryPrimitives.ReadUInt16BigEndian(rawData);
                accel_temp.Y = BinaryPrimitives.ReadUInt16BigEndian(rawData.Slice(2));
                accel_temp.Z = BinaryPrimitives.ReadUInt16BigEndian(rawData.Slice(4));
                gyro_temp.X = BinaryPrimitives.ReadUInt16BigEndian(rawData.Slice(6));
                gyro_temp.Y = BinaryPrimitives.ReadUInt16BigEndian(rawData.Slice(8));
                gyro_temp.Z = BinaryPrimitives.ReadUInt16BigEndian(rawData.Slice(10));

                acceBias += accel_temp;
                gyroBias += gyro_temp;
            }

            // Make the average
            acceBias /= packetCount;
            gyroBias /= packetCount;

            // bias on Z is cumulative
            acceBias.Z += acceBias.Z > 0 ? -AccSensitivity : AccSensitivity;

            // Divide by 4 to get 32.9 LSB per deg/s
            // Biases are additive, so change sign on calculated average gyro biases
            rawData[0] = (byte)(((int)(-gyroBias.X / 4) >> 8) & 0xFF);
            rawData[1] = (byte)((int)(-gyroBias.X / 4) & 0xFF);
            rawData[2] = (byte)(((int)(-gyroBias.Y / 4) >> 8) & 0xFF);
            rawData[3] = (byte)((int)(-gyroBias.Y / 4) & 0xFF);
            rawData[4] = (byte)(((int)(-gyroBias.Z / 4) >> 8) & 0xFF);
            rawData[5] = (byte)((int)(-gyroBias.Z / 4) & 0xFF);

            // Changes all Gyroscope offsets
            WriteRegister(Register.XG_OFFSET_H, rawData[0]);
            WriteRegister(Register.XG_OFFSET_L, rawData[1]);
            WriteRegister(Register.YG_OFFSET_H, rawData[2]);
            WriteRegister(Register.YG_OFFSET_L, rawData[3]);
            WriteRegister(Register.ZG_OFFSET_H, rawData[4]);
            WriteRegister(Register.ZG_OFFSET_L, rawData[5]);

            // Output scaled gyro biases for display in the main program
            _gyroscopeBias = gyroBias / GyroSensitivity;

            // Construct the accelerometer biases for push to the hardware accelerometer
            // bias registers. These registers contain factory trim values which must be
            // added to the calculated accelerometer biases; on boot up these registers
            // will hold non-zero values. In addition, bit 0 of the lower byte must be
            // preserved since it is used for temperature compensation calculations.
            // Accelerometer bias registers expect bias input as 2048 LSB per g, so that
            // the accelerometer biases calculated above must be divided by 8.

            // A place to hold the factory accelerometer trim biases
            Vector3 accel_bias_reg = new Vector3();
            Span<byte> accData = stackalloc byte[2];
            // Read factory accelerometer trim values
            ReadBytes(Register.XA_OFFSET_H, accData);
            accel_bias_reg.X = BinaryPrimitives.ReadUInt16BigEndian(accData);
            ReadBytes(Register.YA_OFFSET_H, accData);
            accel_bias_reg.Y = BinaryPrimitives.ReadUInt16BigEndian(accData);
            ReadBytes(Register.ZA_OFFSET_H, accData);
            accel_bias_reg.Z = BinaryPrimitives.ReadUInt16BigEndian(accData);

            // Define mask for temperature compensation bit 0 of lower byte of
            // accelerometer bias registers
            uint mask = 0x01;
            // Define array to hold mask bit for each accelerometer bias axis
            Span<byte> mask_bit = stackalloc byte[3];

            // If temperature compensation bit is set, record that fact in mask_bit
            mask_bit[0] = (((uint)accel_bias_reg.X & mask) == mask) ? (byte)0x01 : (byte)0x00;
            mask_bit[1] = (((uint)accel_bias_reg.Y & mask) == mask) ? (byte)0x01 : (byte)0x00;
            mask_bit[2] = (((uint)accel_bias_reg.Z & mask) == mask) ? (byte)0x01 : (byte)0x00;

            // Construct total accelerometer bias, including calculated average
            // accelerometer bias from above
            // Subtract calculated averaged accelerometer bias scaled to 2048 LSB/g
            // (16 g full scale) and keep the mask
            accel_bias_reg -= acceBias / 8;
            // Add the "reserved" mask as it was
            rawData[0] = (byte)(((int)accel_bias_reg.X >> 8) & 0xFF);
            rawData[1] = (byte)(((int)accel_bias_reg.X & 0xFF) | mask_bit[0]);
            rawData[2] = (byte)(((int)accel_bias_reg.Y >> 8) & 0xFF);
            rawData[3] = (byte)(((int)accel_bias_reg.Y & 0xFF) | mask_bit[1]);
            rawData[4] = (byte)(((int)accel_bias_reg.Z >> 8) & 0xFF);
            rawData[5] = (byte)(((int)accel_bias_reg.Z & 0xFF) | mask_bit[2]);
            // Push accelerometer biases to hardware registers
            WriteRegister(Register.XA_OFFSET_H, rawData[0]);
            WriteRegister(Register.XA_OFFSET_L, rawData[1]);
            WriteRegister(Register.YA_OFFSET_H, rawData[2]);
            WriteRegister(Register.YA_OFFSET_L, rawData[3]);
            WriteRegister(Register.ZA_OFFSET_H, rawData[4]);
            WriteRegister(Register.ZA_OFFSET_L, rawData[5]);

            // Restore the previous modes
            WriteRegister(Register.USER_CTRL, (byte)(userControls | (byte)UserControls.I2C_MST_EN));
            i2cMaster = (byte)(i2cMaster & (~(byte)(I2cBusFrequency.Frequency348kHz) | (byte)I2cBusFrequency.Frequency400kHz));
            WriteRegister(Register.I2C_MST_CTRL, i2cMaster);
            ///DelayHelper.DelayMilliseconds(10, false);

            // Finally store the acceleration bias
            _accelerometerBias = acceBias / AccSensitivity;

            return (_gyroscopeBias, _accelerometerBias);
        }

        /// <summary>
        /// <![CDATA[
        /// Run a self test and returns the gyroscope and accelerometer vectores
        /// a. If factory Self-Test values ST_OTP≠0, compare the current Self-Test response (GXST, GYST, GZST, AXST, AYST and AZST)
        /// to the factory Self-Test values (ST_OTP) and report Self-Test is passing if all the following criteria are fulfilled:
        /// Axis    | Pass criteria
        /// X-gyro  | (GXST / GXST_OTP) > 0.5
        /// Y-gyro  | (GYST / GYST_OTP) > 0.5
        /// Z-gyro  | (GZST / GZST_OTP) > 0.5
        /// X-Accel |  0.5 < (AXST / AXST_OTP) < 1.5
        /// Y-Accel | 0.5 < (AYST / AYST_OTP) < 1.5
        /// Z-Accel | 0.5 < (AZST / AZST_OTP) < 1.5
        /// b. If factory Self-Test values ST_OTP=0, compare the current Self-Test response (GXST, GYST, GZST, AXST, AYST and AZST)
        /// to the ST absolute limits (ST_AL) and report Self-Test is passing if all the  following criteria are fulfilled.
        /// Axis   | Pass criteria
        /// X-gyro | |GXST| ≥ 60dps
        /// Y-gyro | |GYST| ≥ 60dps
        /// Z-gyro | |GZST| ≥ 60dps
        /// X-Accel| 225mgee ≤ |AXST| ≤ 675mgee
        /// Y-Accel| 225mgee ≤ |AXST| ≤ 675mgee
        /// Z-Accel| 225mgee ≤ |AXST| ≤ 675mgee
        /// c. If the Self-Test passes criteria (a) and (b), it’s necessary to check gyro offset values.
        /// Report passing Self-Test if the following criteria fulfilled.
        /// Axis   | Pass criteria
        /// X-gyro | |GXOFFSET| ≤ 20dps
        /// Y-gyro | |GYOFFSET| ≤ 20dps
        /// Z-gyro | |GZOFFSET| ≤ 20dps
        /// ]]>
        /// </summary>
        /// <returns>the gyroscope and accelerometer vectors</returns>
        public (Vector3 gyroscopeAverage, Vector3 accelerometerAverage) RunGyroscopeAccelerometerSelfTest()
        {
            // Used for the number of cycles to run the test
            // Value is 200 according to documentation AN-MPU-9250A-03
            const int numCycles = 200;

            Vector3 accAverage = new Vector3();
            Vector3 gyroAvegage = new Vector3();
            Vector3 accSelfTestAverage = new Vector3();
            Vector3 gyroSelfTestAverage = new Vector3();
            Vector3 gyroSelfTest = new Vector3();
            Vector3 accSelfTest = new Vector3();
            Vector3 gyroFactoryTrim = new Vector3();
            Vector3 accFactoryTrim = new Vector3();
            // Tests done with slower GyroScale and Accelerator so 2G so value is 0 in both cases
            byte gyroAccScale = 0;

            // Setup the registers for Gyroscope as in documentation
            // DLPF Config | LPF BW | Sampling Rate | Filter Delay
            // 2           | 92Hz   | 1kHz          | 3.9ms
            WriteRegister(Register.SMPLRT_DIV, 0x00);
            WriteRegister(Register.CONFIG, 0x02);
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            // Set full scale range for the gyro to 250 dps
            // Setup the registers for accelerometer as in documentation
            // DLPF Config | LPF BW | Sampling Rate | Filter Delay
            // 2           | 92Hz   | 1kHz          | 7.8ms
            WriteRegister(Register.ACCEL_CONFIG_2, 0x02);
            AccelerometerRange = AccelerometerRange.Range02G;

            // Read the data 200 times as per the documentation page 5
            for (int reading = 0; reading < numCycles; reading++)
            {
                gyroAvegage = GetRawGyroscope();
                accAverage = GetRawAccelerometer();
            }

            accAverage /= numCycles;
            gyroAvegage /= numCycles;

            // Set USR_Reg: (1Bh) Gyro_Config, gdrive_axisCTST [0-2] to b111 to enable Self-Test.
            WriteRegister(Register.ACCEL_CONFIG, 0xE0);
            // Set USR_Reg: (1Ch) Accel_Config, AX/Y/Z_ST_EN   [0-2] to b111 to enable Self-Test.
            WriteRegister(Register.GYRO_CONFIG, 0xE0);
            // Wait 20ms for oscillations to stabilize
            Thread.Sleep(20);

            // Read the gyroscope and accelerometer output at a 1kHz rate and average 200 readings.
            // The averaged values will be the LSB of GX_ST_OS, GY_ST_OS, GZ_ST_OS, AX_ST_OS, AY_ST_OS and AZ_ST_OS in the software
            for (int reading = 0; reading < numCycles; reading++)
            {
                gyroSelfTestAverage = GetRawGyroscope();
                accSelfTestAverage = GetRawAccelerometer();
            }

            accSelfTestAverage /= numCycles;
            gyroSelfTestAverage /= numCycles;

            // To cleanup the configuration after the test
            // Set USR_Reg: (1Bh) Gyro_Config, gdrive_axisCTST [0-2] to b000.
            WriteRegister(Register.ACCEL_CONFIG, 0x00);
            // Set USR_Reg: (1Ch) Accel_Config, AX/Y/Z_ST_EN [0-2] to b000.
            WriteRegister(Register.GYRO_CONFIG, 0x00);
            // Wait 20ms for oscillations to stabilize
            Thread.Sleep(20);

            // Retrieve factory Self-Test code (ST_Code) from USR_Reg in the software
            gyroSelfTest.X = ReadByte(Register.SELF_TEST_X_GYRO);
            gyroSelfTest.Y = ReadByte(Register.SELF_TEST_Y_GYRO);
            gyroSelfTest.Z = ReadByte(Register.SELF_TEST_Z_GYRO);
            accSelfTest.X = ReadByte(Register.SELF_TEST_X_ACCEL);
            accSelfTest.Y = ReadByte(Register.SELF_TEST_Y_ACCEL);
            accSelfTest.Z = ReadByte(Register.SELF_TEST_Z_ACCEL);

            // Calculate all factory trim
            accFactoryTrim.X = (float)((2620 / 1 << gyroAccScale) * (Math.Pow(1.01, accSelfTest.X - 1.0)));
            accFactoryTrim.Y = (float)((2620 / 1 << gyroAccScale) * (Math.Pow(1.01, accSelfTest.Y - 1.0)));
            accFactoryTrim.Z = (float)((2620 / 1 << gyroAccScale) * (Math.Pow(1.01, accSelfTest.Z - 1.0)));
            gyroFactoryTrim.X = (float)((2620 / 1 << gyroAccScale) * (Math.Pow(1.01, gyroSelfTest.X - 1.0)));
            gyroFactoryTrim.Y = (float)((2620 / 1 << gyroAccScale) * (Math.Pow(1.01, gyroSelfTest.Y - 1.0)));
            gyroFactoryTrim.Z = (float)((2620 / 1 << gyroAccScale) * (Math.Pow(1.01, gyroSelfTest.Z - 1.0)));

            if (gyroFactoryTrim.X != 0)
            {
                gyroAvegage.X = (gyroSelfTestAverage.X - gyroAvegage.X) / gyroFactoryTrim.X;
            }
            else
            {
                gyroAvegage.X = Math.Abs(gyroSelfTestAverage.X - gyroAvegage.X);
            }

            if (gyroFactoryTrim.Y != 0)
            {
                gyroAvegage.Y = (gyroSelfTestAverage.Y - gyroAvegage.Y) / gyroFactoryTrim.Y;
            }
            else
            {
                gyroAvegage.Y = Math.Abs(gyroSelfTestAverage.Y - gyroAvegage.Y);
            }

            if (gyroFactoryTrim.Z != 0)
            {
                gyroAvegage.Z = (gyroSelfTestAverage.Z - gyroAvegage.Z) / gyroFactoryTrim.Z;
            }
            else
            {
                gyroAvegage.Z = Math.Abs(gyroSelfTestAverage.Z - gyroAvegage.Z);
            }

            // Accelerator
            if (accFactoryTrim.X != 0)
            {
                accAverage.X = (accSelfTestAverage.X - accAverage.X) / accFactoryTrim.X;
            }
            else
            {
                accAverage.X = Math.Abs(accSelfTestAverage.X - accSelfTestAverage.X);
            }

            if (accFactoryTrim.Y != 0)
            {
                accAverage.Y = (accSelfTestAverage.Y - accAverage.Y) / accFactoryTrim.Y;
            }
            else
            {
                accAverage.Y = Math.Abs(accSelfTestAverage.Y - accSelfTestAverage.Y);
            }

            if (accFactoryTrim.Z != 0)
            {
                accAverage.Z = (accSelfTestAverage.Z - accAverage.Z) / accFactoryTrim.Z;
            }
            else
            {
                accAverage.Z = Math.Abs(accSelfTestAverage.Z - accSelfTestAverage.Z);
            }

            return (gyroAvegage, accAverage);
        }

        #endregion

        #region I2C

        /// <summary>
        /// Write data on any of the I2C slave attached to the MPU9250
        /// </summary>
        /// <param name="i2cChannel">The slave channel to attached to the I2C device</param>
        /// <param name="address">The I2C address of the slave I2C element</param>
        /// <param name="register">The register to write to the slave I2C element</param>
        /// <param name="data">The byte data to write to the slave I2C element</param>
        public void WriteByteToSlaveDevice(I2cChannel i2cChannel, byte address, byte register, byte data)
        {
            // I2C_SLVx_ADDR += 3 * i2cChannel
            byte slvAddress = (byte)((byte)Register.I2C_SLV0_ADDR + 3 * (byte)i2cChannel);
            Span<byte> dataout = stackalloc byte[2]
            {
                slvAddress, address
            };
            _i2cDevice.Write(dataout);
            // I2C_SLVx_REG = I2C_SLVx_ADDR + 1
            dataout[0] = (byte)(slvAddress + 1);
            dataout[1] = register;
            _i2cDevice.Write(dataout);
            // I2C_SLVx_D0 =  I2C_SLV0_DO + i2cChannel
            // Except Channel4
            byte channelData = i2cChannel != I2cChannel.Slave4 ? (byte)((byte)Register.I2C_SLV0_DO + (byte)i2cChannel) : (byte)Register.I2C_SLV4_DO;
            dataout[0] = channelData;
            dataout[1] = data;
            _i2cDevice.Write(dataout);
            // I2C_SLVx_CTRL = I2C_SLVx_ADDR + 2
            dataout[0] = (byte)(slvAddress + 2);
            dataout[1] = 0x81;
            _i2cDevice.Write(dataout);
        }

        /// <summary>
        /// Read data from any of the I2C slave attached to the MPU9250
        /// </summary>
        /// <param name="i2cChannel">The slave channel to attached to the I2C device</param>
        /// <param name="address">The I2C address of the slave I2C element</param>
        /// <param name="register">The register to read from the slave I2C element</param>
        /// <param name="readBytes">The read data</param>
        public void ReadByteFromSlaveDevice(I2cChannel i2cChannel, byte address, byte register, Span<byte> readBytes)
        {
            if (readBytes.Length > 24)
            {
                throw new ArgumentException($"Can't read more than 24 bytes at once");
            }

            byte slvAddress = (byte)((byte)Register.I2C_SLV0_ADDR + 3 * (byte)i2cChannel);
            Span<byte> dataout = stackalloc byte[2]
            {
                slvAddress, (byte)(address | 0x80)
            };
            _i2cDevice.Write(dataout);
            dataout[0] = (byte)(slvAddress + 1);
            dataout[1] = (byte)register;
            _i2cDevice.Write(dataout);
            dataout[0] = (byte)(slvAddress + 2);
            dataout[1] = (byte)(0x80 | readBytes.Length);
            _i2cDevice.Write(dataout);
            // Just need to wait a very little bit
            // For data transfer to happen and process on the MPU9250 side
            ///DelayHelper.DelayMicroseconds(140 + readBytes.Length * 10, false);
            _i2cDevice.WriteByte((byte)Register.EXT_SENS_DATA_00);
            _i2cDevice.Read(readBytes);
        }

        internal void WriteRegister(Register reg, byte data)
        {
            Span<byte> dataout = stackalloc byte[]
            {
                (byte)reg, data
            };
            _i2cDevice.Write(dataout);
        }

        internal byte ReadByte(Register reg)
        {
            _i2cDevice.WriteByte((byte)reg);
            return _i2cDevice.ReadByte();
        }

        internal void ReadBytes(Register reg, Span<byte> readBytes)
        {
            _i2cDevice.WriteByte((byte)reg);
            _i2cDevice.Read(readBytes);
        }

        /// <summary>
        /// Cleanup everything
        /// </summary>
        public void Dispose()
        {
            _i2cDevice?.Dispose();
            _i2cDevice = null;
        }

        #endregion
    }


    /// <summary>
    /// Range of measurement used by the accelerometer in G
    /// </summary>
    public enum AccelerometerRange
    {
        /// <summary>
        /// Range 2G
        /// </summary>
        Range02G = 0,

        /// <summary>
        /// Range 4G
        /// </summary>
        Range04G = 1,

        /// <summary>
        /// Range 8G
        /// </summary>
        Range08G = 2,

        /// <summary>
        /// Range 16G
        /// </summary>
        Range16G = 3
    }

    /// <summary>
    /// Bandwidth used for normal measurement of the accelerometer
    /// using filter block. This can be further reduced using
    /// SampleRateDivider with all modes except 1130Hz.
    /// </summary>
    public enum AccelerometerBandwidth
    {
        /// <summary>
        /// Bandwidth 1130Hz
        /// </summary>
        Bandwidth1130Hz = 0,

        /// <summary>
        /// Bandwidth 460Hz
        /// </summary>
        Bandwidth0460Hz = 0x08,

        /// <summary>
        /// Bandwidth 184Hz
        /// </summary>
        Bandwidth0184Hz = 0x09,

        /// <summary>
        /// Bandwidth 92Hz
        /// </summary>
        Bandwidth0092Hz = 0x0A,

        /// <summary>
        /// Bandwidth 41Hz
        /// </summary>
        Bandwidth0041Hz = 0x0B,

        /// <summary>
        /// Bandwidth 20Hz
        /// </summary>
        Bandwidth0020Hz = 0x0C,

        /// <summary>
        /// Bandwidth 10Hz
        /// </summary>
        Bandwidth0010Hz = 0x0E,

        /// <summary>
        /// Bandwidth 5Hz
        /// </summary>
        Bandwidth0005Hz = 0x0F,
    }

    /// <summary>
    /// Range used for the gyroscope precision measurement
    /// </summary>
    public enum GyroscopeRange
    {
        /// <summary>
        /// Range 250Dps
        /// </summary>
        Range0250Dps = 0,

        /// <summary>
        /// Range 500Dps
        /// </summary>
        Range0500Dps = 1,

        /// <summary>
        /// Range 1000Dps
        /// </summary>
        Range1000Dps = 2,

        /// <summary>
        /// Range 2000Dps
        /// </summary>
        Range2000Dps = 3,
    }

    /// <summary>
    /// Gyroscope frequency used for measurement
    /// </summary>
    public enum GyroscopeBandwidth
    {
        /// <summary>
        /// Bandwidth 250Hz
        /// </summary>
        Bandwidth0250Hz = 0,

        /// <summary>
        /// Bandwidth 184Hz
        /// </summary>
        Bandwidth0184Hz = 1,

        /// <summary>
        /// Bandwidth 92Hz
        /// </summary>
        Bandwidth0092Hz = 2,

        /// <summary>
        /// Bandwidth 41Hz
        /// </summary>
        Bandwidth0041Hz = 3,

        /// <summary>
        /// Bandwidth 20Hz
        /// </summary>
        Bandwidth0020Hz = 4,

        /// <summary>
        /// Bandwidth 10Hz
        /// </summary>
        Bandwidth0010Hz = 5,

        /// <summary>
        /// Bandwidth 5Hz
        /// </summary>
        Bandwidth0005Hz = 6,

        /// <summary>
        /// Bandwidth 3600Hz
        /// </summary>
        Bandwidth3600Hz = 7,

        /// <summary>
        /// Bandwidth 3600Hz FS 32
        /// </summary>
        Bandwidth3600HzFS32 = -1,

        /// <summary>
        /// Bandwidth 8800Hz FS 32
        /// </summary>
        Bandwidth8800HzFS32 = -2,
    }

    /// <summary>
    /// All the documented registers for the MPU99250
    /// </summary>
    internal enum Register
    {
        /// <summary>
        /// X Gyroscope Self-Test Register
        /// </summary>
        SELF_TEST_X_GYRO = 0x00,

        /// <summary>
        /// Y Gyroscope Self-Test Register
        /// </summary>
        SELF_TEST_Y_GYRO = 0x01,

        /// <summary>
        /// Z Gyroscope Self-Test Register
        /// </summary>
        SELF_TEST_Z_GYRO = 0x02,

        /// <summary>
        /// X Accelerometer Self-Test Register
        /// </summary>
        SELF_TEST_X_ACCEL = 0x0D,

        /// <summary>
        /// Y Accelerometer Self-Test Register
        /// </summary>
        SELF_TEST_Y_ACCEL = 0x0E,

        /// <summary>
        /// Z Accelerometer Self-Test Register
        /// </summary>
        SELF_TEST_Z_ACCEL = 0x0F,

        /// <summary>
        /// X Gyroscope High byte Offset Registers
        /// </summary>
        XG_OFFSET_H = 0x13,

        /// <summary>
        /// X Gyroscope Low byte Offset Registers
        /// </summary>
        XG_OFFSET_L = 0x14,

        /// <summary>
        /// Y Gyroscope High byte Offset Registers
        /// </summary>
        YG_OFFSET_H = 0x15,

        /// <summary>
        /// Y Gyroscope Low byte Offset Registers
        /// </summary>
        YG_OFFSET_L = 0x16,

        /// <summary>
        /// Z Gyroscope High byte Offset Registers
        /// </summary>
        ZG_OFFSET_H = 0x17,

        /// <summary>
        /// Z Gyroscope Low byte Offset Registers
        /// </summary>
        ZG_OFFSET_L = 0x18,

        /// <summary>
        /// Sample Rate Divider
        /// </summary>
        SMPLRT_DIV = 0x19,

        /// <summary>
        /// Configuration
        /// </summary>
        CONFIG = 0x1A,

        /// <summary>
        /// Gyroscope Configuration
        /// </summary>
        GYRO_CONFIG = 0x1B,

        /// <summary>
        /// Accelerometer Configuration
        /// </summary>
        ACCEL_CONFIG = 0x1C,

        /// <summary>
        /// Accelerometer Configuration 2
        /// </summary>
        ACCEL_CONFIG_2 = 0x1D,

        /// <summary>
        /// Low Power Accelerometer ODR Control
        /// </summary>
        LP_ACCEL_ODR = 0x1E,

        /// <summary>
        /// Wake-on Motion Threshold
        /// </summary>
        WOM_THR = 0x1F,

        /// <summary>
        /// FIFO Enable
        /// </summary>
        FIFO_EN = 0x23,

        /// <summary>
        /// I2C Master Control
        /// </summary>
        I2C_MST_CTRL = 0x24,

        /// <summary>
        /// I2C Slave 0 Control Address
        /// </summary>
        I2C_SLV0_ADDR = 0x25,

        /// <summary>
        /// I2C Slave 0 Control Register
        /// </summary>
        I2C_SLV0_REG = 0x26,

        /// <summary>
        /// I2C Slave 0 Control
        /// </summary>
        I2C_SLV0_CTRL = 0x27,

        /// <summary>
        /// I2C Slave 1 Control Address
        /// </summary>
        I2C_SLV1_ADDR = 0x28,

        /// <summary>
        /// I2C Slave 1 Control Register
        /// </summary>
        I2C_SLV1_REG = 0x29,

        /// <summary>
        /// I2C Slave 1 Control
        /// </summary>
        I2C_SLV1_CTRL = 0x2A,

        /// <summary>
        /// I2C Slave 2 Control Address
        /// </summary>
        I2C_SLV2_ADDR = 0x2B,

        /// <summary>
        /// I2C Slave 2 Control Register
        /// </summary>
        I2C_SLV2_REG = 0x2C,

        /// <summary>
        /// I2C Slave 2 Control
        /// </summary>
        I2C_SLV2_CTRL = 0x2D,

        /// <summary>
        /// I2C Slave 3 Control Address
        /// </summary>
        I2C_SLV3_ADDR = 0x2E,

        /// <summary>
        /// I2C Slave 3 Control Register
        /// </summary>
        I2C_SLV3_REG = 0x2F,

        /// <summary>
        /// I2C Slave 3 Control
        /// </summary>
        I2C_SLV3_CTRL = 0x30,

        /// <summary>
        /// I2C Slave 4 Control Address
        /// </summary>
        I2C_SLV4_ADDR = 0x31,

        /// <summary>
        /// I2C Slave 4 Control Register
        /// </summary>
        I2C_SLV4_REG = 0x32,

        /// <summary>
        /// I2C Slave 4 Control Data to Write
        /// </summary>
        I2C_SLV4_DO = 0x33,

        /// <summary>
        /// I2C Slave 4 Control
        /// </summary>
        I2C_SLV4_CTRL = 0x34,

        /// <summary>
        /// I2C Slave 4 Control Data to Read
        /// </summary>
        I2C_SLV4_DI = 0x35,

        /// <summary>
        /// I2C Master Status
        /// </summary>
        I2C_MST_STATUS = 0x36,

        /// <summary>
        /// INT Pin / Bypass Enable Configuration
        /// </summary>
        INT_PIN_CFG = 0x37,

        /// <summary>
        /// Interrupt Enable
        /// </summary>
        INT_ENABLE = 0x38,

        /// <summary>
        /// Interrupt Status
        /// </summary>
        INT_STATUS = 0x3A,

        /// <summary>
        /// High byte of accelerometer X-axis data
        /// </summary>
        ACCEL_XOUT_H = 0x3B,

        /// <summary>
        /// Low byte of accelerometer X-axis data
        /// </summary>
        ACCEL_XOUT_L = 0x3C,

        /// <summary>
        /// High byte of accelerometer Y-axis data
        /// </summary>
        ACCEL_YOUT_H = 0x3D,

        /// <summary>
        /// Low byte of accelerometer Y-axis data
        /// </summary>
        ACCEL_YOUT_L = 0x3E,

        /// <summary>
        /// High byte of accelerometer Z-axis data
        /// </summary>
        ACCEL_ZOUT_H = 0x3F,

        /// <summary>
        /// Low byte of accelerometer Z-axis data
        /// </summary>
        ACCEL_ZOUT_L = 0x40,

        /// <summary>
        /// High byte of the temperature sensor output
        /// </summary>
        TEMP_OUT_H = 0x41,

        /// <summary>
        /// Low byte of the temperature sensor output
        /// </summary>
        TEMP_OUT_L = 0x42,

        /// <summary>
        /// High byte of the X-Axis gyroscope output
        /// </summary>
        GYRO_XOUT_H = 0x43,

        /// <summary>
        /// Low byte of the X-Axis gyroscope output
        /// </summary>
        GYRO_XOUT_L = 0x44,

        /// <summary>
        /// High byte of the Y-Axis gyroscope output
        /// </summary>
        GYRO_YOUT_H = 0x45,

        /// <summary>
        /// Low byte of the Y-Axis gyroscope output
        /// </summary>
        GYRO_YOUT_L = 0x46,

        /// <summary>
        /// High byte of the Z-Axis gyroscope output
        /// </summary>
        GYRO_ZOUT_H = 0x47,

        /// <summary>
        /// Low byte of the Z-Axis gyroscope output
        /// </summary>
        GYRO_ZOUT_L = 0x48,

        /// <summary>
        /// External Sensor Data byte 0
        /// </summary>
        EXT_SENS_DATA_00 = 0x49,

        /// <summary>
        /// External Sensor Data byte 1
        /// </summary>
        EXT_SENS_DATA_01 = 0x4A,

        /// <summary>
        /// External Sensor Data byte 2
        /// </summary>
        EXT_SENS_DATA_02 = 0x4B,

        /// <summary>
        /// External Sensor Data byte 3
        /// </summary>
        EXT_SENS_DATA_03 = 0x4C,

        /// <summary>
        /// External Sensor Data byte 4
        /// </summary>
        EXT_SENS_DATA_04 = 0x4D,

        /// <summary>
        /// External Sensor Data byte 5
        /// </summary>
        EXT_SENS_DATA_05 = 0x4E,

        /// <summary>
        /// External Sensor Data byte 6
        /// </summary>
        EXT_SENS_DATA_06 = 0x4F,

        /// <summary>
        /// External Sensor Data byte 7
        /// </summary>
        EXT_SENS_DATA_07 = 0x50,

        /// <summary>
        /// External Sensor Data byte 8
        /// </summary>
        EXT_SENS_DATA_08 = 0x51,

        /// <summary>
        /// External Sensor Data byte 9
        /// </summary>
        EXT_SENS_DATA_09 = 0x52,

        /// <summary>
        /// External Sensor Data byte 10
        /// </summary>
        EXT_SENS_DATA_10 = 0x53,

        /// <summary>
        /// External Sensor Data byte 11
        /// </summary>
        EXT_SENS_DATA_11 = 0x54,

        /// <summary>
        /// External Sensor Data byte 12
        /// </summary>
        EXT_SENS_DATA_12 = 0x55,

        /// <summary>
        /// External Sensor Data byte 13
        /// </summary>
        EXT_SENS_DATA_13 = 0x56,

        /// <summary>
        /// External Sensor Data byte 14
        /// </summary>
        EXT_SENS_DATA_14 = 0x57,

        /// <summary>
        /// External Sensor Data byte 15
        /// </summary>
        EXT_SENS_DATA_15 = 0x58,

        /// <summary>
        /// External Sensor Data byte 16
        /// </summary>
        EXT_SENS_DATA_16 = 0x59,

        /// <summary>
        /// External Sensor Data byte 17
        /// </summary>
        EXT_SENS_DATA_17 = 0x5A,

        /// <summary>
        /// External Sensor Data byte 18
        /// </summary>
        EXT_SENS_DATA_18 = 0x5B,

        /// <summary>
        /// External Sensor Data byte 19
        /// </summary>
        EXT_SENS_DATA_19 = 0x5C,

        /// <summary>
        /// External Sensor Data byte 20
        /// </summary>
        EXT_SENS_DATA_20 = 0x5D,

        /// <summary>
        /// External Sensor Data byte 21
        /// </summary>
        EXT_SENS_DATA_21 = 0x5E,

        /// <summary>
        /// External Sensor Data byte 22
        /// </summary>
        EXT_SENS_DATA_22 = 0x5F,

        /// <summary>
        /// External Sensor Data byte 23
        /// </summary>
        EXT_SENS_DATA_23 = 0x60,

        /// <summary>
        /// I2C Slave 0 Control Data to Write
        /// </summary>
        I2C_SLV0_DO = 0x63,

        /// <summary>
        /// I2C Slave 1 Control Data to Write
        /// </summary>
        I2C_SLV1_DO = 0x64,

        /// <summary>
        /// I2C Slave 2 Control Data to Write
        /// </summary>
        I2C_SLV2_DO = 0x65,

        /// <summary>
        /// I2C Slave 3 Control Data to Write
        /// </summary>
        I2C_SLV3_DO = 0x66,

        /// <summary>
        /// I2C Master Delay Control
        /// </summary>
        I2C_MST_DELAY_CTRL = 0x67,

        /// <summary>
        /// Signal Path Reset
        /// </summary>
        SIGNAL_PATH_RESET = 0x68,

        /// <summary>
        /// Accelerometer Interrupt Control
        /// </summary>
        MOT_DETECT_CTRL = 0x69,

        /// <summary>
        /// User Control
        /// </summary>
        USER_CTRL = 0x6A,

        /// <summary>
        /// Power Management 1
        /// </summary>
        PWR_MGMT_1 = 0x6B,

        /// <summary>
        /// Power Management 2
        /// </summary>
        PWR_MGMT_2 = 0x6C,

        /// <summary>
        /// FIFO Count Registers High byte
        /// </summary>
        FIFO_COUNTH = 0x72,

        /// <summary>
        /// FIFO Count Registers Low byte
        /// </summary>
        FIFO_COUNTL = 0x73,

        /// <summary>
        /// FIFO Read Write
        /// </summary>
        FIFO_R_W = 0x74,

        /// <summary>
        /// Who Am I
        /// </summary>
        WHO_AM_I = 0x75,

        /// <summary>
        /// X-axis Accelerometer Offset Register High byte
        /// </summary>
        XA_OFFSET_H = 0x77,

        /// <summary>
        /// X-axis Accelerometer Offset Register Low byte
        /// </summary>
        XA_OFFSET_L = 0x78,

        /// <summary>
        /// Y-axis Accelerometer Offset Register High byte
        /// </summary>
        YA_OFFSET_H = 0x7A,

        /// <summary>
        /// Y-axis Accelerometer Offset Register Low byte
        /// </summary>
        YA_OFFSET_L = 0x7B,

        /// <summary>
        /// Z-axis Accelerometer Offset Register High byte
        /// </summary>
        ZA_OFFSET_H = 0x7D,

        /// <summary>
        /// Z-axis Accelerometer Offset Register Low byte
        /// </summary>
        ZA_OFFSET_L = 0x7E
    }

    /// <summary>
    /// Disable modes for the gyroscope and accelerometer axes
    /// </summary>
    [Flags]
    public enum DisableModes
    {
        /// <summary>
        /// Disable None
        /// </summary>
        DisableNone = 0,

        /// <summary>
        /// Disable Accelerometer X
        /// </summary>
        DisableAccelerometerX = 0b0010_0000,

        /// <summary>
        /// Disable Accelerometer Y
        /// </summary>
        DisableAccelerometerY = 0b0001_0000,

        /// <summary>
        /// Disable Accelerometer Z
        /// </summary>
        DisableAccelerometerZ = 0b0000_1000,

        /// <summary>
        /// Disable Gyroscope X
        /// </summary>
        DisableGyroscopeX = 0b0000_0100,

        /// <summary>
        /// Disable Gyroscope Y
        /// </summary>
        DisableGyroscopeY = 0b0000_0010,

        /// <summary>
        /// Disable Gyroscope Z
        /// </summary>
        DisableGyroscopeZ = 0b0000_0001,
    }

    /// <summary>
    /// I2C slave channel
    /// </summary>
    public enum I2cChannel
    {
        /// <summary>
        /// Slave 0
        /// </summary>
        Slave0 = 0,

        /// <summary>
        /// Slave 1
        /// </summary>
        Slave1 = 1,

        /// <summary>
        /// Slave 2
        /// </summary>
        Slave2 = 2,

        /// <summary>
        /// Slave 3
        /// </summary>
        Slave3 = 3,

        /// <summary>
        /// Slave 4
        /// </summary>
        Slave4 = 4,
    }

    [Flags]
    internal enum UserControls
    {
        None = 0b0000_0000,
        // 1 – Reset all gyro digital signal path, accel digital signal path, and temp digital signal path.
        // This bit also clears all the sensor registers.  SIG_COND_RST is a pulse of one clk8M wide.
        SIG_COND_RST = 0b0000_0001,
        // 1 – Reset I2C Master module. Reset is asynchronous.  This bit auto clears after one clock cycle.
        // NOTE:  This bit should only be set when the I2C master has hung.  If this bit is set during an active I2C master transaction,
        // the I2C slave will hang, which will require the host to reset the slave.
        I2C_MST_RST = 0b0000_0010,
        // 1 – Reset FIFO module. Reset is asynchronous.  This bit auto clears after one clock cycle
        FIFO_RST = 0b0000_0100,
        // 1 – Reset I2C Slave module and put the serial interface in SPI mode only.  This bit auto clears after one clock cycle.
        I2C_IF_DIS = 0b0001_0000,
        // 1 – Enable the I2C Master I/F module; pins ES_DA and ES_SCL are isolated from pins SDA/SDI and SCL/ SCLK.
        // 0 – Disable I2C Master I/F module; pins ES_DA and ES_SCL are logically driven by pins SDA/SDI and SCL/ SCLK.
        // NOTE:  DMP will run when enabled, even if all internal sensors are disabled, except when the sample rate is set to 8Khz
        I2C_MST_EN = 0b0010_0000,
        // 1 – Enable FIFO operation mode.
        // 0 – Disable FIFO access from serial interface.
        // To disable FIFO writes by dma, use FIFO_EN register.
        // To disable possible FIFO writes from DMP, disable the DMP.
        FIFO_EN = 0b0100_0000,
    }

    /// <summary>
    /// Frequency of the slave I2C bus
    /// </summary>
    public enum I2cBusFrequency
    {
        /// <summary>
        /// Frequency 348kHz
        /// </summary>
        Frequency348kHz = 0,

        /// <summary>
        /// Frequency 333kHz
        /// </summary>
        Frequency333kHz = 1,

        /// <summary>
        /// Frequency 320kHz
        /// </summary>
        Frequency320kHz = 2,

        /// <summary>
        /// Frequency 308kHz
        /// </summary>
        Frequency308kHz = 3,

        /// <summary>
        /// Frequency 296kHz
        /// </summary>
        Frequency296kHz = 4,

        /// <summary>
        /// Frequency 286kHz
        /// </summary>
        Frequency286kHz = 5,

        /// <summary>
        /// Frequency 276kHz
        /// </summary>
        Frequency276kHz = 6,

        /// <summary>
        /// Frequency 267kHz
        /// </summary>
        Frequency267kHz = 7,

        /// <summary>
        /// Frequency 258kHz
        /// </summary>
        Frequency258kHz = 8,

        /// <summary>
        /// Frequency 500kHz
        /// </summary>
        Frequency500kHz = 9,

        /// <summary>
        /// Frequency 471kHz
        /// </summary>
        Frequency471kHz = 10,

        /// <summary>
        /// Frequency 444kHz
        /// </summary>
        Frequency444kHz = 11,

        /// <summary>
        /// Frequency 421kHz
        /// </summary>
        Frequency421kHz = 12,

        /// <summary>
        /// Frequency 400kHz
        /// </summary>
        Frequency400kHz = 13,

        /// <summary>
        /// Frequency 381kHz
        /// </summary>
        Frequency381kHz = 14,

        /// <summary>
        /// Frequency 364kHz
        /// </summary>
        Frequency364kHz = 15,
    }

    /// <summary>
    /// You can select the sensors from which you want data
    /// FIFO modes used to select the accelerometer, gyroscope axises, temperature and I2C slaves
    /// You can combine any of those modes.
    /// </summary>
    [Flags]
    public enum FifoModes
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0b0000_0000,

        /// <summary>
        /// I2C Slave 0
        /// </summary>
        I2CSlave0 = 0b0000_0001,

        /// <summary>
        /// I2C Slave 1
        /// </summary>
        I2CSlave1 = 0b0000_0010,

        /// <summary>
        /// I2C Slave 2
        /// </summary>
        I2CSlave2 = 0b0000_0100,

        /// <summary>
        /// Accelerometer
        /// </summary>
        Accelerometer = 0b0000_1000,

        /// <summary>
        /// Gyroscope Z
        /// </summary>
        GyroscopeZ = 0b0001_0000,

        /// <summary>
        /// Gyroscope Y
        /// </summary>
        GyroscopeY = 0b0010_0000,

        /// <summary>
        /// Gyroscope X
        /// </summary>
        GyroscopeX = 0b0100_0000,

        /// <summary>
        /// Temperature
        /// </summary>
        Temperature = 0b1000_0000
    }

    internal enum ClockSource
    {
        /// <summary>
        /// Internal 20MHz
        /// </summary>
        Internal20MHz = 0,

        /// <summary>
        /// Auto Select
        /// </summary>
        AutoSelect = 1,

        /// <summary>
        /// Stop Clock
        /// </summary>
        StopClock = 7,
    }

    /// <summary>
    /// Frequency used to measure data for the low power consumption mode
    /// The chip will wake up to take a sample of accelerometer
    /// </summary>
    public enum AccelerometerLowPowerFrequency
    {
        /// <summary>
        /// Frequency 0.24Hz
        /// </summary>
        Frequency0Dot24Hz = 0,

        /// <summary>
        /// Frequency 0.49Hz
        /// </summary>
        Frequency0Dot49Hz = 1,

        /// <summary>
        /// Frequency 0.98Hz
        /// </summary>
        Frequency0Dot98Hz = 2,

        /// <summary>
        /// Frequency 1.95Hz
        /// </summary>
        Frequency1Dot95Hz = 3,

        /// <summary>
        /// Frequency 3.91Hz
        /// </summary>
        Frequency3Dot91Hz = 4,

        /// <summary>
        /// Frequency 7.81Hz
        /// </summary>
        Frequency7dot81Hz = 5,

        /// <summary>
        /// Frequency 15.63Hz
        /// </summary>
        Frequency15Dot63Hz = 6,

        /// <summary>
        /// Frequency 31.25Hz
        /// </summary>
        Frequency31Dot25Hz = 7,

        /// <summary>
        /// Frequency 62.5Hz
        /// </summary>
        Frequency62Dot5Hz = 8,

        /// <summary>
        /// Frequency 125Hz
        /// </summary>
        Frequency125Hz = 9,

        /// <summary>
        /// Frequency 250Hz
        /// </summary>
        Frequency250Hz = 10,

        /// <summary>
        /// Frequency 500Hz
        /// </summary>
        Frequency500Hz = 11,
    }
}
