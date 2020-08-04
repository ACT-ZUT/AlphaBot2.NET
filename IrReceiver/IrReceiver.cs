// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
using System;
using System.Device;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace Iot.Device.IrReceiver
{
    /// <summary>
    /// IrReceiver Class
    /// </summary>
    public class IrReceiver : IDisposable
    {
        private GpioController _digital = new GpioController(PinNumberingScheme.Logical);
        private byte _iR;

        /// <summary>
        /// IR Constructor
        /// </summary>
        /// <param name="IR">IR Pin number</param>
        public IrReceiver(byte IR)
        {
            _iR = IR;

            _digital.OpenPin(IR, PinMode.Input);
        }

        /// <summary>
        /// Get Key method
        /// </summary>
        /// <returns></returns>
        public int GetKey()
        {
            int[] data = { 0, 0, 0, 0 };
            int count;

            if (_digital.Read(_iR) == PinValue.Low)
            {
                count = 0;
                while (_digital.Read(_iR) == PinValue.Low & count < 200)
                {
                    count += 1;
                    DelayHelper.DelayMicroseconds(60, true);
                }

                if (count < 10)
                {
                    return 0;
                }

                count = 0;
                while (_digital.Read(_iR) == PinValue.High & count < 80)
                {
                    count += 1;
                    DelayHelper.DelayMicroseconds(60, true);
                }

                int idx = 0;
                int cnt = 0;
                for (int i = 0; i < 32; i++)
                {
                    count = 0;
                    while (_digital.Read(_iR) == PinValue.Low & count < 12)
                    {
                        count += 1;
                        DelayHelper.DelayMicroseconds(60, true);
                    }

                    count = 0;
                    while (_digital.Read(_iR) == PinValue.High & count < 40)
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

                if (data[0] + data[1] == 0xFF & data[2] + data[3] == 0xFF)
                {
                    return data[2];
                }
                else
                {
                    return 999;
                }
            }

            return 0;
        }

        private int GetKeyTemp()
        {
            // int[] data = { 0, 0, 0, 0 };
            if (_digital.Read(_iR) == PinValue.High)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _digital.Dispose();
        }

    }
}
*/