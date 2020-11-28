using DSharpPlus;
using DSharpPlus.Entities;
using HandyHansel.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Services
{
    public class ModerationService
    {
        private readonly BotService _bot;
        private readonly ILogger _logger;
        private readonly IBotAccessProviderBuilder _providerBuilder;
        public ModerationService(BotService bot, ILogger<ModerationService> logger, IBotAccessProviderBuilder providerBuilder)
        {
            this._bot = bot;
            this._logger = logger;
            this._providerBuilder = providerBuilder;
        }

        public async Task UnbanAsync(ulong guildId, ulong userId)
        {
            try
            {
                DiscordClient shardClient = this._bot.Client.GetShard(guildId);
                DiscordUser discordUser = await shardClient.GetUserAsync(userId);
                DiscordGuild discordGuild = await shardClient.GetGuildAsync(guildId);
                await discordUser.UnbanAsync(discordGuild);
            }
            catch (Exception e)
            {
                this._logger.LogError("Error in unbanning user", e);
            }
        }

        public async Task RemoveRole(ulong guildId, ulong userId, ulong roleId)
        {
            try
            {
                DiscordClient shardClient = this._bot.Client.GetShard(guildId);
                DiscordGuild discordGuild = await shardClient.GetGuildAsync(guildId);
                DiscordMember guildMember = await discordGuild.GetMemberAsync(userId);
                DiscordRole guildRole = discordGuild.GetRole(roleId);
                await guildMember.RevokeRoleAsync(guildRole);
                using IBotAccessProvider provider = this._providerBuilder.Build();

            }
            catch (Exception e)
            {
                this._logger.LogError("Error in revoking role", e);
            }
        }
    }
}
