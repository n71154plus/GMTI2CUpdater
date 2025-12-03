// Target: .NET Framework 4.8
// Language: C#
using HidSharp.Reports.Units;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Documents;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    /// <summary>
    /// Intel Graphics Control Library (ControlLib.dll) 封裝。
    /// 透過 IGCL 提供 AUX(DPCD) / I2C 讀寫。
    ///
    /// - 不實作任何共用 interface（先專心做 IGCL 版本）。
    /// - DisplayInfo 會包含 GPU LUID 與輸出索引，可以辨識不同 GPU。
    /// </summary>
    public sealed class IntelIGCLApi : IDisposable
    {
        #region 常數與欄位

        private const uint CtlResultSuccess = 0;

        private const uint CtlOperationTypeRead = 1;
        private const uint CtlOperationTypeWrite = 2;

        private const uint CtlAuxFlagNativeAux = 1 << 0;
        private const uint CtlAuxFlagI2CAux = 1 << 1;
        private const uint CtlAuxFlagI2CAuxMot = 1 << 2;

        private const uint CtlI2CFlag1ByteIndex = 1 << 0;

        private const uint CtlI2CFlag2ByteIndex = 2 << 0;

        private const int CtlAuxMaxDataSize = 0x0084; // 132 bytes

        private const int MaxI2cReadChunk = 0x10;  // 每次 I2C 讀取最大長度
        private const int MaxI2cWriteChunk = 0x04; // 每次 I2C 寫入最大資料 byte 數

        private const uint CtlInitAppVersion = 0x00010001;

        private IntPtr _apiHandle = IntPtr.Zero;
        private bool _disposed;
        private ushort _i2cDelayMs = 20; // 預設 I2C 操作之後等 20ms

        #endregion

        #region 公開屬性 / 建構 / 釋放

        public string Name =
            "Intel Graphics Control Library (IGCL)";

        public IntelIGCLApi()
        {
            var args = new CtlInitArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlInitArgs)),
                Version = 0,
                Reserved = new byte[3],
                AppVersion = CtlInitAppVersion,
                Flags = CtlInitFlags.None,
                SupportedVersion = 0,
                ApplicationUid = new byte[16]
            };

            uint r;
            try
            {
                r = NativeMethods.ctlInit(ref args, out _apiHandle);
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "Intel IGCL: ControlLib.dll not found (interface not available).", ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "Intel IGCL: CtlInit entry point not found (incompatible ControlLib.dll).", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Intel IGCL: unexpected error calling CtlInit.", ex);
            }

            if (r != CtlResultSuccess || _apiHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlInit failed: 0x{0:X8}", r));
            }
        }

        ~IntelIGCLApi()
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

            if (_apiHandle != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.ctlClose(_apiHandle);
                }
                catch
                {
                    // ignore
                }
                _apiHandle = IntPtr.Zero;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("IntelIgcl");
        }

        #endregion

        #region 公開 API：設定 / 列舉顯示器

        /// <summary>設定 I2C 寫入後延遲（毫秒）。</summary>
        public void SetDelay(ushort milliseconds)
        {
            _i2cDelayMs = milliseconds;
        }

        /// <summary>
        /// 列舉目前 IGCL 所能看到的所有顯示輸出。
        /// 這邊會把「第幾張 GPU」和「第幾個輸出」記在 DisplayInfo 內，
        /// 同時也帶出 AdapterLUID / TargetId / VidPnSourceId，方便你辨識不同 GPU。
        /// </summary>
        public I2CAdapterInfo[] GetAvailableDisplays()
        {
            EnsureNotDisposed();
            if (_apiHandle == IntPtr.Zero)
                throw new InvalidOperationException("Intel IGCL: API handle is null.");

            var list = new List<I2CAdapterInfo>();

            // 1. Enumerate devices (GPU)
            uint devCount = 0;
            uint r = NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDevices(count) failed: 0x{0:X8}", r));

            if (devCount == 0)
                return list.ToArray();

            var devs = new IntPtr[devCount];
            r = NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, devs);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDevices(get) failed: 0x{0:X8}", r));

            // 2. For each GPU (device)，enumerate display outputs
            for (int devIndex = 0; devIndex < devs.Length; devIndex++)
            {
                var dev = devs[devIndex];
                if (dev == IntPtr.Zero)
                    continue;

                uint outCount = 0;
                r = NativeMethods.ctlEnumerateDisplayOutputs(dev, ref outCount, null);
                if (r != CtlResultSuccess || outCount == 0)
                    continue;

                var outs = new IntPtr[outCount];
                r = NativeMethods.ctlEnumerateDisplayOutputs(dev, ref outCount, outs);
                if (r != CtlResultSuccess)
                    continue;

                for (int outIndex = 0; outIndex < outs.Length; outIndex++)
                {
                    var outHandle = outs[outIndex];
                    if (outHandle == IntPtr.Zero)
                        continue;

                    var props = new CtlDisplayProperties
                    {
                        Size = (uint)Marshal.SizeOf(typeof(CtlDisplayProperties)),
                        Version = 0,
                    };

                    r = NativeMethods.ctlGetDisplayProperties(outHandle, ref props);
                    if (r != CtlResultSuccess)
                        continue;
                    bool isATTACHED = (props.DisplayConfigFlags & 1u << 1) != 0;
                    if (!isATTACHED)
                        continue;
                    var info = new I2CAdapterInfo
                    {
                        //DisplayHandle = outHandle,
                        DeviceIndex = devIndex,
                        OutputIndex = outIndex,
                        Name = $"Intel,裝置{devIndex}營幕{outIndex}",
                        IsNeedPrivilege = true,
                        IsFromDisplay = true,
                    };
                    list.Add(info);
                }
            }

            return list.ToArray();
        }

        #endregion

        #region 公開 API：DPCD (AUX)

        public byte[] ReadDpcd(I2CAdapterInfo display, uint addr, uint length)
        {
            EnsureNotDisposed();

            if (length == 0)
                throw new ArgumentException("DPCD read length must be greater than zero.", "Length");

            var result = new byte[length];
            uint remaining = length;
            uint offset = addr;
            int writePos = 0;

            while (remaining > 0)
            {
                uint chunk = remaining;
                if (chunk > CtlAuxMaxDataSize)
                    chunk = CtlAuxMaxDataSize;

                byte[] data = Inner_ReadDpcd(display, offset, (int)chunk);
                if (data == null || data.Length != (int)chunk)
                    throw new InvalidOperationException("Intel IGCL: DPCD read chunk size mismatch.");

                Buffer.BlockCopy(data, 0, result, writePos, (int)chunk);
                writePos += (int)chunk;
                offset += chunk;
                remaining -= chunk;
            }

            return result;
        }

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
                if (chunk > CtlAuxMaxDataSize)
                    chunk = CtlAuxMaxDataSize;

                var payload = new byte[chunk];
                Buffer.BlockCopy(data, srcPos, payload, 0, chunk);

                Inner_WriteDpcd(display, offset, payload);

                offset += (uint)chunk;
                srcPos += chunk;
                remaining -= chunk;
            }
        }

        private IntPtr SelectDisplay(I2CAdapterInfo display)
        {
            // 1. Enumerate devices (GPU)
            uint devCount = 0;
            uint r = NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDevices(count) failed: 0x{0:X8}", r));

            var devs = new IntPtr[devCount];
            r = NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, devs);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDevices(get) failed: 0x{0:X8}", r));
            uint outCount = 0;
            r = NativeMethods.ctlEnumerateDisplayOutputs(devs[display.DeviceIndex], ref outCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDisplayOutputs(get) failed: 0x{0:X8}", r));
            var outs = new IntPtr[outCount];
            r = NativeMethods.ctlEnumerateDisplayOutputs(devs[display.DeviceIndex], ref outCount, outs);
            return outs[display.OutputIndex];
        }

        private byte[] Inner_ReadDpcd(I2CAdapterInfo display, uint addr, int length)
        {
            if (length <= 0 || length > CtlAuxMaxDataSize)
                throw new ArgumentOutOfRangeException("Length",
                    string.Format("Intel IGCL: invalid DPCD length {0} (1..{1}).", length, CtlAuxMaxDataSize)
                  );

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeRead,
                Flags = CtlAuxFlagNativeAux,
                Address = addr,
                Rad = 0,
                PortId = 0,
                DataSize = (uint)length,
                Data = new byte[CtlAuxMaxDataSize]
            };

            uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlAUXAccess(read) failed: 0x{0:X8}", r));

            var buf = new byte[length];
            Array.Copy(args.Data, 0, buf, 0, length);
            return buf;
        }

        private void Inner_WriteDpcd(I2CAdapterInfo display, uint addr, byte[] data)
        {
            if (data == null || data.Length == 0 || data.Length > CtlAuxMaxDataSize)
                throw new ArgumentOutOfRangeException("Data", string.Format("Intel IGCL: invalid DPCD payload {0} (1..{}).", CtlAuxMaxDataSize)
                    );

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeWrite,
                Flags = CtlAuxFlagNativeAux,
                Address = addr,
                Rad = 0,
                PortId = 0,
                DataSize = (uint)data.Length,
                Data = new byte[CtlAuxMaxDataSize]
            };

            Array.Copy(data, 0, args.Data, 0, data.Length);

            uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlAUXAccess(write) failed: 0x{0:X8}", r));
        }

        #endregion

        #region I2C over AUX (using CtlAuxAccess)

        // 無 index，寫入單一 byte 到指定 I2C slave
        public void WriteI2CWithoutIndex(I2CAdapterInfo display, byte address, byte data)
        {
            EnsureNotDisposed();

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeWrite,
                Flags = CtlAuxFlagI2CAux,       // Last
                Address = address,                // I2C 7-bit address
                Rad = 0,
                PortId = 0,
                DataSize = 1,
                Data = new byte[CtlAuxMaxDataSize]
            };

            args.Data[0] = data;

            uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlAUXAccess(I2C write) failed: 0x{r:X8}");

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);
        }

        // 1-byte index，從 index 起連續寫入 data
        public void WriteI2CByteIndex(I2CAdapterInfo display, byte address, byte index, byte[] data)
        {
            EnsureNotDisposed();

            if (data == null || data.Length == 0)
                return;

            // 可用資料空間 = CtlAuxMaxDataSize - 1 (index 佔 1 byte)
            const int PayloadMax = MaxI2cWriteChunk;

            int offset = 0;
            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int chunkLen = Math.Min(PayloadMax, remaining);
                int totalLen = 1 + chunkLen;              // index + chunk

                byte effectiveIndex = (byte)(index + offset);

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAux,            // 每一段都是獨立 write，直接當作 last
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)totalLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                args.Data[0] = effectiveIndex;
                Array.Copy(data, offset, args.Data, 1, chunkLen);

                uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write byte-index) failed: 0x{r:X8}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                offset += chunkLen;
            }
        }

        // 16-bit index（大端），從 index 起連續寫入 data
        public void WriteI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, byte[] data)
        {
            EnsureNotDisposed();

            if (data == null || data.Length == 0)
                return;

            // 可用資料空間 = CtlAuxMaxDataSize - 2 (index 佔 2 bytes)
            const int PayloadMax = MaxI2cWriteChunk;

            // big-endian index
            byte hi = (byte)(index >> 8);
            byte lo = (byte)(index & 0xFF);

            int offset = 0;
            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int chunkLen = Math.Min(PayloadMax, remaining);
                int totalLen = 2 + chunkLen;              // 2 bytes index + chunk

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAux,            // 每一段都是獨立 write
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)totalLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                args.Data[0] = hi;
                args.Data[1] = lo;
                Array.Copy(data, offset, args.Data, 2, chunkLen);

                uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write UInt16-index) failed: 0x{r:X8}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                offset += chunkLen;
            }
        }

        // 無 index，讀取單一 byte
        public byte ReadI2CWithoutIndex(I2CAdapterInfo display, byte address)
        {
            EnsureNotDisposed();

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeRead,
                Flags = CtlAuxFlagI2CAux,        // Last
                Address = address,
                Rad = 0,
                PortId = 0,
                DataSize = 1,
                Data = new byte[CtlAuxMaxDataSize]
            };

            uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlAUXAccess(I2C read) failed: 0x{r:X8}");

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);

            return args.Data[0];
        }

        // 1-byte index，從 index 起連續讀取 length 個 byte
        public byte[] ReadI2CByteIndex(I2CAdapterInfo display, byte address, byte index, int length)
        {
            EnsureNotDisposed();

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            var result = new byte[length];

            // 1) 先送出 index，使用 MOT（非最後一包）
            {
                var argsIndex = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAuxMot, // NOT last → MOT
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = 1,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                argsIndex.Data[0] = index;

                uint rIndex = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref argsIndex);
                if (rIndex != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write index) failed: 0x{rIndex:X8}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);
            }

            // 2) 再分段讀資料
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            while (offset < length)
            {
                int remaining = length - offset;
                int chunkLen = Math.Min(ChunkSize, remaining);
                bool isLast = offset + chunkLen >= length;

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeRead,
                    Flags = isLast ? CtlAuxFlagI2CAux : CtlAuxFlagI2CAuxMot,
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)chunkLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C read byte-index) failed: 0x{r:X8}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                Array.Copy(args.Data, 0, result, offset, chunkLen);
                offset += chunkLen;
            }

            return result;
        }

        // 16-bit index（大端），從 index 起連續讀取 length 個 byte
        public byte[] ReadI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, int length)
        {
            EnsureNotDisposed();

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            var result = new byte[length];

            // big-endian index
            byte hi = (byte)(index >> 8);
            byte lo = (byte)(index & 0xFF);

            // 1) 先送出 16-bit index，用 MOT
            {
                var argsIndex = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAuxMot,
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = 2,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                argsIndex.Data[0] = hi;
                argsIndex.Data[1] = lo;

                uint rIndex = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref argsIndex);
                if (rIndex != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write UInt16-index) failed: 0x{rIndex:X8}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);
            }

            // 2) 再分段讀資料
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            while (offset < length)
            {
                int remaining = length - offset;
                int chunkLen = Math.Min(ChunkSize, remaining);
                bool isLast = offset + chunkLen >= length;

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeRead,
                    Flags = isLast ? CtlAuxFlagI2CAux : CtlAuxFlagI2CAuxMot,
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)chunkLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                uint r = NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C read UInt16-index) failed: 0x{r:X8}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                Array.Copy(args.Data, 0, result, offset, chunkLen);
                offset += chunkLen;
            }

            return result;
        }

        #endregion


        #region Interop 結構 & P/Invoke

        [Flags]
        private enum CtlInitFlags : uint
        {
            None = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CtlInitArgs
        {
            public uint Size;
            public byte Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;

            public uint AppVersion;
            public CtlInitFlags Flags;
            public uint SupportedVersion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ApplicationUid;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct CtlAuxAccessArgs
        {
            public uint Size;
            public byte Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;

            public uint OpType;
            public uint Flags;
            public uint Address;
            public ulong Rad;
            public uint PortId;
            public uint DataSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CtlAuxMaxDataSize)]
            public byte[] Data;
        }


        // 對應 ctl_generic_void_datatype_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlGenericVoidDatatype
        {
            public IntPtr pData;   // void*
            public uint size;      // uint32_t
        }

        // 對應 union ctl_os_display_encoder_identifier_t
        [StructLayout(LayoutKind.Explicit)]
        internal struct CtlOsDisplayEncoderIdentifier
        {
            [FieldOffset(0)]
            public uint WindowsDisplayEncoderID;       // Windows 用

            [FieldOffset(0)]
            public CtlGenericVoidDatatype DisplayEncoderID; // 非 Windows 用
        }

        // 對應 ctl_revision_datatype_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlRevisionDatatype
        {
            public byte major_version;
            public byte minor_version;
            public byte revision_version;
            public byte _padding; // 對齊到 4 bytes（C 那邊也是 3 byte + padding）
        }

        // 對應 ctl_display_timing_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlDisplayTiming
        {
            public uint Size;
            public byte Version;
            public byte _pad1;
            public byte _pad2;
            public byte _pad3;

            public ulong PixelClock;
            public uint HActive;
            public uint VActive;
            public uint HTotal;
            public uint VTotal;
            public uint HBlank;
            public uint VBlank;
            public uint HSync;
            public uint VSync;
            public float RefreshRate;
            public uint SignalStandard; // ctl_signal_standard_type_t (enum -> uint32_t)
            public byte VicId;
            public byte _pad4;
            public byte _pad5;
            public byte _pad6;
        }

        // 最重要：對應 _ctl_display_properties_t / ctl_display_properties_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlDisplayProperties
        {
            public uint Size;       // uint32_t
            public byte Version;    // uint8_t
            public byte _pad1;
            public byte _pad2;
            public byte _pad3;

            public CtlOsDisplayEncoderIdentifier OsDisplayEncoderHandle; // union

            // 下面這些在 header 裡都是 typedef 到 uint32_t 的 enum/flags，
            // 用 uint 對應即可（之後你可以再自行包裝成 enum）
            public uint Type;                     // ctl_display_output_types_t
            public uint AttachedDisplayMuxType;   // ctl_attached_display_mux_type_t
            public uint ProtocolConverterOutput;  // ctl_display_output_types_t
            public CtlRevisionDatatype SupportedSpec; // ctl_revision_datatype_t
            public uint SupportedOutputBPCFlags;      // ctl_output_bpc_flags_t
            public uint ProtocolConverterType;        // ctl_protocol_converter_location_flags_t
            public uint DisplayConfigFlags;           // ctl_display_config_flags_t
            public uint FeatureEnabledFlags;          // ctl_std_display_feature_flags_t
            public uint FeatureSupportedFlags;        // ctl_std_display_feature_flags_t
            public uint AdvancedFeatureEnabledFlags;  // ctl_intel_display_feature_flags_t
            public uint AdvancedFeatureSupportedFlags;// ctl_intel_display_feature_flags_t

            public CtlDisplayTiming DisplayTimingInfo; // ctl_display_timing_t

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] ReservedFields;             // uint32_t ReservedFields[16]
        }


        private static class NativeMethods
        {
            private const string DllName = "ControlLib.dll";

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlInit(
                ref CtlInitArgs args,
                out IntPtr apiHandle);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlClose(
                IntPtr apiHandle);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlEnumerateDevices(
                IntPtr apiHandle,
                ref uint count,
                [In, Out] IntPtr[] devices);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlEnumerateDisplayOutputs(
                IntPtr deviceHandle,
                ref uint count,
                [In, Out] IntPtr[] outputs);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlGetDisplayProperties(
                IntPtr outputHandle,
                ref CtlDisplayProperties props);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlAUXAccess(
                IntPtr outputHandle,
                ref CtlAuxAccessArgs args);

        }

        #endregion
    }
}
