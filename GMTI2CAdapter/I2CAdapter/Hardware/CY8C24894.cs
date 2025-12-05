using System.Runtime.InteropServices;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{

    public sealed class CY8C24894 : IDisposable
    {
        private const int WriteTransferDataSize = 61;
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        public CY8C24894()
        {

        }


        public enum TransferDirection : byte
        {
            Write = 0,
            Read = 1
        }

        public enum I2CFrequency : byte
        {
            F100KHz = 0, // 00
            F400KHz = 1, // 01
            F50KHz = 2   // 10
                         // 3 = reserved / invalid
        }
        public enum BridgePowerMode : byte
        {
            External = 0,
            Internal5V = 1,
            Internal3V3 = 2
        }
        /// <summary>
        /// 單一 WriteOutput 的封裝結構。
        /// Buf 固定 0x40 bytes，透過 ByValArray 對應 unmanaged 結構。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct OutputPacketWrite
        {
            public byte ReportId;      // HID Report ID (通常是 0)
            public byte ControlByte;   // 所有 I2C 控制 bit
            public byte LengthRaw;     // 原始 length byte（含 BURST bit）
            public byte DeviceAddress; // 0..127，一般 I2C slave；0x80=橋接器內部控制

            // 實際要寫入或讀取的資料緩衝區（最多 61 bytes）
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = WriteTransferDataSize)]
            public byte[] Data;

            private const byte BridgeInternalAddress = 0x80;

            /// <summary>
            /// 是否是橋接器內部控制（Address = 0x80）
            /// </summary>
            public bool IsBridgeInternalControl
            {
                get => DeviceAddress == BridgeInternalAddress;
                set => DeviceAddress = value ? BridgeInternalAddress : DeviceAddress;
            }
            // ================================
            // ControlByte 封裝（跟之前講的一樣）
            // ================================
            private const byte BitDirection = 1 << 0; // bit0: 0=Write, 1=Read
            private const byte BitStart = 1 << 1; // bit1
            private const byte BitRestart = 1 << 2; // bit2
            private const byte BitStop = 1 << 3; // bit3
            private const byte BitReinitBus = 1 << 4; // bit4
            private const byte BitReconfigure = 1 << 5; // bit5
            private const byte BitsFreqMask = (1 << 2) | (1 << 3); // bit2 & bit3

            public TransferDirection Direction
            {
                get => (ControlByte & BitDirection) != 0
                    ? TransferDirection.Read
                    : TransferDirection.Write;

                set
                {
                    if (value == TransferDirection.Read)
                        ControlByte |= BitDirection; // bit0 = 1
                    else
                        ControlByte &= unchecked((byte)~BitDirection); // bit0 = 0
                }
            }

            public bool StartSignal
            {
                get => (ControlByte & BitStart) != 0;
                set => ControlByte = value
                    ? (byte)(ControlByte | BitStart)
                    : (byte)(ControlByte & unchecked((byte)~BitStart));
            }

            public bool RestartSignal
            {
                get => (ControlByte & BitRestart) != 0;
                set => ControlByte = value
                    ? (byte)(ControlByte | BitRestart)
                    : (byte)(ControlByte & unchecked((byte)~BitRestart));
            }

            public bool StopSignal
            {
                get => (ControlByte & BitStop) != 0;
                set => ControlByte = value
                    ? (byte)(ControlByte | BitStop)
                    : (byte)(ControlByte & unchecked((byte)~BitStop));
            }

            public bool ReinitializeBus
            {
                get => (ControlByte & BitReinitBus) != 0;
                set => ControlByte = value
                    ? (byte)(ControlByte | BitReinitBus)
                    : (byte)(ControlByte & unchecked((byte)~BitReinitBus));
            }

            public bool IsReconfigure
            {
                get => (ControlByte & BitReconfigure) != 0;
                set => ControlByte = value
                    ? (byte)(ControlByte | BitReconfigure)
                    : (byte)(ControlByte & unchecked((byte)~BitReconfigure));
            }

            // ===== 頻率（在 Reconfigure 模式下由 bit3:bit2 表示） =====

            public I2CFrequency? ReconfigureFrequency
            {
                get
                {
                    if (!IsReconfigure)
                        return null;

                    byte bits = (byte)((ControlByte & BitsFreqMask) >> 2);
                    return bits switch
                    {
                        0 => I2CFrequency.F100KHz,
                        1 => I2CFrequency.F400KHz,
                        2 => I2CFrequency.F50KHz,
                        _ => null // reserved
                    };
                }
                set
                {
                    if (value is null)
                    {
                        // 清除 reconfig 與頻率設定
                        ControlByte &= unchecked((byte)~(BitReconfigure | BitsFreqMask));
                        return;
                    }

                    ControlByte |= BitReconfigure; // 開啟 reconfig

                    ControlByte &= unchecked((byte)~BitsFreqMask); // 清掉 bit2,3

                    byte bits = value switch
                    {
                        I2CFrequency.F100KHz => 0,
                        I2CFrequency.F400KHz => 1,
                        I2CFrequency.F50KHz => 2,
                        _ => 0
                    };

                    ControlByte |= (byte)(bits << 2);
                }
            }

            // ================================
            // Length / BURST 封裝
            // ================================

            private const byte BurstBit = 0x40; // bit6

            /// <summary>
            /// 真正要傳的 data 長度（不含 address byte）
            /// 只用低 6 bits，範圍 0..61。
            /// </summary>
            public byte DataLength
            {
                get => (byte)(LengthRaw & 0x3F);
                set
                {
                    if (value > WriteTransferDataSize)
                        throw new ArgumentOutOfRangeException(nameof(value), $"Max {WriteTransferDataSize} bytes");
                    // 保留原本的 Burst bit
                    byte burst = (byte)(LengthRaw & BurstBit);
                    LengthRaw = (byte)(burst | (value & 0x3F));
                }
            }

            /// <summary>
            /// 是否為 BURST 模式（length 的 0x40 bit）
            /// </summary>
            public bool BurstMode
            {
                get => (LengthRaw & BurstBit) != 0;
                set
                {
                    if (value)
                        LengthRaw |= BurstBit;
                    else
                        LengthRaw &= unchecked((byte)~BurstBit);
                }
            }

            /// <summary>
            /// 是否應該帶 Address byte。依 spec：只有 Start 或 Restart 旗標為 1 時才有。
            /// 雖然 HID 報文長度是固定的，但我們可以用這個來決定 Data[0] 是不是資料還是 Address 之後的 data。
            /// </summary>
            public bool HasAddressByte => StartSignal || RestartSignal;
        }
        private static OutputPacketWrite CreateBridgePowerControlPacket(BridgePowerMode mode)
        {
            var packet = new OutputPacketWrite
            {
                DeviceAddress = 0x80,                    // 內部控制
                Data = new byte[WriteTransferDataSize]
            };

            packet.Direction = TransferDirection.Write;
            packet.StartSignal = true;
            packet.StopSignal = true;
            packet.RestartSignal = false;

            packet.DataLength = 1;                      // 只送一個 power mode byte
            packet.Data[0] = (byte)mode;

            return packet;
        }
        private static OutputPacketWrite CreateBridgeIntControlPacket(BridgePowerMode mode)
        {
            var packet = new OutputPacketWrite
            {
                DeviceAddress = 0x80,                    // 內部控制
                Data = new byte[WriteTransferDataSize]
            };

            packet.Direction = TransferDirection.Write;
            packet.StartSignal = false;
            packet.StopSignal = false;
            packet.RestartSignal = true;
            return packet;
        }
        private static OutputPacketWrite CreateWritePacket(
            byte deviceAddress,
            byte[] payload,
            bool start,
            bool stop,
            bool restart = false)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Length > WriteTransferDataSize)
                throw new ArgumentOutOfRangeException(nameof(payload), "Max 61 bytes per packet");

            var packet = new OutputPacketWrite
            {
                DeviceAddress = (byte)(deviceAddress >> 1),
                Data = new byte[WriteTransferDataSize]
            };

            packet.Direction = TransferDirection.Write;
            packet.StartSignal = start;
            packet.StopSignal = stop;
            packet.RestartSignal = restart;

            packet.DataLength = (byte)payload.Length;

            // 將 payload 複製到 Data 開頭
            Buffer.BlockCopy(payload, 0, packet.Data, 0, payload.Length);

            return packet;
        }
        private static OutputPacketWrite CreateI2CFrequencyConfigPacket(
            I2CFrequency frequency,
            bool reinitializeBus = true)
        {
            var packet = new OutputPacketWrite();

            // 這是一個「控制 / 設定」用的封包，不是一般資料傳輸
            packet.Direction = TransferDirection.Write;
            packet.StartSignal = false;                  // 不做實際 I2C 傳輸
            packet.StopSignal = false;
            packet.RestartSignal = false;

            // bit4：是否要 reinitialize I2C bus
            packet.ReinitializeBus = reinitializeBus;

            // bit5=1，並用 bit3:bit2 設定頻率
            packet.ReconfigureFrequency = frequency;

            // 沒有資料 payload，只是透過 ControlByte 來控制
            packet.DataLength = 0;

            return packet;
        }


    }
}