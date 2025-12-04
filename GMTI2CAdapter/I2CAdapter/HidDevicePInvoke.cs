using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;

namespace GMTI2CUpdater.I2CAdapter
{
    public static class HidDevicePInvoke
    {
        [DllImport("Kernel32.dll")]
        internal static extern CySafeFileHandle CreateFile([In] byte[] filename, [In] int fileaccess, [In] int fileshare, [In] int lpSecurityattributes, [In] int creationdisposition, [In] int flags, [In] IntPtr template);

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern bool ReadFile([In] CySafeFileHandle hDevice, [In][Out] byte[] lpBuffer, [In] int nNumberOfBytesToRead, [In][Out] ref int lpNumberOfBytesRead, [Out] IntPtr lpOverlapped);

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern bool WriteFile([In] CySafeFileHandle hDevice, [In] byte[] lpBuffer, [In] int nNumberOfBytesToWrite, [In][Out] ref int lpNumberOfBytesWritten, [Out] IntPtr lpOverlapped);

        [DllImport("Kernel32.dll")]
        public static extern IntPtr CreateEvent([In] uint lpEventAttributes, [In] uint bManualReset, [In] uint bInitialState, [In] uint lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle([In] IntPtr hDevice);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WaitForSingleObject([In] IntPtr h, [In] uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetOverlappedResult([In] CySafeFileHandle h, [In] byte[] lpOverlapped, [In][Out] ref uint bytesXferred, [In] uint bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CancelIo([In] CySafeFileHandle h);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs([In] ref Guid ClassGuid, [In] byte[] Enumerator, [In] IntPtr hwndParent, [In] uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInterfaces([In] IntPtr DeviceInfoSet, [In] uint DeviceInfoData, [In] ref Guid InterfaceClassGuid, [In] uint MemberIndex, [Out] SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail([In] IntPtr DeviceInfoSet, [In] SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, [Out] byte[]? DeviceInterfaceDetailData, [In] int DeviceInterfaceDetailDataSize, [In][Out] ref int RequiredSize, [Out] SP_DEVINFO_DATA? DeviceInfoData);

        [DllImport("setupapi.dll")]
        internal static extern bool SetupDiDestroyDeviceInfoList([In] IntPtr DeviceInfoSet);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_GetFeature([In] CySafeFileHandle hDevice, [In, Out] byte[] lpFeatureData, [In] int bufLen);
        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_SetFeature([In] CySafeFileHandle hDevice, [In] byte[] lpFeatureData, [In] int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern void HidD_GetHidGuid([In][Out] ref Guid HidGuid);

        internal static CySafeFileHandle GetDeviceHandle(string devPath, bool bOverlapped)
        {
            int accessMode = 3;
            int length = devPath.Length;
            byte[] array = new byte[length + 1];
            for (int i = 0; i < length; i++)
            {
                array[i] = (byte)devPath[i];
            }

            accessMode = 3;
            CySafeFileHandle cySafeFileHandle = CreateFile(array, accessMode, 3, 0, 3, bOverlapped ? 1073741824 : 0, IntPtr.Zero);
            if (cySafeFileHandle.IsInvalid)
            {
                accessMode = 1;
                cySafeFileHandle = CreateFile(array, accessMode, 1, 0, 3, bOverlapped ? 1073741824 : 0, IntPtr.Zero);
            }

            if (cySafeFileHandle.IsInvalid)
            {
                accessMode = 0;
                cySafeFileHandle = CreateFile(array, accessMode, 3, 0, 3, bOverlapped ? 1073741824 : 0, IntPtr.Zero);
            }
            Thread.Sleep(500);
            return cySafeFileHandle;
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;

        public Guid InterfaceClassGuid;

        public uint Flags;

        public IntPtr Reserved;
        public SP_DEVICE_INTERFACE_DATA()
        {
            // 自動填入結構長度
            cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class SP_DEVINFO_DATA
    {
        public int cbSize;

        public Guid ClassGuid;

        public uint DevInst;

        public IntPtr Reserved;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OVERLAPPED
    {
        public IntPtr Internal;

        public IntPtr InternalHigh;

        public uint UnionPointerOffsetLow;

        public uint UnionPointerOffsetHigh;

        public IntPtr hEvent;
    }
}
