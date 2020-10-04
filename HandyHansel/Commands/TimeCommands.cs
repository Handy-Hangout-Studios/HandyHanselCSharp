using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using HandyHansel.Models;

namespace HandyHansel.Commands
{
    [Group("time")]
    [Description(
        "All commands associated with current time functionality.\n\nWhen used alone, perform initial set-up of user's timezone.")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TimeCommands : BaseCommandModule
    {
        [GroupCommand]
        // ReSharper disable once UnusedMember.Global
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
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

            if (!result.TimedOut && Program.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                UserTimeZone newUserTimeZone = new UserTimeZone
                {
                    UserId = context.Message.Author.Id,
                    TimeZoneId = result.Result.Content,
                    OperatingSystem = RuntimeInformation.OSDescription,
                };
                dataAccessProvider.AddUserTimeZone(newUserTimeZone);
                await context.RespondAsync(
                    $"I set your timezone as {result.Result.Content} in all guilds I am a member of.");
            }
            else
            {
                await context.RespondAsync(
                    "You either waited too long to respond or gave me invalid input for the timezone.");
            }
        }

        [Command("update")]
        [Description("Perform a time zone update process for the user who called update.")]
        // ReSharper disable once UnusedMember.Global
        public async Task UpdateTimeZone(CommandContext context)
        {
            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result =
                await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author));

            if (!result.TimedOut && Program.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                using IDataAccessProvider dataAccessProvider =
                    new DataAccessPostgreSqlProvider(new PostgreSqlContext());
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