using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using HandyHansel.BotDatabase;
using HandyHansel.Models;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("karma")]
    [Aliases("k")]
    [Description("The karma functionality submodule\n\nIf this command is run by itself it shows how much Karma you have in this Guild.")]
    class KarmaCommands : BaseCommandModule
    {
        private readonly Random _rng = new Random();

        private readonly IBotAccessProviderBuilder providerBuilder;

        public KarmaCommands(IBotAccessProviderBuilder builder)
        {
            providerBuilder = builder;
        }

        [GroupCommand]
        public async Task ExecuteCommandAsync(CommandContext context)
        {
            using IBotAccessProvider provider = providerBuilder.Build();
            GuildKarmaRecord userKarmaRecord = provider.GetUsersGuildKarmaRecord(context.Member.Id, context.Guild.Id);
            await context.RespondAsync($"{context.Member.Mention}, you currently have {userKarmaRecord.CurrentKarma} karma.");
        }

        [Command("give")]
        [Aliases("g")]
        [Cooldown(1, 86400, CooldownBucketType.User)]
        public async Task GiveKarma(CommandContext context, DiscordMember member)
        {
            if (member.Equals(context.Member))
            {
                await context.RespondAsync($"{context.Member.Mention}, you can't give yourself Karma. That would be cheating. You have to earn it from others.");
                return;
            }
            using IBotAccessProvider provider = providerBuilder.Build();

            ulong karmaToAdd = 0;

            for (int i = 0; i < 180; i++)
            {
                karmaToAdd += (ulong)_rng.Next(1, 4);
            }

            provider.AddKarma(member.Id, context.Guild.Id, karmaToAdd);
            await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":arrow_up:"));
        }
    }
}
