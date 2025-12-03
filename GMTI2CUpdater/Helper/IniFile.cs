using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GMTI2CUpdater
{
    // 簡單版 INI 讀寫，不用 Win32
    internal class IniFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> _data
            = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public string FilePath { get; }

        public IniFile(string path)
        {
            if (!System.IO.Path.IsPathRooted(path))
                FilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            else
                FilePath = path;

            if (File.Exists(FilePath))
            {
                Load(FilePath);
            }
        }

        #region Public API

        // 讀取字串
        public string Get(string section, string key, string defaultValue = "")
        {
            if (_data.TryGetValue(section, out var sectionDict) &&
                sectionDict.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        // 設定 / 新增
        public void Set(string section, string key, string value)
        {
            if (!_data.TryGetValue(section, out var sectionDict))
            {
                sectionDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _data[section] = sectionDict;
            }

            sectionDict[key] = value;
        }

        // 判斷是否存在
        public bool ContainsKey(string section, string key)
        {
            return _data.TryGetValue(section, out var sectionDict)
                   && sectionDict.ContainsKey(key);
        }

        // 刪除 key
        public void RemoveKey(string section, string key)
        {
            if (_data.TryGetValue(section, out var sectionDict))
            {
                sectionDict.Remove(key);
            }
        }

        // 刪除整個 section
        public void RemoveSection(string section)
        {
            _data.Remove(section);
        }

        // 存回檔案
        public void Save()
        {
            Save(FilePath);
        }

        #endregion

        #region Load / Save

        private void Load(string path)
        {
            _data.Clear();

            string currentSection = "";

            foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                var line = rawLine.Trim();

                if (line.Length == 0)
                    continue;                // 空行

                if (line.StartsWith(";") || line.StartsWith("#"))
                    continue;                // 註解

                // [Section]
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!_data.ContainsKey(currentSection))
                    {
                        _data[currentSection] =
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    continue;
                }

                // key=value
                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;                // 不合法就跳過

                string key = line.Substring(0, eqIndex).Trim();
                string value = line.Substring(eqIndex + 1).Trim();

                if (!_data.ContainsKey(currentSection))
                {
                    _data[currentSection] =
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                _data[currentSection][key] = value;
            }
        }

        private void Save(string path)
        {
            var sb = new StringBuilder();

            foreach (var sectionPair in _data)
            {
                var section = sectionPair.Key;
                var keys = sectionPair.Value;

                if (!string.IsNullOrEmpty(section))
                {
                    sb.AppendLine("[" + section + "]");
                }

                foreach (var kv in keys)
                {
                    sb.AppendLine($"{kv.Key}={kv.Value}");
                }

                sb.AppendLine(); // 區段間空一行
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        #endregion
    }
}
