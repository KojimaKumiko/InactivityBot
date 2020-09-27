using Discord;
using Discord.WebSocket;
using InactivityBot.Models;
using InactivityBot.Ressources;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                Logger.Debug($"Setup application reactions for guild {guildId}");
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
                Logger.Debug($"Canceled application reactions for guild {guildId}");
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction, ulong guildId)
        {
            Logger.Debug("Reaction added");

            CultureInfo culture = BaseService.GetGuildCulture(guildId);
            BaseService.Model.GuildWaitTime.TryGetValue(guildId, out TimeSpan waitTime);

            if (waitTime == null)
            {
                // in case the guild did not set a custom wait time, set a default one with 15 Minutes.
                waitTime = new TimeSpan(0, 15, 0);
            }

            // Get the message where the reaction was added.
            var message = await cachedMessage.GetOrDownloadAsync();
            if (message == null)
            {
                Logger.Error("Could not find the cached message");
            }

            // Get the user.
            IUser user = reaction.User.Value;

            // If the User object is not set or a bot reacted to the message, don't do anything and just return.
            if (user == null || user.IsBot)
            {
                string warningMessage = user == null ? "The user object was not set" : $"The bot {user.Username} reacted to the message";
                Logger.Warning(warningMessage);
                return;
            }

            // Check if the reaction was added to the inactivity message.
            Model.GuildApplicationMessage.TryGetValue(guildId, out var messageId);
            if (messageId > 0 && message.Id == messageId)
            {
                if (OngoingUsers.Contains(user.Id))
                {
                    Logger.Information($"User {user.Username} with Id {user.Id} reacted to the message again too soon.");
                    await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
                    return;
                }

                Logger.Information($"User {user.Username} ({user.Id}) is applying for the community.");

                OngoingUsers.Add(user.Id);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
                {
                    var applicationInfo = await Client.GetApplicationInfoAsync();

                    try
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
                                    Logger.Information($"User {user.Username} provided an incorrect account name; The provided account name: {accountName}");
                                }

                                accountName = await HelperMethods.GetNextMessage(Client, user, waitTime).ConfigureAwait(false);

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

                            Logger.Information($"Account Name: {accountName} from User: {user.Username}");

                            await dmChannel.SendMessageAsync(Application.Found);
                            var communityFound = await HelperMethods.GetNextMessage(Client, user, waitTime).ConfigureAwait(false);

                            if (communityFound == null)
                            {
                                await dmChannel.SendMessageAsync(Application.Timeout);
                                OngoingUsers.Remove(user.Id);
                                return;
                            }

                            Logger.Information($"Community Found: {communityFound} from User: {user.Username}");

                            await dmChannel.SendMessageAsync(Application.SkillLevel);
                            var applicationSkillLevel = await HelperMethods.GetNextMessage(Client, user, waitTime).ConfigureAwait(false);

                            if (applicationSkillLevel == null)
                            {
                                await dmChannel.SendMessageAsync(Application.Timeout);
                                OngoingUsers.Remove(user.Id);
                                return;
                            }

                            Logger.Information($"Skill Level: {applicationSkillLevel} from User: {user.Username}");

                            Model.GuildDestinationChannel.TryGetValue(guildId, out var channelId);
                            if (await guildUser.Guild.GetChannelAsync(channelId) is ITextChannel channel)
                            {
                                var successEmbed = new EmbedBuilder();
                                successEmbed.WithDescription(Application.Success);

                                await dmChannel.SendMessageAsync(embed: successEmbed.Build());

                                string mention;
                                Model.GuildRoleToMention.TryGetValue(guildId, out ulong roleId);
                                if (user.Id != applicationInfo.Owner.Id && roleId > 0)
                                {
                                    var role = guildUser.Guild.GetRole(roleId);
                                    mention = role.Mention;
                                }
                                else
                                {
                                    mention = applicationInfo.Owner.Mention;
                                }

                                var embedBuilder = new EmbedBuilder();
                                embedBuilder
                                    .WithAuthor(user)
                                    .AddField(Application.Embed_AccountName, accountName.Content)
                                    .AddField(Application.Embed_Found, communityFound?.Content)
                                    .AddField(Application.Embed_SkillLevel, applicationSkillLevel.Content)
                                    .WithTitle(Application.Embed_Title)
                                    .WithColor(Color.LightGrey)
                                    .WithCurrentTimestamp()
                                    .WithDescription(user.Mention);

                                var resultMsg = await channel.SendMessageAsync(text: mention, embed: embedBuilder.Build()).ConfigureAwait(false);

                                var emoteTrue = HelperMethods.GetGuildEmote(Client, "raid_true");
                                var emoteFalse = HelperMethods.GetGuildEmote(Client, "raid_false");

                                if (emoteTrue != null)
                                {
                                    await resultMsg.AddReactionAsync(emoteTrue);
                                }
                                else
                                {
                                    Logger.Debug($"Missing + Reaction/Emote");
                                    await resultMsg.AddReactionAsync(new Emoji("\u2705")); // ✅
                                }

                                if (emoteFalse != null)
                                {
                                    await resultMsg.AddReactionAsync(emoteFalse);
                                }
                                else
                                {
                                    Logger.Debug($"Missing - Reaction/Emote");
                                    await resultMsg.AddReactionAsync(new Emoji("\u274C")); // ❌
                                }
                            }
                            else
                            {
                                Logger.Error("Missing the destination channel");
                                string message = string.Format(culture, Application.Error, applicationInfo.Owner.Mention);
                                await dmChannel.SendMessageAsync(message);
                                await applicationInfo.Owner.SendMessageAsync(message + "\n" + InactivityError.MissingChannel);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.ToString());
                        await applicationInfo.Owner.SendMessageAsync($"Exception: {ex}\nMessage: {ex.Message}");
                    }
                    finally
                    {
                        Logger.Debug($"Released user {user.Username} ({user.Id})");
                        OngoingUsers.Remove(user.Id);
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
    }
}
