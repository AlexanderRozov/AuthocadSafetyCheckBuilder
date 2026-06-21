using Demo.Abstractions;
using Demo.Models;
using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Demo.Services.Hotkeys
{
    public sealed class HotkeyConfigFileRepository : IHotkeyConfigRepository
    {
        private const string ModuleFolder = ".ptautocad_module";
        private const string ConfigFileName = "hotkeys.json";

        public string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ModuleFolder,
            ConfigFileName);

        public HotkeyUserConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return CreateDefault();

                var json = File.ReadAllText(ConfigFilePath);
                var config = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                    .Deserialize<HotkeyUserConfig>(json);

                return Normalize(config ?? CreateDefault());
            }
            catch
            {
                return CreateDefault();
            }
        }

        public void Save(HotkeyUserConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var normalized = Normalize(config);
            var json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(normalized);
            File.WriteAllText(ConfigFilePath, json);
        }

        private static HotkeyUserConfig CreateDefault()
        {
            return new HotkeyUserConfig
            {
                Enabled = true,
                ModeTriggerKey = "F11",
                TimeoutSeconds = 8,
                Bindings =
                {
                    new HotkeyBindingEntry { Key = "D1", ListKind = "device", ItemId = "bth" },
                    new HotkeyBindingEntry { Key = "D2", ListKind = "device", ItemId = "btm" },
                    new HotkeyBindingEntry { Key = "D3", ListKind = "device", ItemId = "bial" },
                    new HotkeyBindingEntry { Key = "C", ListKind = "shape", ItemId = "circle" }
                }
            };
        }

        private static HotkeyUserConfig Normalize(HotkeyUserConfig config)
        {
            if (config.Bindings == null)
                config.Bindings = new System.Collections.Generic.List<HotkeyBindingEntry>();

            if (string.IsNullOrWhiteSpace(config.ModeTriggerKey))
                config.ModeTriggerKey = "F11";
            else if (string.Equals(config.ModeTriggerKey, "F12", StringComparison.OrdinalIgnoreCase))
                config.ModeTriggerKey = "F11";

            if (config.TimeoutSeconds <= 0)
                config.TimeoutSeconds = 8;

            return config;
        }
    }
}
