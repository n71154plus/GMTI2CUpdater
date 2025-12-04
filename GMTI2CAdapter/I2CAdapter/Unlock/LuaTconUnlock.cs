using MoonSharp.Interpreter;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class LuaTconUnlock : TCONUnlockBase
    {
        private readonly Script script;
        private readonly DynValue lockFunction;
        private readonly DynValue unlockFunction;
        private readonly LuaTconContext context;

        public LuaTconUnlock(I2CAdapterBase adapter, string name, Script script, LuaTconContext context, DynValue lockFunction, DynValue unlockFunction)
            : base(adapter)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.script = script ?? throw new ArgumentNullException(nameof(script));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.lockFunction = lockFunction;
            this.unlockFunction = unlockFunction;
        }

        public override string Name { get; }

        public override void Lock(byte deviceAddress = 0x00)
        {
            CallIfFunction(lockFunction, deviceAddress);
        }

        public override void Unlock(byte deviceAddress = 0x00)
        {
            CallIfFunction(unlockFunction, deviceAddress);
        }

        private void CallIfFunction(DynValue function, byte deviceAddress)
        {
            if (function == null || function.Type != DataType.Function)
                return;

            try
            {
                script.Call(function, UserData.Create(context), deviceAddress);
            }
            catch (ScriptRuntimeException ex)
            {
                throw new InvalidOperationException($"執行 TCON 腳本 {Name} 失敗：{ex.DecoratedMessage ?? ex.Message}", ex);
            }
        }
    }
}
