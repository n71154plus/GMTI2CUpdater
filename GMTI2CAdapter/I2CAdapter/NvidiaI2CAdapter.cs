namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 以 NVIDIA NVAPI 為底層實作的 I2CAdapter 範例。
    /// </summary>
    public sealed class NvidiaI2CAdapter : I2CAdapterBase
    {

        /// <summary>
        /// 以指定的介面描述初始化 NVIDIA NVAPI 介面實作。
        /// </summary>
        public NvidiaI2CAdapter(I2CAdapterInfo adapterInfo) : base(adapterInfo)
        {
        }

        /// <summary>
        /// 透過 NVAPI 讀取 DPCD/AUX 指定範圍。
        /// </summary>
        public override byte[] ReadDpcd(uint address, uint count)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadDpcd(AdapterInfo, address, count);
        }

        /// <summary>
        /// 透過 NVAPI 以 8-bit index 讀取 I2C 資料。
        /// </summary>
        public override byte[] ReadI2CByteIndex(byte address, byte index, int length)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadI2CByteIndex(AdapterInfo, address, index, length);
        }

        /// <summary>
        /// 透過 NVAPI 以 16-bit index 讀取 I2C 資料。
        /// </summary>
        public override byte[] ReadI2CUInt16Index(byte address, ushort index, int length)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadI2CUInt16Index(AdapterInfo, address, index, length);
        }

        /// <summary>
        /// 透過 NVAPI 讀取未指定 index 的 I2C 位址。
        /// </summary>
        public override byte ReadI2CWithoutIndex(byte address)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadI2CWithoutIndex(AdapterInfo, address);
        }

        /// <summary>
        /// 透過 NVAPI 寫入 DPCD/AUX 指定範圍。
        /// </summary>
        public override void WriteDpcd(uint address, byte[] data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteDpcd(AdapterInfo, address, data);
        }

        /// <summary>
        /// 透過 NVAPI 以 8-bit index 寫入 I2C 資料。
        /// </summary>
        public override void WriteI2CByteIndex(byte address, byte index, byte[] data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteI2CByteIndex(AdapterInfo, address, index, data);
        }

        /// <summary>
        /// 透過 NVAPI 以 16-bit index 寫入 I2C 資料。
        /// </summary>
        public override void WriteI2CUInt16Index(byte address, ushort index, byte[] data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteI2CUInt16Index(AdapterInfo, address, index, data);
        }

        /// <summary>
        /// 透過 NVAPI 寫入未指定 index 的 I2C 位址。
        /// </summary>
        public override void WriteI2CWithoutIndex(byte address, byte data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteI2CWithoutIndex(AdapterInfo, address, data);
        }

        /// <summary>
        /// 此類別每次呼叫都暫時建立 NVAPI 物件，因此自身無額外資源可釋放。
        /// </summary>
        public override void Dispose()
        {
            // 目前沒有要釋放的額外狀態。
        }
    }
}