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
		private GpioController digital = new GpioController(PinNumberingScheme.Logical);
		
		public SortedList<byte, uint> values = new SortedList<byte, uint>(11);
		public SortedList<byte, uint> last_values = new SortedList<byte, uint>(11);
		private byte CS, DOUT, ADDR, IOCLK, EOC, channels = 11;
		public Channel chargeChannel = Channel.SelfTest512;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ADDR">Address</param>
		/// <param name="CS">Chip Select</param>
		/// <param name="DOUT">Data Out</param>
		/// <param name="IOCLK">I/O Clock</param>
		public Tlc1543(byte ADDR, byte CS, byte DOUT, byte IOCLK)
		{
			this.ADDR = ADDR;
			this.CS = CS;
			this.DOUT = DOUT;
			this.IOCLK = IOCLK;

			digital.OpenPin(ADDR, PinMode.Output);
			digital.OpenPin(CS, PinMode.Output);
			digital.OpenPin(DOUT, PinMode.InputPullUp);
			digital.OpenPin(IOCLK, PinMode.Output);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ADDR">Address</param>
		/// <param name="CS">Chip Select</param>
		/// <param name="DOUT">Data Out</param>
		/// <param name="EOC">End of Conversion</param>
		/// <param name="IOCLK">I/O Clock</param>
		public Tlc1543(byte ADDR, byte CS, byte DOUT, byte IOCLK, byte EOC)
		{
			this.ADDR = ADDR;
			this.CS = CS;
			this.DOUT = DOUT;
			this.EOC = EOC;
			this.IOCLK = IOCLK;
			
			digital.OpenPin(ADDR, PinMode.Output);
			digital.OpenPin(CS, PinMode.Output);
			digital.OpenPin(DOUT, PinMode.InputPullUp);
			digital.OpenPin(EOC, PinMode.InputPullUp);
			digital.OpenPin(IOCLK, PinMode.Output);
		}

		/// <summary>
		/// Reads the sensor values into an integer.
		/// The values returned are a measure of the reflectance in abstract units,
		/// with higher values corresponding to lower reflectance(e.g.a black
		/// surface or a void).
		/// </summary>
		/// <param name="channelNumber">Channel to be read</param>
		/// <returns>A 10 bit value corresponding to relative voltage level on specified device channel</returns>
		public int ReadChannel(Channel channelNumber)
		{
			Read(channelNumber);
			return Read(chargeChannel);
		}

		/// <summary>
		/// Reads the sensor values into an List
		/// The values returned are a measure of the reflectance in abstract units,
		/// with higher values corresponding to lower reflectance(e.g.a black
		/// surface or a void).
		/// </summary>
		/// <param name="channelList">List of channels to read</param>
		/// <returns>List of 10 bit values corresponding to relative voltage level on specified device channels</returns>
		public List<int> ReadChannel(List<Channel> channelList)
		{
			List<int> values = new List<int>(channelList.Count);
			Read(channelList[0]);
			for (int i = 1; i < channelList.Count; i++)
			{
				values.Add(Read(channelList[i]));
			}
			values.Add(Read(chargeChannel));
			return values;
		}

		private int Read(Channel channelNumber)
		{
			int value = 0;
			digital.Write(CS, 0);
			//1.425uS between CS => 0 and first ADDR
			//probably can omit that due to for loop and if condition
			//need to check CS(less than 0.8V) to first ADDR(minimum of 2V)
			//on osciloscope to check how much time it takes on RPi
			DelayHelper.DelayMicroseconds(1, false); 
			for (int i = 0; i < 10; i++)
			{
				if (i < 4)
				{
					//send 4-bit Address
					if (((byte)channelNumber >> (3 - i) & 0x01) != 0)
					{
						digital.Write(ADDR, 1);
					}
					else
					{
						digital.Write(ADDR, 0);
					}
				}
				// read 10-bit data (from earlier loop)
				value <<= 1;
				if (digital.Read(DOUT) == PinValue.High)
				{
					value |= 0x01;
				}
				//time from ADDR to IOCLK minimum 100ns
				digital.Write(IOCLK, 1);
				digital.Write(IOCLK, 0);
				//to check how fast can those clocks can be generated
			}
			digital.Write(CS, 1);
			DelayHelper.DelayMicroseconds(100, false);
			return value;
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
			//digital?.Dispose();
			digital = null;
		}

		public enum Channel
		{
			/// <summary>
			/// Channel A0
			/// </summary>
			A0 = 0,

			/// <summary>
			/// 
			/// </summary>
			A1 = 1,

			/// <summary>
			/// 
			/// </summary>
			A2 = 2,

			/// <summary>
			/// 
			/// </summary>
			A3 = 3,

			/// <summary>
			/// 
			/// </summary>
			A4 = 4,

			/// <summary>
			/// 
			/// </summary>
			A5 = 5,

			/// <summary>
			/// 
			/// </summary>
			A6 = 6,

			/// <summary>
			/// 
			/// </summary>
			A7 = 7,

			/// <summary>
			/// 
			/// </summary>
			A8 = 8,

			/// <summary>
			/// 
			/// </summary>
			A9 = 9,

			/// <summary>
			/// 
			/// </summary>
			A10 = 10,

			/// <summary>
			/// Self Test channel that should charge capacitors to (Vref+ - Vref-)/2
			/// Gives out value of (dec)512
			/// </summary>
			SelfTest512 = 11,

			/// <summary>
			/// Self Test channel that should charge capacitors to Vref-
			/// /// Gives out value of (dec)0
			/// </summary>
			SelfTest0 = 12,

			/// <summary>
			/// Self Test channel that should charge capacitors to Vref+
			/// /// Gives out value of (dec)1023
			/// </summary>
			SelfTest1024 = 13,
		}
	}
}
