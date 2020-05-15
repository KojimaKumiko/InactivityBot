using InactivityBot.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Models
{
    public class CommunityApplicationModel : ISaveable
    {
        public CommunityApplicationModel()
        {
            GuildEmoji = new Dictionary<ulong, string>();
            GuildDestinationChannel = new Dictionary<ulong, ulong>();
            GuildApplicationMessage = new Dictionary<ulong, ulong>();
        }

        public IDictionary<ulong, string> GuildEmoji { get; private set; }
        public IDictionary<ulong, ulong> GuildDestinationChannel { get; private set; }
        public IDictionary<ulong, ulong> GuildApplicationMessage { get; private set; }

        public const string communityApplicationFileName = "inactivity/comApplication.json";

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
                var model = JsonConvert.DeserializeObject<CommunityApplicationModel>(json);

                if (model != null)
                {
                    GuildEmoji = model.GuildEmoji;
                    GuildDestinationChannel = model.GuildDestinationChannel;
                    GuildApplicationMessage = model.GuildApplicationMessage;
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
