using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Models;

namespace HandyHansel.Commands
{
    [Group("time")]
    [Description(
        "All commands associated with current time functionality.\n\nWhen used alone, perform initial set-up of user's timezone.")]
    public class TimeCommands : BaseCommandModule
    {
        private readonly BotService _bot;
        private readonly IBotAccessProviderBuilder _access;

        public TimeCommands(IBotAccessProviderBuilder providerBuilder, BotService bot)
        {
            _bot = bot;
            _access = providerBuilder;
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = _access.Build();
            if (dataAccessProvider.GetUsersTimeZone(context.User.Id) != null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, you already have a timezone set up. To update your timezone please type ^time update.");
                return;
            }

            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result =
                await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author),
                    TimeSpan.FromMinutes(1));

            if (!result.TimedOut && _bot.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                dataAccessProvider.AddUserTimeZone(context.Message.Author.Id, result.Result.Content);
                await context.RespondAsync(
                    $"I set your timezone as {result.Result.Content} in all guilds I am a member of.");
            }
            else if (result.TimedOut)
            {
                await context.RespondAsync(
                    "You waited too long to respond.");
            }
            else if (!_bot.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                await context.RespondAsync("You provided me with an invalid timezone. Try again by typing ^time.");
            }
        }

        [Command("update")]
        [Description("Perform a time zone update process for the user who called update.")]
        public async Task UpdateTimeZone(CommandContext context)
        {
            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result =
                await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author));

            if (!result.TimedOut && _bot.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                using IBotAccessProvider dataAccessProvider = _access.Build();
                UserTimeZone updatedUserTimeZone = dataAccessProvider.GetUsersTimeZone(context.Message.Author.Id);
                updatedUserTimeZone.TimeZoneId = result.Result.Content;
                dataAccessProvider.UpdateUserTimeZone(updatedUserTimeZone);
                await context.RespondAsync(
                    $"I set your timezone as {result.Result.Content} in all guilds I am a member of.");
            }
            else
            {
                await context.RespondAsync(
                    "You either waited too long to respond or gave me invalid input for the timezone.");
            }
        }
    }
}