using System.Diagnostics;
using System.Reflection;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    /// <summary>
    /// 負責從 Scripts/TconUnlock 資料夾載入 Lua 解鎖腳本，並包裝成解鎖物件。
    /// </summary>
    public class LuaTconUnlockLoader
    {
        private readonly Assembly assembly;
        private readonly string resourcePrefix;
        private readonly IReadOnlyList<string> scriptResourceNames;

        static LuaTconUnlockLoader()
        {
            UserData.RegisterType<LuaTconContext>();
        }

        public LuaTconUnlockLoader()
        {
            assembly = typeof(LuaTconUnlockLoader).Assembly;
            resourcePrefix = $"{assembly.GetName().Name}.Scripts.TconUnlock.";
            scriptResourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal) && name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// 依序載入所有 Lua 腳本並產生對應的解鎖實例，若目錄不存在會先建立後返回空集合。
        /// </summary>
        public IEnumerable<TCONUnlockBase> Load(I2CAdapterBase adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            foreach (string resourceName in scriptResourceNames)
            {
                LuaTconUnlock? unlock = null;
                try
                {
                    unlock = LoadScript(resourceName, adapter);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"無法載入 {resourceName}：{ex.Message}");
                }

                if (unlock != null)
                    yield return unlock;
            }
        }

        /// <summary>
        /// 載入單一 Lua 腳本檔案並驗證必要的欄位，返回封裝好的解鎖器實例。
        /// </summary>
        private LuaTconUnlock LoadScript(string resourceName, I2CAdapterBase adapter)
        {
            string scriptContent = ReadEmbeddedScript(resourceName);

            var script = new Script(CoreModules.Preset_SoftSandbox)
            {
                Options =
                {
                    ScriptLoader = new EmbeddedLuaScriptLoader(assembly, resourcePrefix)
                }
            };

            var definition = script.DoString(scriptContent, codeFriendlyName: resourceName);
            if (definition.Type != DataType.Table)
                throw new InvalidDataException($"TCON unlock script {resourceName} 必須回傳 table。");

            Table table = definition.Table;
            DynValue nameValue = table.Get("name");
            string name = nameValue.IsNotNil() ? nameValue.CastToString()! : ExtractDisplayName(resourceName);
            DynValue unlockFunction = table.Get("unlock");
            DynValue lockFunction = table.Get("lock");

            if (unlockFunction.Type != DataType.Function && lockFunction.Type != DataType.Function)
                throw new InvalidDataException($"TCON unlock script {resourceName} 缺少 lock 或 unlock function。");

            var context = new LuaTconContext(script, adapter);
            return new LuaTconUnlock(adapter, name, script, context, lockFunction, unlockFunction);
        }

        private string ReadEmbeddedScript(string resourceName)
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"找不到內嵌資源 {resourceName}。");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private string ExtractDisplayName(string resourceName)
        {
            string scriptName = resourceName.Substring(resourcePrefix.Length);
            int extensionIndex = scriptName.LastIndexOf('.');
            return extensionIndex > 0 ? scriptName.Substring(0, extensionIndex) : scriptName;
        }
    }

    internal class EmbeddedLuaScriptLoader : ScriptLoaderBase
    {
        private readonly Assembly assembly;
        private readonly string resourcePrefix;
        private readonly HashSet<string> resourceNames;

        public EmbeddedLuaScriptLoader(Assembly assembly, string resourcePrefix)
        {
            this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.resourcePrefix = resourcePrefix.EndsWith(",") ? resourcePrefix : resourcePrefix + ".";
            resourceNames = new HashSet<string>(assembly.GetManifestResourceNames(), StringComparer.OrdinalIgnoreCase);
        }

        public override object LoadFile(string file, Table globalContext)
        {
            string resourceName = ToResourceName(file);
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"找不到內嵌資源 {resourceName}。");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public override bool ScriptFileExists(string name)
        {
            string resourceName = ToResourceName(name);
            return resourceNames.Contains(resourceName);
        }

        public override string ResolveFileName(string filename, Table globalContext) => filename;

        public override string ResolveModuleName(string modname, Table globalContext) => modname;

        private string ToResourceName(string name)
        {
            string normalized = name.Replace('\\', '.').Replace('/', '.');
            if (!normalized.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                normalized += ".lua";

            if (!normalized.StartsWith(resourcePrefix, StringComparison.Ordinal))
                normalized = resourcePrefix + normalized;

            return normalized;
        }
    }
}
