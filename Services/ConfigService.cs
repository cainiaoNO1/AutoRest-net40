using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AutoRest.Services
{
    [DataContract]
    public class AppConfig
    {
        [DataMember]
        public bool AutoStartOnBoot { get; set; } = false;

        [DataMember]
        public bool StartMinimizedToTray { get; set; } = false;

        [DataMember]
        public bool MinimizeOnClose { get; set; } = true;

        [DataMember]
        public bool ShowWarningBeforeAction { get; set; } = true;

        [DataMember]
        public bool ForceCloseApps { get; set; } = false;

        [DataMember]
        public int LastAction { get; set; } = 0; // 0: Shutdown, 1: Reboot

        [DataMember]
        public int LastMode { get; set; } = 0; // 0: Countdown, 1: Schedule

        [DataMember]
        public int LastIntervalDays { get; set; } = 1; // 1 ~ 30

        [DataMember]
        public int LastHours { get; set; } = 1;

        [DataMember]
        public int LastMinutes { get; set; } = 30;

        [DataMember]
        public int LastSeconds { get; set; } = 0;

        [DataMember]
        public bool HasActiveTask { get; set; } = false;

        [DataMember]
        public string ActiveTargetTime { get; set; } = string.Empty;

        [DataMember]
        public int ActiveAction { get; set; } = 0;

        [DataMember]
        public int ActiveMode { get; set; } = 0;

        [DataMember]
        public int ActiveIntervalDays { get; set; } = 1;
    }

    public static class ConfigService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoRest");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return new AppConfig();
                }

                using (var stream = new FileStream(ConfigPath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                    var config = serializer.ReadObject(stream) as AppConfig;
                    return config ?? new AppConfig();
                }
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                using (var stream = new FileStream(ConfigPath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                    serializer.WriteObject(stream, config);
                }
            }
            catch
            {
                // 静默忽略落盘异常
            }
        }
    }
}
