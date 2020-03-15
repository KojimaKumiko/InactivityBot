using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Linq;

namespace InactivityBot.Modules
{
    public class BaseModule : ModuleBase<SocketCommandContext>
    {
        public CommandService CommandService { get; set; }

        public IServiceProvider Services { get; set; }

        [Command("Help")]
        [Summary("Returns information about all executable commands.")]
        public async Task Help()
        {
            var executableCommands = await CommandService.GetExecutableCommandsAsync(Context, Services);
            List<string> alreadyListed = new List<string>();

            var embedBuilder = new EmbedBuilder
            {
                Title = "Help!",
                Timestamp = DateTime.Now,
                Color = Color.Blue,
                Description = "Lists all executable commands for the current user! Specify a command to get more information about the command!"
            };

            var applicationInfo = await Context.Client.GetApplicationInfoAsync();
            embedBuilder.WithAuthor(applicationInfo.Owner);

            foreach (var command in executableCommands)
            {
                if (alreadyListed.Contains(command.Name))
                {
                    continue;
                }

                embedBuilder.AddField(command.Name, command.Summary != null ? command.Summary : "No summary available.");

                alreadyListed.Add(command.Name);
            }

            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("Help")]
        [Summary("Returns information about all executable commands.")]
        public async Task Help(string commandName)
        {
            var executableCommands = await CommandService.GetExecutableCommandsAsync(Context, Services);

            var commands = executableCommands.Where(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (commands.Count > 0)
            {
                var embedBuilder = new EmbedBuilder
                {
                    Title = "Help!",
                    Timestamp = DateTime.Now,
                    Color = Color.Blue
                };

                var applicationInfo = await Context.Client.GetApplicationInfoAsync();
                embedBuilder.WithAuthor(applicationInfo.Owner);

                embedBuilder
                    .WithDescription($"`{commands.First().Name}`: {commands.First().Summary}")
                    .AddField("Aliases", string.Join(", ", commands.First().Aliases));

                foreach (var command in commands)
                {
                    string parameterValue = string.Empty;
                    foreach (var parameter in command.Parameters)
                    {
                        string summary = string.IsNullOrWhiteSpace(parameter.Summary) ? "No summary available" : parameter.Summary;
                        parameterValue += $"`{parameter.Name}`: {summary}; Optional: {parameter.IsOptional}\n";
                    }

                    parameterValue = string.IsNullOrWhiteSpace(parameterValue) ? "The command has no parameters." : parameterValue;
                    embedBuilder.AddField("Parameters", parameterValue);
                }

                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync("The requested command was not found.");
            }
        }

        [Command("Directories")]
        [Alias("dir")]
        [RequireOwner]
        public async Task Directories(string path = null)
        {
            string[] result;

            if (string.IsNullOrWhiteSpace(path))
            {
                result = ConfigService.ListDirectories();
            }
            else
            {
                result = ConfigService.ListFiles(path);
            }

            if (result != null)
            {
                await ReplyAsync($"Number of directories and/or files: {result.Length}");
                await ReplyAsync(string.Join('\n', result));
            }
            else
            {
                await ReplyAsync("No directories or files found.");
            }
        }
    }
}
