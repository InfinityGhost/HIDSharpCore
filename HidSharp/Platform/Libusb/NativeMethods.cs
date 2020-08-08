#region License
/* Copyright 2012 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
#endregion

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HidSharp.Platform.Libusb
{
	static class NativeMethods
	{

        // const string Libusb = "libusb-1.0.so.0";
        const string Libusb = "libusb-1.0.dll";

        public enum Error
        {
            None = 0,
            IO = -1,
            InvalidParameter = -2,
			AccessDenied = -3,
			NoDevice = -4,
			NotFound = -5,
			Busy = -6,
			Timeout = -7,
			Overflow = -8,
			Pipe = -9,
			Interrupted = -10,
			OutOfMemory = -11,
			NotSupported = -12,
            Other = -99
        }

		[StructLayout(LayoutKind.Sequential)]
		public struct libusb_device_descriptor
		{
            public byte bLength;
            public byte bDescriptorType;
            public UInt16 bcdUSB;
            public byte bDeviceClass;
            public byte bDeviceSubClass;
            public byte bDeviceProtocol;
            public byte bMaxPacketSize0;
            public UInt16 idVendor;
            public UInt16 idProduct;
            public UInt16 bcdDevice;
            public byte iManufacturer;
            public byte iProduct;
            public byte iSerialNumber;
            public byte bNumConfigurations;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct libusb_endpoint_descriptor
        {
            public byte bLength;
            public byte bDescriptorType;
            public byte bEndpointAddress;
            public byte bmAttributes;
            public UInt16 wMaxPacketSize;
            public byte bInterval;
            public byte bRefresh;
            public byte bSynchAddress;
            public IntPtr extra;
            public int extra_length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct libusb_interface_descriptor
        {
            public byte bLength;
            public byte bDescriptorType;
            public byte bInterfaceNumber;
            public byte bAlternateSetting;
            public byte bNumEndpoints;
            public byte bInterfaceClass;
            public byte bInterfaceSubClass;
            public byte bInterfaceProtocol;
            public byte iInterface;
            public libusb_endpoint_descriptor[] endpoints;
            public IntPtr extra;
            public int extra_length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct libusb_interface
        {
            public libusb_interface_descriptor[] altsetting;
            public int num_altsetting;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct libusb_config_descriptor
        {
            public byte bLength;
            public byte bDescriptorType;
            public UInt16 wTotalLength;
            public byte bNumInterfaces;
            public byte bConfigurationValue;
            public byte iConfiguration;
            public byte bmAttributes;
            public byte MaxPower;
            public libusb_interface[] interfaces;
            public IntPtr extra;
            public int extra_length;
        }

        public struct CombinedEndpoint
        {
            public IntPtr DevicePtr;
            public byte InterfaceNum;
            public byte Address;
            public UInt16 MaxOutputPacketSize;
            public UInt16 MaxInputPacketSize;
            public CombinedEndpoint(IntPtr devicePtr, byte interfaceNum, byte endpoint, UInt16 maxOutputPacketSize, UInt16 maxInputPacketSize)
            {
                DevicePtr = devicePtr;
                InterfaceNum = interfaceNum;
                Address = endpoint;
                MaxOutputPacketSize = maxOutputPacketSize;
                MaxInputPacketSize = maxInputPacketSize;
            }
            public void SetPacketSize(bool inDirection, UInt16 maxPacketSize)
            {
                if (inDirection)
                    MaxInputPacketSize = maxPacketSize;
                else
                    MaxOutputPacketSize = maxPacketSize;
            }
        }
		
		[DllImport(Libusb)]
		public static extern Error libusb_init(out IntPtr context);

        [DllImport(Libusb)]
        public static extern void libusb_exit(IntPtr context);

        [DllImport(Libusb)]
        public static extern int libusb_get_device_list(IntPtr context, out IntPtr[] deviceList);

        [DllImport(Libusb)]
        public static extern void libusb_free_device_list(IntPtr[] deviceList, int unref);

        [DllImport(Libusb)]
        public static extern Error libusb_open(IntPtr device, out IntPtr deviceHandle);

        [DllImport(Libusb)]
        public static extern void libusb_close(IntPtr deviceHandle);

        [DllImport(Libusb)]
        public static extern Error libusb_get_device_descriptor(IntPtr device, out libusb_device_descriptor deviceDescriptor);

        [DllImport(Libusb)]
        public static extern int libusb_get_string_descriptor_ascii(IntPtr deviceHandle, byte index, StringBuilder data, int length);

        [DllImport(Libusb)]
        public static extern Error libusb_get_config_descriptor(IntPtr deviceHandle, out libusb_config_descriptor configDescriptor);

        [DllImport(Libusb)]
        public static extern Error libusb_interrupt_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, ref int actual_length, uint timeout = 0);

        [DllImport(Libusb)]
        public static extern Error libusb_claim_interface(IntPtr deviceHandle, int interfaceNum);

        [DllImport(Libusb)]
        public static extern Error libusb_release_interface(IntPtr deviceHandle, int interfaceNum);
    }
}

