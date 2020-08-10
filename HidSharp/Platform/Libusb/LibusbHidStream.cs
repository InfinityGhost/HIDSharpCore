using System;
using System.IO;
using System.Runtime.InteropServices;
using static HidSharp.Platform.Libusb.INativeMethods;

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidStream<T> : SysHidStream where T : INativeMethods, new()
    {
        private IntPtr _handle;
        private byte _endpoint, _interfaceNum;
        private object _readsync = new object();
        private object _writesync = new object();
        private T libusb = new T();

        internal LibusbHidStream(LibusbHidDevice<T> device) : base(device)
        {

        }

        ~LibusbHidStream()
        {
            libusb.release_interface(_handle, _interfaceNum);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // return device to kernel
                libusb.attach_kernel_driver(_handle, _interfaceNum);
            }
            libusb.close(_handle);
            Close();
        }

        // To follow HidSharp's structure and style
        internal void Init(IntPtr deviceHandle, byte interfaceNum, byte endpoint)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var retD = libusb.detach_kernel_driver(deviceHandle, interfaceNum);
                if (retD < 0)
                {
                    throw new IOException("Failed to detach device interface from kernel. Reason: " + Enum.GetName(typeof(Error), retD));
                }
            }

            var retI = libusb.claim_interface(deviceHandle, interfaceNum);
            if (retI != Error.None)
            {
                throw new IOException("Failed to claim interface. Reason: " + Enum.GetName(typeof(Error), retI));
            }

            _handle = deviceHandle;
            _interfaceNum = interfaceNum;
            _endpoint = endpoint;
        }

        public override void GetFeature(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            byte endpointMode = (byte)(_endpoint | (0x80));
            int transferred = 0;
            int minIn = Device.GetMaxInputReportLength();
            if (minIn <= 0)
                throw new IOException("Can't read from this device.");
            Error error = Error.None;
            lock (_readsync)
            {
                error = libusb.interrupt_transfer(_handle, endpointMode, buffer, count, ref transferred);
                if (error < 0)
                {
                    throw new IOException("Failed reading from device. Reason: " + Enum.GetName(typeof(Error), error));
                }
                return transferred;
            }
        }

        public override void SetFeature(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte endpointMode = (byte)(_endpoint & ~0x80);
            int transferred = 0;
            int minOut = Device.GetMaxInputReportLength();
            if (minOut <= 0)
                throw new IOException("Can't write to this device.");

            lock (_writesync)
            {
                Error error = libusb.interrupt_transfer(_handle, endpointMode, buffer, count, ref transferred);
                if (error < 0)
                {
                    throw new IOException("Failed to write to device. Reason: " + Enum.GetName(typeof(Error), error));
                }
            }
        }

        internal override void HandleFree()
        {
            // Should not free handle while we still have intention of performing I/O
        }

        protected override void Dispose(bool disposing)
        {
            libusb.release_interface(_handle, _interfaceNum);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                libusb.attach_kernel_driver(_handle, _interfaceNum);
            }
            libusb.close(_handle);
            base.Dispose(disposing);
        }
    }
}