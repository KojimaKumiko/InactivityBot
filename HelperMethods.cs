using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InactivityBot
{
    public static class HelperMethods
    {
        public static async Task<SocketMessage> GetNextMessage(DiscordSocketClient client, IUser user, TimeSpan waitTime)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var taskSource = new TaskCompletionSource<SocketMessage>();

            Task Func(SocketMessage msg) => MessageReceived(msg, user, taskSource);

            client.MessageReceived += Func;

            var source = taskSource.Task;
            var delay = Task.Delay(waitTime);
            var task = await Task.WhenAny(source, delay).ConfigureAwait(false);

            client.MessageReceived -= Func;

            return task == source ? await source : null;
        }

        public static GuildEmote GetGuildEmote(DiscordSocketClient client, string emote)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return client.Guilds.SelectMany(g => g.Emotes).FirstOrDefault(e => e.Name.IndexOf(emote, StringComparison.OrdinalIgnoreCase) != -1);
        }

        private static async Task MessageReceived(SocketMessage message, IUser user, TaskCompletionSource<SocketMessage> taskSource)
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
