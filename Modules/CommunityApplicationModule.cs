using Discord;
using Discord.Commands;
using InactivityBot.Models;
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
        public CommunityApplicationModel Model => ApplicationService.ApplicationModel;
        public LoggingService LoggingService { get; set; }

        [Command("reaction")]
        [Description("Command which spawns in a message to which can be reacted for applications.")]
        public async Task ApplicationAsync()
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            Model.GuildDestinationChannel.TryGetValue(guildId, out ulong channelId);

            if (channelId <= 0)
            {
                await ReplyAsync("No channel");
            }

            Model.GuildEmote.TryGetValue(guildId, out string emote);
            if (string.IsNullOrWhiteSpace(emote))
            {
                emote = "\u25B6";
            }

            //var message = await ReplyAsync(string.Format(culture, Inactivity.Inactivity_Message, inactiveEmoji, activeEmoji));
            var message = await ReplyAsync($"React with {emote}");
            await message.AddReactionAsync(new Emoji(emote));

            if (Model.GuildApplicationMessage.ContainsKey(guildId))
            {
                Model.GuildApplicationMessage[guildId] = message.Id;
            }
            else
            {
                Model.GuildApplicationMessage.Add(guildId, message.Id);
            }

            await Model.SaveJsonAsync(InactivityModel.inactivityFileName);
            ApplicationService.SetupApplications(guildId);

            return;
        }

        [Command("cancelApplication")]
        [Alias("cancelBewerbung")]
        [Description("Command which deletes the message and cancels/ends the awaiting of reactions for applications")]
        public async Task CancelApplicationAsync()
        {
            await ReplyAsync("Cancel reactions");
        }

        [Command("emote")]
        [Alias("setEmote")]
        [Description("Sets the emote to use for the reaction.")]
        public async Task SetEmoteAsync(string emoji)
        {
            //CultureInfo culture = InactivityService.GetGuildCulture(Context.Guild);

            await Context.Channel.TriggerTypingAsync();

            if (emoji == null || string.IsNullOrWhiteSpace(emoji))
            {
                //await ReplyAsync(Inactivity.Emoji_NoSpecified);
                await ReplyAsync("No emoji");
                return;
            }

            if (emoji.Contains("<", StringComparison.InvariantCultureIgnoreCase) || emoji.Contains(">", StringComparison.InvariantCultureIgnoreCase))
            {
                //await ReplyAsync(Inactivity.Emoji_Custom);
                await ReplyAsync("No custom emoji");
                return;
            }

            ulong guildId = Context.Guild.Id;
            if (Model.GuildEmote.ContainsKey(guildId))
            {
                Model.GuildEmote[guildId] = emoji;
            }
            else
            {
                Model.GuildEmote.Add(guildId, emoji);
            }

            await Model.SaveJsonAsync(InactivityModel.inactivityFileName);

            //await ReplyAsync(string.Format(culture, Inactivity.Emoji_Success, "Active", emoji));
            await ReplyAsync("Success");
        }

        [Command("emote")]
        [Alias("getEmote")]
        [Description("Gets the emote that is currently used for the application reaction.")]
        public async Task GetEmoteAsync()
        {
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;

            if (!Model.GuildEmote.ContainsKey(guildId))
            {
                await ReplyAsync("No emoji");
                return;
            }

            string emoji = Model.GuildEmote[guildId];

            await ReplyAsync(emoji);
        }

        [Command("channel")]
        [Alias("setChannel")]
        [Description("Sets the destination channel where the bot will post the summary of the Application.")]
        public async Task SetDestinationChannelAsync(ITextChannel channel)
        {
            await Context.Channel.TriggerTypingAsync();

            if (channel == null)
            {
                //await ReplyAsync(Inactivity.SetChannel_NoChannel);
                await ReplyAsync("No Channel");
                return;
            }

            if (Model.GuildDestinationChannel.ContainsKey(channel.GuildId))
            {
                Model.GuildDestinationChannel[channel.GuildId] = channel.Id;
            }
            else
            {
                Model.GuildDestinationChannel.Add(channel.GuildId, channel.Id);
            }

            await Model.SaveJsonAsync(InactivityModel.inactivityFileName);

            //await ReplyAsync(Inactivity.SetChannel_Success);
            await ReplyAsync("Success");
            return;
        }

        [Command("channel")]
        [Alias("getChannel")]
        [Description("Gets the destination channel where the bot will post the summary of the Application.")]
        public async Task GetDestinationChannel()
        {
            CultureInfo culture = new CultureInfo("en-us");
            await Context.Channel.TriggerTypingAsync();

            ulong guildId = Context.Guild.Id;
            if (!Model.GuildDestinationChannel.ContainsKey(guildId))
            {
                await ReplyAsync("No channel");
                return;
            }

            ulong channelId = Model.GuildDestinationChannel[guildId];
            var channel = Context.Guild.GetChannel(channelId);

            if (channel == null)
            {
                await ReplyAsync("No channel");
            }
            else
            {
                await ReplyAsync(channel.Name);
            }
        }
    }
}
