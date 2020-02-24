using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using InactivityBot.Services;

namespace InactivityBot
{
    public class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;

        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 50,
            });

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug,
                CaseSensitiveCommands = false,
            });

            using var services = ConfigureServices();
            services.GetRequiredService<LoggingService>();
            var configSerivce = services.GetRequiredService<ConfigService>();
            var client = services.GetRequiredService<DiscordSocketClient>();

            var config = await configSerivce.LoadJsonAsync(ConfigService.configFileName);
            if (config == null)
            {
                return;
            }

            await services.GetRequiredService<InactivityService>().LoadJson(InactivityService.inactivityFileName);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await Task.Delay(-1);
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
                .BuildServiceProvider();
        }
    }
}
