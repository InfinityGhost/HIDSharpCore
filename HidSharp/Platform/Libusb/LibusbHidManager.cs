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
using HidSharp.Utility;
using static HidSharp.Platform.Libusb.INativeMethods;

namespace HidSharp.Platform.Libusb
{
    sealed class LibusbHidManager<T> : HidManager where T : INativeMethods, new()
    {
        private bool _isSupported;

        private Dictionary<IntPtr, List<LibUsbDevice>> _devices;

        public override string FriendlyName => "Libusb HID";

        public override bool IsSupported { get => _isSupported; }

        private readonly T libusb = new T();

        private static libusb_hotplug_delegate hotplugDelegate;

        private int callbackHandle;

        private object _deviceLock = new object();

        public LibusbHidManager()
        {
            try
            {
                Marshal.PrelinkAll(typeof(T));
                libusb.init(IntPtr.Zero);
                _isSupported = true;
            }
            catch
            {
                _isSupported = false;
            }
        }

        private int HotPlug(IntPtr ctx, IntPtr device, HotplugEvent hotplugEvent, IntPtr user_data)
        {
            switch (hotplugEvent)
            {
                case HotplugEvent.Arrived:
                    lock(_deviceLock)
                    {
                        CreateDevice(device, _devices);
                        DeviceList.Local.RaiseChanged();
                    }
                    break;
                case HotplugEvent.Left:
                    lock(_deviceLock)
                    {
                        _devices.Remove(device);
                        DeviceList.Local.RaiseChanged();
                    }
                    break;
            }
            return 0;
        }

