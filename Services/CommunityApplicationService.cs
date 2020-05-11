using Discord.WebSocket;
using InactivityBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace InactivityBot.Services
{
    public class CommunityApplicationService
    {
        private DiscordSocketClient Client { get; set; }
        private ILogger Logger { get; set; }

        public CommunityApplicationService(DiscordSocketClient client, LoggingService loggingService)
        {
            if (loggingService == null)
            {
                throw new ArgumentNullException(nameof(loggingService));
            }

            Client = client ?? throw new ArgumentNullException(nameof(client));
            Logger = loggingService.Logger;

            ApplicationModel = new CommunityApplicationModel();
        }

        public CommunityApplicationModel ApplicationModel { get; private set; }

        public void SetupApplications(ulong guildId)
        {
            throw new NotImplementedException();
        }
    }
}
