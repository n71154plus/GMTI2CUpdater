using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 封裝單一 HID 裝置的尋找、開啟與 Overlapped 讀寫。
    /// 依 VendorId / ProductId 找到第一個符合的裝置。
    /// </summary>
    public unsafe sealed class HidDevice : IDisposable
    {
        private const uint WaitTimeout = 258;     // WAIT_TIMEOUT
        private const int ERROR_IO_PENDING = 997; // ERROR_IO_PENDING

        private readonly ushort _vendorId;
        private readonly ushort _productId;
        private readonly uint _timeoutMs;
        private readonly int _reportLength;

        // 允許一開始為 null，配合 FindDevice / Open 設定
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
            using (var dev = new HidDevice(vendorId, productId, reportLength: 65))
            {
                return dev.FindDevice();
            }
        }

        public uint LastError => _lastError;

        public bool IsOpen => _handle != null && !_handle.IsInvalid;

        // 對外仍然提供非 nullable string，如果尚未找到裝置，回傳空字串。
        public string DevicePath => _devicePath ?? string.Empty;

        /// <summary>
        /// 尋找並記錄第一個符合 VID/PID 的 HID 裝置路徑。
        /// </summary>
        public bool FindDevice()
        {
            _devicePath = null;
            bool success = false;

            string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}", _vendorId, _productId);
            Guid hidGuid = Guid.Empty;
            HidDevicePInvoke.HidD_GetHidGuid(ref hidGuid);

            // 第二個參數允許 NULL，interop 宣告若是非 nullable，這裡用 null! 告訴編譯器我們確定這樣可以
            IntPtr hInfoSet = HidDevicePInvoke.SetupDiGetClassDevs(ref hidGuid, null!, IntPtr.Zero, 2u | 16u);
            try
            {
                var ifaceData = new SP_DEVICE_INTERFACE_DATA();
                int index = 0;

                while (HidDevicePInvoke.SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref hidGuid, (uint)index, ifaceData))
                {
                    string? path = GetDevicePath(hInfoSet, ifaceData);

                    // path is { Length: > 0 } p 意思是：
                    // - path != null
                    // - path.Length > 0
                    // - 並且在 if 裡用 p 當作「非 nullable string」
                    if (path is { Length: > 0 } p &&
                        p.IndexOf(strSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _devicePath = p;
                        success = true;
                        break;
                    }

                    index++;
                }

            }
            finally
            {
                if (hInfoSet != IntPtr.Zero)
                {
                    HidDevicePInvoke.SetupDiDestroyDeviceInfoList(hInfoSet);
                }
            }

            return success;
        }

        private static string? GetDevicePath(IntPtr hInfoSet, SP_DEVICE_INTERFACE_DATA oInterface)
        {
            int requiredSize = 0;
            int requiredSize2 = 0;

            if (!HidDevicePInvoke.SetupDiGetDeviceInterfaceDetail(hInfoSet, oInterface, null, 0, ref requiredSize, null))
            {
                byte[] detailData = new byte[requiredSize];
                // 第一個 int 是 cbSize
                detailData[0] = (byte)(IntPtr.Size == 8 ? 8 : 5);

                if (HidDevicePInvoke.SetupDiGetDeviceInterfaceDetail(hInfoSet, oInterface, detailData, requiredSize, ref requiredSize2, null))
                {
                    char[] pathChars = new char[requiredSize2 - 3];
                    for (int i = 0; i < requiredSize2 - 4; i++)
                    {
                        pathChars[i] = (char)detailData[i + 4];
                        pathChars[i + 1] = '\0';
                    }

                    return new string(pathChars);
                }
            }

            return null;
        }


        /// <summary>
        /// 確保已開啟 HID 裝置（必要時自動 FindDevice）。
        /// </summary>
        public bool Open()
        {
            if (IsOpen)
                return true;

            if (string.IsNullOrEmpty(_devicePath))
            {
                if (!FindDevice())
                    return false;
            }

            if (string.IsNullOrEmpty(_devicePath))
                return false;

            // 先存到局部變數，再用 null-forgiving 告訴編譯器這裡一定非 null
            string devPath = _devicePath!;
            _handle = HidDevicePInvoke.GetDeviceHandle(devPath, bOverlapped: true);
            if (_handle == null || _handle.IsInvalid)
            {
                return false;
            }

            return true;
        }


        public void Close()
        {
            if (_handle != null && !_handle.IsInvalid)
            {
                _handle.Close();
            }

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

            if (!Open())
                return false;

            var handle = _handle;
            if (handle == null || handle.IsInvalid)
                return false;

            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                int tmp = 0;
                bool ok = DoOverlappedIo(
                    ov => HidDevicePInvoke.WriteFile(handle, buffer, length, ref tmp, (IntPtr)ov),
                    ref tmp);

                if (ok)
                {
                    bytesWritten = tmp;
                    return true;
                }

                if (_lastError == 995) // ERROR_OPERATION_ABORTED
                {
                    if (attempt < maxAttempts)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    return false;
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

            if (!Open())
                return false;

            var handle = _handle;
            if (handle == null || handle.IsInvalid)
                return false;

            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Array.Clear(buffer, 0, buffer.Length);
                int tmp = 0;

                bool ok = DoOverlappedIo(
                    ov => HidDevicePInvoke.ReadFile(handle, buffer, length, ref tmp, (IntPtr)ov),
                    ref tmp);

                if (ok && tmp > 0)
                {
                    bytesRead = tmp;
                    return true;
                }

                if (_lastError == 995) // ERROR_OPERATION_ABORTED
                {
                    if (attempt < maxAttempts)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    return false;
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

        private unsafe bool DoOverlappedIo(Func<IntPtr, bool> startIo, ref int bytesTransferred)
        {
            byte[] ovBuffer = new byte[sizeof(OVERLAPPED)];

            fixed (byte* p = ovBuffer)
            {
                OVERLAPPED* ov = (OVERLAPPED*)p;
                ov->Internal = IntPtr.Zero;
                ov->InternalHigh = IntPtr.Zero;
                ov->UnionPointerOffsetLow = 0;
                ov->UnionPointerOffsetHigh = 0;
                ov->hEvent = HidDevicePInvoke.CreateEvent(0u, 0u, 0u, 0u);

                bytesTransferred = 0;
                _lastError = 0;

                bool started = startIo((IntPtr)ov);
                if (!started)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err != ERROR_IO_PENDING)
                    {
                        _lastError = (uint)err;
                        return false;
                    }
                }

                int wait = HidDevicePInvoke.WaitForSingleObject(ov->hEvent, _timeoutMs);
                uint bytes = 0;

                var handle = _handle;

                switch (wait)
                {
                    case 0: // WAIT_OBJECT_0
                        {
                            if (handle == null || handle.IsInvalid)
                            {
                                _lastError = 6; // ERROR_INVALID_HANDLE
                                return false;
                            }

                            bool ok = HidDevicePInvoke.GetOverlappedResult(handle, ovBuffer, ref bytes, 0u);
                            int err2 = Marshal.GetLastWin32Error();
                            _lastError = (uint)err2;
                            bytesTransferred = (int)bytes;
                            return ok;
                        }

                    case (int)WaitTimeout:
                        {
                            _lastError = (uint)Marshal.GetLastWin32Error();

                            if (handle != null && !handle.IsInvalid)
                            {
                                HidDevicePInvoke.CancelIo(handle);
                            }

                            return false;
                        }

                    default:
                        {
                            _lastError = (uint)Marshal.GetLastWin32Error();
                            return false;
                        }
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
