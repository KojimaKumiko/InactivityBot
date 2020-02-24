using Discord;
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
            GuildInactivityRole = new Dictionary<ulong, IRole>();
            GuildDestinationChannel = new Dictionary<ulong, ITextChannel>();
            GuildActiveEmoji = new Dictionary<ulong, Emoji>();
            GuildInactiveEmoji = new Dictionary<ulong, Emoji>();
            GuildInactivityMessage = new Dictionary<ulong, ulong>();
        }

        /// <summary>
        /// Gets the culture for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, CultureInfo> GuildCulture { get; private set; }

        /// <summary>
        /// Gets the inactive role for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, IRole> GuildInactivityRole { get; private set; }

        /// <summary>
        /// Gets the destination channel, where the bot will write the notifaction about a new inactivity, for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, ITextChannel> GuildDestinationChannel { get; private set; }

        /// <summary>
        /// Gets the active emoji for the Inactivity Message.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, Emoji> GuildActiveEmoji { get; private set; }

        /// <summary>
        /// Gets the inactive emoji for the Inactivity Message.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, Emoji> GuildInactiveEmoji { get; private set; }

        /// <summary>
        /// Gets the inactivity message for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, ulong> GuildInactivityMessage { get; private set; }

        /// <summary>
        /// Field, storing the base file name for the json file.
        /// </summary>
        public const string inactivityFileName = "inactivity.json";

        /// <summary>
        /// Gets or sets the cached Inactivity Service.
        /// </summary>
        private InactivityService Inactivity { get; set; }

        /// <summary>
        /// Loads a json file by the given file name and returns an Inactivity Model.
        /// </summary>
        /// <param name="fileName">The file to load.</param>
        /// <param name="loadFresh">Whether or not to load the file or use the cache.</param>
        /// <returns>The Inactivity Model or null if not existing.</returns>
        public async Task<InactivityService> SaveJson(string fileName, bool loadFresh = false)
        {
            if (Inactivity != null && !loadFresh)
            {
                return Inactivity;
            }

            if (File.Exists(fileName))
            {
                using var sr = new StreamReader(fileName);
                Inactivity = JsonConvert.DeserializeObject<InactivityService>(await sr.ReadToEndAsync());
                return Inactivity;
            }

            await SaveJson(fileName);

            return null;
        }

        /// <summary>
        /// Saves this Inactivity Model instance as a json file.
        /// </summary>
        /// <param name="fileName">The name to save this instance as.</param>
        /// <returns>The task to await.</returns>
        public async Task SaveJson(string fileName)
        {
            using var sw = new StreamWriter(fileName);
            await sw.WriteAsync(JsonConvert.SerializeObject(this));
        }
    }
}
