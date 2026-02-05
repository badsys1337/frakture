using System;
using System.IO;
using System.Text.Json;

namespace TriggerLAG.Core
{
    public class AppConfig
    {
        public int ModeIndex { get; set; } = 0;
        public bool Inbound { get; set; } = true;
        public bool Outbound { get; set; } = true;
        public double Intensity { get; set; } = 50;
        public bool MacroEnabled { get; set; } = false;
        public int BoundKey { get; set; } = 0;
        public int BoundMouseBtn { get; set; } = 0;
        public string BoundKeyName { get; set; } = "None";
        public bool FortniteOnly { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = false;
        public int NotificationStyle { get; set; } = 0; 
        public int HotkeyMode { get; set; } = 0; 
        
        
        public bool DoubleEditEnabled { get; set; } = false;
        public int DoubleEditKey { get; set; } = 0; 
        public int DoubleEditKeyEdit { get; set; } = 0; 
        public int DoubleEditKeyConfirm { get; set; } = 0; 
        public int DoubleEditMouseBtn { get; set; } = 0;
        public int DoubleEditKeyEditMouse { get; set; } = 0;
        public int DoubleEditKeyConfirmMouse { get; set; } = 0;
        public int DoubleEditMode { get; set; } = 1; 
        public int DoubleEditDelay { get; set; } = 30; 
        public int DoubleEditDuration { get; set; } = 30; 

        
        public bool ItemCollectEnabled { get; set; } = false;
        public int ItemCollectTriggerKey { get; set; } = 0;
        public int ItemCollectTriggerMouseBtn { get; set; } = 0;
        public int ItemCollectBindKey { get; set; } = 0;
        public int ItemCollectBindMouseBtn { get; set; } = 0;
        public int ItemCollectMode { get; set; } = 1; 
        public string Language { get; set; } = "en";
    }

    public static class ConfigManager
    {
        private static readonly string ConfigPath = "config.json";

        public static void Save(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
            return new AppConfig();
        }
    }
}
