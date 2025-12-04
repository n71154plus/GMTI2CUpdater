using System;
using System.Runtime.InteropServices;


namespace GMTI2CUpdater.Helper
{
    /// <summary>
    /// 提供與螢幕列舉相關的 P/Invoke helper。
    /// </summary>
    public static class MonitorHelper
    {
        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdc, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        /// <summary>
        /// 透過 Win32 API 列舉顯示器並回傳數量。
        /// </summary>
        public static int GetMonitorCount()
        {
            int count = 0;

            EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdc, ref Rect rect, IntPtr data) =>
                {
                    count++;
                    return true;
                },
                IntPtr.Zero);

            return count;
        }

    }
}
