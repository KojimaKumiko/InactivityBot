using Discord.WebSocket;
using InactivityBot.Interfaces;
using InactivityBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Services
{
    public class BaseService
    {
        public BaseService()
        {
            Model = new BaseModel();
        }

        public BaseModel Model { get; private set; }

        public CultureInfo GetGuildCulture(ulong guildId)
        {
            CultureInfo culture;

            if (guildId <= 0)
            {
                throw new ArgumentException("The guild Id must be greater than 0");
            }

            if (!Model.GuildCulture.ContainsKey(guildId))
            {
                // in case the guild/server has no Culture defined or the method was called in dm's, return en-US as default culture.
                culture = new CultureInfo("en-US");
            }
            else
            {
                culture = Model.GuildCulture[guildId];
            }

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            return culture;
        }
    }
}
