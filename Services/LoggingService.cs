using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InactivityBot.Services
{
    public class LoggingService
    {
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
        }

        private Task LogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();

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
    }
}
