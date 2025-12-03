using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class LuaTconUnlockLoader
    {
        private readonly string scriptDirectory;

        static LuaTconUnlockLoader()
        {
            UserData.RegisterType<LuaTconContext>();
        }

        public LuaTconUnlockLoader()
        {
            scriptDirectory = Path.Combine(AppContext.BaseDirectory, "Scripts", "TconUnlock");
        }

        public IEnumerable<TCONUnlockBase> Load(I2CAdapterBase adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            if (!Directory.Exists(scriptDirectory))
            {
                Directory.CreateDirectory(scriptDirectory);
                yield break;
            }

            foreach (string scriptPath in Directory.EnumerateFiles(scriptDirectory, "*.lua", SearchOption.TopDirectoryOnly))
            {
                LuaTconUnlock? unlock = null;
                try
                {
                    unlock = LoadScript(scriptPath, adapter);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"無法載入 {Path.GetFileName(scriptPath)}：{ex.Message}");
                }

                if (unlock != null)
                    yield return unlock;
            }
        }

        private LuaTconUnlock LoadScript(string scriptPath, I2CAdapterBase adapter)
        {
            var script = new Script(CoreModules.Preset_SoftSandbox)
            {
                Options =
                {
                    ScriptLoader = new FileSystemScriptLoader
                    {
                        ModulePaths = new[] { Path.Combine(Path.GetDirectoryName(scriptPath)!, "?.lua") }
                    }
                }
            };

            var definition = script.DoFile(scriptPath);
            if (definition.Type != DataType.Table)
                throw new InvalidDataException($"TCON unlock script {Path.GetFileName(scriptPath)} 必須回傳 table。");

            Table table = definition.Table;
            DynValue nameValue = table.Get("name");
            string name = nameValue.IsNotNil() ? nameValue.CastToString()! : Path.GetFileNameWithoutExtension(scriptPath);
            DynValue unlockFunction = table.Get("unlock");
            DynValue lockFunction = table.Get("lock");

            if (unlockFunction.Type != DataType.Function && lockFunction.Type != DataType.Function)
                throw new InvalidDataException($"TCON unlock script {Path.GetFileName(scriptPath)} 缺少 lock 或 unlock function。");

            var context = new LuaTconContext(script, adapter);
            return new LuaTconUnlock(adapter, name, script, context, lockFunction, unlockFunction);
        }
    }
}
