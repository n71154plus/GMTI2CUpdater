
using System.Runtime.InteropServices;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    /// <summary>
    /// Intel IgfxExt.CUIExternal COM 封裝（直接透過 vtable index 呼叫）。
    ///
    /// 功能：
    /// - 列舉可用顯示器 (EnumAttachableDevices)
    /// - 透過 AUX 通道存取 DPCD / I2C (GetDeviceData / SetDeviceData)
    ///
    /// 注意：
    /// - 必須在 STA 執行緒上建立與使用（Main 建議加 [STAThread]）。
    /// - using 或手動 Dispose() 釋放資源。
    /// </summary>
    public sealed class IntelIGFXApi : IDisposable
    {
        #region 內部欄位與常數

        private bool _disposed;

        // COM 指標：ICUIExternal8*
        private IntPtr _cuiPtr = IntPtr.Zero;

        // vtable delegate
        private EnumAttachableDevicesDelegate _enumAttachableDevices = null!;
        private GetDeviceDataDelegate _getDeviceData = null!;
        private SetDeviceDataDelegate _setDeviceData = null!;

        // COM 是否由此物件呼叫 CoInitialize
        private bool _coInitialized;

        // I2C 設定
        private ushort _i2cDelayMs;

        // DPCD / I2C chunk 設定
        private const int MaxDpcdChunk = 0x10;    // 每次 DPCD 讀寫最大長度
        private const int MaxI2cReadChunk = 0x04;  // 每次 I2C 讀取最大長度
        private const int MaxI2cWriteChunk = 0x04; // 每次 I2C 寫入最大資料 byte 數

        private const int AuxBufferSize = 0x40;    // AUX buffer 固定大小
        private const int MaxAuxChunk = 0x20;      // AUX 允許的最大 chunk
        private const int DefaultI2cDelayMs = 20;  // 預設 I2C 寫入後延遲 (ms)

        // AuxIo.Op 操作碼
        private const int AuxOpI2cWriteNotLast = 4;
        private const int AuxOpI2cWriteLast = 0;
        private const int AuxOpI2cReadLast = 1;
        private const int AuxOpI2cReadNotLast = 5;
        private const int AuxOpDpcdWrite = 8;
        private const int AuxOpDpcdRead = 9;

        // HRESULT
        private const uint RPC_E_CHANGED_MODE = 0x80010106;
        private const uint REGDB_E_CLASSNOTREG = 0x80040154;

        // CLSCTX
        private const uint CLSCTX_INPROC_SERVER = 0x1;
        private const uint CLSCTX_INPROC_HANDLER = 0x2;
        private const uint CLSCTX_LOCAL_SERVER = 0x4;
        private const uint CLSCTX_REMOTE_SERVER = 0x10;

        // AUX GUID（由 IgfxAuxBlob 解出）
        private static readonly Guid AuxGuid = new Guid("BFB9816C-AEB0-434B-99F3-0F94E6BEBF0D");

        // ICUIExternal8 介面 IID
        private static readonly Guid IID_ICUIExternal8 = new Guid("f932c038-6484-45ca-8fa1-7c8c279f7aee");

        // vtable index（包含 IUnknown 三個函式之後的絕對 index）
        // 這些 index 是「已知的固定位置」，不需要宣告 Dummy methods：
        //   0: IUnknown::QueryInterface
        //   1: IUnknown::AddRef
        //   2: IUnknown::Release
        //
        //   12: EnumAttachableDevices
        //   43: GetDeviceData
        //   44: SetDeviceData
        private const int VtblIndex_EnumAttachableDevices = 12;
        private const int VtblIndex_GetDeviceData = 43;
        private const int VtblIndex_SetDeviceData = 44;

        #endregion

        #region 公開 helper 型別


        #endregion

        #region 建構 / 釋放

        /// <summary>
        /// 建構 IntelCui，初始化 COM，建立 IgfxExt.CUIExternal 並解析 vtable。
        /// </summary>
        public IntelIGFXApi()
        {
            // 1. 初始化 COM（STA）
            int hr = NativeMethods.CoInitialize(IntPtr.Zero);
            if (NativeMethods.FAILED(hr) && (uint)hr != RPC_E_CHANGED_MODE)
            {
                throw new InvalidOperationException(
                    string.Format("CoInitialize failed: 0x{0:X8}", (uint)hr));
            }

            _coInitialized = (uint)hr != RPC_E_CHANGED_MODE;
            _i2cDelayMs = DefaultI2cDelayMs;

            // 2. 取得 CLSID
            Guid clsid;
            hr = NativeMethods.CLSIDFromProgID("IgfxExt.CUIExternal", out clsid);
            if (NativeMethods.FAILED(hr))
            {
                HandleClsIdError(hr, _coInitialized);
            }
            Guid guid = IID_ICUIExternal8;
            // 3. 直接 CoCreateInstance，要求 ICUIExternal8 介面指標
            hr = NativeMethods.CoCreateInstance(
                ref clsid,
                IntPtr.Zero,
                CLSCTX_LOCAL_SERVER,
                ref guid,
                out _cuiPtr);

            if (NativeMethods.FAILED(hr) || _cuiPtr == IntPtr.Zero)
            {
                CleanupCom();
                if ((uint)hr == REGDB_E_CLASSNOTREG)
                    throw new InvalidOperationException("Intel IGFX: COM class 'IgfxExt.CUIExternal' not registered.");

                throw new InvalidOperationException(
                    string.Format("CoCreateInstance(IgfxExt.CUIExternal) failed: 0x{0:X8}", (uint)hr));
            }

            // 4. 從 vtable 解析函式指標
            InitVTableDelegates(_cuiPtr);
        }

        /// <summary>
        /// 解構子，保險起見（主要還是依賴 using / Dispose）。
        /// </summary>
        ~IntelIGFXApi()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            CleanupCom();
        }

        private void CleanupCom()
        {
            if (_cuiPtr != IntPtr.Zero)
            {
                Marshal.Release(_cuiPtr);
                _cuiPtr = IntPtr.Zero;
            }

            _enumAttachableDevices = null!;
            _getDeviceData = null!;
            _setDeviceData = null!;

            if (_coInitialized)
            {
                NativeMethods.CoUninitialize();
                _coInitialized = false;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("IntelCUI");
        }

        private void EnsureComObject()
        {
            if (_cuiPtr == IntPtr.Zero ||
                _enumAttachableDevices == null ||
                _getDeviceData == null ||
                _setDeviceData == null)
            {
                throw new InvalidOperationException("Intel IGFX COM object or vtable delegates are not initialized.");
            }
        }

        #endregion

        #region 公開屬性 / 設定

        /// <summary>
        /// 實作描述字串，僅供顯示。
        /// </summary>
        public string Name = "Intel Graphics Command Center AUX/I2C Bridge (vtable-only)";

        /// <summary>
        /// 設定每次 I2C Write 之後的延遲時間（毫秒）。預設 20ms。
        /// </summary>
        public void SetDelay(ushort milliseconds)
        {
            _i2cDelayMs = milliseconds;
        }

        #endregion

        #region 顯示器列舉

        /// <summary>
        /// 使用 EnumAttachableDevices 列出目前可透過 Intel 驅動操作的顯示器。
        ///
        /// 備註：
        /// - 目前只嘗試 "\\.\DISPLAY1" 和 "\\.\DISPLAY2"，如需更多輸出可自行擴充。
        /// - 只要 uidMonitor != 0 且 HRESULT OK 就視為有效顯示器。
        /// </summary>
        public I2CAdapterInfo[] GetAvailableDisplays()
        {
            EnsureNotDisposed();
            EnsureComObject();

            var list = new List<I2CAdapterInfo>();

            string[] adapterNames = { @"\\.\DISPLAY1", @"\\.\DISPLAY2" };
            uint[] indicesToTry = { 0, 1, 2, 3 };

            for (int i = 0; i < adapterNames.Length; i++)
            {
                string adapterName = adapterNames[i];
                foreach (uint indexMonitor in indicesToTry)
                {
                    uint uidMonitor;
                    uint deviceType = unchecked(0x20000000);
                    uint status;

                    int hr = _enumAttachableDevices(
                        _cuiPtr,
                        adapterName,
                        indexMonitor,
                        out uidMonitor,
                        ref deviceType,
                        out status);

                    if (NativeMethods.FAILED(hr))
                        continue;

                    if (uidMonitor != 0)
                    {
                        var info = new I2CAdapterInfo
                        {
                            MonitorUid = uidMonitor,
                            Name = $"Intel,裝置{i}營幕{indexMonitor}",
                            IsFromDisplay = true,
                            IsNeedPrivilege = false,
                        };

                        list.Add(info);
                    }
                }
            }

            return list.ToArray();
        }

        #endregion

        #region 高階 DPCD API

        /// <summary>
        /// 以 DPCD 方式從指定顯示器讀取連續位址範圍。
        /// </summary>
        public byte[] ReadDpcd(I2CAdapterInfo display, uint addr, uint length)
        {
            EnsureNotDisposed();
            EnsureComObject();

            if (length == 0)
                throw new ArgumentException("DPCD read length must be greater than zero.", "Length");

            var result = new byte[length];
            uint remaining = length;
            uint offset = addr;
            int writePos = 0;

            while (remaining > 0)
            {
                uint chunk = remaining;
                if (chunk > MaxDpcdChunk)
                    chunk = MaxDpcdChunk;

                byte[] data = Inner_ReadDpcd(display, offset, chunk);
                if (data == null || data.Length != (int)chunk)
                    throw new InvalidOperationException("Intel IGFX: DPCD read chunk size mismatch.");

                Buffer.BlockCopy(data, 0, result, writePos, (int)chunk);
                writePos += (int)chunk;
                offset += chunk;
                remaining -= chunk;
            }

            return result;
        }

        /// <summary>
        /// 以 DPCD 方式對指定顯示器寫入連續位址範圍。
        /// </summary>
        public void WriteDpcd(I2CAdapterInfo display, uint addr, byte[] data)
        {
            EnsureNotDisposed();
            EnsureComObject();

            if (data == null || data.Length == 0)
                return;

            uint offset = addr;
            int remaining = data.Length;
            int srcPos = 0;

            while (remaining > 0)
            {
                int chunk = remaining;
                if (chunk > MaxDpcdChunk)
                    chunk = MaxDpcdChunk;

                var payload = new byte[chunk];
                Buffer.BlockCopy(data, srcPos, payload, 0, chunk);

                Inner_WriteDpcd(display, offset, payload);

                offset += (uint)chunk;
                srcPos += chunk;
                remaining -= chunk;
            }
        }

        #endregion

        #region 高階 I2C API

        /// <summary>
        /// 對指定 I2C 位址寫入單一 byte（無額外 index）。
        /// </summary>
        public void WriteI2CWithoutIndex(I2CAdapterInfo display, byte address, byte data)
        {
            EnsureNotDisposed();
            EnsureComObject();

            var io = new AuxIo
            {
                Display = display.MonitorUid,
                Op = AuxOpI2cWriteLast,
                Len = 1,
                Address = address,
                Buf = new byte[AuxBufferSize]
            };

            io.Buf[0] = data;

            int devErr = 0;
            int hr = CallAuxWrite(ref io, ref devErr);
            if (NativeMethods.FAILED(hr) || devErr != 0)
                throw new InvalidOperationException(AuxError("I2CWrite", hr, devErr));

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);
        }

        /// <summary>
        /// 使用單一 byte index，從 index 起連續寫入 data。
        /// index + offset 會做為實際 I2C 起始位址。
        /// </summary>
        public void WriteI2CByteIndex(I2CAdapterInfo display, byte address, byte index, byte[] data)
        {
            EnsureNotDisposed();
            EnsureComObject();

            if (data == null || data.Length == 0)
                return;

            const int ChunkSize = MaxI2cWriteChunk;
            int offset = 0;

            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);

                var io = new AuxIo
                {
                    Display = display.MonitorUid,
                    Op = AuxOpI2cWriteLast,
                    Len = 1 + currentLen, // 1 byte index + N bytes data
                    Address = address,
                    Buf = new byte[AuxBufferSize]
                };

                io.Buf[0] = (byte)(index + offset);
                Array.Copy(data, offset, io.Buf, 1, currentLen);

                int devErr = 0;
                int hr = CallAuxWrite(ref io, ref devErr);
                if (NativeMethods.FAILED(hr) || devErr != 0)
                    throw new InvalidOperationException(AuxError("I2CWrite", hr, devErr));

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                offset += currentLen;
            }
        }

        /// <summary>
        /// 使用 16-bit index，從 index 起連續寫入 data。
        /// index + offset 會做為實際 I2C 起始位址。
        /// </summary>
        public void WriteI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, byte[] data)
        {
            EnsureNotDisposed();
            EnsureComObject();

            if (data == null || data.Length == 0)
                return;

            const int ChunkSize = MaxI2cWriteChunk;
            int offset = 0;

            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);

                var io = new AuxIo
                {
                    Display = display.MonitorUid,
                    Op = AuxOpI2cWriteLast,
                    Len = 2 + currentLen, // 2 bytes index + N bytes data
                    Address = address,
                    Buf = new byte[AuxBufferSize]
                };

                byte[] indexBytes = BitConverter.GetBytes(index);
                Array.Reverse(indexBytes); // big-endian
                Array.Copy(indexBytes, 0, io.Buf, 0, indexBytes.Length);

                Array.Copy(data, offset, io.Buf, 2, currentLen);

                int devErr = 0;
                int hr = CallAuxWrite(ref io, ref devErr);
                if (NativeMethods.FAILED(hr) || devErr != 0)
                    throw new InvalidOperationException(AuxError("I2CWrite", hr, devErr));

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                offset += currentLen;
            }
        }

        /// <summary>
        /// 從指定 I2C 位址讀取單一 byte（無額外 index）。
        /// </summary>
        public byte ReadI2CWithoutIndex(I2CAdapterInfo display, byte address)
        {
            EnsureNotDisposed();
            EnsureComObject();

            var io = new AuxIo
            {
                Display = display.MonitorUid,
                Op = AuxOpI2cReadLast,
                Len = 1,
                Address = address,
                Buf = new byte[AuxBufferSize]
            };

            for (int i = io.Buf.Length - 1; i >= 0; i--)
            {
                io.Buf[i] = 0xFF;
            }

            int devErr = 0;
            int hr = CallAuxRead(ref io, ref devErr);
            if (NativeMethods.FAILED(hr) || devErr != 0)
                throw new InvalidOperationException(AuxError("I2CRead", hr, devErr));

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);

            return io.Buf[0];
        }

        /// <summary>
        /// 使用單一 byte index，從 index 起連續讀取 length 個 byte。
        /// </summary>
        public byte[] ReadI2CByteIndex(I2CAdapterInfo display, byte address, byte index, int length)
        {
            EnsureNotDisposed();
            EnsureComObject();

            if (length <= 0)
                throw new ArgumentOutOfRangeException("Length", "Length must be greater than zero.");

            byte[] result = new byte[length];
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            // 先寫入 index（不結束 transaction）
            var io = new AuxIo
            {
                Display = display.MonitorUid,
                Op = AuxOpI2cWriteNotLast,
                Len = 1,
                Address = address,
                Buf = new byte[AuxBufferSize]
            };
            io.Buf[0] = index;

            int devErr = 0;
            int hr = CallAuxWrite(ref io, ref devErr);
            if (NativeMethods.FAILED(hr) || devErr != 0)
                throw new InvalidOperationException(AuxError("I2CWrite", hr, devErr));

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);

            // 再分段讀取
            while (offset < length)
            {
                int remaining = length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);
                bool isLastChunk = remaining <= ChunkSize;

                io = new AuxIo
                {
                    Display = display.MonitorUid,
                    Op = isLastChunk ? AuxOpI2cReadLast : AuxOpI2cReadNotLast,
                    Len = currentLen,
                    Address = address,
                    Buf = new byte[AuxBufferSize]
                };

                devErr = 0;
                hr = CallAuxRead(ref io, ref devErr);
                if (NativeMethods.FAILED(hr) || devErr != 0)
                    throw new InvalidOperationException(AuxError("I2CRead", hr, devErr));

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                Array.Copy(io.Buf, 0, result, offset, currentLen);
                offset += currentLen;
            }

            return result;
        }

        /// <summary>
        /// 使用 16-bit index，從 index 起連續讀取 length 個 byte。
        /// </summary>
        public byte[] ReadI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, int length)
        {
            EnsureNotDisposed();
            EnsureComObject();

            if (length <= 0)
                throw new ArgumentOutOfRangeException("Length", "Length must be greater than zero.");

            byte[] result = new byte[length];
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            // 先寫入 16-bit index（不結束 transaction）
            var io = new AuxIo
            {
                Display = display.MonitorUid,
                Op = AuxOpI2cWriteNotLast,
                Len = 2,
                Address = address,
                Buf = new byte[AuxBufferSize]
            };

            byte[] indexBytes = BitConverter.GetBytes(index);
            Array.Reverse(indexBytes); // big-endian
            Array.Copy(indexBytes, 0, io.Buf, 0, indexBytes.Length);

            int devErr = 0;
            int hr = CallAuxWrite(ref io, ref devErr);
            if (NativeMethods.FAILED(hr) || devErr != 0)
                throw new InvalidOperationException(AuxError("I2CWrite", hr, devErr));

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);

            // 再分段讀取
            while (offset < length)
            {
                int remaining = length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);
                bool isLastChunk = remaining <= ChunkSize;

                io = new AuxIo
                {
                    Display = display.MonitorUid,
                    Op = isLastChunk ? AuxOpI2cReadLast : AuxOpI2cReadNotLast,
                    Len = currentLen,
                    Address = address,
                    Buf = new byte[AuxBufferSize]
                };

                devErr = 0;
                hr = CallAuxRead(ref io, ref devErr);
                if (NativeMethods.FAILED(hr) || devErr != 0)
                    throw new InvalidOperationException(AuxError("I2CRead", hr, devErr));

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                Array.Copy(io.Buf, 0, result, offset, currentLen);
                offset += currentLen;
            }

            return result;
        }

        #endregion

        #region CLSID / 錯誤處理 helper

        private void HandleClsIdError(int hr, bool needUninit)
        {
            try
            {
                if ((uint)hr == REGDB_E_CLASSNOTREG)
                    throw new InvalidOperationException("Intel IGFX: COM interface not available (class not registered).");

                throw new InvalidOperationException(
                    string.Format("CLSIDFromProgID failed: 0x{0:X8}", (uint)hr));
            }
            finally
            {
                if (needUninit)
                    NativeMethods.CoUninitialize();
            }
        }

        #endregion

        #region 低階 DPCD 實作（透過 GetDeviceData / SetDeviceData）

        private byte[] Inner_ReadDpcd(I2CAdapterInfo display, uint offset, uint length)
        {
            EnsureComObject();

            if (length == 0 || length > MaxAuxChunk)
                throw new ArgumentException("Intel IGFX: invalid DPCD read length " + length, "Length");

            var io = new AuxIo
            {
                Display = display.MonitorUid,
                Op = AuxOpDpcdRead,
                Len = (int)length,
                Address = (int)offset,
                Buf = new byte[AuxBufferSize]
            };

            int devErr = 0;
            int hr = CallAuxRead(ref io, ref devErr);

            if (NativeMethods.FAILED(hr) || devErr != 0)
                throw new InvalidOperationException(AuxError("ReadDPCD", hr, devErr));

            if (io.Op != AuxOpDpcdRead)
                throw new InvalidOperationException("Intel IGFX: unexpected AUX status byte " + io.Op);

            var buf = new byte[length];
            Array.Copy(io.Buf, 0, buf, 0, (int)length);
            return buf;
        }

        private void Inner_WriteDpcd(I2CAdapterInfo display, uint offset, byte[] data)
        {
            EnsureComObject();

            int payloadSize = data == null ? 0 : data.Length;
            if (payloadSize == 0 || payloadSize > MaxAuxChunk)
            {
                throw new ArgumentException(
                    "Intel IGFX: invalid DPCD payload size " + payloadSize, "Data");
            }

            var io = new AuxIo
            {
                Display = display.MonitorUid,
                Op = AuxOpDpcdWrite,
                Len = payloadSize,
                Address = (int)offset,
                Buf = new byte[AuxBufferSize]
            };

            Array.Copy(data, 0, io.Buf, 0, payloadSize);

            int devErr = 0;
            int hr = CallAuxWrite(ref io, ref devErr);

            if (NativeMethods.FAILED(hr) || devErr != 0)
                throw new InvalidOperationException(AuxError("WriteDPCD", hr, devErr));
        }

        #endregion

        #region AUX 呼叫 helper（vtable -> delegate）

        private int CallAuxRead(ref AuxIo io, ref int devErr)
        {
            EnsureComObject();

            byte[] data = StructToBytes(io);
            uint extra;
            Guid guid = AuxGuid;

            int hr = _getDeviceData(_cuiPtr, ref guid, (uint)data.Length, data, out extra);
            devErr = (int)extra;
            io = BytesToStruct<AuxIo>(data);
            return hr;
        }

        private int CallAuxWrite(ref AuxIo io, ref int devErr)
        {
            EnsureComObject();

            byte[] data = StructToBytes(io);
            uint extra;
            Guid guid = AuxGuid;

            int hr = _setDeviceData(_cuiPtr, ref guid, (uint)data.Length, data, out extra);
            devErr = (int)extra;
            io = BytesToStruct<AuxIo>(data);
            return hr;
        }

        private static byte[] StructToBytes<T>(T value) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(value, ptr, false);
                var data = new byte[size];
                Marshal.Copy(ptr, data, 0, size);
                return data;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static T BytesToStruct<T>(byte[] data) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            if (data == null || data.Length < size)
                throw new ArgumentException("Buffer size too small for struct " + typeof(T).Name, "Data");

            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.Copy(data, 0, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        #endregion

        #region vtable 解析與 delegate 宣告

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumAttachableDevicesDelegate(
            IntPtr @this,
            [MarshalAs(UnmanagedType.BStr)] string strDeviceName,
            uint nIndex,
            out uint puidMonitor,
            ref uint pdwDeviceType,
            out uint pdwStatus);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDeviceDataDelegate(
            IntPtr @this,
            ref Guid guid,
            uint dwSize,
            [In, Out] byte[] pData,
            out uint pExtraErrorCode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetDeviceDataDelegate(
            IntPtr @this,
            ref Guid guid,
            uint dwSize,
            [In, Out] byte[] pData,
            out uint pExtraErrorCode);

        private void InitVTableDelegates(IntPtr comPtr)
        {
            if (comPtr == IntPtr.Zero)
                throw new ArgumentNullException("COMPtr");

            // comPtr 是 ICUIExternal8*，指向 vtable 指標
            IntPtr vtbl = Marshal.ReadIntPtr(comPtr);

            _enumAttachableDevices = GetVTableDelegate<EnumAttachableDevicesDelegate>(vtbl, VtblIndex_EnumAttachableDevices);
            _getDeviceData = GetVTableDelegate<GetDeviceDataDelegate>(vtbl, VtblIndex_GetDeviceData);
            _setDeviceData = GetVTableDelegate<SetDeviceDataDelegate>(vtbl, VtblIndex_SetDeviceData);
        }

        private static T GetVTableDelegate<T>(IntPtr vtbl, int index) where T : class
        {
            IntPtr fnPtr = Marshal.ReadIntPtr(vtbl, index * IntPtr.Size);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(T));
        }

        #endregion

        #region AUX 錯誤訊息

        private static string AuxError(string op, int hr, int code)
        {
            string msg = string.Empty;

            switch (code)
            {
                case 67: msg = "Invalid AUX device"; break;
                case 68: msg = "Invalid AUX address"; break;
                case 69: msg = "Invalid AUX data size"; break;
                case 70: msg = "AUX defer"; break;
                case 71: msg = "AUX timeout"; break;
                case 0: msg = string.Empty; break;
                default:
                    msg = "AUX unknown error (" + code + ")";
                    break;
            }

            if (NativeMethods.FAILED(hr))
            {
                if (string.IsNullOrEmpty(msg))
                    msg = "AUX call failed";

                msg = msg + "; hr=0x" + ((uint)hr).ToString("X8");
            }

            if (!string.IsNullOrEmpty(op))
                msg = op + ": " + msg;

            if (string.IsNullOrEmpty(msg))
                msg = "Intel IGFX: unexpected AUX error.";

            return msg;
        }

        #endregion

        #region AUX 結構

        /// <summary>
        /// 單一 AUX 交易的封裝結構。
        /// Buf 固定 0x40 bytes，透過 ByValArray 對應 unmanaged 結構。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct AuxIo
        {
            public uint Display;
            public int Op;
            public int Len;
            public int Address;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = AuxBufferSize)]
            public byte[] Buf;
        }

        #endregion
    }

    #region NativeMethods (Win32 & COM)

    internal static class NativeMethods
    {
        [DllImport("ole32.dll")]
        public static extern int CoInitialize(IntPtr pvReserved);

        [DllImport("ole32.dll")]
        public static extern void CoUninitialize();

        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        public static extern int CLSIDFromProgID(
            string lpszProgID,
            out Guid pclsid);

        [DllImport("ole32.dll")]
        public static extern int CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            uint dwClsContext,
            ref Guid riid,
            out IntPtr ppv);

        public static bool FAILED(int hr)
        {
            return hr < 0;
        }
    }

    #endregion
}
