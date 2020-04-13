using System;
using System.Collections.Generic;
using System.Text;

using Emgu.CV;

using static DelayHelper.Delay;
using static Emgu.CV.CvEnum.CapProp;

namespace AlphaBot2
{
    public class Camera : IDisposable
    {
        private VideoCapture _camera;
        int _frameRate = 30;
        int _frameWidth = 1280;
        int _frameHeight = 720;
        Mat frame;

        public Camera(int cameraId = 0)
        {
            _camera = new VideoCapture(cameraId);

            //SetCaptureProperty(Autofocus, 0);
            //SetCaptureProperty(AutoExposure, 0);
            //SetCaptureProperty(AutoWb, 0);
            SetCaptureProperty(Fps, _frameRate);
            SetCaptureProperty(FrameWidth, _frameWidth);
            SetCaptureProperty(FrameHeight, _frameHeight);

            _camera.ImageGrabbed += ProcessFrame;
        }

        public void Start()
        {
            if (_camera != null)
            {
                try
                {
                    _camera.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public bool SetCaptureProperty(Emgu.CV.CvEnum.CapProp prop, double value)
        {
            return _camera.SetCaptureProperty(prop, value);
        }


        private void ProcessFrame(object sender, EventArgs e)
        {
            DateTime date = DateTime.Now;
            if (_camera != null && _camera.Ptr != IntPtr.Zero)
            {
                _camera.Retrieve(frame, 0);
                frame.Save($@"/home/pi/dataset/test_{date.Year:D4}{date.Month:D2}{date.Day:D2}_{date.Hour:D2}{date.Minute:D2}{date.Millisecond:D3}.jpg");
                Console.WriteLine("Frame Processed");
                DelayMilliseconds(25, true); // need to calculate how much time it takes to process one frame
                //VideoW.Write(frame);
                //rest of processing 
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
                    _camera.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                frame = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Camera()
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
}
