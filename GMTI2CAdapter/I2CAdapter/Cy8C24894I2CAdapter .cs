using System;
using System.Threading;
using GMTI2CUpdater.I2CAdapter.Hardware;

namespace GMTI2CUpdater.I2CAdapter
{
    public enum I2C_Frequency : byte
    {
        F50K = 0x28,
        F100K = 0x20,
        F400K = 0x24,
    }

    /// <summary>
    /// 透過 Cypress CY8C24894 HID Bridge 對外提供 I2C 存取的 Adapter。
    /// </summary>
    public sealed class Cy8C24894Adapter : I2CAdapterBase
    {
        private const int ReportLength = CY8C24894.ReportLength;
        private const int MaxI2CChunk = 32;

        private const byte I2cWriteNotLast = 0x02;
        private const byte I2cWriteLast = 0x0A;
        private const byte I2CWriteStop = 0x08;
        private const byte I2CReadeStop = 0x09;
        private const byte I2cReadLast = 0x0B;
        private const byte I2cReadNotLast = 0x03;

        private readonly ushort _vendorId;
        private readonly ushort _productId;
        private readonly I2C_Frequency _frequency;

        public Cy8C24894Adapter(I2CAdapterInfo info,
                                ushort vendorId,
                                ushort productId,
                                I2C_Frequency frequency = I2C_Frequency.F100K)
            : base(info)
        {
            _vendorId = vendorId;
            _productId = productId;
            _frequency = frequency;
        }

        /// <summary>
        /// 工廠方法：使用預設 Cypress VID/PID (0x04B4 / 0xF232)。
        /// </summary>
        public static Cy8C24894Adapter CreateDefault(I2CAdapterInfo info,
                                                     I2C_Frequency frequency = I2C_Frequency.F400K)
        {
            const ushort VendorId = 0x04B4;
            const ushort ProductId = 0xF232;
            return new Cy8C24894Adapter(info, VendorId, ProductId, frequency);
        }

        #region 初始化

        /// <summary>
        /// 確保 HID 已開啟，且已送出初始化指令與頻率設定。
        /// </summary>
        private static void ClearBuffers(byte[] output, byte[] input)
        {
            Array.Clear(output, 0, output.Length);
            Array.Clear(input, 0, input.Length);
        }

        private HidDevice CreateHidDevice()
        {
            return new HidDevice(_vendorId, _productId, ReportLength, timeoutMs: 200);
        }

        private static (byte[] Output, byte[] Input) CreateReportBuffers()
        {
            return (new byte[ReportLength], new byte[ReportLength]);
        }

        private void EnsureReady(HidDevice hid, byte[] output, byte[] input)
        {
            if (!hid.Open())
            {
                throw new Exception("無法連接 CY8C24894 HID 裝置，請確認治具是否連接。");
            }

            ClearBuffers(output, input);
            output[0] = 0;      // Report ID
            output[1] = 0x0A;
            output[2] = 0x02;
            output[3] = 0x80;
            output[4] = 0x02;

            while (input[1] != 0x05)
            {
                Thread.Sleep(100);
                hid.Write(output, ReportLength, out _);
                _ = hid.Read(input, ReportLength, out _);
            }

            ClearBuffers(output, input);
            output[0] = 0;
            output[1] = (byte)_frequency;

            if (!hid.Write(output, ReportLength, out _))
            {
                _ = hid.Read(input, ReportLength, out _);
                throw new Exception("CY8C24894 設定頻率失敗(Write)。");
            }

            _ = hid.Read(input, ReportLength, out _);
        }

        private static void SendPacket(
            HidDevice hid,
            byte[] output,
            byte[] input,
            CY8C24894.OutputPacketWrite packet)
        {
            CY8C24894.WritePacketToBuffer(packet, output);
            hid.Write(output, ReportLength, out _);
            hid.Read(input, ReportLength, out _);
        }

        private void UseDevice(Action<HidDevice, byte[], byte[]> action)
        {
            using var hid = CreateHidDevice();
            var (output, input) = CreateReportBuffers();

            EnsureReady(hid, output, input);
            action(hid, output, input);
        }

