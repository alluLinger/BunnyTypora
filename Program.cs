using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BunnyCDN.Net.Storage;

namespace BunnyTypora
{
    public class Config
    {
        public string storageZoneName { get; set; } = "";
        public string apiAccessKey { get; set; } = "";
        public string mainReplicationRegion { get; set; } = "de";
        
        public string path { get; set; } = "";
        public string customDomain {get; set;} = "";
    }
    
    class Program
    {
        private static string _configPath;
        
        static async Task Main(string[] args)
        {
            _configPath = System.AppDomain.CurrentDomain.BaseDirectory + "/config.json";
            if (!File.Exists(_configPath))
            {
                Console.WriteLine("Config not exist!");
                return;
            }

            var configText = File.ReadAllText(_configPath);
            
            Config config = JsonSerializer.Deserialize<Config>(configText);
            config ??= new Config();
            if (!CheckConfig(config))
            {
                Console.WriteLine("Invalid Config");
                return;
            }
            
            Console.WriteLine($"{config.apiAccessKey} {config.storageZoneName} {config.mainReplicationRegion}");
            var bunnyCDNStorage = new BunnyCDNStorage(config.storageZoneName, config.apiAccessKey, config.mainReplicationRegion);
            
            foreach(var file in args)
            {
                FileInfo info = new FileInfo(file);
                if(!info.Exists) Console.WriteLine($"{info.FullName} not exists!");
                string fileUploadPath = config.path + "/" + info.Name;
                if (fileUploadPath.StartsWith('/')) fileUploadPath = fileUploadPath.Remove(0, 1);
                
                string path = $"/{config.storageZoneName}/{fileUploadPath}";
                Stream stream = new FileStream(file, FileMode.Open);
                await bunnyCDNStorage.UploadAsync(stream, path);

                if(!string.IsNullOrWhiteSpace(config.customDomain)) {
                    Console.WriteLine(config.customDomain + "/" + fileUploadPath);
                } else {
                    Console.WriteLine(config.storageZoneName + ".b-cdn.net/" + fileUploadPath);
                }
            }
        }

        static private bool CheckConfig(Config config)
        {
            bool isValid = true;
            
            // path can be empty, but cannot ends with '/'
            if (!string.IsNullOrWhiteSpace(config.path))
            {
                while (config.path.EndsWith('/'))
                {
                    config.path.Remove(config.path.LastIndexOf('/'), 1);
                }
            }
            else
            {
                config.path = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(config.apiAccessKey)) isValid = false;
            if (string.IsNullOrWhiteSpace(config.mainReplicationRegion)) isValid = false;
            if (string.IsNullOrWhiteSpace(config.storageZoneName)) isValid = false;

            if (!string.IsNullOrWhiteSpace(config.customDomain)) 
            {
                while (config.customDomain.EndsWith('/'))
                {
                    config.customDomain.Remove(config.path.LastIndexOf('/'), 1);
                }

                if (!config.customDomain.StartsWith("http"))
                {
                    config.customDomain.Insert(0, "http://");
                }
            }
            return isValid;
        }
    }
}
