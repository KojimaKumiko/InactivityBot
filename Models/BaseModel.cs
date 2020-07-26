using InactivityBot.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Models
{
    public class BaseModel : ISaveable
    {
        public BaseModel()
        {
            GuildCulture = new Dictionary<ulong, CultureInfo>();
            UserCulture = new Dictionary<ulong, CultureInfo>();
            GuildWaitTime = new Dictionary<ulong, TimeSpan>();
        }

        /// <summary>
        /// Gets the culture for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, CultureInfo> GuildCulture { get; private set; }

        [JsonProperty]
        public IDictionary<ulong, CultureInfo> UserCulture { get; private set; }

        [JsonProperty]
        public IDictionary<ulong, TimeSpan> GuildWaitTime { get; private set; }

        [JsonIgnore]
        public const string baseFileName = "inactivity/baseService.json";

        public async Task LoadJsonAsync(string fileName)
        {
            if (!Directory.Exists("inactivity"))
            {
                Directory.CreateDirectory("inactivity");
            }

            if (File.Exists(fileName))
            {
                using var sr = new StreamReader(fileName, Encoding.Unicode);
                var json = await sr.ReadToEndAsync();
                var model = JsonConvert.DeserializeObject<BaseModel>(json);

                if (model != null)
                {
                    GuildCulture = model.GuildCulture;
                    UserCulture = model.UserCulture;
                    GuildWaitTime = model.GuildWaitTime;
                }
            }
            else
            {
                await SaveJsonAsync(fileName);
            }
        }

        public async Task SaveJsonAsync(string fileName)
        {
            using var sw = new StreamWriter(fileName, false, Encoding.Unicode);
            await sw.WriteAsync(JsonConvert.SerializeObject(this));
        }
    }
}
