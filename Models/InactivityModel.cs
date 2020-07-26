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
    public class InactivityModel : ISaveable
    {
        public InactivityModel()
        {
            GuildInactivityRole = new Dictionary<ulong, ulong>();
            GuildDestinationChannel = new Dictionary<ulong, ulong>();
            GuildActiveEmoji = new Dictionary<ulong, string>();
            GuildInactiveEmoji = new Dictionary<ulong, string>();
            GuildInactivityMessage = new Dictionary<ulong, ulong>();
            GuildRaidRoles = new Dictionary<ulong, List<ulong>>();
            GuildMemberUpdateEvents = new Dictionary<ulong, bool>();
        }

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

        /// <summary>
        /// Gets a collection of raid roles for a given guild.
        /// </summary>
        [JsonProperty]
        public IDictionary<ulong, List<ulong>> GuildRaidRoles { get; private set; }

        /// <summary>
        /// Gets a collection of bools which decide wether or not to listen to the GuildMemberUpdated event.
        /// </summary>
        public IDictionary<ulong, bool> GuildMemberUpdateEvents { get; private set; }

        /// <summary>
        /// Field, storing the base file name for the json file.
        /// </summary>
        public const string inactivityFileName = "inactivity/inactivity.json";

        /// <summary>
        /// Loads a json file by the given file name.
        /// </summary>
        /// <param name="fileName">The file to load.</param>
        /// <returns>The Task to await.</returns>
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
                var model = JsonConvert.DeserializeObject<InactivityModel>(json);

                if (model != null)
                {
                    GuildDestinationChannel = model.GuildDestinationChannel;
                    GuildInactiveEmoji = model.GuildInactiveEmoji;
                    GuildInactivityMessage = model.GuildInactivityMessage;
                    GuildInactivityRole = model.GuildInactivityRole;
                    GuildActiveEmoji = model.GuildActiveEmoji;
                    GuildRaidRoles = model.GuildRaidRoles;
                    GuildMemberUpdateEvents = model.GuildMemberUpdateEvents;
                }
            }
            else
            {
                await SaveJsonAsync(fileName);
            }
        }

        /// <summary>
        /// Saves this Inactivity Model instance as a json file.
        /// </summary>
        /// <param name="fileName">The name to save this instance as.</param>
        /// <returns>The task to await.</returns>
        public async Task SaveJsonAsync(string fileName)
        {
            using var sw = new StreamWriter(fileName, false, Encoding.Unicode);
            await sw.WriteAsync(JsonConvert.SerializeObject(this));
        }
    }
}
