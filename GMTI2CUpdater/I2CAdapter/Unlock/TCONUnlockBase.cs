namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    /// <summary>
    /// TCON 解鎖/上鎖流程的抽象基底，提供統一介面讓不同實作共用。
    /// </summary>
    public abstract class TCONUnlockBase(I2CAdapterBase adapter)
    {
        private readonly I2CAdapterBase adapter = adapter;
        /// <summary>
        /// 解鎖方案名稱，顯示在 UI 下拉選單中。
        /// </summary>
        public abstract string Name { get; }

        abstract public void Lock(byte deviceAddress = 0x00);
        abstract public void Unlock(byte deviceAddress = 0x00);

    }
}