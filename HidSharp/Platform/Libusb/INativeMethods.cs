using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HidSharp.Platform.Libusb
{
    public interface INativeMethods
    {
        internal class Endpoint
        {
            public int Interface;
            public int Address;
            public uint OutputReportLength;
            public uint InputReportLength;
            public bool IsReadable { get => InputReportLength > 0; }

            public bool IsWritable { get => OutputReportLength > 0; }

            public Endpoint(int Interface, int address, uint output = 0, uint input = 0)
            {
                this.Interface = Interface;
                Address = address;
                OutputReportLength = output;
                InputReportLength = input;
            }

            public void SetReportLength(bool inDirection, uint length)
            {
                if (inDirection)
                    InputReportLength = length;
                else
                    OutputReportLength = length;
            }
        }

        internal class LibUsbDevice
        {
            public IntPtr Device;
            public int VendorID;
            public int ProductID;
            public Endpoint Endpoint;
        }
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

        public enum libusb_descriptor_type
        {
            LIBUSB_DT_DEVICE = 0x01,
            LIBUSB_DT_CONFIG = 0x02,
            LIBUSB_DT_STRING = 0x03,
            LIBUSB_DT_INTERFACE = 0x04,
            LIBUSB_DT_ENDPOINT = 0x05,
            LIBUSB_DT_BOS = 0x0f,
            LIBUSB_DT_DEVICE_CAPABILITY = 0x10,
            LIBUSB_DT_HID = 0x21,
            LIBUSB_DT_REPORT = 0x22,
            LIBUSB_DT_PHYSICAL = 0x23,
            LIBUSB_DT_HUB = 0x29,
            LIBUSB_DT_SUPERSPEED_HUB = 0x2a,
            LIBUSB_DT_SS_ENDPOINT_COMPANION = 0x30
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

            /// <summary>libusb_endpoint_descriptor[]</summary>
            public IntPtr endpoints;
            public IntPtr extra;
            public int extra_length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct libusb_interface
        {
            /// <summary>libusb_interface_descriptor[]</summary>
            public IntPtr altsetting;
            public int num_altsetting;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct libusb_config_descriptor
        {
            public byte bLength;
            public byte bDescriptorType;
            public UInt16 wTotalLength;
            public byte bNumInterfaces;
            public byte bConfigurationValue;
            public byte iConfiguration;
            public byte bmAttributes;
            public byte MaxPower;
            /// <summary> libusb_interface[] </summary>
            public IntPtr interfaces;
            public IntPtr extra;
            public int extra_length;
        }

        public Error init(IntPtr context);

        public void exit(IntPtr context);

        public int get_device_list(IntPtr context, out IntPtr deviceList);

        public void free_device_list(IntPtr deviceList, int unref);

        public Error open(IntPtr device, out IntPtr deviceHandle);

        public void close(IntPtr deviceHandle);

        public Error get_device_descriptor(IntPtr device, out libusb_device_descriptor deviceDescriptor);

        public int get_string_descriptor_ascii(IntPtr deviceHandle, byte index, StringBuilder data, int length);

        public Error get_config_descriptor(IntPtr device, byte configIndex, out IntPtr configDescriptor);

        public Error interrupt_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, ref int actual_length, uint timeout = 0);

        public Error claim_interface(IntPtr deviceHandle, int interfaceNum);

        public Error release_interface(IntPtr deviceHandle, int interfaceNum);

        public Error detach_kernel_driver(IntPtr deviceHandle, int interfaceNum);

        public Error attach_kernel_driver(IntPtr deviceHandle, int interfaceNum);
    }
}