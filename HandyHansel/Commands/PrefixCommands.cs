using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Models;

namespace HandyHansel.Commands
{
    [Group("prefix"), Description("All functionalities associated with prefixes in Handy Hansel.\n\nWhen used alone, show all guild's prefixes separated by spaces")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PrefixCommands : BaseCommandModule
    {
        private readonly IBotAccessProviderBuilder botAccessProvider;

        public PrefixCommands(IBotAccessProviderBuilder accessProviderBuilder)
        {
            botAccessProvider = accessProviderBuilder;
        }

        [GroupCommand]
        // ReSharper disable once UnusedMember.Global
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = botAccessProvider.Build();
            string prefixString; 
            List<GuildPrefix> guildPrefixes = dataAccessProvider
                .GetAllAssociatedGuildPrefixes(context.Guild.Id).ToList();
            if(!guildPrefixes.Any())
            {
                prefixString = "^";
            }
            else if (guildPrefixes.Count == 1)
            {
                prefixString = guildPrefixes.First().Prefix;
            }
            else
            {
                prefixString = guildPrefixes
                    .Select(
                        prefix => 
                            prefix.Prefix)
                    .Aggregate(
                        (partial, next) 
                            => $"{partial} {next}");
            }
            
            await context.RespondAsync($"{context.User.Mention}, the prefixes are: {prefixString}");
        }
        
        [Command("add"), Description("Add prefix to guild's prefixes")]
        // ReSharper disable once UnusedMember.Global
        public async Task AddPrefix(CommandContext context, string newPrefix)
        {
            using IBotAccessProvider dataAccessProvider = botAccessProvider.Build();
            GuildPrefix newGuildPrefix = new GuildPrefix
            {
                Prefix = newPrefix,
                GuildId = context.Guild.Id,
            };
            dataAccessProvider.AddGuildPrefix(newGuildPrefix);
            await context.RespondAsync(
                $"Congratulations, you have added the prefix {newPrefix} to your server's prefixes for Handy Hansel.\nJust a reminder, this disables the default prefix for Handy Hansel unless you specifically add that prefix in again later or do not have any prefixes of your own.");
        }

        [Command("remove")]
        [Description("Remove a prefix from the guild's prefixes")]
        // ReSharper disable once UnusedMember.Global
        public async Task RemovePrefix(CommandContext context, string prefixToRemove)
        {
            using IBotAccessProvider dataAccessProvider = botAccessProvider.Build();
            GuildPrefix guildPrefix = dataAccessProvider.GetAllAssociatedGuildPrefixes(context.Guild.Id)
                .FirstOrDefault(e => e.Prefix.Equals(prefixToRemove));
            if (guildPrefix is null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, I'm sorry but the prefix you have given me does not exist for this guild.");
                return;
            }

            dataAccessProvider.DeleteGuildPrefix(guildPrefix);
            await context.RespondAsync(
                $"{context.User.Mention}, I have removed the prefix {guildPrefix.Prefix} for this server.");
        }

        [Command("iremove")]
        [Description("Starts an interactive removal process allowing you to choose which prefix to remove")]
        // ReSharper disable once UnusedMember.Global
        public async Task InteractiveRemovePrefix(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = botAccessProvider.Build();
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to remove a prefix from your guild list?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            await context.RespondAsync("Ok, which event do you want to remove?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildPrefixPages(context.Guild.Id, interactivity, dataAccessProvider),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose a prefix by typing: <prefix number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut) return;

            GuildPrefix selectedPrefix =
                dataAccessProvider.GetAllAssociatedGuildPrefixes(context.Guild.Id)
                    .ToList()[int.Parse(result.Result.Content) - 1];

            dataAccessProvider.DeleteGuildPrefix(selectedPrefix);

            await context.RespondAsync(
                $"You have deleted the prefix \"{selectedPrefix.Prefix}\" from this guild's prefixes.");
        }

        private static IEnumerable<Page> GetGuildPrefixPages(ulong guildId, InteractivityExtension interactivity,
            IBotAccessProvider dataAccessProvider)
        {
            StringBuilder guildPrefixesStringBuilder = new StringBuilder();
            List<GuildPrefix> guildPrefixes = dataAccessProvider.GetAllAssociatedGuildPrefixes(guildId).ToList();
            int count = 1;
            foreach (GuildPrefix prefix in guildPrefixes)
            {
                guildPrefixesStringBuilder.AppendLine($"{count}. {prefix.Prefix}");
                count++;
            }

            return interactivity.GeneratePagesInEmbed(guildPrefixesStringBuilder.ToString(), SplitType.Line);
        }
    }
}