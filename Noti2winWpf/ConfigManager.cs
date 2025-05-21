using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noti2winWpf
{
    public class AppConfig
    {
        public string QQPath { get; set; } = string.Empty;
        public string WeChatPath { get; set; } = string.Empty;
        public int AutoMinimize { get; set; } = 1;
    }

    public static class ConfigManager
    {
        // 配置文件路径，这里放在应用程序基目录下
        private static readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appConfig.json");

        // 加载配置，如果文件不存在则创建默认配置文件
        public static AppConfig LoadConfig()
        {
            if (!File.Exists(configFilePath))
            {
                var defaultConfig = new AppConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(configFilePath);
                    return JsonConvert.DeserializeObject<AppConfig>(json);
                }
                catch (Exception e) {
                    Utils.WriteLog($"Failed to load config file: {e.Message}", Utils.LogErr);
                    var defaultConfig = new AppConfig();
                    SaveConfig(defaultConfig);
                    return defaultConfig;
                }
            
            }
        }

        // 保存配置
        public static void SaveConfig(AppConfig config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configFilePath, json);
        }
    }
}
