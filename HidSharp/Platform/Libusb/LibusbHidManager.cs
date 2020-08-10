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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static HidSharp.Platform.Libusb.INativeMethods;

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidManager<T> : HidManager where T : INativeMethods, new()
    {
        public override string FriendlyName => "Libusb HID";

        public override bool IsSupported
        {
            get
            {
                try
                {
                    Marshal.PrelinkAll(typeof(T));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public T libusb = new T();

        public LibusbHidManager()
        {
            libusb.init(IntPtr.Zero);
        }

        protected override object[] GetBleDeviceKeys()
        {
            throw new NotImplementedException();
        }

        // TODO: cleanup
        protected unsafe override object[] GetHidDeviceKeys()
        {
            var devices = new List<LibUsbDevice>();

            var devCount = libusb.get_device_list(IntPtr.Zero, out var deviceListRaw);
            var deviceList = new IntPtr[devCount];
            Marshal.Copy(deviceListRaw, deviceList, 0, devCount);

            foreach (var device in deviceList)
            {
                libusb.get_device_descriptor(device, out var deviceDescriptor);

                if (libusb.open(device, out var deviceHandle) < 0)
                {
                    // invalid, skip
                    continue;
                }

                var configCount = deviceDescriptor.bNumConfigurations;

                for (byte configIndex = 0; configIndex < configCount; configIndex++)
                {
                    libusb.get_config_descriptor(device, configIndex, out var configDescriptorPtr);
                    libusb_config_descriptor configDescriptor;
                    configDescriptor = (libusb_config_descriptor)Marshal.PtrToStructure(configDescriptorPtr, typeof(libusb_config_descriptor));

                    for (int interfaceIndex = 0; interfaceIndex < configDescriptor.bNumInterfaces; interfaceIndex++)
                    {
                        var myInterface = (libusb_interface)Marshal.PtrToStructure(configDescriptor.interfaces + sizeof(libusb_interface) * interfaceIndex, typeof(libusb_interface));

                        for (int settingIndex = 0; settingIndex < myInterface.num_altsetting; settingIndex++)
                        {
                            var mySetting = (libusb_interface_descriptor)Marshal.PtrToStructure(myInterface.altsetting + sizeof(libusb_interface_descriptor) * settingIndex, typeof(libusb_interface_descriptor));
                            var endpointDict = new Dictionary<int, Endpoint>();

                            for (int endpointIndex = 0; endpointIndex < mySetting.bNumEndpoints; endpointIndex++)
                            {
                                var myEndpoint = (libusb_endpoint_descriptor)Marshal.PtrToStructure(mySetting.endpoints + sizeof(libusb_endpoint_descriptor) * endpointIndex, typeof(libusb_endpoint_descriptor));
                                var direction = (myEndpoint.bEndpointAddress & (0x80)) == 0x80;
                                var address = myEndpoint.bEndpointAddress & (0x0F);

                                if (!endpointDict.ContainsKey(address))
                                {
                                    endpointDict[address] = new Endpoint(interfaceIndex, address);
                                    endpointDict[address].SetReportLength(direction, myEndpoint.wMaxPacketSize);
                                }
                                else
                                {
                                    endpointDict[address].SetReportLength(direction, myEndpoint.wMaxPacketSize);
                                }
                            }

                            foreach (var endpoint in endpointDict.Values)
                            {
                                LibUsbDevice libusbdevice = new LibUsbDevice
                                {
                                    Device = device,
                                    VendorID = deviceDescriptor.idVendor,
                                    ProductID = deviceDescriptor.idProduct,
                                    Endpoint = endpoint
                                };
                                devices.Add(libusbdevice);
                            }
                        }
                    }
                }
                libusb.close(deviceHandle);
            }
            libusb.free_device_list(deviceListRaw, 1);
            return devices.Cast<object>().ToArray();
        }

        protected override object[] GetSerialDeviceKeys()
        {
            throw new NotImplementedException();
        }

        protected override bool TryCreateBleDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }

        protected override bool TryCreateHidDevice(object key, out Device device)
        {
            device = LibusbHidDevice<T>.TryCreate((LibUsbDevice)key);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }
    }
}

