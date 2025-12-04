namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 以 Intel IGCL 為底層實作的 I2CAdapter 範例。
    /// </summary>
    public sealed class IntelIGCLI2CAdapter : I2CAdapterBase
    {
        /// <summary>
        /// 底層 API 傳回的介面資訊，包含來源、權限等屬性。
        /// </summary>
        public I2CAdapterInfo AdapterInfo { get; }

        /// <summary>
        /// 以指定的介面描述初始化 Intel IGCL 介面實作。
        /// </summary>
        public IntelIGCLI2CAdapter(I2CAdapterInfo adapterInfo) : base(adapterInfo)
        {
            AdapterInfo = adapterInfo;
        }

        /// <summary>
        /// 透過 IGCL 讀取 DPCD/AUX 指定範圍。
        /// </summary>
        public override byte[] ReadDpcd(uint address, uint count)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            return igcl.ReadDpcd(AdapterInfo, address, count);
        }

        /// <summary>
        /// 透過 IGCL 以 8-bit index 讀取 I2C 資料。
        /// </summary>
        public override byte[] ReadI2CByteIndex(byte address, byte index, int length)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            return igcl.ReadI2CByteIndex(AdapterInfo, address, index, length);
        }

        /// <summary>
        /// 透過 IGCL 以 16-bit index 讀取 I2C 資料。
        /// </summary>
        public override byte[] ReadI2CUInt16Index(byte address, ushort index, int length)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            return igcl.ReadI2CUInt16Index(AdapterInfo, address, index, length);
        }

        /// <summary>
        /// 透過 IGCL 讀取未指定 index 的 I2C 位址。
        /// </summary>
        public override byte ReadI2CWithoutIndex(byte address)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            return igcl.ReadI2CWithoutIndex(AdapterInfo, address);
        }

        /// <summary>
        /// 透過 IGCL 寫入 DPCD/AUX 指定範圍。
        /// </summary>
        public override void WriteDpcd(uint address, byte[] data)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            igcl.WriteDpcd(AdapterInfo, address, data);
        }

        /// <summary>
        /// 透過 IGCL 以 8-bit index 寫入 I2C 資料。
        /// </summary>
        public override void WriteI2CByteIndex(byte address, byte index, byte[] data)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            igcl.WriteI2CByteIndex(AdapterInfo, address, index, data);
        }

        /// <summary>
        /// 透過 IGCL 以 16-bit index 寫入 I2C 資料。
        /// </summary>
        public override void WriteI2CUInt16Index(byte address, ushort index, byte[] data)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            igcl.WriteI2CUInt16Index(AdapterInfo, address, index, data);
        }

        /// <summary>
        /// 透過 IGCL 寫入未指定 index 的 I2C 位址。
        /// </summary>
        public override void WriteI2CWithoutIndex(byte address, byte data)
        {
            using var igcl = new Hardware.IntelIGCLApi();
            igcl.WriteI2CWithoutIndex(AdapterInfo, address, data);
        }

        /// <summary>
        /// 由於每次呼叫皆使用 using 建立 API 實例，這裡無需釋放其他資源。
        /// </summary>
        public override void Dispose()
        {
            // 目前沒有要釋放的額外狀態。
        }
    }
}