using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Modules
{
    public class BaseModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync() => ReplyAsync("pong!");

        [Command("Help")]
        public Task Help()
        {
            return ReplyAsync("There is currently no help, I'm vewy sowwy :c");
        }
    }
}
