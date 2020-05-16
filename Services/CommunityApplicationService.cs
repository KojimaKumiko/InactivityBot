using Discord;
using Discord.WebSocket;
using InactivityBot.Models;
using InactivityBot.Ressources;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InactivityBot.Services
{
    public class CommunityApplicationService
    {
        private DiscordSocketClient Client { get; set; }
        private BaseService BaseService { get; set; }
        private ILogger Logger { get; set; }
        private List<ulong> OngoingUsers { get; set; }

        public CommunityApplicationService(DiscordSocketClient client, BaseService baseService, LoggingService loggingService)
        {
            if (loggingService == null)
            {
                throw new ArgumentNullException(nameof(loggingService));
            }

            Client = client ?? throw new ArgumentNullException(nameof(client));
            BaseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            Logger = loggingService.Logger;

            ReactionAddedPointers = new Dictionary<ulong, Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();
            Model = new CommunityApplicationModel();
            OngoingUsers = new List<ulong>();
        }

        public CommunityApplicationModel Model { get; private set; }
        public IDictionary<ulong, Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>> ReactionAddedPointers { get; private set; }

        public void SetupApplications(ulong guildId)
        {
            if (!ReactionAddedPointers.ContainsKey(guildId))
            {
                Logger.Debug($"Setup inactivity reactions for guild {guildId}");
                Task Func(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction) => ReactionAdded(cachedMessage, channel, reaction, guildId);
                ReactionAddedPointers.Add(guildId, Func);
                Client.ReactionAdded += Func;
            }
        }

        public void CancelApplication(ulong guildId)
        {
            if (ReactionAddedPointers.ContainsKey(guildId))
            {
                Client.ReactionAdded -= ReactionAddedPointers[guildId];
                ReactionAddedPointers.Remove(guildId);
                Logger.Debug($"Canceled inactivity reactions for guild {guildId}");
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction, ulong guildId)
        {
            Logger.Debug("Reaction added");

            CultureInfo culture = BaseService.GetGuildCulture(guildId);

            // Get the message where the reaction was added.
            var message = await cachedMessage.GetOrDownloadAsync();
            if (message != null)
            {
                // Get the user.
                IUser user = reaction.User.Value;

                // Check if the user object is set and if the user is not a bot.
                if (user != null && !user.IsBot)
                {
                    if (OngoingUsers.Contains(user.Id))
                    {
                        Logger.Debug($"User with Id {user.Id} reacted to the message again too soon.");
                        await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
                        return;
                    }

                    OngoingUsers.Add(user.Id);

                    // Check if the reaction was added to the inactivity message.
                    Model.GuildApplicationMessage.TryGetValue(guildId, out var messageId);
                    if (messageId > 0 && message.Id == messageId)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(async () =>
                        {
                            var emoji = new Emoji(reaction.Emote.Name);

                            await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);

                            Model.GuildEmoji.TryGetValue(guildId, out var applicationEmoji);

                            var guildUser = user as IGuildUser;

                            if (emoji.Name == applicationEmoji)
                            {
                                var dmChannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);

                                SocketMessage accountName = null;
                                await dmChannel.SendMessageAsync(Application.AccountName);
                                bool regexMatch = false;

                                do
                                {
                                    if (accountName != null)
                                    {
                                        await dmChannel.SendMessageAsync(Application.AccountName_Incorrect);
                                    }

                                    accountName = await HelperMethods.GetNextMessage(Client, user).ConfigureAwait(false);

                                    if (accountName != null)
                                    {
                                        regexMatch = Regex.IsMatch(accountName.Content, @"[a-zA-Z]+\.\d{4}$");
                                    }
                                }
                                while (accountName != null && !regexMatch);

                                if (accountName == null)
                                {
                                    await dmChannel.SendMessageAsync(Application.Timeout);
                                    OngoingUsers.Remove(user.Id);
                                    return;
                                }

                                await dmChannel.SendMessageAsync(Application.Reason);
                                var applicationReason = await HelperMethods.GetNextMessage(Client, user).ConfigureAwait(false);

                                if (applicationReason == null)
                                {
                                    await dmChannel.SendMessageAsync(Application.Timeout);
                                    OngoingUsers.Remove(user.Id);
                                    return;
                                }

                                await dmChannel.SendMessageAsync(Application.Found);
                                var communityFound = await HelperMethods.GetNextMessage(Client, user).ConfigureAwait(false);

                                Model.GuildDestinationChannel.TryGetValue(guildId, out var channelId);
                                if (await guildUser.Guild.GetChannelAsync(channelId) is ITextChannel channel)
                                {
                                    await dmChannel.SendMessageAsync(Application.Success);

                                    var embedBuilder = new EmbedBuilder();
                                    embedBuilder
                                        .WithAuthor(user)
                                        .AddField(Application.Embed_AccountName, accountName.Content)
                                        .AddField(Application.Embed_Reason, applicationReason.Content)
                                        .AddField(Application.Embed_Found, communityFound?.Content)
                                        .WithTitle(Application.Embed_Title)
                                        .WithColor(Color.LightGrey)
                                        .WithCurrentTimestamp()
                                        .WithDescription(user.Mention);

                                    await channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
                                }
                                else
                                {
                                    var applicationInfo = await Client.GetApplicationInfoAsync();
                                    await dmChannel.SendMessageAsync(string.Format(culture, Inactivity.Error, applicationInfo.Owner.Mention, InactivityError.MissingChannel));
                                }
                            }

                            OngoingUsers.Remove(user.Id);
                        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
            }
        }
    }
}
