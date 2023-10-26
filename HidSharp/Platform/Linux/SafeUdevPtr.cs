using System;
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Linux
{
    sealed class SafeUdevHandle : SafeHandle
    {
        public SafeUdevHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public SafeUdevHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get { return IntPtr.Zero == handle; }
        }

        protected override bool ReleaseHandle()
        {
            NativeMethodsLibudev.Instance.udev_unref(handle);
            return true;
        }
    }

    sealed class SafeUdevDeviceHandle : SafeHandle
    {
        public SafeUdevDeviceHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public SafeUdevDeviceHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get { return IntPtr.Zero == handle; }
        }

        protected override bool ReleaseHandle()
        {
            NativeMethodsLibudev.Instance.udev_device_unref(handle);
            return true;
        }
    }
}