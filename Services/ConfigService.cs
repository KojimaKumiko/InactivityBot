using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot
{
    public class ConfigService
    {
        public const string configFileName = "config.json";

        [JsonProperty]
        public string Token { get; set; }

        [JsonProperty]
        public string CommandPrefix { get; set; }

        private ConfigService Config { get; set; }

        public async Task<ConfigService> LoadJsonAsync(string fileName, bool loadFresh = false)
        {
            if (Config != null && !loadFresh)
            {
                return Config;
            }

            if (File.Exists(fileName))
            {
                using var sr = new StreamReader(fileName);
                Config = JsonConvert.DeserializeObject<ConfigService>(await sr.ReadToEndAsync());
                return Config;
            }

            SaveJson(fileName);

            return null;
        }

        public void SaveJson(string fileName)
        {
            using var sw = new StreamWriter(fileName);
            sw.Write(JsonConvert.SerializeObject(this));
        }

        public static string[] ListDirectories()
        {
            try
            {
                return Directory.GetDirectories(@"/", "*.*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

            return null;
        }

        public static string[] ListFiles(string path)
        {
            try
            {
                return Directory.GetFiles(@"/", "*.*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

            return null;
        }
    }
}
