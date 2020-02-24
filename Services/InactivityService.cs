using Discord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace InactivityBot.Services
{
    public class InactivityService
    {
        public InactivityService ()
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
        public IDictionary<ulong, CultureInfo> GuildCulture { get; private set; }

        /// <summary>
        /// Gets the inactive role for a given guild.
        /// </summary>
        public IDictionary<ulong, IRole> GuildInactivityRole { get; private set; }

        /// <summary>
        /// Gets the destination channel, where the bot will write the notifaction about a new inactivity, for a given guild.
        /// </summary>
        public IDictionary<ulong, ITextChannel> GuildDestinationChannel { get; private set; }

        /// <summary>
        /// Gets the active emoji for the Inactivity Message.
        /// </summary>
        public IDictionary<ulong, Emoji> GuildActiveEmoji { get; private set; }

        /// <summary>
        /// Gets the inactive emoji for the Inactivity Message.
        /// </summary>
        public IDictionary<ulong, Emoji> GuildInactiveEmoji { get; private set; }

        /// <summary>
        /// Gets the inactivity message for a given guild.
        /// </summary>
        public IDictionary<ulong, ulong> GuildInactivityMessage { get; private set; }
    }
}