        private T UseDevice<T>(Func<HidDevice, byte[], byte[], T> action)
        {
            using var hid = CreateHidDevice();
            var (output, input) = CreateReportBuffers();

            EnsureReady(hid, output, input);
            return action(hid, output, input);
        }

        #endregion

        #region I2CAdapterBase 實作

        // 這顆 Cypress 只做 I2C，不提供 DPCD/AUX，先丟 NotSupported。
        public override byte[] ReadDpcd(uint address, uint count)
        {
            throw new NotSupportedException("CY8C24894 不支援 DPCD/AUX 讀取。");
        }

        public override void WriteDpcd(uint address, byte[] data)
        {
            throw new NotSupportedException("CY8C24894 不支援 DPCD/AUX 寫入。");
        }

        public override byte[] ReadI2CByteIndex(byte address, byte index, int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

            byte[] buffer = new byte[length];
            I2CRead(address, index, ref buffer);
            return buffer;
        }

        public override byte[] ReadI2CUInt16Index(byte address, ushort index, int length)
        {
            // 你的原始協定沒有 16-bit index 寫法，先丟 NotSupported，
            // 日後若有實際規格再補實作。
            throw new NotSupportedException("CY8C24894 目前未實作 16-bit index 讀取。");
        }

        public override byte ReadI2CWithoutIndex(byte address)
        {
            byte data = 0;
            I2C_Single_Read(address, ref data);
            return data;
        }

        public override void WriteI2CByteIndex(byte address, byte index, byte[] data)
        {
            I2CWrite(address, index, data);
        }

        public override void WriteI2CUInt16Index(byte address, ushort index, byte[] data)
        {
            throw new NotSupportedException("CY8C24894 目前未實作 16-bit index 寫入。");
        }

        public override void WriteI2CWithoutIndex(byte address, byte data)
        {
            I2C_Single_Write(address, data);
        }

        public override void Dispose()
        {
            // 每次操作都以 using 建立 HidDevice，沒有長時間持有的 unmanaged 資源。
        }

        #endregion

        #region 封裝原本的 I2C 協定實作

