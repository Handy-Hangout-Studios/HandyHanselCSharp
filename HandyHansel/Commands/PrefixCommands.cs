﻿using System;
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
using HandyHansel.Models;

namespace HandyHansel.Commands
{
    [Group("prefix"), Description("All functionalities associated with prefixes in Handy Hansel.\n\nWhen used alone, add prefix to guild's prefixes")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PrefixCommands : BaseCommandModule
    {
        [Command("add")]
        // ReSharper disable once UnusedMember.Global
        public async Task AddPrefix(CommandContext context, string newPrefix)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            GuildPrefix newGuildPrefix = new GuildPrefix
            {
                Prefix = newPrefix,
                GuildId = context.Guild.Id,
            };
            dataAccessProvider.AddGuildPrefix(newGuildPrefix);
            await context.RespondAsync(
                $"Congratulations, you have added the prefix {newPrefix} to your server's prefixes for Handy Hansel.\nJust a reminder, this disables the default prefix for Handy Hansel unless you specifically add that prefix in again later or do not have any prefixes of your own.");
        }

        [Command("remove"), Description("Remove a prefix from the guild's prefixes")]
        // ReSharper disable once UnusedMember.Global
        public async Task RemovePrefix(CommandContext context, string prefixToRemove)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            GuildPrefix guildPrefix = dataAccessProvider.GetAllAssociatedGuildPrefixes(context.Guild.Id)
                .FirstOrDefault(e => e.Prefix.Equals(prefixToRemove));
            if (guildPrefix is null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, I'm sorry but the prefix you have given me does not exist for this guild.");
                return;
            }
            
            dataAccessProvider.DeleteGuildPrefix(guildPrefix);
            await context.RespondAsync($"{context.User.Mention}, I have removed the prefix {guildPrefix.Prefix} for this server.");
        }

        [Command("iremove"),
         Description("Starts an interactive removal process allowing you to choose which prefix to remove")]
        // ReSharper disable once UnusedMember.Global
        public async Task InteractiveRemovePrefix(CommandContext context)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to remove a prefix from your guild list?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            await context.RespondAsync("Ok, which event do you want to remove?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildPrefixPages(context.Guild.Id, interactivity, dataAccessProvider), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

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
        
        private static IEnumerable<Page> GetGuildPrefixPages(ulong guildId, InteractivityExtension interactivity, IDataAccessProvider dataAccessProvider)
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