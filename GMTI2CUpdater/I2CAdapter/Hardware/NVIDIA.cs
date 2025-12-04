// Target: .NET Framework 4.8
// Language: C#
//
// 這個類別是把你貼的 Golang NVAPI 範例，改寫成 C# 版本，
// 結構與用法盡量貼近 IntelIGFX.IntelCui：
//
// using (var nv = new NvidiaNVAPI.NvidiaNvapi())
// {
//     var displays = nv.GetAvailableDisplays();
//     if (displays.Length == 0)
//         return;
//
//     var d = displays[0];
//     var dpcd = nv.ReadDpcd(d, 0x0000, 16);
//     nv.WriteDpcd(d, 0x0010, new byte[] { 0x01, 0x02 });
// }
//
// 注意：
// - 僅實作 DPCD（AUX）存取，I2C 尚未實作。
// - 如果系統未安裝 NVIDIA 驅動或沒有實體 GPU，建構子會丟 InvalidOperationException。
// - 需 x64 + nvapi64.dll 存在。

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    /// <summary>
    /// NVIDIA NVAPI DPCD / AUX 封裝。
    /// 參考原本 Golang nvapiDriver + nvapiProcs 的邏輯，
    /// 但介面風格比照 IntelIGFX.IntelCui。
    /// </summary>
    public sealed class NvidiaApi : IDisposable
    {
        #region 內部欄位與常數

        private bool _disposed;

        // NVAPI function delegates
        private NvAPI_InitializeDelegate _nvInitialize = null!;
        private NvAPI_EnumPhysicalGPUsDelegate _nvEnumPhysicalGPUs = null!;
        private NvAPI_EnumNvidiaDisplayHandleDelegate _nvEnumNvidiaDisplayHandle = null!;
        private NvAPI_GetAssociatedDisplayOutputIdDelegate _nvGetAssociatedDisplayOutputId = null!;
        private NvAPI_GetAssociatedNvidiaDisplayHandleDelegate _nvGetAssociatedNvidiaDisplayHandle = null!;
        private NvAPI_GetDisplayPortInfoDelegate _nvGetDisplayPortInfo = null!;
        private NvAPI_GetErrorMessageDelegate _nvGetErrorMessage = null!;
        private NvAPI_Disp_DpAuxChannelControlDelegate _nvDisp_DpAuxChannelControl = null!;

        // 狀態碼（與 Golang 範例一致）
        private const int NvapiStatusOk = 0x00000000;
        private const int NvapiStatusEndEnumeration = unchecked((int)0xFFFFFFF9);
        private const int NvapiDpAuxTimeout = 0x000000FF;

        // QueryInterface ID（與 Golang 範例一致）
        private const uint Qi_Initialize = 0x0150E828;
        private const uint Qi_EnumPhysicalGPUs = 0xE5AC921F;
        private const uint Qi_EnumNvidiaDisplayHandle = 0x9ABDD40D;
        private const uint Qi_GetAssociatedDisplayOutputID = 0xD995937E;
        private const uint Qi_GetAssociatedNvidiaDisplayHandle = 0x9E4B6097;
        private const uint Qi_GetDisplayPortInfo = 0xC64FF367;
        private const uint Qi_GetErrorMessage = 0x6C2D048C;
        private const uint Qi_Disp_DpAuxChannelControl = 0x8EB56969;
        private const uint Qi_NvUnload = 0xD22BDD7E;

        // nvDPInfoV1 size / version（與 Golang 一致）
        private const int NvDPInfoV1Size = 44;
        private const uint NvDPInfoV1Version = 0x10000u | NvDPInfoV1Size;

        // nvDpAuxParamsV1 version
        private const uint NvDpAuxParamsV1Version = 0x00010028;

        // AUX 操作碼與限制（與 Golang 一致）
        private const uint DpAuxOpWriteDpcd = 0;
        private const uint DpAuxOpReadDpcd = 1;
        private const uint DpAuxOpWriteI2CLast = 2;
        private const uint DpAuxOpReadI2CLast = 3;
        private const uint DpAuxOpWriteI2CNotLast = 5;
        private const uint DpAuxOpReadI2CNotLast = 6;
        private const int DpAuxMaxPayload = 16;

        private const int MaxI2cReadChunk = 0x10;  // 每次 I2C 讀取最大長度
        private const int MaxI2cWriteChunk = 0x04; // 每次 I2C 寫入最大資料 byte 數

        #endregion


        #region 建構 / 釋放

        /// <summary>
        /// 建構 NvidiaNvapi：載入 nvapi64.dll，透過 QueryInterface 綁定所有需要的函式，並呼叫 NvAPI_Initialize。
        /// </summary>
        public NvidiaApi()
        {
            InitializeNvapi();
            EnsurePhysicalGpuExists();
        }

        ~NvidiaApi()
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

            // NVAPI 官方有 NvAPI_Unload，但原本 Golang 範例也沒呼叫，
            // 一般來說讓 process 結束時 OS 自行清理即可。
            // 如果要更完整，可以再加上 QueryInterface 取得 NvAPI_Unload。
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("NvidiaNvapi");
        }

        #endregion

        #region 公開屬性

        /// <summary>
        /// 實作描述字串，僅供顯示。
        /// </summary>
        public string Name = "NVIDIA NVAPI AUX/DPCD Bridge";

        #endregion

        #region 公開 API：顯示器列舉

        /// <summary>
        /// 使用 NvAPI_EnumNvidiaDisplayHandle / GetAssociatedDisplayOutputId / GetDisplayPortInfo
        /// 列出目前啟用的 DisplayPort 輸出（Flags & 1 != 0）。
        /// </summary>
        public I2CAdapterInfo[] GetAvailableDisplays()
        {
            EnsureNotDisposed();

            if (_nvEnumNvidiaDisplayHandle == null ||
                _nvGetAssociatedDisplayOutputId == null ||
                _nvGetDisplayPortInfo == null)
            {
                throw new InvalidOperationException("NVIDIA NVAPI: required functions are not initialized.");
            }

            var list = new List<I2CAdapterInfo>();
            uint index = 0;

            while (true)
            {
                IntPtr handle;
                int status = _nvEnumNvidiaDisplayHandle(index, out handle);

                if (status == NvapiStatusEndEnumeration)
                    break;

                if (status != NvapiStatusOk)
                    throw StatusError(status, "NvAPI_EnumNvidiaDisplayHandle");

                if (handle == IntPtr.Zero)
                {
                    index++;
                    continue;
                }

                uint outputId = 0;
                status = _nvGetAssociatedDisplayOutputId(handle, ref outputId);
                if (status != NvapiStatusOk)
                {
                    // 這裡與 Golang 範例一樣：如果失敗就 skip
                    index++;
                    continue;
                }

                var info = CreateDpInfo();
                status = _nvGetDisplayPortInfo(handle, outputId, ref info);
                if (status != NvapiStatusOk)
                {
                    index++;
                    continue;
                }

                // Flags 的最低位表示該 DP output 是否啟用
                if ((info.Flags & 1) == 0)
                {
                    index++;
                    continue;
                }

                var di = new I2CAdapterInfo
                {
                    DisplayHandle = handle,
                    MonitorUid = outputId,
                    DeviceIndex = 0,
                    OutputIndex = (int)index,
                    Name = $"Nvidia,裝置{0}營幕{index}",
                    Description = "NVIDIA Display " + index,
                    IsNeedPrivilege = true,
                    IsFromDisplay = true,
                };

                list.Add(di);
                index++;
            }

            return list.ToArray();
        }

        #endregion

        #region 公開 API：DPCD 讀寫

        /// <summary>
        /// 以 DPCD 方式，從指定 NVIDIA 顯示輸出讀取連續位址範圍。
        /// 一次最大 16 bytes，超過會自動切 chunk。
        /// </summary>
        public byte[] ReadDpcd(I2CAdapterInfo display, uint addr, uint length)
        {
            EnsureNotDisposed();

            if (length == 0)
                throw new ArgumentException("DPCD read length must be greater than zero.", "length");

            var result = new byte[length];
            uint remaining = length;
            uint offset = addr;
            int writePos = 0;

            while (remaining > 0)
            {
                uint chunk = remaining;
                if (chunk > DpAuxMaxPayload)
                    chunk = DpAuxMaxPayload;

                byte[] data = Inner_ReadDpcd(display, offset, chunk);
                if (data == null || data.Length != (int)chunk)
                    throw new InvalidOperationException("NVIDIA NVAPI: DPCD read chunk size mismatch.");

                Buffer.BlockCopy(data, 0, result, writePos, (int)chunk);
                writePos += (int)chunk;
                offset += chunk;
                remaining -= chunk;
            }

            return result;
        }

        /// <summary>
        /// 以 DPCD 方式，對指定 NVIDIA 顯示輸出寫入連續位址範圍。
        /// 一次最大 16 bytes，會自動切 chunk。
        /// </summary>
        public void WriteDpcd(I2CAdapterInfo display, uint addr, byte[] data)
        {
            EnsureNotDisposed();

            if (data == null || data.Length == 0)
                return;

            uint offset = addr;
            int remaining = data.Length;
            int srcPos = 0;

            while (remaining > 0)
            {
                int chunk = remaining;
                if (chunk > DpAuxMaxPayload)
                    chunk = DpAuxMaxPayload;

                var payload = new byte[chunk];
                Buffer.BlockCopy(data, srcPos, payload, 0, chunk);

                Inner_WriteDpcd(display, offset, payload);

                offset += (uint)chunk;
                srcPos += chunk;
                remaining -= chunk;
            }
        }

        #endregion

        #region I2C over AUX (NVAPI)

        // 無 index，寫入單一 byte 到指定 I2C slave
        public void WriteI2CWithoutIndex(I2CAdapterInfo display, byte address, byte data)
        {
            byte address7bit = (byte)(address >> 1);
            EnsureNotDisposed();
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            var p = CreateAuxParams();
            p.OutputId = display.MonitorUid;
            p.Op = address7bit;
            p.Address = address;
            p.LenMinus1 = 0; // 1 byte

            p.Buf[0] = data;

            uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
            int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

            HandleAuxStatus(status, ref p, "NvAPI_Disp_DpAuxChannelControl(I2C Write)");


        }

        // 1-byte index，從 index 起連續寫入 data
        public void WriteI2CByteIndex(I2CAdapterInfo display, byte address, byte index, byte[] data)
        {
            byte address7bit = (byte)(address >> 1);
            EnsureNotDisposed();
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            if (data == null || data.Length == 0)
                return;

            // Buf 最多 16 bytes，其中 1 byte 是 index → 一次最多寫 15 bytes 資料
            const int ChunkSize = MaxI2cWriteChunk;

            int offset = 0;
            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);
                int totalLen = 1 + currentLen; // index + data
                byte idx = (byte)(index + offset);

                var p = CreateAuxParams();
                p.OutputId = display.MonitorUid;
                p.Op = DpAuxOpWriteI2CLast;
                p.Address = address7bit;
                p.LenMinus1 = (uint)(totalLen - 1);

                p.Buf[0] = idx;
                Array.Copy(data, offset, p.Buf, 1, currentLen);

                uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
                int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

                HandleAuxStatus(status, ref p, "NvAPI_Disp_DpAuxChannelControl(I2C Write ByteIndex)");



                offset += currentLen;
            }
        }

        // 16-bit index，從 index 起連續寫入 data（index 為大端）
        public void WriteI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, byte[] data)
        {
            byte address7bit = (byte)(address >> 1);
            EnsureNotDisposed();
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            if (data == null || data.Length == 0)
                return;

            // Buf 16 bytes，其中 2 bytes 是 index → 一次最多寫 14 bytes 資料
            const int ChunkSize = MaxI2cWriteChunk;

            // big-endian index
            byte hi = (byte)(index >> 8);
            byte lo = (byte)(index & 0xFF);

            int offset = 0;
            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);
                int totalLen = 2 + currentLen; // 2 bytes index + data

                var p = CreateAuxParams();
                p.OutputId = display.MonitorUid;
                p.Op = DpAuxOpWriteI2CLast;
                p.Address = address7bit;
                p.LenMinus1 = (uint)(totalLen - 1);

                p.Buf[0] = hi;
                p.Buf[1] = lo;
                Array.Copy(data, offset, p.Buf, 2, currentLen);

                uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
                int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

                HandleAuxStatus(status, ref p, "NvAPI_Disp_DpAuxChannelControl(I2C Write UInt16Index)");



                offset += currentLen;
            }
        }

        // 無 index，讀取單一 byte
        public byte ReadI2CWithoutIndex(I2CAdapterInfo display, byte address)
        {
            byte address7bit = (byte)(address >> 1);
            EnsureNotDisposed();
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            var p = CreateAuxParams();
            p.OutputId = display.MonitorUid;
            p.Op = DpAuxOpReadI2CLast;
            p.Address = address7bit;
            p.LenMinus1 = 0; // 1 byte

            uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
            int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

            HandleAuxStatus(status, ref p, "NvAPI_Disp_DpAuxChannelControl(I2C Read)");



            return p.Buf[0];
        }

        // 1-byte index，從 index 起連續讀取 length 個 byte
        public byte[] ReadI2CByteIndex(I2CAdapterInfo display, byte address, byte index, int length)
        {
            byte address7bit = (byte)(address >> 1);
            EnsureNotDisposed();
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            var result = new byte[length];

            // 先送出 index（不結束 transaction）
            {
                var pIndex = CreateAuxParams();
                pIndex.OutputId = display.MonitorUid;
                pIndex.Op = DpAuxOpWriteI2CNotLast;
                pIndex.Address = address7bit;
                pIndex.LenMinus1 = 0; // 1 byte
                pIndex.Buf[0] = index;

                uint sizeIdx = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
                int statusIdx = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref pIndex, sizeIdx);

                HandleAuxStatus(statusIdx, ref pIndex, "NvAPI_Disp_DpAuxChannelControl(I2C WriteIndex)");


            }

            // 再分段讀取資料
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            while (offset < length)
            {
                int remaining = length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);
                bool isLast = offset + currentLen >= length;

                var p = CreateAuxParams();
                p.OutputId = display.MonitorUid;
                p.Op = isLast ? DpAuxOpReadI2CLast : DpAuxOpReadI2CNotLast;
                p.Address = address7bit;
                p.LenMinus1 = (uint)(currentLen - 1);

                uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
                int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

                HandleAuxStatus(status, ref p, "NvAPI_Disp_DpAuxChannelControl(I2C Read ByteIndex)");



                Array.Copy(p.Buf, 0, result, offset, currentLen);
                offset += currentLen;
            }

            return result;
        }

        // 16-bit index，從 index 起連續讀取 length 個 byte（index 大端）
        public byte[] ReadI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, int length)
        {
            byte address7bit = (byte)(address >> 1);
            EnsureNotDisposed();
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            var result = new byte[length];

            // big-endian index
            byte hi = (byte)(index >> 8);
            byte lo = (byte)(index & 0xFF);

            // 先送出 16-bit index（不結束 transaction）
            {
                var pIndex = CreateAuxParams();
                pIndex.OutputId = display.MonitorUid;
                pIndex.Op = DpAuxOpWriteI2CNotLast;
                pIndex.Address = address7bit;
                pIndex.LenMinus1 = 1; // 2 bytes
                pIndex.Buf[0] = hi;
                pIndex.Buf[1] = lo;

                uint sizeIdx = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
                int statusIdx = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref pIndex, sizeIdx);

                HandleAuxStatus(statusIdx, ref pIndex, "NvAPI_Disp_DpAuxChannelControl(I2C WriteUInt16Index)");


            }

            // 再分段讀取資料
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            while (offset < length)
            {
                int remaining = length - offset;
                int currentLen = Math.Min(ChunkSize, remaining);
                bool isLast = offset + currentLen >= length;

                var p = CreateAuxParams();
                p.OutputId = display.MonitorUid;
                p.Op = isLast ? DpAuxOpReadI2CLast : DpAuxOpReadI2CNotLast;
                p.Address = address7bit;
                p.LenMinus1 = (uint)(currentLen - 1);

                uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
                int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

                HandleAuxStatus(status, ref p, "NvAPI_Disp_DpAuxChannelControl(I2C Read UInt16Index)");



                Array.Copy(p.Buf, 0, result, offset, currentLen);
                offset += currentLen;
            }

            return result;
        }

        #endregion

        #region 內部：NVAPI 初始化 & 檢查

        private void InitializeNvapi()
        {
            try
            {
                _nvInitialize = GetProc<NvAPI_InitializeDelegate>(Qi_Initialize);
                _nvEnumPhysicalGPUs = GetProc<NvAPI_EnumPhysicalGPUsDelegate>(Qi_EnumPhysicalGPUs);
                _nvEnumNvidiaDisplayHandle = GetProc<NvAPI_EnumNvidiaDisplayHandleDelegate>(Qi_EnumNvidiaDisplayHandle);
                _nvGetAssociatedDisplayOutputId = GetProc<NvAPI_GetAssociatedDisplayOutputIdDelegate>(Qi_GetAssociatedDisplayOutputID);
                _nvGetAssociatedNvidiaDisplayHandle = GetProc<NvAPI_GetAssociatedNvidiaDisplayHandleDelegate>(Qi_GetAssociatedNvidiaDisplayHandle);
                _nvGetDisplayPortInfo = GetProc<NvAPI_GetDisplayPortInfoDelegate>(Qi_GetDisplayPortInfo);
                _nvGetErrorMessage = GetProc<NvAPI_GetErrorMessageDelegate>(Qi_GetErrorMessage);
                _nvDisp_DpAuxChannelControl = GetProc<NvAPI_Disp_DpAuxChannelControlDelegate>(Qi_Disp_DpAuxChannelControl);
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException("NVIDIA NVAPI: nvapi64.dll not found.", ex);
            }

            if (_nvInitialize == null ||
                _nvEnumPhysicalGPUs == null ||
                _nvEnumNvidiaDisplayHandle == null ||
                _nvGetAssociatedDisplayOutputId == null ||
                _nvGetDisplayPortInfo == null ||
                _nvDisp_DpAuxChannelControl == null)
            {
                throw new InvalidOperationException("NVIDIA NVAPI: required entry points are missing.");
            }

            int status = _nvInitialize();
            if (status != NvapiStatusOk)
                throw StatusError(status, "NvAPI_Initialize");
        }

        private void EnsurePhysicalGpuExists()
        {
            if (_nvEnumPhysicalGPUs == null)
                throw new InvalidOperationException("NVIDIA NVAPI: EnumPhysicalGPUs not available.");

            IntPtr[] handles = new IntPtr[64];
            int count = 0;
            int status = _nvEnumPhysicalGPUs(handles, ref count);

            if (status != NvapiStatusOk)
                throw StatusError(status, "NvAPI_EnumPhysicalGPUs");

            if (count <= 0)
                throw new InvalidOperationException("NVIDIA NVAPI: no physical GPU detected.");
        }

        #endregion

        #region 內部：DPCD 實作（透過 NvAPI_Disp_DpAuxChannelControl）
        private void HandleAuxStatus(int status, ref NvDpAuxParamsV1 p, string context)
        {
            if (status != NvapiStatusOk)
            {
                if (p.Status == NvapiDpAuxTimeout)
                    throw new InvalidOperationException("NVIDIA NVAPI: DP AUX transaction timed out.");

                throw StatusError(status, context);
            }

            if (p.Status == NvapiDpAuxTimeout)
                throw new InvalidOperationException("NVIDIA NVAPI: DP AUX transaction timed out.");

            if (p.Status != 0)
                throw new InvalidOperationException(
                    string.Format("NVIDIA NVAPI: DP AUX error status 0x{0:X}", p.Status));
        }

        private byte[] Inner_ReadDpcd(I2CAdapterInfo display, uint addr, uint length)
        {
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            if (length == 0 || length > DpAuxMaxPayload)
                throw new ArgumentException("NVIDIA NVAPI: invalid DPCD read length " + length, "length");

            var p = CreateAuxParams();
            p.OutputId = display.MonitorUid;
            p.Op = DpAuxOpReadDpcd;
            p.Address = addr;
            p.LenMinus1 = length - 1;

            uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
            int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

            if (status != NvapiStatusOk)
            {
                if (p.Status == NvapiDpAuxTimeout)
                    throw new InvalidOperationException("NVIDIA NVAPI: DP AUX transaction timed out.");

                throw StatusError(status, "NvAPI_Disp_DpAuxChannelControl");
            }

            if (p.Status == NvapiDpAuxTimeout)
                throw new InvalidOperationException("NVIDIA NVAPI: DP AUX transaction timed out.");

            if (p.Status != 0)
                throw new InvalidOperationException(
                    string.Format("NVIDIA NVAPI: DP AUX error status 0x{0:X}", p.Status));

            int actual = (int)(p.LenMinus1 + 1);
            if (actual < 0)
                actual = 0;
            if (actual > length)
                actual = (int)length;

            var buf = new byte[actual];
            Buffer.BlockCopy(p.Buf, 0, buf, 0, actual);
            return buf;
        }

        private void Inner_WriteDpcd(I2CAdapterInfo display, uint addr, byte[] data)
        {
            if (_nvDisp_DpAuxChannelControl == null)
                throw new InvalidOperationException("NVIDIA NVAPI: AUX function not initialized.");

            int payloadSize = data == null ? 0 : data.Length;
            if (payloadSize <= 0 || payloadSize > DpAuxMaxPayload)
            {
                throw new ArgumentException("NVIDIA NVAPI: invalid DPCD payload size " + payloadSize, "data");
            }

            var p = CreateAuxParams();
            p.OutputId = display.MonitorUid;
            p.Op = DpAuxOpWriteDpcd;
            p.Address = addr;
            p.LenMinus1 = (uint)(payloadSize - 1);

            Array.Copy(data, 0, p.Buf, 0, payloadSize);

            uint size = (uint)Marshal.SizeOf(typeof(NvDpAuxParamsV1));
            int status = _nvDisp_DpAuxChannelControl(display.DisplayHandle, ref p, size);

            if (status != NvapiStatusOk)
            {
                if (p.Status == NvapiDpAuxTimeout)
                    throw new InvalidOperationException("NVIDIA NVAPI: DP AUX transaction timed out.");

                throw StatusError(status, "NvAPI_Disp_DpAuxChannelControl");
            }

            if (p.Status == NvapiDpAuxTimeout)
                throw new InvalidOperationException("NVIDIA NVAPI: DP AUX transaction timed out.");

            if (p.Status != 0)
                throw new InvalidOperationException(
                    string.Format("NVIDIA NVAPI: DP AUX error status 0x{0:X}", p.Status));
        }

        #endregion

        #region 內部：錯誤處理

        private Exception StatusError(int status, string context)
        {
            string message = string.Format("status 0x{0:X8}", (uint)status);

            if (_nvGetErrorMessage != null)
            {
                var buf = new byte[256];
                _nvGetErrorMessage(status, buf);

                int len = Array.IndexOf(buf, (byte)0);
                if (len < 0) len = buf.Length;

                string s = Encoding.ASCII.GetString(buf, 0, len).Trim();
                if (!string.IsNullOrEmpty(s))
                    message = s;
            }

            if (!string.IsNullOrEmpty(context))
            {
                return new InvalidOperationException(
                    string.Format("{0}: {1} (0x{2:X8})", context, message, (uint)status));
            }

            return new InvalidOperationException(
                string.Format("NVIDIA NVAPI error: {0} (0x{1:X8})", message, (uint)status));
        }

        #endregion

        #region 內部：QueryInterface & 結構 helper

        private static T? GetProc<T>(uint id) where T : class
        {
            IntPtr ptr = NvapiNative.NvAPI_QueryInterface(id);
            if (ptr == IntPtr.Zero)
                return null;

            return (T)(object)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }

        private static NvDPInfoV1 CreateDpInfo()
        {
            return new NvDPInfoV1
            {
                Version = NvDPInfoV1Version,
                Reserved0 = new byte[36],
                Pad = new byte[3]
            };
        }

        private static NvDpAuxParamsV1 CreateAuxParams()
        {
            return new NvDpAuxParamsV1
            {
                Version = NvDpAuxParamsV1Version,
                OutputId = 0,
                Op = 0,
                Address = 0,
                Buf = new byte[DpAuxMaxPayload],
                LenMinus1 = 0,
                Status = 0,
                DataLo = 0,
                DataHi = 0,
                Reserved1 = new byte[48]
            };
        }

        #endregion

        #region P/Invoke & 結構宣告

        private static class NvapiNative
        {
            [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NvAPI_QueryInterface(uint id);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_InitializeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumPhysicalGPUsDelegate(
            [Out] IntPtr[] handles,
            ref int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumNvidiaDisplayHandleDelegate(
            uint thisEnum,
            out IntPtr displayHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetAssociatedDisplayOutputIdDelegate(
            IntPtr displayHandle,
            ref uint outputId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetAssociatedNvidiaDisplayHandleDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string displayName,
            out IntPtr displayHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetDisplayPortInfoDelegate(
            IntPtr displayHandle,
            uint outputId,
            ref NvDPInfoV1 info);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetErrorMessageDelegate(
            int status,
            [Out] byte[] buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_Disp_DpAuxChannelControlDelegate(
            IntPtr displayHandle,
            ref NvDpAuxParamsV1 parameters,
            uint size);

        [StructLayout(LayoutKind.Sequential)]
        private struct NvDPInfoV1
        {
            public uint Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] Reserved0;

            public byte Flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NvDpAuxParamsV1
        {
            public uint Version;
            public uint OutputId;
            public uint Op;
            public uint Address;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = DpAuxMaxPayload)]
            public byte[] Buf;

            public uint LenMinus1;
            public int Status;
            public ulong DataLo;
            public ulong DataHi;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] Reserved1;
        }

        #endregion
    }
}
