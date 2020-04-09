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
using System.Device.Gpio;

namespace Iot.Device.IrReceiver
{
    public class IrReceiver : IDisposable
    {
		private GpioController digital = new GpioController(PinNumberingScheme.Logical);
		private byte IR;
		

		public IrReceiver(byte IR)
		{
			this.IR = IR;

			digital.OpenPin(IR, PinMode.Input);
		}

		public int GetKey()
		{
			int[] data = { 0, 0, 0, 0 };
			int count = 0;

			if (digital.Read(IR) == PinValue.Low)
			{
				count = 0;
				while(digital.Read(IR) == PinValue.Low & count < 200)
				{
					count += 1;
					DelayHelper.DelayMicroseconds(60, true);
				}
				if(count < 10)
				{
					return 0;
				}
				count = 0;
				while (digital.Read(IR) == PinValue.High & count < 80)
				{
					count += 1;
					DelayHelper.DelayMicroseconds(60, true);
				}

				int idx = 0;
				int cnt = 0;
				for (int i = 0; i < 32; i++)
				{
					count = 0;
					while (digital.Read(IR) == PinValue.Low & count < 12)
					{
						count += 1;
						DelayHelper.DelayMicroseconds(60, true);
					}
					count = 0;
					while (digital.Read(IR) == PinValue.High & count < 40)
					{
						count += 1;
						DelayHelper.DelayMicroseconds(60, true);
					}
					if (count > 7)
					{
						data[idx] |= 1 << cnt;
					}
					if (cnt == 7)
					{
						cnt = 0;
						idx += 1;
					}
					else
					{
						cnt += 1;
					}
				}
			}
			if (data[0] + data[1] == 0xFF & data[2]+data[3] == 0xFF)
			{
				return data[2];
			}
			else
			{
				return 999;
			}
		}

		public void Dispose()
		{
			digital.Dispose();
		}

	}
}
