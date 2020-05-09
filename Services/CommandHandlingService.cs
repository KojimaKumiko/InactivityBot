using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _logger = services.GetRequiredService<LoggingService>().Logger;
            _services = services;

            // Hooking into CommandExecuted for post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;

            // hooking into MessageReceived to process each message to see if it's actually a command.
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            int argPos = 0;
            var configService = _services.GetRequiredService<ConfigService>();
            var config = await configService.LoadJsonAsync(ConfigService.configFileName);

            if (config == null)
            {
                return;
            }

            char prefix = config.CommandPrefix.ToCharArray()[0];

            if (!(message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);

            // Perfom the execution of the command that matches the message, if one exists.
            await _commands.ExecuteAsync(context, argPos, _services);

            // Handling of the result happens in the method CommandExecutedAssync.
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found).
            if (!command.IsSpecified)
            {
                return;
            }

            // the command was successfull, we don't care about this result.
            if (result.IsSuccess)
            {
                return;
            }

            _logger.Error($"User {context.User} tried to execute command {command.Value.Name} but it failed. Reason: '{result}'");

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"{result.ErrorReason}");
        }
    }
}
