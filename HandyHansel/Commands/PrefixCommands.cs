﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("prefix")]
    [Description("All functionalities associated with prefixes in Handy Hansel.\n\nWhen used alone, show all guild's prefixes separated by spaces")]
    [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
    public class PrefixCommands : BaseCommandModule
    {
        private readonly IBotAccessProviderBuilder botAccessProvider;

        public PrefixCommands(IBotAccessProviderBuilder accessProviderBuilder)
        {
            this.botAccessProvider = accessProviderBuilder;
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = this.botAccessProvider.Build();
            string prefixString;
            List<GuildPrefix> guildPrefixes = dataAccessProvider
                .GetAllAssociatedGuildPrefixes(context.Guild.Id).ToList();
            if (!guildPrefixes.Any())
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
        public async Task AddPrefix(CommandContext context, string newPrefix)
        {
            if (newPrefix.Length < 1)
            {
                await context.RespondAsync("I'm sorry, but any new prefix must be at least one character.");
                return;
            }

            using IBotAccessProvider dataAccessProvider = this.botAccessProvider.Build();
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
        public async Task RemovePrefix(CommandContext context, string prefixToRemove)
        {
            using IBotAccessProvider dataAccessProvider = this.botAccessProvider.Build();
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
        public async Task InteractiveRemovePrefix(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = this.botAccessProvider.Build();

            List<GuildPrefix> guildPrefixes = dataAccessProvider.GetAllAssociatedGuildPrefixes(context.Guild.Id).ToList();
            if (!guildPrefixes.Any())
            {
                await context.RespondAsync("You don't have any custom prefixes to remove");
                return;
            }

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
                GetGuildPrefixPages(interactivity, guildPrefixes),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose a prefix by typing: <prefix number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut)
            {
                return;
            }

            GuildPrefix selectedPrefix =
                dataAccessProvider.GetAllAssociatedGuildPrefixes(context.Guild.Id)
                    .ToList()[int.Parse(result.Result.Content) - 1];

            dataAccessProvider.DeleteGuildPrefix(selectedPrefix);

            await context.RespondAsync(
                $"You have deleted the prefix \"{selectedPrefix.Prefix}\" from this guild's prefixes.");
        }

        private static IEnumerable<Page> GetGuildPrefixPages(InteractivityExtension interactivity,
            List<GuildPrefix> guildPrefixes)
        {
            StringBuilder guildPrefixesStringBuilder = new StringBuilder();
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