        private void I2CWrite(byte devAddress, byte regAddress, byte[] writeDataArray)
        {
            if (writeDataArray == null) throw new ArgumentNullException(nameof(writeDataArray));

            UseDevice((hid, output, input) =>
            {
                if (writeDataArray.Length <= MaxI2CChunk)
                {
                    var payload = new byte[1 + writeDataArray.Length];
                    payload[0] = regAddress;
                    Buffer.BlockCopy(writeDataArray, 0, payload, 1, writeDataArray.Length);

                    var packet = CY8C24894.CreateRawPacket(
                        I2cWriteLast,
                        (byte)payload.Length,
                        (byte)(devAddress >> 1),
                        payload);

                    SendPacket(hid, output, input, packet);

                    if (input[1] == 0)
                    {
                        throw new Exception("找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                    }
                }
                else
                {
                    var headerPacket = CY8C24894.CreateRawPacket(
                        I2cWriteNotLast,
                        1,
                        (byte)(devAddress >> 1),
                        new[] { regAddress });

                    SendPacket(hid, output, input, headerPacket);

                    if (input[1] == 0)
                    {
                        var stopPacket = CY8C24894.CreateRawPacket(
                            I2CWriteStop,
                            0,
                            0,
                            Array.Empty<byte>());
                        SendPacket(hid, output, input, stopPacket);
                        throw new Exception("找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                    }

                    int fullChunks = writeDataArray.Length / MaxI2CChunk;
                    int remain = writeDataArray.Length % MaxI2CChunk;

                    for (int chunk = 0; chunk < fullChunks; chunk++)
                    {
                        var chunkPayload = new byte[MaxI2CChunk];
                        Buffer.BlockCopy(writeDataArray, MaxI2CChunk * chunk, chunkPayload, 0, MaxI2CChunk);

                        byte control = (byte)((remain == 0 && chunk == fullChunks - 1) ? I2CWriteStop : 0);

                        var chunkPacket = CY8C24894.CreateRawPacket(
                            control,
                            (byte)MaxI2CChunk,
                            0,
                            chunkPayload);

                        SendPacket(hid, output, input, chunkPacket);
                    }

                    if (remain != 0)
                    {
                        var remainPayload = new byte[remain];
                        Buffer.BlockCopy(writeDataArray, writeDataArray.Length - remain, remainPayload, 0, remain);

                        var tailPacket = CY8C24894.CreateRawPacket(
                            I2CWriteStop,
                            (byte)remain,
                            0,
                            remainPayload);

                        SendPacket(hid, output, input, tailPacket);
                    }
                }
            });
        }

        private void I2CRead(byte devAddress, byte regAddress, ref byte[] readDataArray)
        {
            if (readDataArray == null) throw new ArgumentNullException(nameof(readDataArray));

            UseDevice((hid, output, input) =>
            {
                var headerPacket = CY8C24894.CreateRawPacket(
                    I2cWriteNotLast,
                    1,
                    (byte)(devAddress >> 1),
                    new[] { regAddress });

                SendPacket(hid, output, input, headerPacket);

                if (input[1] == 0)
                {
                    var stopPacket = CY8C24894.CreateRawPacket(I2CWriteStop, 0, 0, Array.Empty<byte>());
                    SendPacket(hid, output, input, stopPacket);
                    throw new Exception("送Device Address找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                }

                if (readDataArray.Length <= MaxI2CChunk)
                {
                    var packet = CY8C24894.CreateRawPacket(
                        I2cReadLast,
                        (byte)readDataArray.Length,
                        (byte)(devAddress >> 1),
                        Array.Empty<byte>());

                    SendPacket(hid, output, input, packet);

                    if (input[1] == 0)
                    {
                        throw new Exception("I2C讀取Command失敗");
                    }

                    for (int j = 0; j < readDataArray.Length; j++)
                    {
                        readDataArray[j] = input[2 + j];
                    }
                }
                else
                {
                    int fullChunks = readDataArray.Length / MaxI2CChunk;
                    int remain = readDataArray.Length % MaxI2CChunk;

                    for (int k = 0; k < fullChunks; k++)
                    {
                        byte control;
                        byte device = 0;

                        if (k == 0)
                        {
                            control = I2cReadNotLast;
                            device = (byte)(devAddress >> 1);
                        }
                        else
                        {
                            control = (byte)((remain != 0 || k != fullChunks - 1) ? 1 : 9);
                        }

                        var packet = CY8C24894.CreateRawPacket(
                            control,
                            MaxI2CChunk,
                            device,
                            Array.Empty<byte>());

                        SendPacket(hid, output, input, packet);

                        for (int l = 0; l < MaxI2CChunk; l++)
                        {
                            readDataArray[k * MaxI2CChunk + l] = input[2 + l];
                        }
                    }

                    if (remain != 0)
                    {
                        var packet = CY8C24894.CreateRawPacket(
                            I2CReadeStop,
                            (byte)remain,
                            0,
                            Array.Empty<byte>());

                        SendPacket(hid, output, input, packet);

                        for (int m = 0; m < remain; m++)
                        {
                            readDataArray[readDataArray.Length - remain + m] = input[2 + m];
                        }
                    }
                }
            });
        }

        private void I2C_Single_Write(byte devAddress, byte writeData)
        {
            UseDevice((hid, output, input) =>
            {
                var packet = CY8C24894.CreateRawPacket(
                    I2cWriteLast,
                    1,
                    (byte)(devAddress >> 1),
                    new[] { writeData });

                SendPacket(hid, output, input, packet);
            });
        }

        private void I2C_Single_Read(byte devAddress, ref byte readData)
        {
            UseDevice((hid, output, input) =>
            {
                var packet = CY8C24894.CreateRawPacket(
                    I2cReadLast,
                    1,
                    (byte)(devAddress >> 1),
                    Array.Empty<byte>());

                SendPacket(hid, output, input, packet);

                if (input[1] == 0)
                {
                    throw new Exception("找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                }

                readData = input[2];
            });
        }

        #endregion
    }
}
