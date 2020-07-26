using Discord;
using Discord.Commands;
using Discord.WebSocket;
using InactivityBot.Models;
using InactivityBot.Ressources;
using InactivityBot.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InactivityBot
{
    [Group("inactivity")]
    [Alias("inaktivität")]
    [RequireGuildChat]
    [RequireUserPermission(GuildPermission.ManageChannels | GuildPermission.ManageMessages)]
    public class InactivityModule : ModuleBase<SocketCommandContext>
    {
        public InactivityService InactivityService { get; set; }
        public InactivityModel InactivityModel => InactivityService.Model;
        public BaseService BaseService { get; set; }
        public DiscordSocketClient Client { get; set; }
        public LoggingService LoggingService { get; set; }

        [Command("start")]
        [Summary("Sends a message with reactions and reacts to them.")]
        public async Task InactivityAsync()
        {
            CultureInfo culture = BaseService.GetGuildCulture(Context.Guild.Id);

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

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);
            InactivityService.SetupInactivityReaction(guildId);

            return;
        }

        [Command("setChannel")]
        [Alias("channel", "destinationChannel", "setDestinationChannel")]
        [Summary("Sets the channel where the bot will write the information about a User wanting to become inactive. Can be specified by pinging/referencing the channel with a # or by writing the name of it.")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            BaseService.GetGuildCulture(Context.Guild.Id);

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

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(Inactivity.SetChannel_Success);
            return;
        }

        [Command("setRole")]
        [Alias("role", "inactiveRole", "setInactiveRole")]
        [Summary("Sets the role the bot will take away from an inactive user that wants to become active again. Can be specified by pinging the role or writing it's name.")]
        public async Task SetInactiveRole(IRole role)
        {
            BaseService.GetGuildCulture(Context.Guild.Id);

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

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(Inactivity.SetRole_Success);
            return;
        }

        [Command("setRole")]
        [Alias("role", "inactiveRole", "setInactiveRole")]
        [Summary("Sets the role the bot will take away from an inactive user that wants to become active again. Can be specified by pinging the role or writing it's name.")]
        public async Task SetInactiveRole([Remainder] string role)
        {
            BaseService.GetGuildCulture(Context.Guild.Id);

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

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(Inactivity.SetRole_Success);
            return;
        }

        [Command("cancel")]
        [Alias("cancelInactivity")]
        [Summary("Cancels the ongoing inactivity reaction check and deletes the associated message")]
        public async Task CancleInactivityReaction()
        {
            BaseService.GetGuildCulture(Context.Guild.Id);

            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;

            InactivityService.CancelInactivityReaction(guildId);

            InactivityModel.GuildInactivityMessage.TryGetValue(guildId, out ulong messageId);
            InactivityModel.GuildInactivityMessage.Remove(guildId);
            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

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
            CultureInfo culture = BaseService.GetGuildCulture(Context.Guild.Id);

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

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(string.Format(culture, Inactivity.Emoji_Success, "Active", emoji));
        }

        [Command("setInactive")]
        [Alias("setInactiveEmoji", "inactive", "inactiveEmoji")]
        [Summary("Sets the new inactive emoji for the inactivity check.")]
        public async Task SetInactiveEmoji(string emoji)
        {
            CultureInfo culture = BaseService.GetGuildCulture(Context.Guild.Id);

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

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(string.Format(culture, Inactivity.Emoji_Success, "Inactive", emoji));
        }

        [Command("getRole")]
        [Alias("role")]
        [Summary("Gets the current inactivity role.")]
        public async Task GetRoleAsync()
        {
            CultureInfo culture = BaseService.GetGuildCulture(Context.Guild.Id);

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
        public async Task GetChannelAsync()
        {
            CultureInfo culture = BaseService.GetGuildCulture(Context.Guild.Id);

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

        [Command("getRaids")]
        [Alias("raids")]
        [Summary("Gets all the Raid Roles")]
        public async Task GetRaidsAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            CultureInfo culture = BaseService.GetGuildCulture(Context.Guild.Id);

            if (!InactivityModel.GuildRaidRoles.ContainsKey(Context.Guild.Id))
            {
                await ReplyAsync(Inactivity.GetRaid_NoRaids);
                return;
            }

            var roleIds = InactivityModel.GuildRaidRoles[Context.Guild.Id];
            var roles = Context.Guild.Roles.Where(r => roleIds.Contains(r.Id));

            await ReplyAsync(Inactivity.GetRaid_Success + string.Join(", ", roles.Select(r => r.Name)));
        }

        [Command("setRaid")]
        [Alias("raid")]
        [Summary("Inserts the Raid role to the bot's internal collection.")]
        public async Task SetRaid(IRole raid)
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            if (raid == null)
            {
                await ReplyAsync(Inactivity.SetRaid_NotFound);
                return;
            }

            if (InactivityModel.GuildRaidRoles.ContainsKey(guildId))
            {
                List<ulong> roles = InactivityModel.GuildRaidRoles[guildId];
                if (roles == null)
                {
                    InactivityModel.GuildRaidRoles[guildId] = new List<ulong>();
                }
                else if (roles.Contains(raid.Id))
                {
                    await ReplyAsync(Inactivity.SetRaid_AlreadyExisting);
                    return;
                }

                InactivityModel.GuildRaidRoles[guildId].Add(raid.Id);
            }
            else
            {
                List<ulong> raidRoles = new List<ulong> { raid.Id };
                InactivityModel.GuildRaidRoles.Add(guildId, raidRoles);
            }

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);
            await ReplyAsync(Inactivity.SetRaid_Success);
        }

        [Command("setRaids")]
        [Alias("raids")]
        [Summary("Inserts multiple raid roles to the bot's internal collection.")]
        public async Task SetRaid([Remainder] List<IRole> raids)
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            if (raids == null || raids.Count <= 0)
            {
                await ReplyAsync(Inactivity.SetRaid_NotFound);
                return;
            }

            if (InactivityModel.GuildRaidRoles.ContainsKey(guildId))
            {
                List<ulong> roles = InactivityModel.GuildRaidRoles[guildId];
                if (roles == null)
                {
                    InactivityModel.GuildRaidRoles[guildId] = new List<ulong>();
                }

                var newRoles = raids.Select(r => r.Id).Except(roles);

                InactivityModel.GuildRaidRoles[guildId].AddRange(newRoles);
            }
            else
            {
                InactivityModel.GuildRaidRoles.Add(guildId, raids.Select(r => r.Id).ToList());
            }

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);
            await ReplyAsync(Inactivity.SetRaid_Success);
        }

        [Command("removeRaid")]
        [Summary("Removes the Raid role from the internal collection of the bot.")]
        public async Task RemoveRaid(IRole raid)
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            if (!InactivityModel.GuildRaidRoles.ContainsKey(guildId))
            {
                await ReplyAsync(Inactivity.GetRaid_NoRaids);
                return;
            }

            if (raid == null)
            {
                await ReplyAsync(Inactivity.SetRaid_NotFound);
                return;
            }

            var raidCollection = InactivityModel.GuildRaidRoles[guildId];

            if (!raidCollection.Contains(raid.Id))
            {
                await ReplyAsync(Inactivity.RemoveRaid_RaidNotFound);
                return;
            }

            raidCollection.Remove(raid.Id);
            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(Inactivity.RemoveRaid_Success);
        }

        [Command("removeRaids")]
        [Description("Removes multiple raid roles from the internal collection of the bot.")]
        public async Task RemoveRaids([Remainder] List<IRole> raids)
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            if (!InactivityModel.GuildRaidRoles.ContainsKey(guildId))
            {
                await ReplyAsync(Inactivity.GetRaid_NoRaids);
                return;
            }

            if (raids == null || raids.Count <= 0)
            {
                await ReplyAsync(Inactivity.SetRaid_NotFound);
                return;
            }

            var raidCollection = InactivityModel.GuildRaidRoles[guildId];

            foreach (var raid in raids.Select(r => r.Id))
            {
                raidCollection.Remove(raid);
            }

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            await ReplyAsync(Inactivity.RemoveRaid_Success);
        }

        [Command("startGuildMemberUpdateEvent")]
        [Alias("guildUpdate", "guildUserUpdate", "memberUpdate", "guildMemberUpdate", "userUpdateEvent", "startUpdateEvent")]
        [Description("Command to let the bot start listening to role changes and react when the inactivity role got set.")]
        public async Task StartGuildMemberUpdateEvents()
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            if (!InactivityModel.GuildInactivityRole.ContainsKey(guildId))
            {
                await ReplyAsync(Inactivity.Inactivity_MissingRole);
                return;
            }

            if (!InactivityModel.GuildDestinationChannel.ContainsKey(guildId))
            {
                await ReplyAsync(Inactivity.Inactivity_MissingChannel);
                return;
            }

            if (InactivityModel.GuildMemberUpdateEvents.ContainsKey(guildId))
            {
                InactivityModel.GuildMemberUpdateEvents[guildId] = true;
            }
            else
            {
                InactivityModel.GuildMemberUpdateEvents.Add(guildId, true);
            }

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            InactivityService.SetupGuildMemberUpdated(guildId);

            await ReplyAsync(Inactivity.GuildMemberUpdated_Start);
            return;
        }

        [Command("stopGuildMemberUpdateEvent")]
        [Alias("stopUpdateEvent", "stopMemberUpdate", "stopGuildUpdate")]
        public async Task StipGuildMemberUpdateEvent()
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            if (InactivityModel.GuildMemberUpdateEvents.ContainsKey(guildId))
            {
                InactivityModel.GuildMemberUpdateEvents[guildId] = false;
            }

            await InactivityModel.SaveJsonAsync(InactivityModel.inactivityFileName);

            InactivityService.RemoveGuildMemberUpdated(guildId);

            await ReplyAsync(Inactivity.GuildMemberUpdated_Stop);
            return;
        }

        private Task NotImplemented() => ReplyAsync("I'm vewwy sowwy but this command is currently not yet implemented :c");
    }
}