        internal unsafe void CreateDevice(IntPtr device, Dictionary<IntPtr, List<LibUsbDevice>> deviceList)
        {
            libusb.get_device_descriptor(device, out var deviceDescriptor);

            if (libusb.open(device, out var deviceHandle) < 0)
            {
                // invalid, skip
                return;
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
                            lock(_deviceLock)
                            {
                                if (!deviceList.ContainsKey(device))
                                {
                                    deviceList.Add(device, new List<LibUsbDevice>());
                                }
                                deviceList[device].Add(libusbdevice);
                            }
                        }
                    }
                }
            }
            libusb.close(deviceHandle);
            return;
        }

        private void GenerateDeviceList()
        {
            _devices = new Dictionary<IntPtr, List<LibUsbDevice>>();
            var _devCount = libusb.get_device_list(IntPtr.Zero, out var _deviceListRaw);
            var deviceList = new IntPtr[_devCount];
            Marshal.Copy(_deviceListRaw, deviceList, 0, _devCount);
            foreach (var device in deviceList)
            {
                CreateDevice(device, _devices);
            }
        }

        protected override void Run(Action readyCallback)
        {
            // Cache device list since this is slow
            GenerateDeviceList();

            hotplugDelegate = new libusb_hotplug_delegate(HotPlug);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RunWindowsHotPlug(readyCallback);
            }
            else
            {
                libusb.hotplug_register_callback(IntPtr.Zero, HotplugEvent.Arrived | HotplugEvent.Left, HotplugFlag.NoFlags, -1, -1, -1, hotplugDelegate, IntPtr.Zero, ref callbackHandle);
                readyCallback();
                GC.KeepAlive(hotplugDelegate);
            }
        }

        private void RunWindowsHotPlug(Action readyCallback)
        {
            const string className = "HidSharpDeviceMonitor";

            Windows.NativeMethods.WindowProc windowProc = DeviceMonitorWindowProc;
            var wc = new Windows.NativeMethods.WNDCLASS() { ClassName = className, WindowProc = windowProc };
            RunAssert(0 != Windows.NativeMethods.RegisterClass(ref wc), "HidSharp RegisterClass failed.");

            var hwnd = Windows.NativeMethods.CreateWindowEx(0, className, className, 0,
                                                    Windows.NativeMethods.CW_USEDEFAULT, Windows.NativeMethods.CW_USEDEFAULT, Windows.NativeMethods.CW_USEDEFAULT, Windows.NativeMethods.CW_USEDEFAULT,
                                                    Windows.NativeMethods.HWND_MESSAGE,
                                                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            RunAssert(hwnd != IntPtr.Zero, "HidSharp CreateWindow failed.");

            var hidNotifyHandle = RegisterDeviceNotification(hwnd, Windows.NativeMethods.HidD_GetHidGuid());

            readyCallback();

            while (true)
            {
                int result = Windows.NativeMethods.GetMessage(out Windows.NativeMethods.MSG msg, hwnd, 0, 0);
                if (result == 0 || result == -1) { break; }

                Windows.NativeMethods.TranslateMessage(ref msg);
                Windows.NativeMethods.DispatchMessage(ref msg);
            }

            UnregisterDeviceNotification(hidNotifyHandle);
            RunAssert(Windows.NativeMethods.DestroyWindow(hwnd), "HidSharp DestroyWindow failed.");
            RunAssert(Windows.NativeMethods.UnregisterClass(className, IntPtr.Zero), "HidSharp UnregisterClass failed.");
            GC.KeepAlive(windowProc);
        }

        private IntPtr RegisterDeviceNotification(IntPtr hwnd, Guid guid)
        {
            var notifyFilter = new Windows.NativeMethods.DEV_BROADCAST_DEVICEINTERFACE()
            {
                Size = Marshal.SizeOf(typeof(Windows.NativeMethods.DEV_BROADCAST_DEVICEINTERFACE)),
                ClassGuid = guid,
                DeviceType = Windows.NativeMethods.DBT_DEVTYP_DEVICEINTERFACE
            };
            var notifyHandle = Windows.NativeMethods.RegisterDeviceNotification(hwnd, ref notifyFilter, 0);
            RunAssert(notifyHandle != IntPtr.Zero, "HidSharp RegisterDeviceNotification failed.");
            return notifyHandle;
        }

        private void UnregisterDeviceNotification(IntPtr handle)
        {
            RunAssert(Windows.NativeMethods.UnregisterDeviceNotification(handle), "HidSharp UnregisterDeviceNotification failed.");
        }

        unsafe IntPtr DeviceMonitorWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (message == Windows.NativeMethods.WM_DEVICECHANGE)
            {
                var ev = (Windows.NativeMethods.WM_DEVICECHANGE_wParam)(int)(long)wParam;
                HidSharpDiagnostics.Trace("Received a device change event, {0}.", ev);

                var eventArgs = (Windows.NativeMethods.DEV_BROADCAST_HDR*)(void*)lParam;

                if (ev == Windows.NativeMethods.WM_DEVICECHANGE_wParam.DBT_DEVICEARRIVAL || ev == Windows.NativeMethods.WM_DEVICECHANGE_wParam.DBT_DEVICEREMOVECOMPLETE)
                {
                    if (eventArgs->DeviceType == Windows.NativeMethods.DBT_DEVTYP_DEVICEINTERFACE)
                    {
                        var diEventArgs = (Windows.NativeMethods.DEV_BROADCAST_DEVICEINTERFACE*)eventArgs;

                        if (diEventArgs->ClassGuid == Windows.NativeMethods.HidD_GetHidGuid())
                        {
                            // We won't know what device is added or removed so regenerate
                            GenerateDeviceList();
                            DeviceList.Local.RaiseChanged();
                        }
                    }
                }
                return (IntPtr)1;
            }
            return Windows.NativeMethods.DefWindowProc(window, message, wParam, lParam);
        }

        protected override object[] GetBleDeviceKeys()
        {
            throw new NotImplementedException();
        }

        // TODO: cleanup
        protected unsafe override object[] GetHidDeviceKeys()
        {
            var list = new List<LibUsbDevice>();
            foreach (var deviceList in _devices.Values)
                foreach (var device in deviceList)
                    list.Add(device);

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
            device = LibusbHidDevice<T>.TryCreate((LibUsbDevice)key);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }
    }
}

