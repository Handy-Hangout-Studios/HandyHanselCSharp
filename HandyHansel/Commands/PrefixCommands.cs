using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Models;
using HandyHansel.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("prefix")]
    [Description("All functionalities associated with prefixes in Handy Hansel.\n\nWhen used alone, show all guild's prefixes separated by spaces")]
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
        [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
        public async Task AddPrefix(
            CommandContext context,
            [Description("The new prefix that you want to add to the guild's prefixes. Must be at least one character")]
            string newPrefix)
        {
            if (newPrefix.Length < 1)
            {
                await context.RespondAsync("I'm sorry, but any new prefix must be at least one character.");
                return;
            }

            if (newPrefix.Length > 20)
            {
                await context.RespondAsync("I'm sorry, but any new prefix must be less than 20 characters.");
                return;
            }

            using IBotAccessProvider dataAccessProvider = this.botAccessProvider.Build();
            dataAccessProvider.AddGuildPrefix(context.Guild.Id, newPrefix);
            await context.RespondAsync(
                $"Congratulations, you have added the prefix {newPrefix} to your server's prefixes for Handy Hansel.\nJust a reminder, this disables the default prefix for Handy Hansel unless you specifically add that prefix in again later or do not have any prefixes of your own.");
        }

        [Command("remove")]
        [Description("Remove a prefix from the guild's prefixes")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
        public async Task RemovePrefix(
            CommandContext context,
            [Description("The specific string prefix to remove from the guild's prefixes.")]
            string prefixToRemove)
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
        [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
        public async Task InteractiveRemovePrefix(CommandContext context)
        {
            using IBotAccessProvider provider = this.botAccessProvider.Build();

            List<GuildPrefix> guildPrefixes = provider.GetAllAssociatedGuildPrefixes(context.Guild.Id).ToList();
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
                DiscordMessage snark = await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return;
            }

            DiscordEmbedBuilder removeEventEmbed = new DiscordEmbedBuilder()
                .WithTitle("Select a prefix to remove by typing: <prefix number>")
                .WithColor(context.Member.Color);

            Task<(bool, int)> messageValidationAndReturn(MessageCreateEventArgs messageE)
            {
                if (messageE.Author.Equals(context.User) && int.TryParse(messageE.Message.Content, out int eventToChoose))
                {
                    return Task.FromResult((true, eventToChoose));
                }
                else
                {
                    return Task.FromResult((false, -1));
                }
            }
            await msg.DeleteAllReactionsAsync();
            CustomResult<int> result = await context.WaitForMessageAndPaginateOnMsg(
                GetGuildPrefixPages(guildPrefixes, interactivity, removeEventEmbed),
                messageValidationAndReturn,
                msg: msg);

            if (result.TimedOut || result.Cancelled)
            {
                DiscordMessage snark = await context.RespondAsync("You never gave me a valid input. Thanks for wasting my time. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return;
            }

            GuildPrefix selectedPrefix = guildPrefixes[result.Result - 1];

            provider.DeleteGuildPrefix(selectedPrefix);

            await msg.ModifyAsync(
                $"You have deleted the prefix \"{selectedPrefix.Prefix}\" from this guild's prefixes.", embed: null);
        }

        private static IEnumerable<Page> GetGuildPrefixPages(List<GuildPrefix> guildPrefixes, InteractivityExtension interactivity, DiscordEmbedBuilder pageEmbedBase = null)
        {
            StringBuilder guildPrefixesStringBuilder = new StringBuilder();
            int count = 1;
            foreach (GuildPrefix prefix in guildPrefixes)
            {
                guildPrefixesStringBuilder.AppendLine($"{count}. {prefix.Prefix}");
                count++;
            }

            return interactivity.GeneratePagesInEmbed(guildPrefixesStringBuilder.ToString(), SplitType.Line, pageEmbedBase);
        }
    }
}