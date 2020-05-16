using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InactivityBot.TypeReaders
{
    public class RoleListTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            List<IRole> result = new List<IRole>();
            var roles = input.Split(' ');

            foreach (string role in roles)
            {
                var guildRole = context.Guild.Roles.Where(r => r.Mention.Equals(role, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (guildRole != null)
                {
                    result.Add(guildRole);
                }
                else
                {
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Could not find role: {role}"));
                }
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(result));
        }
    }
}
