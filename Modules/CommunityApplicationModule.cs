﻿using Discord;
using Discord.Commands;
using InactivityBot.Models;
using InactivityBot.Ressources;
using InactivityBot.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Modules
{
    [Group("application")]
    [Alias("bewerbung")]
    [RequireGuildChat]
    [RequireUserPermission(GuildPermission.ManageChannels | GuildPermission.ManageMessages)]
    public class CommunityApplicationModule : ModuleBase<SocketCommandContext>
    {
        public CommunityApplicationService ApplicationService { get; set; }
        public CommunityApplicationModel Model => ApplicationService.Model;
        public BaseService BaseService { get; set; }
        public LoggingService LoggingService { get; set; }

        [Command("start")]
        [Description("Command which spawns in a message to which can be reacted for applications.")]
        public async Task ApplicationAsync()
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            CultureInfo culture = BaseService.GetGuildCulture(guildId);

            Model.GuildDestinationChannel.TryGetValue(guildId, out ulong channelId);

            if (channelId <= 0)
            {
                await ReplyAsync(Application.Start_MissingChannel);
                return;
            }

            Model.GuildEmoji.TryGetValue(guildId, out string emote);
            if (string.IsNullOrWhiteSpace(emote))
            {
                emote = "\u25B6";
                Model.GuildEmoji.Add(guildId, emote);
            }

            var message = await ReplyAsync(string.Format(culture, Application.Start_Reaction, emote));
            await message.AddReactionAsync(new Emoji(emote));

            if (Model.GuildApplicationMessage.ContainsKey(guildId))
            {
                Model.GuildApplicationMessage[guildId] = message.Id;
            }
            else
            {
                Model.GuildApplicationMessage.Add(guildId, message.Id);
            }

            await Model.SaveJsonAsync(CommunityApplicationModel.communityApplicationFileName);
            ApplicationService.SetupApplications(guildId);
        }

        [Command("cancel")]
        [Alias("stop")]
        [Description("Command which deletes the message and cancels/ends the awaiting of reactions for applications")]
        public async Task CancelApplicationAsync()
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);
            ApplicationService.CancelApplication(guildId);

            Model.GuildApplicationMessage.TryGetValue(guildId, out ulong messageId);
            Model.GuildApplicationMessage.Remove(guildId);
            await Model.SaveJsonAsync(CommunityApplicationModel.communityApplicationFileName);

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

        [Command("emote")]
        [Alias("setEmote")]
        [Description("Sets the emote to use for the reaction.")]
        public async Task SetEmoteAsync(string emoji)
        {
            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);

            await Context.Channel.TriggerTypingAsync();

            if (emoji == null || string.IsNullOrWhiteSpace(emoji))
            {
                await ReplyAsync(Application.Emote_MissingEmote);
                return;
            }

            if (emoji.Contains("<", StringComparison.InvariantCultureIgnoreCase) || emoji.Contains(">", StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync(Application.Emote_CustomEmote);
                return;
            }

            if (Model.GuildEmoji.ContainsKey(guildId))
            {
                Model.GuildEmoji[guildId] = emoji;
            }
            else
            {
                Model.GuildEmoji.Add(guildId, emoji);
            }

            await Model.SaveJsonAsync(CommunityApplicationModel.communityApplicationFileName);

            await ReplyAsync(Application.Emote_Success);
        }

        [Command("emote")]
        [Alias("getEmote")]
        [Description("Gets the emote that is currently used for the application reaction.")]
        public async Task GetEmoteAsync()
        {
            ulong guildId = Context.Guild.Id;
            CultureInfo culture = BaseService.GetGuildCulture(guildId);

            await Context.Channel.TriggerTypingAsync();

            if (!Model.GuildEmoji.ContainsKey(guildId))
            {
                await ReplyAsync(Application.Emote_MissingEmote);
                return;
            }

            string emoji = Model.GuildEmoji[guildId];

            await ReplyAsync(string.Format(culture, Application.Emote_GetEmote, emoji));
        }

        [Command("channel")]
        [Alias("setChannel")]
        [Description("Sets the destination channel where the bot will post the summary of the Application.")]
        public async Task SetDestinationChannelAsync(ITextChannel channel)
        {
            ulong guildId = Context.Guild.Id;
            BaseService.GetGuildCulture(guildId);
            await Context.Channel.TriggerTypingAsync();

            if (channel == null)
            {
                await ReplyAsync(Application.SetChannel_NoChannel);
                return;
            }

            if (Model.GuildDestinationChannel.ContainsKey(guildId))
            {
                Model.GuildDestinationChannel[guildId] = channel.Id;
            }
            else
            {
                Model.GuildDestinationChannel.Add(guildId, channel.Id);
            }

            await Model.SaveJsonAsync(CommunityApplicationModel.communityApplicationFileName);

            await ReplyAsync(Application.SetChannel_Success);
            return;
        }

        [Command("channel")]
        [Alias("getChannel")]
        [Description("Gets the destination channel where the bot will post the summary of the Application.")]
        public async Task GetDestinationChannel()
        {
            ulong guildId = Context.Guild.Id;
            CultureInfo culture = BaseService.GetGuildCulture(guildId);
            await Context.Channel.TriggerTypingAsync();

            if (!Model.GuildDestinationChannel.ContainsKey(guildId))
            {
                await ReplyAsync(Application.SetChannel_NoChannel);
                return;
            }

            ulong channelId = Model.GuildDestinationChannel[guildId];
            var channel = Context.Guild.GetChannel(channelId);

            if (channel == null)
            {
                await ReplyAsync(Application.GetChannel_NotFound);
            }
            else
            {
                await ReplyAsync(string.Format(culture, Application.GetChannel_Success, channel.Name));
            }
        }
    }
}
