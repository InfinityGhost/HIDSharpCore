using System;
using System.IO;

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidStream : SysHidStream
    {
        private IntPtr _handle;
        private byte _endpoint, _interfaceNum;
        private object _readsync = new object();
        private object _writesync = new object();

        internal LibusbHidStream(LibusbHidDevice device) : base(device)
        {

        }

        ~LibusbHidStream()
        {
            NativeMethods.libusb_release_interface(_handle, _interfaceNum);
            NativeMethods.libusb_close(_handle);
            Close();
        }

        // To follow HidSharp's structure and style
        internal void Init(IntPtr deviceHandle, byte interfaceNum, byte endpoint)
        {
            var err = NativeMethods.libusb_claim_interface(deviceHandle, interfaceNum);
            if (err != NativeMethods.Error.None)
            {
                switch (err)
                {
                    case NativeMethods.Error.Busy:
                        throw new IOException("Device interface busy");
                }
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
            NativeMethods.Error error = NativeMethods.Error.None;
            try
            {
                lock(_readsync)
                {
                    error = NativeMethods.libusb_interrupt_transfer(_handle, endpointMode, buffer, count, ref transferred);
                    if (error != NativeMethods.Error.None)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        if (transferred == 0)
                        {
                            throw new IOException("Unexpected zero read.");
                        }
                    }
                    return transferred;
                }
            }
            catch
            {
                throw new IOException("Reading from device failed.");
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
            try
            {
                lock (_writesync)
                {
                    NativeMethods.Error error = NativeMethods.libusb_interrupt_transfer(_handle, endpointMode, buffer, count, ref transferred);
                    if (error != NativeMethods.Error.None)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        if (transferred == 0)
                        {
                            throw new IOException("Unexpected zero write.");
                        }
                    }
                }
            }
            catch
            {
                throw new IOException("Writing to device failed.");
            }
        }

        internal override void HandleFree()
        {
            // Should not free handle while we still have intention of performing I/O
        }

        protected override void Dispose(bool disposing)
        {
            NativeMethods.libusb_release_interface(_handle, _interfaceNum);
            NativeMethods.libusb_close(_handle);
            base.Dispose(disposing);
        }
    }
}