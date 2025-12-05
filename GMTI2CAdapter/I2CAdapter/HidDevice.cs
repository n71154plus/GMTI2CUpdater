using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 封裝單一 HID 裝置的尋找、開啟與 Overlapped 讀寫。
    /// 依 VendorId / ProductId 找到第一個符合的裝置。
    /// </summary>
    public sealed class HidDevice : IDisposable
    {
        private const uint WAIT_TIMEOUT = 258;      // WAIT_TIMEOUT
        private const int ERROR_IO_PENDING = 997;   // ERROR_IO_PENDING
        private const int ERROR_INVALID_HANDLE = 6; // ERROR_INVALID_HANDLE
        private const int ERROR_OPERATION_ABORTED = 995;

        private readonly ushort _vendorId;
        private readonly ushort _productId;
        private readonly uint _timeoutMs;
        private readonly int _reportLength;

        private string? _devicePath;
        private CySafeFileHandle? _handle;
        private uint _lastError;
        private bool _disposed;

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
            ThrowIfDisposed();

            _devicePath = null;
            bool success = false;

            string search = $"vid_{_vendorId:x4}&pid_{_productId:x4}";

            Guid hidGuid = Guid.Empty;
            HidDevicePInvoke.HidD_GetHidGuid(ref hidGuid);

            IntPtr hInfoSet = HidDevicePInvoke.SetupDiGetClassDevs(
                ref hidGuid,
                null,
                IntPtr.Zero,
                HidDevicePInvoke.DIGCF_PRESENT | HidDevicePInvoke.DIGCF_DEVICEINTERFACE);

            if (hInfoSet == IntPtr.Zero || hInfoSet.ToInt64() == -1)
                return false;

            try
            {
                var ifaceData = new SP_DEVICE_INTERFACE_DATA();
                uint index = 0;

                while (HidDevicePInvoke.SetupDiEnumDeviceInterfaces(
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
                        _devicePath = path;
                        success = true;
                        break;
                    }

                    index++;
                }
            }
            finally
            {
                HidDevicePInvoke.SetupDiDestroyDeviceInfoList(hInfoSet);
            }

            return success;
        }

        private static string? GetDevicePath(IntPtr hInfoSet, ref SP_DEVICE_INTERFACE_DATA ifaceData)
        {
            // 先問需要多少 buffer
            int requiredSize;
            var dummyDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA
            {
                cbSize = SP_DEVICE_INTERFACE_DETAIL_DATA.CalcCbSize()
            };

            if (!HidDevicePInvoke.SetupDiGetDeviceInterfaceDetail(
                    hInfoSet,
                    ref ifaceData,
                    ref dummyDetail,
                    0,
                    out requiredSize,
                    IntPtr.Zero))
            {
                // 這裡預期會失敗，僅是拿 requiredSize
            }

            var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA
            {
                cbSize = SP_DEVICE_INTERFACE_DETAIL_DATA.CalcCbSize()
            };

            if (!HidDevicePInvoke.SetupDiGetDeviceInterfaceDetail(
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

        /// <summary>
        /// 確保已開啟 HID 裝置（必要時自動 FindDevice）。
        /// </summary>
        public bool Open()
        {
            ThrowIfDisposed();

            if (IsOpen)
                return true;

            if (string.IsNullOrEmpty(_devicePath))
            {
                if (!FindDevice())
                    return false;
            }

            if (string.IsNullOrEmpty(_devicePath))
                return false;

            string devPath = _devicePath!;
            _handle = HidDevicePInvoke.GetDeviceHandle(devPath, overlapped: true);

            if (_handle == null || _handle.IsInvalid)
                return false;

            return true;
        }

        public void Close()
        {
            ThrowIfDisposed();

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

            ThrowIfDisposed();

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (length <= 0 || length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            if (length > _reportLength) throw new ArgumentOutOfRangeException(nameof(length), "length 超過 report 長度");

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
                    ov => HidDevicePInvoke.WriteFile(handle, buffer, length, ref tmp, ov),
                    ref tmp);

                if (ok)
                {
                    bytesWritten = tmp;
                    return true;
                }

                // ERROR_OPERATION_ABORTED 可以稍等重試
                if (_lastError == ERROR_OPERATION_ABORTED && attempt < maxAttempts)
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

            ThrowIfDisposed();

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
                    ov => HidDevicePInvoke.ReadFile(handle, buffer, length, ref tmp, ov),
                    ref tmp);

                if (ok && tmp > 0)
                {
                    bytesRead = tmp;
                    return true;
                }

                if (_lastError == ERROR_OPERATION_ABORTED && attempt < maxAttempts)
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

            ThrowIfDisposed();

            // 建立 Event
            IntPtr hEvent = HidDevicePInvoke.CreateEvent(IntPtr.Zero, false, false, null);
            if (hEvent == IntPtr.Zero || hEvent.ToInt64() == -1)
            {
                _lastError = (uint)Marshal.GetLastWin32Error();
                return false;
            }

            // 配置 unmanaged OVERLAPPED 結構
            int ovSize = Marshal.SizeOf(typeof(OVERLAPPED));
            IntPtr pOv = Marshal.AllocHGlobal(ovSize);

            try
            {
                var ov = new OVERLAPPED
                {
                    Internal = IntPtr.Zero,
                    InternalHigh = IntPtr.Zero,
                    Offset = 0,
                    OffsetHigh = 0,
                    hEvent = hEvent
                };

                Marshal.StructureToPtr(ov, pOv, false);

                bool started = startIo(pOv);
                if (!started)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err != ERROR_IO_PENDING)
                    {
                        _lastError = (uint)err;
                        return false;
                    }
                }

                // 等待 I/O 完成或逾時
                uint wait = HidDevicePInvoke.WaitForSingleObject(hEvent, _timeoutMs);
                var handle = _handle;

                if (wait == 0) // WAIT_OBJECT_0
                {
                    if (handle == null || handle.IsInvalid)
                    {
                        _lastError = ERROR_INVALID_HANDLE;
                        return false;
                    }

                    uint bytes = 0;
                    bool ok = HidDevicePInvoke.GetOverlappedResult(handle, pOv, out bytes, false);
                    _lastError = (uint)Marshal.GetLastWin32Error();
                    bytesTransferred = (int)bytes;
                    return ok;
                }

                if (wait == WAIT_TIMEOUT)
                {
                    _lastError = WAIT_TIMEOUT;

                    if (handle != null && !handle.IsInvalid)
                    {
                        HidDevicePInvoke.CancelIo(handle);
                    }

                    return false;
                }

                // 其他錯誤
                _lastError = (uint)Marshal.GetLastWin32Error();
                return false;
            }
            finally
            {
                if (hEvent != IntPtr.Zero)
                {
                    HidDevicePInvoke.CloseHandle(hEvent);
                }

                if (pOv != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pOv);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Close();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HidDevice));
            }
        }
    }
}
