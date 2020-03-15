using Discord;
using Discord.Commands;
using Discord.WebSocket;
using InactivityBot.Ressources;
using InactivityBot.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InactivityBot
{
    [RequireGuildChat]
    [RequireUserPermission(GuildPermission.ManageChannels | GuildPermission.ManageMessages)]
    public class InactivityModule : ModuleBase<SocketCommandContext>
    {
        public InactivityService InactivityModel { get; set; }
        public DiscordSocketClient Client { get; set; }

        [Command("inactivity")]
        [Summary("Sends a message with reactions and reacts to them.")]
        public async Task InactivityAsync()
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            await Context.Message.DeleteAsync();

            ulong guildId = Context.Guild.Id;
            InactivityModel.GuildDestinationChannel.TryGetValue(guildId, out var destChannel);
            if (destChannel <= 0)
            {
                await ReplyAsync(Inactivity.Inactivity_MissingChannel);
                return;
            }

            InactivityModel.GuildInactivityRole.TryGetValue(guildId, out var role);
            if (role <= 0)
            {
                await ReplyAsync(Inactivity.Inactivity_MissingRole);
                return;
            }

            InactivityModel.GuildActiveEmoji.TryGetValue(guildId, out var activeEmoji);
            InactivityModel.GuildInactiveEmoji.TryGetValue(guildId, out var inactiveEmoji);

            if (string.IsNullOrWhiteSpace(activeEmoji))
            {
                activeEmoji = "\u25B6";
                InactivityModel.GuildActiveEmoji.Add(guildId, activeEmoji);
            }

            if (string.IsNullOrWhiteSpace(inactiveEmoji))
            {
                inactiveEmoji = "\u23F8\uFE0F";
                InactivityModel.GuildInactiveEmoji.Add(guildId, inactiveEmoji);
            }

            var message = await ReplyAsync(string.Format(culture, Inactivity.Inactivity_Message, inactiveEmoji, activeEmoji));
            await message.AddReactionsAsync(new[] { new Emoji(inactiveEmoji), new Emoji(activeEmoji) });

            if (InactivityModel.GuildInactivityMessage.ContainsKey(guildId))
            {
                InactivityModel.GuildInactivityMessage[guildId] = message.Id;
            }
            else
            {
                InactivityModel.GuildInactivityMessage.Add(guildId, message.Id);
            }

            Client.ReactionAdded -= ReactionAdded;
            Client.ReactionAdded += ReactionAdded;
            InactivityModel.ReactionAddedPointer = ReactionAdded;

            return;
        }

        [Command("setLanguage")]
        [Alias("language", "culture")]
        [Summary("Changes the language of the Bot for the Guild. Use \"en-us\" for English or \"de-de\" for German")]
        public async Task SetLanguageAsync(string locale)
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            if (string.IsNullOrWhiteSpace(locale))
            {
                await ReplyAsync(Inactivity.SetLanguage_NoLocale);
                return;
            }

            if (!locale.Equals("en-US", StringComparison.InvariantCultureIgnoreCase) && !locale.Equals("en", StringComparison.InvariantCultureIgnoreCase)
                && !locale.Equals("de-DE", StringComparison.InvariantCultureIgnoreCase) && !locale.Equals("de", StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync(Inactivity.SetLanguage_NotSupported);
                return;
            }

            culture = new CultureInfo(locale);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            if (InactivityModel.GuildCulture.ContainsKey(Context.Guild.Id))
            {
                InactivityModel.GuildCulture[Context.Guild.Id] = culture;
            }
            else
            {
                InactivityModel.GuildCulture.Add(Context.Guild.Id, culture);
            }

            await InactivityModel.SaveJson(InactivityService.inactivityFileName);

            await ReplyAsync(Inactivity.SetLanguage_Success);
        }

        [Command("setChannel")]
        [Alias("channel", "destinationChannel", "setDestinationChannel")]
        [Summary("Sets the channel where the bot will write the information about a User wanting to become inactive. Can be specified by pinging/referencing the channel with a # or by writing the name of it.")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            if (channel == null)
            {
                await ReplyAsync(Inactivity.SetChannel_NoChannel);
                return;
            }

            if (InactivityModel.GuildDestinationChannel.ContainsKey(channel.GuildId))
            {
                InactivityModel.GuildDestinationChannel[channel.GuildId] = channel.Id;
            }
            else
            {
                InactivityModel.GuildDestinationChannel.Add(channel.GuildId, channel.Id);
            }

            await InactivityModel.SaveJson(InactivityService.inactivityFileName);

            await ReplyAsync(Inactivity.SetChannel_Success);
            return;
        }

        [Command("setRole")]
        [Alias("role", "inactiveRole", "setInactiveRole")]
        [Summary("Sets the role the bot will take away from an inactive user that wants to become active again. Can be specified by pinging the role or writing it's name.")]
        public async Task SetInactiveRole(IRole role)
        {
            GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            if (role == null)
            {
                await ReplyAsync(Inactivity.SetRole_NoRole);
                return;
            }

            if (InactivityModel.GuildInactivityRole.ContainsKey(Context.Guild.Id))
            {
                InactivityModel.GuildInactivityRole[Context.Guild.Id] = role.Id;
            }
            else
            {
                InactivityModel.GuildInactivityRole.Add(Context.Guild.Id, role.Id);
            }

            await InactivityModel.SaveJson(InactivityService.inactivityFileName);

            await ReplyAsync(Inactivity.SetRole_Success);
            return;
        }

        [Command("setRole")]
        [Alias("role", "inactiveRole", "setInactiveRole")]
        [Summary("Sets the role the bot will take away from an inactive user that wants to become active again. Can be specified by pinging the role or writing it's name.")]
        public async Task SetInactiveRole([Remainder] string role)
        {
            GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            if (string.IsNullOrWhiteSpace(role))
            {
                await ReplyAsync(Inactivity.SetRole_NoRole);
                return;
            }

            var guildRole = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(role, StringComparison.InvariantCultureIgnoreCase));

            if (guildRole == null)
            {
                await ReplyAsync(Inactivity.SetRole_RoleNotFound);
                return;
            }

            if (InactivityModel.GuildInactivityRole.ContainsKey(Context.Guild.Id))
            {
                InactivityModel.GuildInactivityRole[Context.Guild.Id] = guildRole.Id;
            }
            else
            {
                InactivityModel.GuildInactivityRole.Add(Context.Guild.Id, guildRole.Id);
            }

            await InactivityModel.SaveJson(InactivityService.inactivityFileName);

            await ReplyAsync(Inactivity.SetRole_Success);
            return;
        }

        [Command("cancel")]
        [Alias("cancelInactivity")]
        [Summary("Cancels the ongoing inactivity reaction check and deletes the associated message")]
        public async Task CancleInactivityReaction()
        {
            GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            Client.ReactionAdded -= InactivityModel.ReactionAddedPointer;

            InactivityModel.GuildInactivityMessage.TryGetValue(Context.Guild.Id, out ulong messageId);

            if (messageId > 0)
            {
                foreach (var channel in Context.Guild.TextChannels)
                {
                    var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
                    var channelPermissions = botUser.GetPermissions(channel);
                    if (channelPermissions.ReadMessageHistory && channelPermissions.ManageMessages)
                    {
                        var message = await channel.GetMessageAsync(messageId);
                        if (message == null)
                        {
                            continue;
                        }

                        await message.DeleteAsync();

                        await ReplyAsync(Inactivity.Cancel_Success);
                        return;
                    }
                }

                await ReplyAsync(Inactivity.Cancel_MissingPermissions);
                return;
            }

            await ReplyAsync(Inactivity.Cancel_MessageNotFound);
        }

        [Command("setActive")]
        [Alias("setActiveEmoji", "active", "activeEmoji")]
        [Summary("Sets the new active emoji for the inactivity check.")]
        public async Task SetActiveEmoji(string emoji)
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();
            
            if (emoji == null || string.IsNullOrWhiteSpace(emoji))
            {
                await ReplyAsync(Inactivity.Emoji_NoSpecified);
                return;
            }

            if (emoji.Contains("<", StringComparison.InvariantCultureIgnoreCase) || emoji.Contains(">", StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync(Inactivity.Emoji_Custom);
                return;
            }

            ulong guildId = Context.Guild.Id;
            if (InactivityModel.GuildActiveEmoji.ContainsKey(guildId))
            {
                InactivityModel.GuildActiveEmoji[guildId] = emoji;
            }
            else
            {
                InactivityModel.GuildActiveEmoji.Add(guildId, emoji);
            }

            await InactivityModel.SaveJson(InactivityService.inactivityFileName);

            await ReplyAsync(string.Format(culture, Inactivity.Emoji_Success, "Active", emoji));
        }

        [Command("setInactive")]
        [Alias("setInactiveEmoji", "inactive", "inactiveEmoji")]
        [Summary("Sets the new inactive emoji for the inactivity check.")]
        public async Task SetInactiveEmoji(string emoji)
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            if (emoji == null || string.IsNullOrWhiteSpace(emoji))
            {
                await ReplyAsync(Inactivity.Emoji_NoSpecified);
                return;
            }

            if (emoji.Contains("<", StringComparison.InvariantCultureIgnoreCase) || emoji.Contains(">", StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync(Inactivity.Emoji_Custom);
                return;
            }

            ulong guildId = Context.Guild.Id;
            if (InactivityModel.GuildInactiveEmoji.ContainsKey(guildId))
            {
                InactivityModel.GuildInactiveEmoji[guildId] = emoji;
            }
            else
            {
                InactivityModel.GuildInactiveEmoji.Add(guildId, emoji);
            }

            await InactivityModel.SaveJson(InactivityService.inactivityFileName);

            await ReplyAsync(string.Format(culture, Inactivity.Emoji_Success, "Inactive", emoji));
        }

        [Command("getRole")]
        [Alias("role")]
        [Summary("Gets the current inactivity role.")]
        public async Task GetRole()
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            ulong roleId = 0;

            if (InactivityModel.GuildInactivityRole.ContainsKey(Context.Guild.Id))
            {
                roleId = InactivityModel.GuildInactivityRole[Context.Guild.Id];
            }

            var role = Context.Guild.GetRole(roleId);

            if (role != null)
            {
                await ReplyAsync(string.Format(culture, Inactivity.Inactivity_Role, role.Name));
            }
            else
            {
                await ReplyAsync(Inactivity.Inactivity_NoRole);
            }
        }

        [Command("getChannel")]
        [Alias("channel")]
        [Summary("Gets the current destination channel.")]
        public async Task GetChannel()
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            ulong channelId = 0;

            if (InactivityModel.GuildDestinationChannel.ContainsKey(Context.Guild.Id))
            {
                channelId = InactivityModel.GuildDestinationChannel[Context.Guild.Id];
            }

            var channel = Context.Guild.GetChannel(channelId);

            if (channel != null)
            {
                await ReplyAsync(string.Format(culture, Inactivity.Inactivity_Channel, channel.Name));
            }
            else
            {
                await ReplyAsync(Inactivity.Inactivity_NoChannel);
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            CultureInfo culture = GetGuildCulture(Context.Guild);

            // Get the message where the reaction was added.
            var message = await cachedMessage.GetOrDownloadAsync();
            if (message != null)
            {
                // Get the user.
                IUser user;

                if (reaction.User.IsSpecified)
                {
                    user = reaction.User.Value;
                }
                else
                {
                    user = Context.Client.GetUser(reaction.UserId);
                }

                // Check if the user object is set and if the user is not a bot.
                if (user != null && !user.IsBot)
                {
                    ulong guildId = Context.Guild.Id;

                    // Check if the reaction was added to the inactivity message.
                    InactivityModel.GuildInactivityMessage.TryGetValue(guildId, out var messageId);
                    if (messageId > 0 && message.Id == messageId)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(async () =>
                        {
                            var emoji = new Emoji(reaction.Emote.Name);

                            await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);

                            InactivityModel.GuildInactiveEmoji.TryGetValue(guildId, out var inactiveEmoji);
                            InactivityModel.GuildActiveEmoji.TryGetValue(guildId, out var activeEmoji);

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
                                    await dmChannel.SendMessageAsync("Time out!");
                                    return;
                                }

                                await dmChannel.SendMessageAsync(Inactivity.Inactivity_Duration);
                                var inactivityPeriod = await GetNextMessage(user).ConfigureAwait(false);

                                await dmChannel.SendMessageAsync(Inactivity.Inactivity_Reason);
                                var reason = await GetNextMessage(user).ConfigureAwait(false);

                                InactivityModel.GuildDestinationChannel.TryGetValue(guildId, out var channelId);
                                if (await guildUser.Guild.GetChannelAsync(channelId) is ITextChannel channel)
                                {
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

                                    await channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
                                }
                            }
                            else if (emoji.Name == activeEmoji)
                            {
                                InactivityModel.GuildInactivityRole.TryGetValue(guildId, out var roleId);
                                InactivityModel.GuildDestinationChannel.TryGetValue(guildId, out var channelId);

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

        private async Task<SocketMessage> GetNextMessage(IUser user)
        {
            var taskSource = new TaskCompletionSource<SocketMessage>();

            Task Func(SocketMessage msg) => MessageReceived(msg, user, taskSource);

            Context.Client.MessageReceived += Func;

            var source = taskSource.Task;
            var delay = Task.Delay(TimeSpan.FromSeconds(120));
            var task = await Task.WhenAny(source, delay).ConfigureAwait(false);

            Context.Client.MessageReceived -= Func;

            return task == source ? await source : null;
        }

        private CultureInfo GetGuildCulture(SocketGuild guild)
        {
            CultureInfo culture;

            if (!InactivityModel.GuildCulture.ContainsKey(guild.Id))
            {
                // in case the guild/server has no Culture defined or the method was called in dm's, return en-US as default culture.
                culture = new CultureInfo("en-US");
            }
            else
            {
                culture = InactivityModel.GuildCulture[guild.Id];
            }

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            return culture;
        }

        private Task NotImplemented() => ReplyAsync("I'm vewwy sowwy but this command is currently not yet implemented :c");
    }
}
