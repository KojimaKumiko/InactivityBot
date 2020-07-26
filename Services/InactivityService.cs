using Discord;
using Discord.Rest;
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
        private BaseService BaseService { get; set; }
        private ILogger Logger { get; set; }
        private List<ulong> OngoingUsers { get; set; }

        public InactivityService(DiscordSocketClient client, BaseService baseService, LoggingService loggingService)
        {
            if (loggingService == null)
            {
                throw new ArgumentNullException(nameof(loggingService));
            }

            Client = client ?? throw new ArgumentNullException(nameof(client));
            BaseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            Logger = loggingService.Logger;

            ReactionAddedPointers = new Dictionary<ulong, Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();
            GuildMemberUpdatedPointers = new Dictionary<ulong, Func<SocketGuildUser, SocketGuildUser, Task>>();
            Model = new InactivityModel();
            OngoingUsers = new List<ulong>();
        }

        public InactivityModel Model { get; set; }

        public IDictionary<ulong, Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>> ReactionAddedPointers { get; private set; }

        public IDictionary<ulong, Func<SocketGuildUser, SocketGuildUser, Task>> GuildMemberUpdatedPointers { get; private set; }

        public void SetupInactivityReaction(ulong guildId)
        {
            if (!ReactionAddedPointers.ContainsKey(guildId))
            {
                Logger.Debug($"Setup inactivity reactions for guild {guildId}");
                Task Func(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction) => ReactionAdded(cachedMessage, channel, reaction, guildId);
                ReactionAddedPointers.Add(guildId, Func);
                Client.ReactionAdded += Func;
            }
        }

        public void CancelInactivityReaction(ulong guildId)
        {
            if (ReactionAddedPointers.ContainsKey(guildId))
            {
                Client.ReactionAdded -= ReactionAddedPointers[guildId];
                ReactionAddedPointers.Remove(guildId);
                Logger.Debug($"Canceled inactivity reactions for guild {guildId}");
            }
        }

        public void SetupGuildMemberUpdated(ulong guildId)
        {
            if (!GuildMemberUpdatedPointers.ContainsKey(guildId))
            {
                Logger.Debug($"Setup up GuildMemberUpdated event for the guild {guildId}");
                Task Func(SocketGuildUser oldUser, SocketGuildUser newUser) => GuildMemberUpdated(oldUser, newUser, guildId);
                GuildMemberUpdatedPointers.Add(guildId, Func);
                Client.GuildMemberUpdated += Func;
            }
        }

        public void RemoveGuildMemberUpdated(ulong guildId)
        {
            if (GuildMemberUpdatedPointers.ContainsKey(guildId))
            {
                Client.GuildMemberUpdated -= GuildMemberUpdatedPointers[guildId];
                GuildMemberUpdatedPointers.Remove(guildId);
                Logger.Debug($"Removed GuildMemberUpdated event for the guild {guildId}");
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction, ulong guildId)
        {
            Logger.Debug("Reaction added");

            CultureInfo culture = BaseService.GetGuildCulture(guildId);
            BaseService.Model.GuildWaitTime.TryGetValue(guildId, out TimeSpan waitTime);

            if (waitTime == null)
            {
                // incase the guild did not set a custom wait time, set a default one with 15 Minutes.
                waitTime = new TimeSpan(0, 15, 0);
            }

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
                        if (OngoingUsers.Contains(user.Id))
                        {
                            Logger.Debug($"User with Id {user.Id} reacted to the message again too soon.");
                            await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
                            return;
                        }

                        OngoingUsers.Add(user.Id);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(async () =>
                        {
                            var applicationInfo = await Client.GetApplicationInfoAsync();

                            try
                            {
                                var emoji = new Emoji(reaction.Emote.Name);

                                await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);

                                Model.GuildInactiveEmoji.TryGetValue(guildId, out var inactiveEmoji);
                                Model.GuildActiveEmoji.TryGetValue(guildId, out var activeEmoji);
                                Model.GuildInactivityRole.TryGetValue(guildId, out var inacRoleId);
                                Model.GuildDestinationChannel.TryGetValue(guildId, out var destChannelId);

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
                                            await dmChannel.SendMessageAsync(Inactivity.AccountName_Incorrect);
                                            Logger.Information($"The User {user.Username} provided an incorrect account name");
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
                                        await dmChannel.SendMessageAsync(Inactivity.Timeout);
                                        OngoingUsers.Remove(user.Id);
                                        Logger.Information($"User {user.Username} provided no account name");
                                        return;
                                    }

                                    Logger.Information($"Account Name: {accountName} from User: {user.Username}");

                                    await dmChannel.SendMessageAsync(Inactivity.Inactivity_Duration);
                                    var inactivityPeriod = await HelperMethods.GetNextMessage(Client, user, waitTime).ConfigureAwait(false);

                                    if (inactivityPeriod == null)
                                    {
                                        await dmChannel.SendMessageAsync(Inactivity.Timeout);
                                        OngoingUsers.Remove(user.Id);
                                        Logger.Information($"User {user.Username} provided no inactivity period");
                                        return;
                                    }

                                    Logger.Information($"Inactivity Period: {inactivityPeriod} from User: {user.Username}");

                                    await dmChannel.SendMessageAsync(Inactivity.Inactivity_Reason);
                                    var reason = await HelperMethods.GetNextMessage(Client, user, waitTime).ConfigureAwait(false);

                                    if (reason == null)
                                    {
                                        await dmChannel.SendMessageAsync(Inactivity.Missing_Reason);
                                        OngoingUsers.Remove(user.Id);
                                        Logger.Information($"User {user.Username} provided no reason");
                                        return;
                                    }

                                    Logger.Information($"Reason: {reason} from User: {user.Username}");

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

                                        if (inacRoleId > 0)
                                        {
                                            var inactivityRole = guildUser.Guild.Roles.Single(r => r.Id == inacRoleId);
                                            if (inactivityRole != null)
                                            {
                                                await guildUser.AddRoleAsync(inactivityRole);
                                                //embedBuilder.AddField(string.Empty, Inactivity.InactivityRole_Added);
                                                embedBuilder.WithFooter(Inactivity.InactivityRole_Added);
                                                Logger.Debug($"Added Role {inactivityRole} to User {user.Username}");
                                            }
                                        }
                                        await channel.SendMessageAsync(text: string.Join(" ", raids), embed: embedBuilder.Build()).ConfigureAwait(false);

                                        Logger.Debug("Successfully created the Inactivity Embed");
                                    }
                                    else
                                    {
                                        await dmChannel.SendMessageAsync(string.Format(culture, Inactivity.Error, applicationInfo.Owner.Mention, InactivityError.MissingChannel));
                                    }
                                }
                                else if (emoji.Name == activeEmoji)
                                {
                                    if (inacRoleId > 0 && guildUser.RoleIds.Contains(inacRoleId))
                                    {
                                        var role = guildUser.Guild.Roles.Single(r => r.Id == inacRoleId);
                                        await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
                                        await guildUser.SendMessageAsync(Inactivity.Inactivity_Active).ConfigureAwait(false);

                                        if (await guildUser.Guild.GetChannelAsync(destChannelId) is ITextChannel channel)
                                        {
                                            await channel.SendMessageAsync(string.Format(culture, Inactivity.Inactivity_Active_Lead, user.Mention)).ConfigureAwait(false);
                                        }
                                    }
                                }

                                OngoingUsers.Remove(user.Id);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex.ToString());
                                await applicationInfo.Owner.SendMessageAsync($"Exception Message: {ex.Message}\nException: {ex}");
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

        private async Task GuildMemberUpdated(SocketGuildUser oldUser, SocketGuildUser newUser, ulong guildId)
        {
            if (oldUser.IsBot)
            {
                return;
            }

            var applicationInfo = await Client.GetApplicationInfoAsync();

            var newRoles = newUser.Roles.Except(oldUser.Roles);

            try
            {
                Model.GuildInactivityRole.TryGetValue(guildId, out var inactiveRoleId);
                var inactiveRole = newUser.Guild.GetRole(inactiveRoleId);

                if (newRoles.Any(r => r.Name.Equals(inactiveRole.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    CultureInfo culture = BaseService.GetGuildCulture(guildId);

                    Logger.Debug($"Searching audit logs for the user {newUser.Username}");
                    var asyncAuditLogs = newUser.Guild.GetAuditLogsAsync(10, actionType: ActionType.MemberRoleUpdated)
                        .Where(a => a.Any(l => ((MemberRoleAuditLogData)l.Data).Target.Id == newUser.Id));
                    await foreach (var auditLogs in asyncAuditLogs)
                    {
                        if (auditLogs.Count <= 0)
                        {
                            Logger.Debug($"No audit logs were found.");
                            return;
                        }

                        var auditLog = auditLogs.First();

                        if (auditLog.User.IsBot)
                        {
                            Logger.Debug($"The user {newUser.Username} was set inactive by the bot.");
                            return;
                        }

                        Logger.Debug($"The user {newUser.Username} was set inactive by {auditLog.User.Username}");

                        List<string> raids = new List<string>();
                        Model.GuildRaidRoles.TryGetValue(guildId, out var guildRoles);
                        if (guildRoles != null)
                        {
                            var raidIds = newUser.Roles.Where(r => guildRoles.Contains(r.Id)).Select(r => r.Id);
                            raids = newUser.Guild.Roles.Where(r => raidIds.Contains(r.Id)).Select(r => r.Mention).ToList();
                        }

                        Model.GuildDestinationChannel.TryGetValue(guildId, out var channelId);
                        if (newUser.Guild.GetChannel(channelId) is ITextChannel channel)
                        {
                            string description = string.Format(culture, Inactivity.Inactivity_Forced, newUser.Mention, auditLog.User.Mention);
                            var embedBuilder = new EmbedBuilder();
                            embedBuilder
                                .WithAuthor(newUser)
                                .WithTitle(Inactivity.Inactivity_Embed_Title)
                                .WithDescription(description)
                                .WithCurrentTimestamp()
                                .WithColor(Color.LightGrey);

                            if (raids.Count > 0)
                            {
                                embedBuilder.AddField(Inactivity.Inactivity_Embed_Raids, string.Join(" ", raids));
                            }

                            await channel.SendMessageAsync(text: string.Join(" ", raids), embed: embedBuilder.Build()).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An exception occured!");
                await applicationInfo.Owner.SendMessageAsync($"Exception Message: {ex.Message}\nException: {ex}");
            }
        }
    }
}
