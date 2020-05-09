using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace InactivityBot.Services
{
    public class LoggingService
    {
        public ILogger Logger { get; private set; }

        public LoggingService(DiscordSocketClient client, CommandService command)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            client.Log += LogAsync;
            command.Log += LogAsync;

            Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }

        private Task LogAsync(LogMessage message)
        {
            Logger.Write(GetLogLevel(message.Severity), message.Message);

            //if (message.Exception is CommandException cmdException)
            //{
            //    Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Name}"
            //        + $" failed to execute in {cmdException.Context.Channel}.");
            //    Console.WriteLine(cmdException);
            //}
            //else
            //{
            //    Console.WriteLine($"[General/{message.Severity}] {message}");
            //}

            return Task.CompletedTask;
        }

        private static LogEventLevel GetLogLevel(LogSeverity severity) => (LogEventLevel)Math.Abs((int)severity - 5);
    }
}
