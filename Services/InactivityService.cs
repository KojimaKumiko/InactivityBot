using Discord;
using Discord.WebSocket;
using InactivityBot.Interfaces;
using InactivityBot.Models;
using InactivityBot.Ressources;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InactivityBot.Services
{
    public class InactivityService
    {
        private DiscordSocketClient Client { get; set; }
        private ILogger Logger { get; set; }

        public InactivityService(DiscordSocketClient client, LoggingService loggingService)
        {
            if (loggingService == null)
            {
                throw new ArgumentNullException(nameof(loggingService));
            }

            Client = client ?? throw new ArgumentNullException(nameof(client));
            Logger = loggingService.Logger;

            ReactionAddedPointers = new Dictionary<ulong, Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();
            Model = new InactivityModel();
        }

        public InactivityModel Model { get; set; }

        public IDictionary<ulong, Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>> ReactionAddedPointers { get; private set; }

        public void SetupInactivity(ulong guildId)
        {
            if (!ReactionAddedPointers.ContainsKey(guildId))
            {
                Logger.Debug($"Setup inactivity reactions for guild {guildId}");
                Task Func(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction) => ReactionAdded(cachedMessage, channel, reaction, guildId);
                ReactionAddedPointers.Add(guildId, Func);
                Client.ReactionAdded += Func;
            }
        }

        public void CancelInactivity(ulong guildId)
        {
            if (ReactionAddedPointers.ContainsKey(guildId))
            {
                Client.ReactionAdded -= ReactionAddedPointers[guildId];
                ReactionAddedPointers.Remove(guildId);
                Logger.Debug($"Canceled inactivity reactions for guild {guildId}");
            }
        }

        public async Task<SocketMessage> GetNextMessage(IUser user)
        {
            var taskSource = new TaskCompletionSource<SocketMessage>();

            Task Func(SocketMessage msg) => MessageReceived(msg, user, taskSource);

            Client.MessageReceived += Func;

            var source = taskSource.Task;
            var delay = Task.Delay(TimeSpan.FromSeconds(120));
            var task = await Task.WhenAny(source, delay).ConfigureAwait(false);

            Client.MessageReceived -= Func;

            return task == source ? await source : null;
        }

        public CultureInfo GetGuildCulture(SocketGuild guild)
        {
            CultureInfo culture;

            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (!Model.GuildCulture.ContainsKey(guild.Id))
            {
                // in case the guild/server has no Culture defined or the method was called in dm's, return en-US as default culture.
                culture = new CultureInfo("en-US");
            }
            else
            {
                culture = Model.GuildCulture[guild.Id];
            }

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            return culture;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction, ulong guildId)
        {
            Logger.Debug("Reaction added");

            CultureInfo culture = Model.GuildCulture[guildId];

            // Get the message where the reaction was added.
            var message = await cachedMessage.GetOrDownloadAsync();
            if (message != null)
            {
                // Get the user.
                IUser user = reaction.User.Value;

                // Check if the user object is set and if the user is not a bot.
                if (user != null && !user.IsBot)
                {
                    // Check if the reaction was added to the inactivity message.
                    Model.GuildInactivityMessage.TryGetValue(guildId, out var messageId);
                    if (messageId > 0 && message.Id == messageId)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(async () =>
                        {
                            var emoji = new Emoji(reaction.Emote.Name);

                            await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);

                            Model.GuildInactiveEmoji.TryGetValue(guildId, out var inactiveEmoji);
                            Model.GuildActiveEmoji.TryGetValue(guildId, out var activeEmoji);

                            var guildUser = user as IGuildUser;

                            if (emoji.Name == inactiveEmoji)
                            {
                                var dmChannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);

                                SocketMessage accountName = null;
                                await dmChannel.SendMessageAsync(Inactivity.Inactivity_AccountName);
                                bool regexMatch = false;

                                do
                                {
                                    if (accountName != null)
                                    {
                                        await dmChannel.SendMessageAsync(Inactivity.Inactivity_AccountName_DigitsMissing);
                                    }

                                    accountName = await GetNextMessage(user).ConfigureAwait(false);

                                    if (accountName != null)
                                    {
                                        regexMatch = Regex.IsMatch(accountName.Content, @"\.\d{4}$");
                                    }
                                }
                                while (accountName != null && !regexMatch);

                                if (accountName == null)
                                {
                                    await dmChannel.SendMessageAsync(Inactivity.Timeout);
                                    return;
                                }

                                await dmChannel.SendMessageAsync(Inactivity.Inactivity_Duration);
                                var inactivityPeriod = await GetNextMessage(user).ConfigureAwait(false);

                                if (inactivityPeriod == null)
                                {
                                    await dmChannel.SendMessageAsync(Inactivity.Timeout);
                                    return;
                                }

                                await dmChannel.SendMessageAsync(Inactivity.Inactivity_Reason);
                                var reason = await GetNextMessage(user).ConfigureAwait(false);

                                if (reason == null)
                                {
                                    await dmChannel.SendMessageAsync(Inactivity.Missing_Reason);
                                    return;
                                }

                                List<string> raids = new List<string>();
                                Model.GuildRaidRoles.TryGetValue(guildId, out var guildRoles);
                                if (guildRoles != null)
                                {
                                    var raidIds = guildUser.RoleIds.Where(r => guildRoles.Contains(r));
                                    raids = guildUser.Guild.Roles.Where(r => raidIds.Contains(r.Id)).Select(r => r.Mention).ToList();
                                }

                                Model.GuildDestinationChannel.TryGetValue(guildId, out var channelId);
                                if (await guildUser.Guild.GetChannelAsync(channelId) is ITextChannel channel)
                                {
                                    await dmChannel.SendMessageAsync(Inactivity.Inactive_Success);

                                    var embedBuilder = new EmbedBuilder();
                                    embedBuilder
                                        .WithAuthor(user)
                                        .AddField(Inactivity.Inactivity_Embed_AccountName, accountName.Content)
                                        .AddField(Inactivity.Inactivity_Embed_Period, inactivityPeriod.Content)
                                        .AddField(Inactivity.Inactivity_Embed_Reason, reason.Content)
                                        .WithColor(Color.LightGrey)
                                        .WithCurrentTimestamp()
                                        .WithTitle(Inactivity.Inactivity_Embed_Title)
                                        .WithDescription(user.Mention);

                                    if (raids.Count > 0)
                                    {
                                        embedBuilder.AddField(Inactivity.Inactivity_Embed_Raids, string.Join(" ", raids));
                                    }

                                    await channel.SendMessageAsync(text: string.Join(" ", raids), embed: embedBuilder.Build()).ConfigureAwait(false);
                                }
                                else
                                {
                                    var applicationInfo = await Client.GetApplicationInfoAsync();
                                    await dmChannel.SendMessageAsync(string.Format(culture, Inactivity.Error, applicationInfo.Owner.Mention, InactivityError.MissingChannel));
                                }
                            }
                            else if (emoji.Name == activeEmoji)
                            {
                                Model.GuildInactivityRole.TryGetValue(guildId, out var roleId);
                                Model.GuildDestinationChannel.TryGetValue(guildId, out var channelId);

                                if (roleId > 0 && guildUser.RoleIds.Contains(roleId))
                                {
                                    var role = guildUser.Guild.Roles.Single(r => r.Id == roleId);
                                    await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
                                    await guildUser.SendMessageAsync(Inactivity.Inactivity_Active).ConfigureAwait(false);

                                    if (await guildUser.Guild.GetChannelAsync(channelId) is ITextChannel channel)
                                    {
                                        await channel.SendMessageAsync(string.Format(culture, Inactivity.Inactivity_Active_Lead, user.Mention)).ConfigureAwait(false);
                                    }
                                }
                            }
                        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
            }
        }

        private async Task MessageReceived(SocketMessage message, IUser user, TaskCompletionSource<SocketMessage> taskSource)
        {
            if (!(message.Channel is IDMChannel))
            {
                return;
            }

            if (message.Author.Id != user.Id)
            {
                return;
            }

            taskSource.SetResult(message);
        }
    }
}
