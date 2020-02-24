using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot
{
    public class RequireGuildChatAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.User is SocketGuildUser)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You must be in a guild to use this command"));
            }
        }
    }
}
