namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    public abstract class TCONUnlockBase(I2CAdapterBase adapter)
    {
        private readonly I2CAdapterBase adapter = adapter;
        public abstract string Name { get; }

        abstract public void Lock(byte deviceAddress = 0x00);
        abstract public void Unlock(byte deviceAddress = 0x00);

    }
}