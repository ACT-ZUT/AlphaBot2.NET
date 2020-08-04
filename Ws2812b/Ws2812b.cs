using System;
using System.Device;
using System.Device.Gpio;
//using System.Drawing;
using Iot.Device.Graphics;

namespace Iot.Device.Ws2812b
{
    public class Ws2812b : IDisposable
    {
        /// <summary>
        /// SPI device used for communication with the LED driver
        /// </summary>
        //protected readonly PinDevice _pinDevice;
        private GpioController digital = new GpioController(PinNumberingScheme.Logical);
        byte DATA;

        public BitmapImage Image { get; protected set; }
        public Ws2812b(byte DATA, int width, int height = 1)
        {
            Image = new BitmapImageNeo3(width, height);
            this.DATA = DATA;

            digital.OpenPin(this.DATA, PinMode.Output);
        }

        public void Update()
        {
            for (int i = 0; i < Image.Width; i++)
            {
                foreach (var item in Image.Data)
                {
                    if ((item >> (3 - i) & 0x01) != 0)
                    {
                        digital.Write(DATA, 1);
                        digital.Write(DATA, 0);
                        DelayHelper.DelayMicroseconds(1, true);
                    }
                    else
                    {
                        digital.Write(DATA, 1);
                        DelayHelper.DelayMicroseconds(1, true);
                        digital.Write(DATA, 0);
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Ws2812b()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    /// <summary>
    /// Special 24bit RGB format for Neo pixel LEDs where each bit is converted to 3 bits.
    /// A one is converted to 110, a zero is converted to 100.
    /// </summary>
    internal class BitmapImageNeo3 : BitmapImage
    {
        private const int BytesPerComponent = 3;
        private const int BytesPerPixel = BytesPerComponent * 3;
        // The Neo Pixels require a 50us delay (all zeros) after. Since Spi freq is not exactly
        // as requested 100us is used here with good practical results. 100us @ 2.4Mbps and 8bit
        // data means we have to add 30 bytes of zero padding.
        private const int ResetDelayInBytes = 30;

        public BitmapImageNeo3(int width, int height)
            : base(new byte[width * height * BytesPerPixel + ResetDelayInBytes], width, height, width * BytesPerPixel)
        {
        }

        public override void SetPixel(int x, int y, System.Drawing.Color c)
        {
            var offset = y * Stride + x * BytesPerPixel;
            Data[offset++] = _lookup[c.G * BytesPerComponent + 0];
            Data[offset++] = _lookup[c.G * BytesPerComponent + 1];
            Data[offset++] = _lookup[c.G * BytesPerComponent + 2];
            Data[offset++] = _lookup[c.R * BytesPerComponent + 0];
            Data[offset++] = _lookup[c.R * BytesPerComponent + 1];
            Data[offset++] = _lookup[c.R * BytesPerComponent + 2];
            Data[offset++] = _lookup[c.B * BytesPerComponent + 0];
            Data[offset++] = _lookup[c.B * BytesPerComponent + 1];
            Data[offset++] = _lookup[c.B * BytesPerComponent + 2];
        }

        private static readonly byte[] _lookup = new byte[256 * BytesPerComponent];
        static BitmapImageNeo3()
        {
            for (int i = 0; i < 256; i++)
            {
                int data = 0;
                for (int j = 7; j >= 0; j--)
                {
                    data = (data << 3) | 0b100 | ((i >> j) << 1) & 2;
                }

                _lookup[i * BytesPerComponent + 0] = unchecked((byte)(data >> 16));
                _lookup[i * BytesPerComponent + 1] = unchecked((byte)(data >> 8));
                _lookup[i * BytesPerComponent + 2] = unchecked((byte)(data >> 0));
            }
        }
    }
}
