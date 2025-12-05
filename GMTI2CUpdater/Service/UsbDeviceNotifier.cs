using System;
using System.Runtime.InteropServices;

namespace GMTI2CUpdater.Service
{
    public class UsbDeviceNotifier : IDisposable
    {
        // --- 常數定義 ---
        private const int WM_DEVICECHANGE = 0x0219;

        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        // 只關心 USB 裝置
        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE =
            new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        // --- P/Invoke 結構與方法 ---
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

            // 可變長度字串的開頭，實際上記憶體後面還會接著完整字串
            public short dbcc_name;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(
            IntPtr hRecipient,
            IntPtr notificationFilter,
            int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        private IntPtr _notificationHandle = IntPtr.Zero;

        public event EventHandler<string>? UsbAttached;
        public event EventHandler<string>? UsbRemoved;

        public UsbDeviceNotifier(IntPtr windowHandle)
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
                            string? path = GetDevicePath(lParam);
                            UsbAttached?.Invoke(this, path ?? string.Empty);
                            break;
                        }
                    case DBT_DEVICEREMOVECOMPLETE:
                        {
                            string? path = GetDevicePath(lParam);
                            UsbRemoved?.Invoke(this, path ?? string.Empty);
                            break;
                        }
                }
            }

            return IntPtr.Zero;
        }

        private static string? GetDevicePath(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
                return null;

            var hdr = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(lParam);

            if (hdr.dbch_devicetype != DBT_DEVTYP_DEVICEINTERFACE)
                return null;

            // 取得結構中 dbcc_name 欄位的位移
            int offset = Marshal.OffsetOf(typeof(DEV_BROADCAST_DEVICEINTERFACE), "dbcc_name").ToInt32();
            IntPtr pName = IntPtr.Add(lParam, offset);

            // 裝置路徑是以 0 結尾的 Unicode 字串
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
}
