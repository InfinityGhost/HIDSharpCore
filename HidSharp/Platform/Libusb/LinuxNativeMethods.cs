using System;
using System.Runtime.InteropServices;
using System.Text;
using static HidSharp.Platform.Libusb.INativeMethods;

namespace HidSharp.Platform.Libusb
{
    internal class LinuxNativeMethods : INativeMethods
    {
        const CallingConvention convention = CallingConvention.Cdecl;
        const string Libusb = "libusb-1.0.so.0";

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_init(IntPtr context);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern void libusb_exit(IntPtr context);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern int libusb_get_device_list(IntPtr context, out IntPtr deviceList);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern void libusb_free_device_list(IntPtr deviceList, int unref);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_open(IntPtr device, out IntPtr deviceHandle);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern void libusb_close(IntPtr deviceHandle);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_get_device_descriptor(IntPtr device, out libusb_device_descriptor deviceDescriptor);

        [DllImport(Libusb, CharSet = CharSet.Ansi, CallingConvention = convention)]
        private static extern int libusb_get_string_descriptor_ascii(IntPtr deviceHandle, byte index, StringBuilder data, int length);

        [DllImport(Libusb, CallingConvention = convention)]
        private static unsafe extern Error libusb_get_config_descriptor(IntPtr device, byte configIndex, out IntPtr configDescriptor);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_interrupt_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, ref int actual_length, uint timeout = 0);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_claim_interface(IntPtr deviceHandle, int interfaceNum);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_release_interface(IntPtr deviceHandle, int interfaceNum);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_detach_kernel_driver(IntPtr deviceHandle, int interfaceNum);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_attach_kernel_driver(IntPtr deviceHandle, int interfaceNum);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern Error libusb_hotplug_register_callback(IntPtr ctx, HotplugEvent events, HotplugFlag flags, int vendorId, int productId, int devClass, libusb_hotplug_delegate callback, IntPtr userData, ref int callbackHandle);

        [DllImport(Libusb, CallingConvention = convention)]
        private static extern void libusb_hotplug_deregister_callback(IntPtr ctx, int callbackHandle);

        public Error init(IntPtr context)
        {
            return libusb_init(context);
        }

        public void exit(IntPtr context)
        {
            libusb_exit(context);
        }

        public int get_device_list(IntPtr context, out IntPtr deviceList)
        {
            return libusb_get_device_list(context, out deviceList);
        }

        public void free_device_list(IntPtr deviceList, int unref)
        {
            libusb_free_device_list(deviceList, unref);
        }

        public Error open(IntPtr device, out IntPtr deviceHandle)
        {
            return libusb_open(device, out deviceHandle);
        }

        public void close(IntPtr deviceHandle)
        {
            libusb_close(deviceHandle);
        }

        public Error get_device_descriptor(IntPtr device, out libusb_device_descriptor deviceDescriptor)
        {
            return libusb_get_device_descriptor(device, out deviceDescriptor);
        }

        public int get_string_descriptor_ascii(IntPtr deviceHandle, byte index, StringBuilder data, int length)
        {
            return libusb_get_string_descriptor_ascii(deviceHandle, index, data, length);
        }

        public Error get_config_descriptor(IntPtr device, byte configIndex, out IntPtr configDescriptor)
        {
            return libusb_get_config_descriptor(device, configIndex, out configDescriptor);
        }

        public Error interrupt_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, ref int actual_length, uint timeout = 0)
        {
            return libusb_interrupt_transfer(deviceHandle, endpoint, data, length, ref actual_length, timeout);
        }

        public Error claim_interface(IntPtr deviceHandle, int interfaceNum)
        {
            return libusb_claim_interface(deviceHandle, interfaceNum);
        }

        public Error release_interface(IntPtr deviceHandle, int interfaceNum)
        {
            return libusb_release_interface(deviceHandle, interfaceNum);
        }

        public Error detach_kernel_driver(IntPtr deviceHandle, int interfaceNum)
        {
            return libusb_detach_kernel_driver(deviceHandle, interfaceNum);
        }

        public Error attach_kernel_driver(IntPtr deviceHandle, int interfaceNum)
        {
            return libusb_attach_kernel_driver(deviceHandle, interfaceNum);
        }

        public Error hotplug_register_callback(IntPtr ctx, HotplugEvent events, HotplugFlag flags, int vendorId, int productId, int devClass, libusb_hotplug_delegate callback, IntPtr userData, ref int callbackHandle)
        {
            return libusb_hotplug_register_callback(ctx, events, flags, vendorId, productId, devClass, callback, userData, ref callbackHandle);
        }

        public void hotplug_deregister_callback(IntPtr ctx, int callbackHandle)
        {
            libusb_hotplug_deregister_callback(ctx, callbackHandle);
        }
    }
}

