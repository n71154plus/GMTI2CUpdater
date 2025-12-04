
namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// I2CAdapter 的抽象基底，包含共用的參數檢查與 AdapterInfo。
    /// </summary>
    public abstract class I2CAdapterBase : II2CAdapter, IDisposable
    {
        /// <summary>
        /// 介面名稱，對應 <see cref="AdapterInfo"/> 中的描述。
        /// </summary>
        public string Name => AdapterInfo.Name;

        /// <summary>
        /// 此介面的基本屬性集合，包含來源、權限需求等資訊。
        /// </summary>
        public I2CAdapterInfo AdapterInfo { get; }

        /// <summary>
        /// 建立介面實例並保存底層的介面資訊描述。
        /// </summary>
        /// <param name="adapterInfo">底層 API 傳回的介面資訊。</param>
        protected I2CAdapterBase(I2CAdapterInfo adapterInfo)
        {
            AdapterInfo = adapterInfo ?? throw new ArgumentNullException(nameof(adapterInfo));
        }

        /// <summary>
        /// 讀取 DisplayPort DPCD/AUX 區塊中的指定範圍。
        /// </summary>
        public abstract byte[] ReadDpcd(uint address, uint count);

        /// <summary>
        /// 以 8-bit index 讀取 I2C 裝置資料。
        /// </summary>
        public abstract byte[] ReadI2CByteIndex(byte address, byte index, int length);

        /// <summary>
        /// 以 16-bit index 讀取 I2C 裝置資料。
        /// </summary>
        public abstract byte[] ReadI2CUInt16Index(byte address, ushort index, int length);

        /// <summary>
        /// 直接讀取 I2C 裝置，不指定 index 位址。
        /// </summary>
        public abstract byte ReadI2CWithoutIndex(byte address);

        /// <summary>
        /// 將資料寫入 DisplayPort DPCD/AUX 指定區塊。
        /// </summary>
        public abstract void WriteDpcd(uint address, byte[] data);

        /// <summary>
        /// 以 8-bit index 寫入 I2C 裝置資料。
        /// </summary>
        public abstract void WriteI2CByteIndex(byte address, byte index, byte[] data);

        /// <summary>
        /// 以 16-bit index 寫入 I2C 裝置資料。
        /// </summary>
        public abstract void WriteI2CUInt16Index(byte address, ushort index, byte[] data);

        /// <summary>
        /// 直接寫入 I2C 裝置，不指定 index 位址。
        /// </summary>
        public abstract void WriteI2CWithoutIndex(byte address, byte data);

        /// <summary>
        /// 釋放介面所使用的任何非受控資源，若無則可為空實作。
        /// </summary>
        public abstract void Dispose();
    }
}
