using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using System;

namespace HandyHansel
{
    internal class Config
    {
        internal Config()
        {
            ClientConfig = new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
            };

            CommandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "^" },
            };

            InteractivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2),
            };
        }

        internal DiscordConfiguration ClientConfig { get; set; }
        internal CommandsNextConfiguration CommandsConfig { get; set; }
        internal InteractivityConfiguration InteractivityConfig { get; set; }
    }
}
