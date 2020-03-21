// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Spi;
using System.Diagnostics.CodeAnalysis;
using System.Device;
using System.Device.I2c;
using System.IO;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using Iot.Units;
using System.Device.Gpio;

namespace Iot.Device.Adc
{
	/// <summary>
	/// TLC1543 ADC device
	/// </summary>
	public class Tlc1543 : IDisposable
	{
		public GpioController digital = new GpioController(PinNumberingScheme.Logical);
		public byte CS, DOUT, ADDR, IOCLK, numSensors;
		public int calibratedMin, calibratedMax;
		public SortedList<byte, uint> last_values = new SortedList<byte, uint>();
		public SortedList<byte, uint> values = new SortedList<byte, uint>();

		/// <summary>
		/// the number of single ended input channel on the ADC
		/// </summary>
		public Tlc1543 (byte CS, byte DOUT, byte ADDR, byte IOCLK, byte numSensors)
		{
			this.numSensors = numSensors;
			for (byte i = 0; i < numSensors; i++)
			{
				values.Add(i, 0);
				last_values.Add(i, 0);
			}
			this.calibratedMin = 0 * numSensors;
			this.calibratedMax = 1023 * numSensors;
			
			this.CS = CS;
			this.DOUT = DOUT;
			this.ADDR = ADDR;
			this.IOCLK = IOCLK;
			digital.OpenPin(CS, PinMode.Output);
			digital.OpenPin(DOUT, PinMode.InputPullUp);
			digital.OpenPin(ADDR, PinMode.Output);
			digital.OpenPin(IOCLK, PinMode.Output);
		}

		/// <summary>
		/// 	Reads the sensor values into an array. There *MUST* be space
		/// for as many values as there were sensors specified in the constructor.
		/// Example usage:
		/// unsigned int sensor_values[8];
		/// sensors.read(sensor_values);
		/// The values returned are a measure of the reflectance in abstract units,
		/// with higher values corresponding to lower reflectance(e.g.a black
		/// surface or a void).
		/// </summary>
		public SortedList<byte, uint> AnalogRead()
		{
			//SortedList<byte, uint> values = new SortedList<byte, uint>(numSensors);
			for (byte i = 0; i < numSensors; i++)
			{
				values[i] = 0;
			}
			for (byte j = 0; j < numSensors; j++)
			{
				digital.Write(CS, 0);
				for (int i = 0; i < 4; i++)
				{
					//send 4-bit Address
					if ((j >> (3 - i) & 0x01) != 0)
					{
						digital.Write(ADDR, 1);
					}
					else
					{
						digital.Write(ADDR, 0);
					}
					// read MSB 4-bit data
					values[j] <<= 1;
					if(digital.Read(DOUT) == PinValue.High)
					{
						values[j] |= 0x01;
					}
					digital.Write(IOCLK, 1);
					digital.Write(IOCLK, 0);
				}
				for (int i = 0; i < 6; i++)
				{
					// read LSB 8-bit data
					values[j] <<= 1;
					if(digital.Read(DOUT) == PinValue.High)
					{
						values[j] |= 0x01;
					}
					digital.Write(IOCLK, 1);
					digital.Write(IOCLK, 0);
				}
				Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
				digital.Write(IOCLK, 1);
			}
			last_values = values;
			return values;
		}

		/// <summary>
		/// Reads the sensors 10 times and uses the results for
		/// calibration. The sensor values are not returned; instead, the
		/// maximum and minimum values found over time are stored internally
		/// and used for the readCalibrated() method.
		/// </summary>
		public void Calibrate()
		{

		}

		/// <summary>
		/// Returns values calibrated to a value between 0 and 1000, where
		/// 0 corresponds to the minimum value read by calibrate() and 1000
		/// corresponds to the maximum value. Calibration values are
		/// stored separately for each sensor, so that differences in the
		/// sensors are accounted for automatically.
		/// </summary>
		public void ReadCalibrated()
		{

		}

		/// <summary>
		/// Reads a value from the device
		/// </summary>
		/// <param name="channel">Channel which value should be read from (valid values: 0 to channelcount - 1)</param>
		/// <returns>A value corresponding to relative voltage level on specified device channel</returns>
		public void ReadLine()
		{
			
		}

		/// <summary>
		/// Cleanup everything
		/// </summary>
		public void Dispose()
		{
			digital?.Dispose();
			digital = null;
		}
	}
}
