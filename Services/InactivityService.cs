using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Services
{
    public class InactivityService
    {
        public InactivityService()
        {
            GuildCulture = new Dictionary<ulong, CultureInfo>();
            GuildInactivityRole = new Dictionary<ulong, ulong>();
            GuildDestinationChannel = new Dictionary<ulong, ulong>();
            GuildActiveEmoji = new Dictionary<ulong, string>();
            GuildInactiveEmoji = new Dictionary<ulong, string>();
            GuildInactivityMessage = new Dictionary<ulong, ulong>();
        }

        // TODO: Fix circular reference loop.

        /// <summary>
        /// Gets the culture for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, CultureInfo> GuildCulture { get; private set; }

        /// <summary>
        /// Gets the inactive role for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, ulong> GuildInactivityRole { get; private set; }

        /// <summary>
        /// Gets the destination channel, where the bot will write the notifaction about a new inactivity, for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, ulong> GuildDestinationChannel { get; private set; }

        /// <summary>
        /// Gets the active emoji for the Inactivity Message.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, string> GuildActiveEmoji { get; private set; }

        /// <summary>
        /// Gets the inactive emoji for the Inactivity Message.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, string> GuildInactiveEmoji { get; private set; }

        /// <summary>
        /// Gets the inactivity message for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, ulong> GuildInactivityMessage { get; private set; }

        [JsonIgnore]
        public Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionAddedPointer { get; set; }

        /// <summary>
        /// Field, storing the base file name for the json file.
        /// </summary>
        public const string inactivityFileName = "inactivity/inactivity.json";

        /// <summary>
        /// Loads a json file by the given file name.
        /// </summary>
        /// <param name="fileName">The file to load.</param>
        /// <returns>The Task to await.</returns>
        public async Task LoadJson(string fileName)
        {
            if (!Directory.Exists("inactivity"))
            {
                Directory.CreateDirectory("inactivity");
            }

            if (File.Exists(fileName))
            {
                using var sr = new StreamReader(fileName, Encoding.Unicode);
                var json = await sr.ReadToEndAsync();
                var model = JsonConvert.DeserializeObject<InactivityService>(json);

                if (model != null)
                {
                    GuildCulture = model.GuildCulture;
                    GuildDestinationChannel = model.GuildDestinationChannel;
                    GuildInactiveEmoji = model.GuildInactiveEmoji;
                    GuildInactivityMessage = model.GuildInactivityMessage;
                    GuildInactivityRole = model.GuildInactivityRole;
                    GuildActiveEmoji = model.GuildActiveEmoji;
                }
            }
            else
            {
                await SaveJson(fileName);
            }
        }

        /// <summary>
        /// Saves this Inactivity Model instance as a json file.
        /// </summary>
        /// <param name="fileName">The name to save this instance as.</param>
        /// <returns>The task to await.</returns>
        public async Task SaveJson(string fileName)
        {
            using var sw = new StreamWriter(fileName, false, Encoding.Unicode);
            await sw.WriteAsync(JsonConvert.SerializeObject(this));
        }
    }
}
