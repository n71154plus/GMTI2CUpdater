using System;
using System.Collections.Generic;

namespace GMTI2CUpdater
{
    /// <summary>
    /// 代表整個 Intel HEX 映像：絕對位址 -> byte
    /// </summary>
    public sealed class IntelHexImage
    {
        public IntelHexImage(Dictionary<int, byte> data, int minAddress, int maxAddress)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            MinAddress = minAddress;
            MaxAddress = maxAddress;
        }

        /// <summary>
        /// 絕對位址 -> byte
        /// </summary>
        public Dictionary<int, byte> Data { get; }

        /// <summary>
        /// 影像中最小位址（含）
        /// </summary>
        public int MinAddress { get; }

        /// <summary>
        /// 影像中最大位址（含）
        /// </summary>
        public int MaxAddress { get; }
    }

    /// <summary>
    /// Intel HEX 檔案解析器
    /// 支援 record type 00, 01, 02, 04
    /// </summary>
    public static class IntelHexParser
    {
        /// <summary>
        /// 解析 Intel HEX 的每一行，回傳 IntelHexImage
        /// </summary>
        public static IntelHexImage Parse(IEnumerable<string> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var data = new Dictionary<int, byte>();
            int? minAddress = null;
            int? maxAddress = null;

            int upperLinearBase = 0;   // type 04: upper 16 bits << 16
            int upperSegmentBase = 0;  // type 02: upper 16 bits << 4
            bool useLinear = false;
            bool eofSeen = false;
            int lineNumber = 0;

            foreach (var rawLine in lines)
            {
                lineNumber++;
                string line = rawLine?.Trim() ?? string.Empty;
                if (line.Length == 0)
                    continue;

                if (line[0] != ':')
                    throw new FormatException($"第 {lineNumber} 行：缺少 ':' 開頭。");

                if (line.Length < 11)
                    throw new FormatException($"第 {lineNumber} 行：長度過短。");

                // byte count
                int byteCount = ParseByte(line, 1);
                int expectedLength = 1 + 2 + 4 + 2 + (byteCount * 2) + 2; // ':' + ll + addr + type + data + checksum
                if (line.Length != expectedLength)
                    throw new FormatException($"第 {lineNumber} 行：行長度與 byte count 不一致。");

                int address = ParseUShort(line, 3);
                int recordType = ParseByte(line, 7);

                // checksum 驗證
                int checksum = ParseByte(line, line.Length - 2);
                int sum = 0;
                for (int i = 1; i < line.Length - 2; i += 2)
                {
                    sum += ParseByte(line, i);
                }
                sum += checksum;
                if ((sum & 0xFF) != 0)
                    throw new FormatException($"第 {lineNumber} 行：Checksum 錯誤。");

                int dataStartIndex = 9;

                switch (recordType)
                {
                    case 0x00: // data record
                        {
                            int baseAddr;
                            if (useLinear)
                            {
                                baseAddr = upperLinearBase + address;
                            }
                            else
                            {
                                baseAddr = upperSegmentBase + address;
                            }

                            for (int i = 0; i < byteCount; i++)
                            {
                                int value = ParseByte(line, dataStartIndex + i * 2);
                                int absoluteAddress = checked(baseAddr + i);

                                data[absoluteAddress] = (byte)value;

                                if (!minAddress.HasValue || absoluteAddress < minAddress.Value)
                                    minAddress = absoluteAddress;
                                if (!maxAddress.HasValue || absoluteAddress > maxAddress.Value)
                                    maxAddress = absoluteAddress;
                            }
                            break;
                        }

                    case 0x01: // EOF
                        eofSeen = true;
                        goto EndParsing;

                    case 0x02: // Extended Segment Address
                        {
                            if (byteCount != 2)
                                throw new FormatException($"第 {lineNumber} 行：type 02 應該有 2 個資料 byte。");

                            int seg = (ParseByte(line, dataStartIndex) << 8) |
                                       ParseByte(line, dataStartIndex + 2);
                            upperSegmentBase = seg << 4;
                            useLinear = false;
                            break;
                        }

                    case 0x04: // Extended Linear Address
                        {
                            if (byteCount != 2)
                                throw new FormatException($"第 {lineNumber} 行：type 04 應該有 2 個資料 byte。");

                            int lin = (ParseByte(line, dataStartIndex) << 8) |
                                       ParseByte(line, dataStartIndex + 2);
                            upperLinearBase = lin << 16;
                            useLinear = true;
                            break;
                        }

                    case 0x03: // Start Segment Address (忽略)
                    case 0x05: // Start Linear Address (忽略)
                        // 這兩種通常只用在啟動向量，直接忽略
                        break;

                    default:
                        throw new FormatException($"第 {lineNumber} 行：不支援的 Record Type: 0x{recordType:X2}");
                }
            }

        EndParsing:

            // 如果你要強制 EOF 存在，可以這裡改成 throw
            // if (!eofSeen)
            //     throw new FormatException("Intel HEX 檔缺少 EOF (type 01) 記錄。");
            _ = eofSeen;

            if (!minAddress.HasValue || !maxAddress.HasValue)
            {
                // 沒有任何 data record
                return new IntelHexImage(new Dictionary<int, byte>(), 0, 0);
            }

            return new IntelHexImage(data, minAddress.Value, maxAddress.Value);
        }

        private static int ParseByte(string line, int index)
        {
            return Convert.ToInt32(line.Substring(index, 2), 16);
        }

        private static int ParseUShort(string line, int index)
        {
            return Convert.ToInt32(line.Substring(index, 4), 16);
        }
    }
}
