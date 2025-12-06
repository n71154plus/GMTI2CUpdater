using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    /// <summary>
    /// 封裝單一 HID 裝置的尋找、開啟與 Overlapped 讀寫。
    /// 依 VendorId / ProductId 找到第一個符合的裝置。
    /// </summary>
    public sealed class HidDevice : IDisposable
    {
        private const uint WaitTimeout = 258;      // WAIT_TIMEOUT
        private const int ErrorIoPending = 997;    // ERROR_IO_PENDING
        private const int ErrorInvalidHandle = 6;  // ERROR_INVALID_HANDLE
        private const int ErrorOperationAborted = 995;

        private readonly ushort _vendorId;
        private readonly ushort _productId;
        private readonly uint _timeoutMs;
        private readonly int _reportLength;

        private string? _devicePath;
        private CySafeFileHandle? _handle;
        private uint _lastError;

        public HidDevice(ushort vendorId, ushort productId,
                         int reportLength,
                         uint timeoutMs = 500)
        {
            _vendorId = vendorId;
            _productId = productId;
            _timeoutMs = timeoutMs;
            _reportLength = reportLength;
        }

        /// <summary>
        /// 只檢查指定 VID / PID 的 HID 裝置是否存在，不開啟任何 handle。
        /// </summary>
        public static bool Exists(ushort vendorId, ushort productId)
        {
            using var dev = new HidDevice(vendorId, productId, reportLength: 65);
            return dev.FindDevice();
        }

        public uint LastError => _lastError;

        public bool IsOpen => _handle != null && !_handle.IsInvalid;

        public string DevicePath => _devicePath ?? string.Empty;

        /// <summary>
        /// 尋找並記錄第一個符合 VID/PID 的 HID 裝置路徑。
        /// </summary>
        public bool FindDevice()
        {
            _devicePath = UsbInfoManager.FindDevice(_vendorId, _productId);
            return !string.IsNullOrEmpty(_devicePath);
        }

        /// <summary>
        /// 確保已開啟 HID 裝置（必要時自動 FindDevice）。
        /// </summary>
        public bool Open()
        {
            if (IsOpen)
            {
                return true;
            }

            if (string.IsNullOrEmpty(_devicePath) && !FindDevice())
            {
                return false;
            }

            if (string.IsNullOrEmpty(_devicePath))
            {
                return false;
            }

            string devPath = _devicePath!;
            _handle = NativeMethods.GetDeviceHandle(devPath, overlapped: true);

            return _handle != null && !_handle.IsInvalid;
        }

        public void Close()
        {
            _handle?.Close();
            _handle = null;
        }

        /// <summary>
        /// 寫入一整個 Report (或指定長度)，使用 Overlapped I/O。
        /// </summary>
        public bool Write(byte[] buffer, int length, out int bytesWritten)
        {
            bytesWritten = 0;

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (length <= 0 || length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            if (length > _reportLength) throw new ArgumentOutOfRangeException(nameof(length), "length 超過 report 長度");

            if (!Open())
            {
                return false;
            }

            var handle = _handle;
            if (handle == null || handle.IsInvalid)
            {
                return false;
            }

            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                int tmp = 0;
                bool ok = DoOverlappedIo(
                    ov => NativeMethods.WriteFile(handle, buffer, length, ref tmp, ov),
                    ref tmp);

                if (ok)
                {
                    bytesWritten = tmp;
                    return true;
                }

                if (_lastError == ErrorOperationAborted && attempt < maxAttempts)
                {
                    Thread.Sleep(50);
                    continue;
                }

                if (attempt < maxAttempts)
                {
                    Thread.Sleep(50);
                    continue;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// 讀取一整個 Report (或指定長度)，使用 Overlapped I/O。
        /// </summary>
        public bool Read(byte[] buffer, int length, out int bytesRead)
        {
            bytesRead = 0;

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (length <= 0 || length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            if (length > _reportLength) throw new ArgumentOutOfRangeException(nameof(length), "length 超過 report 長度");

            if (!Open())
            {
                return false;
            }

            var handle = _handle;
            if (handle == null || handle.IsInvalid)
            {
                return false;
            }

            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Array.Clear(buffer, 0, buffer.Length);
                int tmp = 0;

                bool ok = DoOverlappedIo(
                    ov => NativeMethods.ReadFile(handle, buffer, length, ref tmp, ov),
                    ref tmp);

                if (ok && tmp > 0)
                {
                    bytesRead = tmp;
                    return true;
                }

                if (_lastError == ErrorOperationAborted && attempt < maxAttempts)
                {
                    Thread.Sleep(50);
                    continue;
                }

                if (attempt < maxAttempts)
                {
                    Thread.Sleep(50);
                    continue;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// 共用的 Overlapped I/O 執行流程：建立 OVERLAPPED + Event，啟動 I/O 並等待。
        /// </summary>
        private bool DoOverlappedIo(Func<IntPtr, bool> startIo, ref int bytesTransferred)
        {
            bytesTransferred = 0;
            _lastError = 0;

            using var hEvent = NativeMethods.CreateEvent(IntPtr.Zero, false, false, null);
            if (hEvent == null || hEvent.IsInvalid)
            {
                _lastError = (uint)Marshal.GetLastWin32Error();
                return false;
            }

            int ovSize = Marshal.SizeOf<NativeOverlapped>();
            IntPtr pOv = Marshal.AllocHGlobal(ovSize);

            try
            {
                var ov = new NativeOverlapped
                {
                    Internal = IntPtr.Zero,
                    InternalHigh = IntPtr.Zero,
                    Offset = 0,
                    OffsetHigh = 0,
                    EventHandle = hEvent.DangerousGetHandle()
                };

                Marshal.StructureToPtr(ov, pOv, fDeleteOld: false);

                bool started = startIo(pOv);
                if (!started)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err != ErrorIoPending)
                    {
                        _lastError = (uint)err;
                        return false;
                    }
                }

                uint wait = NativeMethods.WaitForSingleObject(hEvent.DangerousGetHandle(), _timeoutMs);
                var handle = _handle;

                if (wait == 0) // WAIT_OBJECT_0
                {
                    if (handle == null || handle.IsInvalid)
                    {
                        _lastError = ErrorInvalidHandle;
                        return false;
                    }

                    bool ok = NativeMethods.GetOverlappedResult(handle, pOv, out uint bytes, false);
                    _lastError = (uint)Marshal.GetLastWin32Error();
                    bytesTransferred = (int)bytes;
                    return ok;
                }

                if (wait == WaitTimeout)
                {
                    _lastError = WaitTimeout;

                    if (handle != null && !handle.IsInvalid)
                    {
                        NativeMethods.CancelIo(handle);
                    }

                    return false;
                }

                _lastError = (uint)Marshal.GetLastWin32Error();
                return false;
            }
            finally
            {
                if (pOv != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pOv);
                }
            }
        }

        public void Dispose()
        {
            Close();
        }

        private static class NativeMethods
        {
            public const uint GenericRead = 0x80000000;
            public const uint GenericWrite = 0x40000000;

            public const uint FileShareRead = 0x00000001;
            public const uint FileShareWrite = 0x00000002;

            public const uint OpenExisting = 3;
            public const uint FileFlagOverlapped = 0x40000000;

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
            internal static extern SafeEventHandle CreateEvent(
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

            /// <summary>
            /// 依 device path 開啟 HID 裝置，必要時指定 overlapped。
            /// </summary>
            internal static CySafeFileHandle GetDeviceHandle(string devPath, bool overlapped)
            {
                uint flags = overlapped ? FileFlagOverlapped : 0;

                var handle = CreateFile(
                    devPath,
                    GenericRead | GenericWrite,
                    FileShareRead | FileShareWrite,
                    IntPtr.Zero,
                    OpenExisting,
                    flags,
                    IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    handle = CreateFile(
                        devPath,
                        GenericRead,
                        FileShareRead | FileShareWrite,
                        IntPtr.Zero,
                        OpenExisting,
                        flags,
                        IntPtr.Zero);
                }

                if (handle.IsInvalid)
                {
                    handle = CreateFile(
                        devPath,
                        0,
                        FileShareRead | FileShareWrite,
                        IntPtr.Zero,
                        OpenExisting,
                        flags,
                        IntPtr.Zero);
                }

                return handle;
            }
        }

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        private sealed class CySafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private CySafeFileHandle()
                : base(ownsHandle: true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(handle);
            }
        }

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        private sealed class SafeEventHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeEventHandle()
                : base(ownsHandle: true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(handle);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeOverlapped
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr EventHandle;
        }
    }
}
