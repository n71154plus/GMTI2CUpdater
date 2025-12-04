namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 以 NVIDIA NVAPI 為底層實作的 I2CAdapter 範例。
    /// </summary>
    public sealed class NvidiaI2CAdapter : I2CAdapterBase
    {
        public I2CAdapterInfo AdapterInfo { get; }

        public NvidiaI2CAdapter(I2CAdapterInfo adapterInfo) : base(adapterInfo)
        {
            AdapterInfo = adapterInfo;
        }

        public override byte[] ReadDpcd(uint address, uint count)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadDpcd(AdapterInfo, address, count);
        }

        public override byte[] ReadI2CByteIndex(byte address, byte index, int length)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadI2CByteIndex(AdapterInfo, address, index, length);
        }

        public override byte[] ReadI2CUInt16Index(byte address, ushort index, int length)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadI2CUInt16Index(AdapterInfo, address, index, length);
        }

        public override byte ReadI2CWithoutIndex(byte address)
        {
            using var nvidia = new Hardware.NvidiaApi();
            return nvidia.ReadI2CWithoutIndex(AdapterInfo, address);
        }

        public override void WriteDpcd(uint address, byte[] data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteDpcd(AdapterInfo, address, data);
        }

        public override void WriteI2CByteIndex(byte address, byte index, byte[] data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteI2CByteIndex(AdapterInfo, address, index, data);
        }

        public override void WriteI2CUInt16Index(byte address, ushort index, byte[] data)
        {
            using var nvidia = new Hardware.NvidiaApi();
            nvidia.WriteI2CUInt16Index(AdapterInfo, address, index, data);
        }

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
            throw new NotImplementedException();
        }
    }
}