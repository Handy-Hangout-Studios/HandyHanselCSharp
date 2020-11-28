using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using HandyHansel.Attributes;
using HandyHansel.BotDatabase;
using HandyHansel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Obsolete("This command module has been deactivated")]
    internal class KarmaCommands : BaseCommandModule
    {
        private readonly Random _rng = new Random();

        private readonly IBotAccessProviderBuilder providerBuilder;

        public KarmaCommands(IBotAccessProviderBuilder builder)
        {
            this.providerBuilder = builder;
        }

        [Command("karma")]
        [Description("Tells you how much karma you have currently or how much karma another has currently.")]
        [BotCategory("Karma")]
        [Aliases("k")]
        public async Task ShowKarmaAsync(CommandContext context)
        {
            await this.ShowKarmaOfOtherAsync(context, context.Member);
        }

        [Command("karma")]
        public async Task ShowKarmaOfOtherAsync(CommandContext context, [Description("The member whose karma you'd like to view.")] DiscordMember member)
        {
            using IBotAccessProvider provider = this.providerBuilder.Build();
            GuildKarmaRecord userKarmaRecord = provider.GetUsersGuildKarmaRecord(member.Id, context.Guild.Id);
            DiscordEmbed response = new DiscordEmbedBuilder()
                .AddField("Karma:", $"{userKarmaRecord.CurrentKarma}", true)
                .WithAuthor(member.DisplayName, iconUrl: member.AvatarUrl)
                .WithColor(member.Color);
            await context.RespondAsync(embed: response);
        }

        [Command("upvote")]
        [Aliases("++", "u")]
        [BotCategory("Karma")]
        [Description("Upvote another Discord user and bestow them 3 hours worth of karma from nothingness.\n\n*This karma is not taken from you.*")]
        [Cooldown(1, 86400, CooldownBucketType.User | CooldownBucketType.Guild)]
        public async Task GiveKarma(CommandContext context, [Description("The discord user you'd like to give karma.")] DiscordUser member)
        {
            if (member.Equals(context.Member))
            {
                await context.RespondAsync($"{context.Member.Mention}, you can't give yourself Karma. That would be cheating. You have to earn it from others.");
                return;
            }

            await context.Message.DeleteAsync();
            using IBotAccessProvider provider = this.providerBuilder.Build();

            ulong karmaToAdd = 0;

            for (int i = 0; i < 180; i++)
            {
                karmaToAdd += (ulong)this._rng.Next(1, 4);
            }

            provider.AddKarma(member.Id, context.Guild.Id, karmaToAdd);
            DiscordEmbed response = new DiscordEmbedBuilder()
                .WithDescription($"{member.Mention}, you have been bestowed {karmaToAdd} karma.");
            await context.RespondAsync(embed: response);
        }

        [Command("leaderboard")]
        [Aliases("lb")]
        [BotCategory("Karma")]
        [Description("Show the leaderboard ")]
        public async Task ShowLeaderBoard(CommandContext context)
        {
            using IBotAccessProvider provider = this.providerBuilder.Build();
            List<GuildKarmaRecord> leaderboardRecords = provider.GetGuildKarmaRecords(context.Guild.Id)
                .OrderByDescending(record => record.CurrentKarma).ToList();
            int requestorRanking = leaderboardRecords.FindIndex(record => record.UserId == context.User.Id) + 1;

            StringBuilder leaderboardBuilder = new StringBuilder()
                .AppendLine("```")
                .AppendLine($"Your rank: {requestorRanking}")
                .AppendLine(new string('-', 20));

            int index = 1;
            foreach (GuildKarmaRecord record in leaderboardRecords)
            {
                try
                {
                    DiscordMember member = await context.Guild.GetMemberAsync(record.UserId);
                    leaderboardBuilder.AppendLine($"{index}. {member.DisplayName} - {record.CurrentKarma} karma");
                    index++;

                    if (index > 10)
                    {
                        break;
                    }
                }
                catch (NotFoundException) { }
            }

            leaderboardBuilder.Append("```");
            await context.RespondAsync(leaderboardBuilder.ToString());
        }
    }
}
