using System;
using System.Text;
using static HidSharp.Platform.Libusb.INativeMethods;

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidDevice<T> : HidDevice where T : INativeMethods, new()
    {
        public override int ProductID { get => _descriptor.idProduct; }

        public override int ReleaseNumberBcd { get => _descriptor.bcdUSB; }

        public override int VendorID { get => _descriptor.idVendor; }

        private T libusb = new T();

        // Unsupported by libusb
        public override string DevicePath {get => ""; }

        private LibUsbDevice _dev;
        private IntPtr _deviceHandle;
        private libusb_device_descriptor _descriptor;

        internal static LibusbHidDevice<T> TryCreate(LibUsbDevice device)
        {
            T libusb = new T();
            var hid = new LibusbHidDevice<T> { _dev = device };
            var err = libusb.open(hid._dev.Device, out hid._deviceHandle);
            if (err > 0)
            {
                return null;
            }

            err = libusb.get_device_descriptor(hid._dev.Device, out hid._descriptor);
            if (err > 0)
            {
                libusb.close(hid._dev.Device);
                return null;
            }

            return hid;
        }

        public override string GetFileSystemName()
        {
            throw new NotImplementedException();
        }

        public override string GetManufacturer()
        {
            var manufacturer = new StringBuilder(256);
            var err = libusb.get_string_descriptor_ascii(_deviceHandle, _descriptor.iManufacturer, manufacturer, 256);
            if (err < 0)
            {
                throw DeviceException.CreateIOException(this, "Failed to read report descriptor.");
            }
            return manufacturer.ToString();
        }

        public override int GetMaxFeatureReportLength()
        {
            throw new NotImplementedException();
        }

        public override int GetMaxInputReportLength()
        {
            return (int)_dev.Endpoint.InputReportLength;
        }

        public override int GetMaxOutputReportLength()
        {
            return (int)_dev.Endpoint.OutputReportLength;
        }

        public override string GetProductName()
        {
            var product = new StringBuilder(256);
            var err = libusb.get_string_descriptor_ascii(_deviceHandle, _descriptor.iProduct, product, 256);
            if (err < 0)
            {
                throw DeviceException.CreateIOException(this, "Failed to read report descriptor.");
            }
            return product.ToString();
        }

        public override string GetSerialNumber()
        {
            var serial = new StringBuilder(256);
            var err = libusb.get_string_descriptor_ascii(_deviceHandle, _descriptor.iSerialNumber, serial, 256);
            if (err < 0)
            {
                throw DeviceException.CreateIOException(this, "Failed to read report descriptor.");
            }
            return serial.ToString();
        }

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            var stream = new LibusbHidStream<T>(this);
            try { stream.Init(_deviceHandle, (byte)_dev.Endpoint.Interface, (byte)_dev.Endpoint.Address); return stream; }
            catch { stream.Close(); throw; }
        }
    }
}