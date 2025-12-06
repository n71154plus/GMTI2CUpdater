using System;
using System.Runtime.InteropServices;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    public static class UsbInfoManager
    {
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE =
            new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        public static string? FindDevice(ushort vendorId, ushort productId)
        {
            string search = $"vid_{vendorId:x4}&pid_{productId:x4}";

            Guid hidGuid = Guid.Empty;
            HidD_GetHidGuid(ref hidGuid);

            IntPtr hInfoSet = SetupDiGetClassDevs(
                ref hidGuid,
                null,
                IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            if (hInfoSet == IntPtr.Zero || hInfoSet.ToInt64() == -1)
                return null;

            try
            {
                var ifaceData = new SP_DEVICE_INTERFACE_DATA();
                uint index = 0;

                while (SetupDiEnumDeviceInterfaces(
                           hInfoSet,
                           IntPtr.Zero,
                           ref hidGuid,
                           index,
                           ref ifaceData))
                {
                    string? path = GetDevicePath(hInfoSet, ref ifaceData);

                    if (!string.IsNullOrEmpty(path) &&
                        path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return path;
                    }

                    index++;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hInfoSet);
            }

            return null;
        }

        private static string? GetDevicePath(IntPtr hInfoSet, ref SP_DEVICE_INTERFACE_DATA ifaceData)
        {
            int requiredSize;
            var dummyDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA
            {
                cbSize = SP_DEVICE_INTERFACE_DETAIL_DATA.CalcCbSize()
            };

            if (!SetupDiGetDeviceInterfaceDetail(
                    hInfoSet,
                    ref ifaceData,
                    ref dummyDetail,
                    0,
                    out requiredSize,
                    IntPtr.Zero))
            {
                // 預期會失敗，用來取得 requiredSize
            }

            var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA
            {
                cbSize = SP_DEVICE_INTERFACE_DETAIL_DATA.CalcCbSize()
            };

            if (!SetupDiGetDeviceInterfaceDetail(
                    hInfoSet,
                    ref ifaceData,
                    ref detail,
                    Marshal.SizeOf(detail),
                    out requiredSize,
                    IntPtr.Zero))
            {
                return null;
            }

            return detail.DevicePath;
        }

        public static UsbDeviceNotificationRegistration RegisterUsbDeviceNotifications(IntPtr windowHandle)
        {
            return new UsbDeviceNotificationRegistration(windowHandle);
        }

        #region SetupAPI

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            string? Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid,
            uint MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            out int RequiredSize,
            IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        #endregion

        #region HID.DLL

        [DllImport("hid.dll", SetLastError = true)]
        private static extern void HidD_GetHidGuid(ref Guid HidGuid);

        #endregion

        #region USER32

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(
            IntPtr hRecipient,
            IntPtr notificationFilter,
            int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;

            public SP_DEVICE_INTERFACE_DATA()
            {
                cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                InterfaceClassGuid = Guid.Empty;
                Flags = 0;
                Reserved = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;

            public static int CalcCbSize()
            {
                return IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize;
            }
        }

        public sealed class UsbDeviceNotificationRegistration : IDisposable
        {
            public event EventHandler<string>? UsbAttached;
            public event EventHandler<string>? UsbRemoved;

            private IntPtr _notificationHandle = IntPtr.Zero;

            internal UsbDeviceNotificationRegistration(IntPtr windowHandle)
            {
                RegisterForUsbNotifications(windowHandle);
            }

            private void RegisterForUsbNotifications(IntPtr windowHandle)
            {
                var dbi = new DEV_BROADCAST_DEVICEINTERFACE
                {
                    dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                    dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                    dbcc_reserved = 0,
                    dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE
                };

                IntPtr buffer = IntPtr.Zero;

                try
                {
                    buffer = Marshal.AllocHGlobal(dbi.dbcc_size);
                    Marshal.StructureToPtr(dbi, buffer, false);

                    _notificationHandle = RegisterDeviceNotification(
                        windowHandle,
                        buffer,
                        DEVICE_NOTIFY_WINDOW_HANDLE);
                }
                finally
                {
                    if (buffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffer);
                }
            }

            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_DEVICECHANGE)
                {
                    int eventType = wParam.ToInt32();

                    switch (eventType)
                    {
                        case DBT_DEVICEARRIVAL:
                            {
                                string? path = GetDevicePathFromLParam(lParam);
                                UsbAttached?.Invoke(this, path ?? string.Empty);
                                break;
                            }
                        case DBT_DEVICEREMOVECOMPLETE:
                            {
                                string? path = GetDevicePathFromLParam(lParam);
                                UsbRemoved?.Invoke(this, path ?? string.Empty);
                                break;
                            }
                    }
                }

                return IntPtr.Zero;
            }

            private static string? GetDevicePathFromLParam(IntPtr lParam)
            {
                if (lParam == IntPtr.Zero)
                    return null;

                var hdr = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(lParam);

                if (hdr.dbch_devicetype != DBT_DEVTYP_DEVICEINTERFACE)
                    return null;

                int offset = Marshal.OffsetOf(typeof(DEV_BROADCAST_DEVICEINTERFACE), "dbcc_name").ToInt32();
                IntPtr pName = IntPtr.Add(lParam, offset);

                return Marshal.PtrToStringUni(pName);
            }

            public void Dispose()
            {
                if (_notificationHandle != IntPtr.Zero)
                {
                    UnregisterDeviceNotification(_notificationHandle);
                    _notificationHandle = IntPtr.Zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_HDR
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;

            public short dbcc_name;
        }
    }
}
