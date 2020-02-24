using Discord;
using Discord.Commands;
using Discord.WebSocket;
using InactivityBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot
{
    [RequireGuildChat]
    public class InactivityModule : ModuleBase<SocketCommandContext>
    {
        public InactivityService InactivityModel { get; set; }

        [Command("Echo")]
        public Task EchoAsync([Remainder] string message) => ReplyAsync(message);

        [Command("inactivity")]
        public async Task InactivityAsync()
        {
            await Context.Channel.TriggerTypingAsync();

            await Context.Message.DeleteAsync();

            ulong guildId = Context.Guild.Id;
            InactivityModel.GuildDestinationChannel.TryGetValue(guildId, out var destChannel);
            if (destChannel == null)
            {
                await ReplyAsync("No destination channel is specified. Please make sure to specifiy one, using the command !setChannel.");
                return;
            }

            InactivityModel.GuildInactivityRole.TryGetValue(guildId, out var role);
            if (role == null)
            {
                await ReplyAsync("No inactivity role is specified. Please make sure to specifiy one, using the command !setRole.");
                return;
            }

            InactivityModel.GuildActiveEmoji.TryGetValue(guildId, out var activeEmoji);
            InactivityModel.GuildInactiveEmoji.TryGetValue(guildId, out var inactiveEmoji);

            if (activeEmoji == null)
            {
                activeEmoji = new Emoji("\u25B6");
                InactivityModel.GuildActiveEmoji.Add(guildId, activeEmoji);
            }

            if (inactiveEmoji == null)
            {
                inactiveEmoji = new Emoji("\u23F8\uFE0F");
                InactivityModel.GuildInactiveEmoji.Add(guildId, inactiveEmoji);
            }

            var message = await ReplyAsync($"React with {inactiveEmoji} to be inactiv or {activeEmoji} to be active again!");
            await message.AddReactionsAsync(new[] { inactiveEmoji, activeEmoji });

            if (InactivityModel.GuildInactivityMessage.ContainsKey(guildId))
            {
                InactivityModel.GuildInactivityMessage[guildId] = message.Id;
            }
            else
            {
                InactivityModel.GuildInactivityMessage.Add(guildId, message.Id);
            }

            Context.Client.ReactionAdded -= ReactionAdded;
            Context.Client.ReactionAdded += ReactionAdded;

            return;
        }

        [Command("setLanguage")]
        [Alias("language", "culture")]
        public Task SetLanguageAsync(string locale)
        {
            return NotImplemented();
        }

        [Command("setChannel")]
        [Alias("channel", "destinationChannel", "setDestinationChannel")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            await Context.Channel.TriggerTypingAsync();

            if (channel == null)
            {
                await ReplyAsync("Please provide a channel by referencing it with # and the channel name. e.g. #inactivity");
                return;
            }

            if (InactivityModel.GuildDestinationChannel.ContainsKey(channel.GuildId))
            {
                InactivityModel.GuildDestinationChannel[channel.GuildId] = channel;
            }
            else
            {
                InactivityModel.GuildDestinationChannel.Add(channel.GuildId, channel);
            }

            await ReplyAsync("Succesfully set the destination channel!");
            return;
        }

        [Command("setRole")]
        [Alias("role", "inactiveRole", "setInactiveRole")]
        public async Task SetInactiveRole(IRole role)
        {
            await Context.Channel.TriggerTypingAsync();

            if (role == null)
            {
                await ReplyAsync("Please specify a role by referencing it with an @. e.g. @inactive, @admin, ...");
                return;
            }

            if (InactivityModel.GuildInactivityRole.ContainsKey(Context.Guild.Id))
            {
                InactivityModel.GuildInactivityRole[Context.Guild.Id] = role;
            }
            else
            {
                InactivityModel.GuildInactivityRole.Add(Context.Guild.Id, role);
            }

            await ReplyAsync("Successfully set the inactive role!");
            return;
        }

        [Command("setRole")]
        [Alias("role", "inactiveRole", "setInactiveRole")]
        public async Task SetInactiveRole([Remainder] string role)
        {
            await Context.Channel.TriggerTypingAsync();

            if (string.IsNullOrWhiteSpace(role))
            {
                await ReplyAsync("Please specify a role by it's name. e.g. inactive, admin, ...");
                return;
            }

            var guildRole = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(role, StringComparison.InvariantCultureIgnoreCase));

            if (guildRole == null)
            {
                await ReplyAsync("Could not find the specified role. Please make sure that the name is correct and it exists.");
                return;
            }

            if (InactivityModel.GuildInactivityRole.ContainsKey(Context.Guild.Id))
            {
                InactivityModel.GuildInactivityRole[Context.Guild.Id] = guildRole;
            }
            else
            {
                InactivityModel.GuildInactivityRole.Add(Context.Guild.Id, guildRole);
            }

            await ReplyAsync("Successfully set the inactive role!");
            return;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
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

                            if (emoji.Name == inactiveEmoji.Name)
                            {
                                var dmChannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);

                                await dmChannel.SendMessageAsync("Your Guild Wars 2 account name");
                                var accountName = await GetNextMessage(user).ConfigureAwait(false);

                                await dmChannel.SendMessageAsync("How long will you be inactive/paused (please specifiy with a date)?");
                                var inactivityPeriod = await GetNextMessage(user).ConfigureAwait(false);

                                await dmChannel.SendMessageAsync("Please state the reason, as to why you are pausing");
                                var reason = await GetNextMessage(user).ConfigureAwait(false);

                                InactivityModel.GuildDestinationChannel.TryGetValue(guildId, out var channel);
                                if (channel != null)
                                {
                                    await channel.SendMessageAsync($"Discord name: {user.Mention}\nAccount name: {accountName.Content}\nPeriod: {inactivityPeriod.Content}\nReason: {reason.Content}")
                                        .ConfigureAwait(false);
                                }
                            }
                            else if (emoji == activeEmoji)
                            {
                                var guildUser = user as IGuildUser;
                                InactivityModel.GuildInactivityRole.TryGetValue(guildId, out var role);
                                InactivityModel.GuildDestinationChannel.TryGetValue(guildId, out var channel);

                                if (role != null && guildUser.RoleIds.Contains(role.Id))
                                {
                                    await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
                                    await guildUser.SendMessageAsync("You are no longer inactive!").ConfigureAwait(false);

                                    if (channel != null)
                                    {
                                        await channel.SendMessageAsync($"{guildUser.Mention} is no longer inactive!").ConfigureAwait(false);
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

        private Task NotImplemented() => ReplyAsync("I'm vewwy sowwy but this command is currently not yet implemented :c");
    }
}
