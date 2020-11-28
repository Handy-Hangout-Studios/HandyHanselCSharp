using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Services
{
    internal class EventService
    {
        private readonly BotService _bot;
        private readonly ILogger _logger;
        public EventService(BotService bot, ILogger<EventService> logger)
        {
            this._bot = bot;
            this._logger = logger;
        }

        public async Task SendEmbedWithMessageToChannelAsUser(ulong guildId, ulong userId, ulong channelId, string message, string title, string description)
        {
            try
            {
                DiscordClient shardClient = this._bot.Client.GetShard(guildId);
                DiscordChannel channel = await shardClient.GetChannelAsync(channelId);
                DiscordUser poster = await shardClient.GetUserAsync(userId);
                this._bot.Client.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                        .WithTitle(title)
                        .WithAuthor(poster.Username, iconUrl: poster.AvatarUrl)
                        .WithDescription(description)
                        .Build();
                await shardClient.SendMessageAsync(channel, content: message, embed: embed);
            }
            catch (Exception e)
            {
                this._logger.LogError(e, "Error in Sending Embed", guildId, userId, message, title, description);
            }
        }
    }
}
