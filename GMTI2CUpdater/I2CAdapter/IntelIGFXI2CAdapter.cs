namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 以 Intel IGCL 為底層實作的 I2CAdapter 範例。
    /// </summary>
    public sealed class IntelIGFXI2CAdapter : I2CAdapterBase
    {
        public I2CAdapterInfo AdapterInfo { get; }

        public IntelIGFXI2CAdapter(I2CAdapterInfo adapterInfo) : base(adapterInfo)
        {
            AdapterInfo = adapterInfo;
        }

        public override byte[] ReadDpcd(uint address, uint count)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            return igfx.ReadDpcd(AdapterInfo, address, count);
        }

        public override byte[] ReadI2CByteIndex(byte address, byte index, int length)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            return igfx.ReadI2CByteIndex(AdapterInfo, address, index, length);
        }

        public override byte[] ReadI2CUInt16Index(byte address, ushort index, int length)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            return igfx.ReadI2CUInt16Index(AdapterInfo, address, index, length);
        }

        public override byte ReadI2CWithoutIndex(byte address)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            return igfx.ReadI2CWithoutIndex(AdapterInfo, address);
        }

        public override void WriteDpcd(uint address, byte[] data)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            igfx.WriteDpcd(AdapterInfo, address, data);
        }

        public override void WriteI2CByteIndex(byte address, byte index, byte[] data)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            igfx.WriteI2CByteIndex(AdapterInfo, address, index, data);
        }

        public override void WriteI2CUInt16Index(byte address, ushort index, byte[] data)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            igfx.WriteI2CUInt16Index(AdapterInfo, address, index, data);
        }

        public override void WriteI2CWithoutIndex(byte address, byte data)
        {
            using var igfx = new Hardware.IntelIGFXApi();
            igfx.WriteI2CWithoutIndex(AdapterInfo, address, data);
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}