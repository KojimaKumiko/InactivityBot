using Discord;
using Discord.Commands;
using Discord.WebSocket;
using InactivityBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

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
            services.GetRequiredService<LoggingService>();
            var configSerivce = services.GetRequiredService<ConfigService>();
            var client = services.GetRequiredService<DiscordSocketClient>();

            var config = await configSerivce.LoadJsonAsync(ConfigService.configFileName);
            if (config == null)
            {
                return;
            }

            await services.GetRequiredService<InactivityService>().LoadJsonAsync(InactivityService.inactivityFileName);

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
                .AddSingleton<BaseService>()
                .BuildServiceProvider();
        }
    }
}
