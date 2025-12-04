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

        /// <summary>
        /// 直接寫入 I2C 裝置的單一位元組，不帶 index。
        /// </summary>
        /// <param name="address">I2C 裝置位址。</param>
        /// <param name="data">要寫入的資料。</param>
        void WriteI2CWithoutIndex(byte address, byte data);

        /// <summary>
        /// 以 8-bit index 寫入 I2C 裝置資料。
        /// </summary>
        /// <param name="address">I2C 裝置位址。</param>
        /// <param name="index">8-bit index。</param>
        /// <param name="data">要寫入的資料。</param>
        void WriteI2CByteIndex(byte address, byte index, byte[] data);

        /// <summary>
        /// 以 16-bit index 寫入 I2C 裝置資料。
        /// </summary>
        /// <param name="address">I2C 裝置位址。</param>
        /// <param name="index">16-bit index。</param>
        /// <param name="data">要寫入的資料。</param>
        void WriteI2CUInt16Index(byte address, ushort index, byte[] data);

        /// <summary>
        /// 直接讀取 I2C 裝置的單一位元組，不帶 index。
        /// </summary>
        /// <param name="address">I2C 裝置位址。</param>
        byte ReadI2CWithoutIndex(byte address);

        /// <summary>
        /// 以 8-bit index 讀取 I2C 裝置資料。
        /// </summary>
        /// <param name="address">I2C 裝置位址。</param>
        /// <param name="index">8-bit index。</param>
        /// <param name="length">要讀取的長度。</param>
        byte[] ReadI2CByteIndex(byte address, byte index, int length);

        /// <summary>
        /// 以 16-bit index 讀取 I2C 裝置資料。
        /// </summary>
        /// <param name="address">I2C 裝置位址。</param>
        /// <param name="index">16-bit index。</param>
        /// <param name="length">要讀取的長度。</param>
        byte[] ReadI2CUInt16Index(byte address, ushort index, int length);
    }
}
