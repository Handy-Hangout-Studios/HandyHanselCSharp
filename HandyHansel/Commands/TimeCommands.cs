using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Models;
using NodaTime;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("time")]
    [Description(
        "All commands associated with current time functionality.\n\nWhen used alone, outputs the time of the user mentioned.")]
    public class TimeCommands : BaseCommandModule
    {
        private readonly IBotAccessProviderBuilder providerBuilder;
        private readonly IDateTimeZoneProvider timeZoneProvider;
        private readonly IClock clock;

        public TimeCommands(IBotAccessProviderBuilder providerBuilder, IDateTimeZoneProvider timeZoneProvider, IClock clock)
        {
            this.providerBuilder = providerBuilder;
            this.timeZoneProvider = timeZoneProvider;
            this.clock = clock;
        }

        [GroupCommand]
        public async Task CurrentTimeAsync(CommandContext context)
        {
            using IBotAccessProvider provider = this.providerBuilder.Build();
            UserTimeZone memberTimeZone = provider.GetUsersTimeZone(context.User.Id);
            if (memberTimeZone == null)
            {
                await context.RespondAsync("You don't have a timezone set up. Please try again after using `time init`");
                return;
            }

            DateTimeZone memberDateTimeZone = this.timeZoneProvider[memberTimeZone.TimeZoneId];
            this.clock.GetCurrentInstant().InZone(memberDateTimeZone).Deconstruct(out LocalDateTime localDateTime, out _, out _);
            localDateTime.Deconstruct(out _, out LocalTime localTime);
            DiscordEmbed outputEmbed = new DiscordEmbedBuilder()
                .WithAuthor(iconUrl: context.User.AvatarUrl)
                .WithTitle($"{localTime.ToString("t", null)}");

            await context.RespondAsync(embed: outputEmbed);
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext context, [Description("User to request current time for")] DiscordUser member)
        {
            using IBotAccessProvider provider = this.providerBuilder.Build();
            UserTimeZone memberTimeZone = provider.GetUsersTimeZone(member.Id);
            if (memberTimeZone == null)
            {
                await context.RespondAsync("This user doesn't have a timezone set up. Please try again after the mentioned user has set up their timezone using `time init`");
                return;
            }

            DateTimeZone memberDateTimeZone = this.timeZoneProvider[memberTimeZone.TimeZoneId];
            this.clock.GetCurrentInstant().InZone(memberDateTimeZone).Deconstruct(out LocalDateTime localDateTime, out _, out _);
            localDateTime.Deconstruct(out _, out LocalTime localTime);
            DiscordEmbed outputEmbed = new DiscordEmbedBuilder()
                .WithAuthor(iconUrl: member.AvatarUrl)
                .WithTitle($"{localTime.ToString("t", null)}");

            await context.RespondAsync(embed: outputEmbed);
        }

        [Command("init")]
        [Description("Perform initial set-up of user's timezone.")]
        public async Task InitializeTimeZoneAsync(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = this.providerBuilder.Build();
            if (dataAccessProvider.GetUsersTimeZone(context.User.Id) != null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, you already have a timezone set up. To update your timezone please type `time update`.");
                return;
            }

            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result =
                await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author),
                    TimeSpan.FromMinutes(1));

            if (!result.TimedOut)
            {
                DateTimeZone test = this.timeZoneProvider.GetZoneOrNull(result.Result.Content);
                if (test != null)
                {
                    dataAccessProvider.AddUserTimeZone(context.Message.Author.Id, result.Result.Content);
                    await context.RespondAsync($"I set your timezone as {result.Result.Content} in all guilds I am a member of.");
                }
                else
                {
                    await context.RespondAsync("You provided me with an invalid timezone. Try again by typing `time init`.");
                }
            }
            else
            {
                await context.RespondAsync(
                    "You waited too long to respond.");
            }
        }

        [Command("update")]
        [Description("Perform the time zone update process for the user who called update.")]
        public async Task UpdateTimeZone(CommandContext context)
        {
            using IBotAccessProvider accessProvider = this.providerBuilder.Build();

            if (accessProvider.GetUsersTimeZone(context.User.Id) == null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, you don't have a timezone set up. To initialize your timezone please type `time init`.");
                return;
            }
            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author));

            if (!result.TimedOut)
            {
                DateTimeZone test = this.timeZoneProvider.GetZoneOrNull(result.Result.Content);
                if (test != null)
                {
                    UserTimeZone updatedUserTimeZone = accessProvider.GetUsersTimeZone(context.Message.Author.Id);
                    updatedUserTimeZone.TimeZoneId = result.Result.Content;
                    accessProvider.UpdateUserTimeZone(updatedUserTimeZone);
                    await context.RespondAsync(
                        $"I updated your timezone to {result.Result.Content} in all guilds I am a member of.");
                }
                else
                {
                    await context.RespondAsync("You provided me with an invalid timezone. Try again by typing `time update`.");
                }
            }
            else
            {
                await context.RespondAsync(
                    "You either waited too long to respond. Try again by typing `time update`.");
            }
        }
    }
}