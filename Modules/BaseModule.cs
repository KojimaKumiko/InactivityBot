using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using System.Linq;
using InactivityBot.Ressources;
using System.Globalization;

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
                Description = Base.Help_Description
            };

            var applicationInfo = await Context.Client.GetApplicationInfoAsync();
            embedBuilder.WithAuthor(applicationInfo.Owner);

            foreach (var command in executableCommands)
            {
                if (alreadyListed.Contains(command.Name))
                {
                    continue;
                }

                embedBuilder.AddField(command.Name, command.Summary != null ? command.Summary : Base.Help_NoSummary);

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
                        string summary = string.IsNullOrWhiteSpace(parameter.Summary) ? Base.Help_NoSummary : parameter.Summary;
                        parameterValue += $"`{parameter.Name}`: {summary}; Optional: {parameter.IsOptional}\n";
                    }

                    parameterValue = string.IsNullOrWhiteSpace(parameterValue) ? Base.Help_NoParameters : parameterValue;
                    embedBuilder.AddField("Parameters", parameterValue);
                }

                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync(Base.Help_CommandNotFound);
            }
        }

        [Command("Author")]
        [Summary("Gets information about the author of this bot.")]
        public async Task Author()
        {
            await Context.Channel.TriggerTypingAsync();

            var embedBuilder = new EmbedBuilder
            {
                Title = "Author",
                Timestamp = DateTime.Now,
                Color = Color.Purple,
                Description = "Information about the author of this bot."
            };

            var emote = Emote.Parse("<:LUL:617742413582172160>");

            var applicationInfo = await Context.Client.GetApplicationInfoAsync();
            embedBuilder.WithAuthor(applicationInfo.Owner);

            embedBuilder
                .AddField("Name", "Kojima Kumiko, Kojima, Koji, Kumiko")
                .AddField("Gw2 Account name", "playerismc.6184")
                .AddField("Race", Base.Author_Race)
                .AddField("Age", "[404 Not Found](https://en.wikipedia.org/wiki/HTTP_404)")
                .AddField("Coffee", string.Format(CultureInfo.InvariantCulture, Base.Author_Coffee, "[418 I'm a teapot](https://developer.mozilla.org/de/docs/Web/HTTP/Status/418)", "<:LUL:688691998395203833>"));

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("problems")]
        [Summary("What to do when you encounter problems with this bot.")]
        public async Task Problems()
        {
            await Context.Channel.TriggerTypingAsync();

            await ReplyAsync(string.Format(CultureInfo.InvariantCulture, Base.Problems, "https://github.com/KojimaKumiko/InactivityBot/issues/new"));
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
