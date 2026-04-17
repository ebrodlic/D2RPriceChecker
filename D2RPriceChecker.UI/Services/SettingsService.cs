using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;

namespace D2RPriceChecker.Services
{
    public class SettingsService
    {
        private readonly string _filePath;

        public AppSettings Settings { get; private set; }

        public SettingsService(string rootDir)
        {
            _filePath = Path.Combine(rootDir, "settings.json");

            Settings = new AppSettings();
        }
        public void Initialize()
        {
            if (!File.Exists(_filePath))
            {
                Save();
            }
            else
            {
                Load();
            }
        }

        public void Load()
        {
            var json = File.ReadAllText(_filePath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }

    public class AppSettings
    {
        public bool AutoCheckForUpdates { get; set; } = true;
        public bool SaveImagesToDisk { get; set; } = true;
    }
}
