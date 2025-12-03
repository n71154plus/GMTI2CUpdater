using GMTI2CUpdater.I2CAdapter;
using System;

namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 對外統一的 I2C / AUX 介面。
    /// </summary>
    public interface II2CAdapter
    {
        /// <summary>
        /// 這個介面的基本資訊（名稱 / BusId / 描述等）。
        /// </summary>
        I2CAdapterInfo AdapterInfo { get; }

        /// <summary>
        /// 讀取 DisplayPort DPCD (AUX)。
        /// </summary>
        /// <param name="address">DPCD 起始位址。</param>
        /// <param name="buffer">用來接收資料的緩衝區。</param>
        /// <param name="offset">寫入到 buffer 時的起始索引。</param>
        /// <param name="count">要讀取的位元組數。</param>
        /// <returns>實際讀到的位元組數。</returns>
        byte[] ReadDpcd(uint address, uint count);

        /// <summary>
        /// 寫入 DisplayPort DPCD (AUX)。
        /// </summary>
        /// <param name="address">DPCD 起始位址。</param>
        /// <param name="data">要寫入的資料。</param>
        /// <param name="offset">從 data 的哪個 index 開始寫。</param>
        /// <returns>實際寫入的位元組數。</returns>
        void WriteDpcd(uint address, byte[] data);

        void WriteI2CWithoutIndex(byte address, byte data);
        void WriteI2CByteIndex(byte address, byte index, byte[] data);
        void WriteI2CUInt16Index(byte address, ushort index, byte[] data);
        byte ReadI2CWithoutIndex(byte address);
        byte[] ReadI2CByteIndex(byte address, byte index, int length);
        byte[] ReadI2CUInt16Index(byte address, ushort index, int length);
    }
}
