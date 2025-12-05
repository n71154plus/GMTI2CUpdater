using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace GMTI2CUpdater.I2CAdapter
{
    public static class HidDevicePInvoke
    {
        #region 常數

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;

        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;

        public const uint OPEN_EXISTING = 3;
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        public const uint DIGCF_DEFAULT = 0x00000001;
        public const uint DIGCF_PRESENT = 0x00000002;
        public const uint DIGCF_ALLCLASSES = 0x00000004;
        public const uint DIGCF_PROFILE = 0x00000008;
        public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

        #endregion

        #region Kernel32

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern CySafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadFile(
            CySafeFileHandle hFile,
            [In, Out] byte[] lpBuffer,
            int nNumberOfBytesToRead,
            ref int lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteFile(
            CySafeFileHandle hFile,
            byte[] lpBuffer,
            int nNumberOfBytesToWrite,
            ref int lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes,
            bool bManualReset,
            bool bInitialState,
            string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(
            IntPtr hHandle,
            uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetOverlappedResult(
            CySafeFileHandle hFile,
            IntPtr lpOverlapped,
            out uint lpNumberOfBytesTransferred,
            bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CancelIo(CySafeFileHandle hFile);

        #endregion

        #region SetupAPI

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            string? Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid,
            uint MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            out int RequiredSize,
            IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        #endregion

        #region HID.DLL

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetFeature(
            CySafeFileHandle hDevice,
            [In, Out] byte[] lpFeatureData,
            int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_SetFeature(
            CySafeFileHandle hDevice,
            [In] byte[] lpFeatureData,
            int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern void HidD_GetHidGuid(ref Guid HidGuid);

        #endregion

        /// <summary>
        /// 依 device path 開啟 HID 裝置，必要時指定 overlapped。
        /// </summary>
        internal static CySafeFileHandle GetDeviceHandle(string devPath, bool overlapped)
        {
            uint flags = overlapped ? FILE_FLAG_OVERLAPPED : 0;

            // 先試著用 R/W 開啟
            var handle = CreateFile(
                devPath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                flags,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                // 再試試只讀
                handle = CreateFile(
                    devPath,
                    GENERIC_READ,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    flags,
                    IntPtr.Zero);
            }

            if (handle.IsInvalid)
            {
                // 再試試 0 存取
                handle = CreateFile(
                    devPath,
                    0,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    flags,
                    IntPtr.Zero);
            }

            // 部分裝置剛開啟時會需要一點時間 ready，保留你原本的 delay（可以視情況調整）
            //Thread.Sleep(100);
            return handle;
        }
    }

    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public sealed class CySafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private CySafeFileHandle()
            : base(ownsHandle: true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return HidDevicePInvoke.CloseHandle(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVICE_INTERFACE_DATA
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
    internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        public int cbSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DevicePath;

        public static int CalcCbSize()
        {
            // 官方建議：32-bit = 4 + 2, 64-bit = 8
            return IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OVERLAPPED
    {
        public IntPtr Internal;
        public IntPtr InternalHigh;
        public uint Offset;
        public uint OffsetHigh;
        public IntPtr hEvent;
    }
}
