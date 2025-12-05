using System;

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
        private const int ReportLength = 65;
        private const int MaxI2CChunk = 32;

        private const byte I2cWriteNotLast = 0x02;
        private const byte I2cWriteLast = 0x0A;
        private const byte I2CWriteStop = 0x08;
        private const byte I2CReadeStop = 0x09;
        private const byte I2cReadLast = 0x0B;
        private const byte I2cReadNotLast = 0x03;

        private readonly HidDevice _hid;
        private readonly I2C_Frequency _frequency;

        // 每一個 Report 專用 buffer
        private readonly byte[] _output = new byte[ReportLength];
        private readonly byte[] _input = new byte[ReportLength];

        private bool _initialized;

        public Cy8C24894Adapter(I2CAdapterInfo info,
                                ushort vendorId,
                                ushort productId,
                                I2C_Frequency frequency = I2C_Frequency.F100K)
            : base(info)
        {
            _frequency = frequency;
            _hid = new HidDevice(vendorId, productId, ReportLength, timeoutMs: 200);
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
        private void EnsureReady()
        {
            if (!_hid.Open())
            {
                throw new Exception("無法連接 CY8C24894 HID 裝置，請確認治具是否連接。");
            }
            //Thread.Sleep(100);
            _output[0] = 0;      // Report ID
            _output[1] = 0x0A;
            _output[2] = 0x02;
            _output[3] = 0x80;
            _output[4] = 0x02;
            Array.Clear(_input, 0, _input.Length);
            while (_input[1] != 0x05)
            {
                Thread.Sleep(100);
                _hid.Write(_output, ReportLength, out _);
                _ = _hid.Read(_input, ReportLength, out _);
            }

            // 設定 I2C 頻率
            Array.Clear(_output, 0, _output.Length);
            _output[0] = 0;
            _output[1] = (byte)_frequency;
            Array.Clear(_input, 0, _output.Length);
            if (!_hid.Write(_output, ReportLength, out _))
            {
                _ = _hid.Read(_input, ReportLength, out _);
                _hid.Close();
                throw new Exception("CY8C24894 設定頻率失敗(Write)。");
            }
            _ = _hid.Read(_input, ReportLength, out _);

            _initialized = true;
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
            _hid?.Dispose();
        }

        #endregion

        #region 封裝原本的 I2C 協定實作

        private void I2CWrite(byte devAddress, byte regAddress, byte[] writeDataArray)
        {
            if (writeDataArray == null) throw new ArgumentNullException(nameof(writeDataArray));

            EnsureReady();

            try
            {
                int num = 0;

                if (writeDataArray.Length <= MaxI2CChunk)
                {
                    num = 0;
                    Array.Clear(_output, 0, _output.Length);
                    Array.Clear(_input, 0, _input.Length);

                    _output[num++] = 0; // Report ID
                    _output[num++] = I2cWriteLast;
                    _output[num++] = (byte)(1u + writeDataArray.Length);
                    _output[num++] = (byte)(devAddress >> 1);
                    _output[num++] = regAddress;

                    for (int j = 0; j < writeDataArray.Length; j++)
                    {
                        _output[num++] = writeDataArray[j];
                    }

                    _hid.Write(_output, ReportLength, out _);
                    _hid.Read(_input, ReportLength, out _);

                    if (_input[1] == 0)
                    {
                        throw new Exception("找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                    }
                }
                else
                {
                    // 先送起始位址
                    num = 0;
                    Array.Clear(_output, 0, _output.Length);
                    Array.Clear(_input, 0, _input.Length);
                    _output[num++] = 0;
                    _output[num++] = I2cWriteNotLast;
                    _output[num++] = 1;
                    _output[num++] = (byte)(devAddress >> 1);
                    _output[num++] = regAddress;

                    _hid.Write(_output, ReportLength, out _);
                    _hid.Read(_input, ReportLength, out _);
                    if (_input[1] == 0)
                    {
                        num = 0;
                        Array.Clear(_output, 0, _output.Length);
                        Array.Clear(_input, 0, _input.Length);
                        _output[num++] = 0;
                        _output[num++] = 0x08;
                        _hid.Write(_output, ReportLength, out _);
                        _hid.Read(_input, ReportLength, out _);
                        throw new Exception("找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                    }

                    int fullChunks = writeDataArray.Length / MaxI2CChunk;
                    int remain = writeDataArray.Length % MaxI2CChunk;

                    for (int chunk = 0; chunk < fullChunks; chunk++)
                    {
                        num = 0;
                        Array.Clear(_output, 0, _output.Length);
                        Array.Clear(_input, 0, _input.Length);
                        _output[num++] = 0;
                        _output[num++] = (byte)((remain == 0 && chunk == fullChunks - 1) ? 8 : 0);
                        _output[num++] = (byte)MaxI2CChunk;

                        for (int m = 0; m < MaxI2CChunk; m++)
                        {
                            _output[num++] = writeDataArray[MaxI2CChunk * chunk + m];
                        }

                        _hid.Write(_output, ReportLength, out _);
                        _hid.Read(_input, ReportLength, out _);
                    }

                    if (remain != 0)
                    {
                        num = 0;
                        Array.Clear(_output, 0, _output.Length);
                        Array.Clear(_input, 0, _input.Length);
                        _output[num++] = 0;
                        _output[num++] = I2CWriteStop;
                        _output[num++] = (byte)remain;

                        for (int n = 0; n < remain; n++)
                        {
                            _output[num++] = writeDataArray[writeDataArray.Length - remain + n];
                        }

                        _hid.Write(_output, ReportLength, out _);
                        _hid.Read(_input, ReportLength, out _);
                    }
                }
            }
            finally
            {
                // 視需求決定是否每次都關閉 HID。
                // 這裡先保持開啟，讓整個 Adapter 生命週期共用一個 handle。
                // 若你希望和舊版一樣每次都關，這裡可以呼叫 _hid.Close();
                _hid.Close();
            }
        }

        private void I2CRead(byte devAddress, byte regAddress, ref byte[] readDataArray)
        {
            if (readDataArray == null) throw new ArgumentNullException(nameof(readDataArray));

            EnsureReady();

            try
            {
                int num = 0;

                // 先寫入 index
                num = 0;
                Array.Clear(_output, 0, _output.Length);
                Array.Clear(_input, 0, _input.Length);
                _output[num++] = 0;
                _output[num++] = I2cWriteNotLast;
                _output[num++] = 1;
                _output[num++] = (byte)(devAddress >> 1);
                _output[num++] = regAddress;

                _hid.Write(_output, ReportLength, out _);
                _hid.Read(_input, ReportLength, out _);

                if (_input[1] == 0)
                {
                    num = 0;
                    Array.Clear(_output, 0, _output.Length);
                    Array.Clear(_input, 0, _input.Length);
                    _output[num++] = 0;
                    _output[num++] = I2CWriteStop;
                    _hid.Write(_output, ReportLength, out _);
                    _hid.Read(_input, ReportLength, out _);
                    _hid.Close();
                    throw new Exception("送Device Address找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
                }

                if (readDataArray.Length <= MaxI2CChunk)
                {
                    num = 0;
                    Array.Clear(_output, 0, _output.Length);
                    Array.Clear(_input, 0, _input.Length);
                    _output[num++] = 0;
                    _output[num++] = I2cReadLast;
                    _output[num++] = (byte)readDataArray.Length;
                    _output[num++] = (byte)(devAddress >> 1);

                    _hid.Write(_output, ReportLength, out _);
                    _hid.Read(_input, ReportLength, out _);

                    if (_input[1] == 0)
                    {
                        _hid.Close();
                        throw new Exception("I2C讀取Command失敗");
                    }

                    for (int j = 0; j < readDataArray.Length; j++)
                    {
                        readDataArray[j] = _input[2 + j];
                    }
                }
                else
                {
                    int fullChunks = readDataArray.Length / MaxI2CChunk;
                    int remain = readDataArray.Length % MaxI2CChunk;

                    for (int k = 0; k < fullChunks; k++)
                    {
                        num = 0;
                        Array.Clear(_output, 0, _output.Length);
                        Array.Clear(_input, 0, _input.Length);
                        _output[num++] = 0;

                        if (k == 0)
                        {
                            _output[num++] = I2cReadNotLast;
                            _output[num++] = MaxI2CChunk;
                            _output[num++] = (byte)(devAddress >> 1);
                        }
                        else
                        {
                            _output[num++] = (byte)((remain != 0 || k != fullChunks - 1) ? 1 : 9);
                            _output[num++] = MaxI2CChunk;
                        }

                        _hid.Write(_output, ReportLength, out _);
                        _hid.Read(_input, ReportLength, out _);

                        for (int l = 0; l < MaxI2CChunk; l++)
                        {
                            readDataArray[k * MaxI2CChunk + l] = _input[2 + l];
                        }
                    }

                    if (remain != 0)
                    {
                        num = 0;
                        Array.Clear(_output, 0, _output.Length);
                        Array.Clear(_input, 0, _input.Length);
                        _output[num++] = 0;
                        _output[num++] = I2CReadeStop;
                        _output[num++] = (byte)remain;

                        _hid.Write(_output, ReportLength, out _);
                        _hid.Read(_input, ReportLength, out _);

                        for (int m = 0; m < remain; m++)
                        {
                            readDataArray[readDataArray.Length - remain + m] = _input[2 + m];
                        }
                    }
                }
            }
            finally
            {
                // 同上，預設不關，讓整個 adapter 生命週期共用。
                _hid.Close();
            }
        }

        private void I2C_Single_Write(byte devAddress, byte writeData)
        {
            EnsureReady();

            Array.Clear(_output, 0, _output.Length);
            Array.Clear(_input, 0, _input.Length);

            int num = 0;
            _output[num++] = 0;
            _output[num++] = 10;
            _output[num++] = 1;
            _output[num++] = (byte)(devAddress >> 1);
            _output[num++] = writeData;

            _hid.Write(_output, ReportLength, out _);
            _hid.Read(_input, ReportLength, out _);
            _hid.Close();
        }

        private void I2C_Single_Read(byte devAddress, ref byte readData)
        {
            EnsureReady();

            Array.Clear(_output, 0, _output.Length);
            Array.Clear(_input, 0, _input.Length);

            int num = 0;
            _output[num++] = 0;
            _output[num++] = 11;
            _output[num++] = 1;
            _output[num++] = (byte)(devAddress >> 1);

            _hid.Write(_output, ReportLength, out _);
            _hid.Read(_input, ReportLength, out _);

            if (_input[1] == 0)
            {
                throw new Exception("找不到此IC的Device Address，請檢查I2C中SDA/SCL的連線");
            }

            readData = _input[2];
            _hid.Close();
        }

        #endregion
    }
}
