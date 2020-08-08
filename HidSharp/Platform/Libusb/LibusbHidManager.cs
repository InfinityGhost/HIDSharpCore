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

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidManager : HidManager
    {
        public override string FriendlyName => "Libusb HID";

        public override bool IsSupported { get => true; }

        protected override object[] GetBleDeviceKeys()
        {
            throw new NotImplementedException();
        }

        // TODO: cleanup
        protected override unsafe object[] GetHidDeviceKeys()
        {
            NativeMethods.libusb_init(IntPtr.Zero);

            var count = NativeMethods.libusb_get_device_list(IntPtr.Zero, out IntPtr deviceListRaw);
            IntPtr[] deviceList = new IntPtr[count];
            Marshal.Copy(deviceListRaw, deviceList, 0, count);

            var list = new List<NativeMethods.CombinedEndpoint>();

            foreach (var device in deviceList)
            {
                NativeMethods.libusb_get_device_descriptor(device, out var deviceDescriptor);
                byte configCount = deviceDescriptor.bNumConfigurations;
                for (byte configIndex = 0; configIndex < configCount; configIndex++)
                {
                    if (NativeMethods.libusb_get_config_descriptor(device, configIndex, out var configDescriptor) != NativeMethods.Error.None)
                        continue;

                    if (configDescriptor.bDescriptorType != (byte)NativeMethods.libusb_descriptor_type.LIBUSB_DT_CONFIG)
                        continue;

                    foreach (var iinterface in configDescriptor.interfaces)
                    {
                        foreach (var iinterfaceSetting in iinterface.altsetting)
                        {
                            var endpointDict = new Dictionary<byte, NativeMethods.CombinedEndpoint>();
                            foreach (var endpoint in iinterfaceSetting.endpoints)
                            {
                                var endpointAddress = (byte)(endpoint.bEndpointAddress & 0x0F);
                                var endpointDirection = ((endpoint.bEndpointAddress & 0x80) >> 7) == 1;
                                if (!endpointDict.ContainsKey(endpointAddress))
                                {
                                    endpointDict.Add(endpointAddress, new NativeMethods.CombinedEndpoint(device, iinterfaceSetting.bInterfaceNumber, endpointAddress,
                                        endpointDirection ? (UInt16)0 : endpoint.wMaxPacketSize,
                                        endpointDirection ? endpoint.wMaxPacketSize : (UInt16)0));
                                }
                                else
                                {
                                    endpointDict[endpointAddress].SetPacketSize(endpointDirection, endpoint.wMaxPacketSize);
                                }
                            }
                            foreach (var endpoint in endpointDict.Values)
                            {
                                list.Add(endpoint);
                            }
                        }
                    }
                }
            }
            NativeMethods.libusb_free_device_list(deviceListRaw, 1);
            return list.Cast<object>().ToArray();
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
            device = LibusbHidDevice.TryCreate((NativeMethods.CombinedEndpoint)key);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }
    }
}

