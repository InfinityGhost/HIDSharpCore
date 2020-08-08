using System;
using System.Text;

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidDevice : HidDevice
    {
        public override int ProductID { get => _descriptor.idProduct; }

        public override int ReleaseNumberBcd { get => _descriptor.bcdUSB; }

        public override int VendorID { get => _descriptor.idVendor; }

        // Unsupported by libusb
        public override string DevicePath {get => ""; }

        private IntPtr _device;
        private IntPtr _deviceHandle;
        private NativeMethods.libusb_device_descriptor _descriptor;
        private byte _endpoint, _interfaceNum;
        private int _maxOutputReportLength, _maxInputReportLength;

        internal static LibusbHidDevice TryCreate(NativeMethods.CombinedEndpoint combinedEndpoint)
        {
            var hid = new LibusbHidDevice { _device = combinedEndpoint.DevicePtr };
            var err = NativeMethods.libusb_open(hid._device, out hid._deviceHandle);
            if (err > 0)
            {
                return null;
            }

            err = NativeMethods.libusb_get_device_descriptor(hid._device, out hid._descriptor);
            if (err > 0)
            {
                NativeMethods.libusb_close(hid._device);
                return null;
            }

            hid._interfaceNum = combinedEndpoint.InterfaceNum;
            hid._endpoint = combinedEndpoint.Address;
            hid._maxInputReportLength = combinedEndpoint.MaxInputPacketSize;
            hid._maxOutputReportLength = combinedEndpoint.MaxOutputPacketSize;
            return hid;
        }

        public override string GetFileSystemName()
        {
            throw new NotImplementedException();
        }

        public override string GetManufacturer()
        {
            var manufacturer = new StringBuilder(256);
            var err = NativeMethods.libusb_get_string_descriptor_ascii(_deviceHandle, _descriptor.iManufacturer, manufacturer, 256);
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
            return _maxInputReportLength;
        }

        public override int GetMaxOutputReportLength()
        {
            return _maxOutputReportLength;
        }

        public override string GetProductName()
        {
            var product = new StringBuilder(256);
            var err = NativeMethods.libusb_get_string_descriptor_ascii(_deviceHandle, _descriptor.iProduct, product, 256);
            if (err < 0)
            {
                throw DeviceException.CreateIOException(this, "Failed to read report descriptor.");
            }
            return product.ToString();
        }

        public override string GetSerialNumber()
        {
            var serial = new StringBuilder(256);
            var err = NativeMethods.libusb_get_string_descriptor_ascii(_deviceHandle, _descriptor.iSerialNumber, serial, 256);
            if (err < 0)
            {
                throw DeviceException.CreateIOException(this, "Failed to read report descriptor.");
            }
            return serial.ToString();
        }

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            var stream = new LibusbHidStream(this);
            try { stream.Init(_deviceHandle, _interfaceNum, _endpoint); return stream; }
            catch { stream.Close(); throw; }
        }
    }
}