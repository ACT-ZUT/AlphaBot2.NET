using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Management;

using Emgu.CV;

using static DelayHelper.Delay;
using static Emgu.CV.CvEnum.CapProp;


namespace AlphaBot2
{
    public class Camera : IDisposable
    {
        VideoCapture _camera;
        int _frameRate = 30;
        int _frameWidth = 1920;
        int _frameHeight = 1080;
        Mat frame;

        public Camera(int cameraId = 0)
        {
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_PnPEntity WHERE PNPClass = ""Image""");
            ManagementObjectCollection collection = objectSearcher.Get();
            List<CameraInfo> cameras = new List<CameraInfo>();
            foreach (var device in collection)
            {
                var name = (string)device.GetPropertyValue("Name");
                var PNPClass = (string)device.GetPropertyValue("PNPClass");
                Debug.WriteLine($"{PNPClass}: {name}");

                if (PNPClass == "Image")
                {
                    CameraInfo cameraInfo = new CameraInfo();
                    //cameraInfo.Availability = (uint)device.GetPropertyValue("Availability");
                    cameraInfo.Caption = (string)device.GetPropertyValue("Caption");
                    cameraInfo.ClassGuid = (string)device.GetPropertyValue("ClassGuid");
                    cameraInfo.CompatibleID = (string[])device.GetPropertyValue("CompatibleID"); //[]
                    cameraInfo.ConfigManagerErrorCode = (uint)device.GetPropertyValue("ConfigManagerErrorCode");
                    cameraInfo.ConfigManagerUserConfig = (bool)device.GetPropertyValue("ConfigManagerUserConfig");
                    cameraInfo.CreationClassName = (string)device.GetPropertyValue("CreationClassName");
                    cameraInfo.Description = (string)device.GetPropertyValue("Description");
                    cameraInfo.DeviceID = (string)device.GetPropertyValue("DeviceID");
                    //cameraInfo.ErrorCleared = (bool)device.GetPropertyValue("ErrorCleared");
                    cameraInfo.ErrorDescription = (string)device.GetPropertyValue("ErrorDescription");
                    cameraInfo.HardwareID = (string[])device.GetPropertyValue("HardwareID"); //[]
                    cameraInfo.InstallDate = (string)device.GetPropertyValue("InstallDate"); //datetime
                    //cameraInfo.LastErrorCode = (uint)device.GetPropertyValue("LastErrorCode");
                    cameraInfo.Manufacturer = (string)device.GetPropertyValue("Manufacturer");
                    cameraInfo.Name = (string)device.GetPropertyValue("Name");
                    cameraInfo.PNPClass = (string)device.GetPropertyValue("PNPClass");
                    cameraInfo.PNPDeviceID = (string)device.GetPropertyValue("PNPDeviceID");
                    cameraInfo.PowerManagementCapabilities = (uint[])device.GetPropertyValue("PowerManagementCapabilities"); //[]
                    //cameraInfo.PowerManagementSupported = (bool)device.GetPropertyValue("PowerManagementSupported");
                    cameraInfo.Present = (bool)device.GetPropertyValue("Present");
                    //cameraInfo.ProtocolCode = (string)device.GetPropertyValue("ProtocolCode");
                    cameraInfo.Service = (string)device.GetPropertyValue("Service");
                    cameraInfo.Status = (string)device.GetPropertyValue("Status");
                    //cameraInfo.StatusInfo = (uint)device.GetPropertyValue("StatusInfo");
                    cameraInfo.SystemCreationClassName = (string)device.GetPropertyValue("SystemCreationClassName");
                    cameraInfo.SystemName = (string)device.GetPropertyValue("SystemName");
                    cameras.Add(cameraInfo);
                }


            }

            collection.Dispose();
            objectSearcher.Dispose();

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
            frame = new Mat();
            DateTime date = DateTime.Now;
            if (_camera != null && _camera.Ptr != IntPtr.Zero)
            {
                _camera.Retrieve(frame, 0);
                frame.Save($@"/home/pi/dataset/test_{date.Year:D4}{date.Month:D2}{date.Day:D2}_{date.Hour:D2}{date.Minute:D2}{date.Millisecond:D3}.jpg");
                Console.WriteLine("Frame Processed");
                DelayMilliseconds(25, true); // need to calculate how much time it takes to process one frame (saving is longest)
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


    public class CameraInfo
    {
        public uint Availability { get; set; }
        public string Caption { get; set; }
        public string ClassGuid { get; set; }
        public string[] CompatibleID { get; set; } //[]
        public uint ConfigManagerErrorCode { get; set; }
        public bool ConfigManagerUserConfig { get; set; }
        public string CreationClassName { get; set; }
        public string Description { get; set; }
        public string DeviceID { get; set; }
        public bool ErrorCleared { get; set; }
        public string ErrorDescription { get; set; }
        public string[] HardwareID { get; set; } //[]
        public string InstallDate { get; set; } //datetime
        public uint LastErrorCode { get; set; }
        public string Manufacturer { get; set; }
        public string Name { get; set; }
        public string PNPClass { get; set; }
        public string PNPDeviceID { get; set; }
        public uint[] PowerManagementCapabilities { get; set; } //[]
        public bool PowerManagementSupported { get; set; }
        public bool Present { get; set; }
        public string ProtocolCode { get; set; }
        public string Service { get; set; }
        public string Status { get; set; }
        public uint StatusInfo { get; set; }
        public string SystemCreationClassName { get; set; }
        public string SystemName { get; set; }
    }
}
