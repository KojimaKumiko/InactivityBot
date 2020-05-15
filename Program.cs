using Discord;
using Discord.Commands;
using Discord.WebSocket;
using InactivityBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Reflection;
using InactivityBot.Models;

namespace InactivityBot
{
    public class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private InactivityService inactivityService;
        private CommunityApplicationService communityApplicationService;

        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
#if DEBUG
                LogLevel = LogSeverity.Debug,
#elif RELEASE
                LogLevel = LogSeverity.Info,
#endif
                MessageCacheSize = 50,
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.AlwaysRetry,
            });

            _commands = new CommandService(new CommandServiceConfig
            {
#if DEBUG
                LogLevel = LogSeverity.Debug,
#elif RELEASE
                LogLevel = LogSeverity.Info,
#endif
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true,
            });

            using var services = ConfigureServices();
            Logger = services.GetRequiredService<LoggingService>().Logger;
            var configSerivce = services.GetRequiredService<ConfigService>();
            var client = services.GetRequiredService<DiscordSocketClient>();

            var config = await configSerivce.LoadJsonAsync(ConfigService.configFileName);
            if (config == null)
            {
                Logger.Error("No Config was found.");
                return;
            }

            client.Ready += Client_Ready;

            var baseService = services.GetRequiredService<BaseService>();
            await baseService.Model.LoadJsonAsync(BaseModel.baseFileName);

            inactivityService = services.GetRequiredService<InactivityService>();
            await inactivityService.Model.LoadJsonAsync(InactivityModel.inactivityFileName);

            communityApplicationService = services.GetRequiredService<CommunityApplicationService>();
            await communityApplicationService.Model.LoadJsonAsync(CommunityApplicationModel.communityApplicationFileName);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await Task.Delay(-1);
        }

        public ILogger Logger { get; set; }

        private Task Client_Ready()
        {
            Logger.Information("Client Ready event fired.");

            foreach (var guild in inactivityService.Model.GuildInactivityMessage.Keys)
            {
                inactivityService.SetupInactivity(guild);
            }

            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<LoggingService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<InactivityService>()
                .AddSingleton<CommunityApplicationService>()
                .AddSingleton<BaseService>()
                .BuildServiceProvider();
        }
    }
}
