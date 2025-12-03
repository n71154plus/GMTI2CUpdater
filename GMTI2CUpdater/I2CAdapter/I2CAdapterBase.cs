using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// I2CAdapter 的抽象基底，包含共用的參數檢查與 AdapterInfo。
    /// </summary>
    public abstract class I2CAdapterBase : II2CAdapter, IDisposable
    {
        public string Name => AdapterInfo.Name;
        public I2CAdapterInfo AdapterInfo { get; }
        protected I2CAdapterBase(I2CAdapterInfo adapterInfo)
        {
            AdapterInfo = adapterInfo ?? throw new ArgumentNullException(nameof(adapterInfo));
        }

        public abstract byte[] ReadDpcd(uint address, uint count);

        public abstract byte[] ReadI2CByteIndex(byte address, byte index, int length);

        public abstract byte[] ReadI2CUInt16Index(byte address, ushort index, int length);

        public abstract byte ReadI2CWithoutIndex(byte address);

        public abstract void WriteDpcd(uint address, byte[] data);

        public abstract void WriteI2CByteIndex(byte address, byte index, byte[] data);

        public abstract void WriteI2CUInt16Index(byte address, ushort index, byte[] data);

        public abstract void WriteI2CWithoutIndex(byte address, byte data);

        public abstract void Dispose();
    }
}
