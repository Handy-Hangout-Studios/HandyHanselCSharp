using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using HandyHansel.Models;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("time"), Description("All commands associated with current time functionality.")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TimeCommands : BaseCommandModule
    {
        private IDataAccessProvider DataAccessProvider { get; }

        // ReSharper disable once UnusedParameter.Local
        public TimeCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DataAccessProvider = dataAccessProvider;
        }

        [GroupCommand, Description("Perform initial set-up of user's timezone.")]
        // ReSharper disable once UnusedMember.Global
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author), TimeSpan.FromMinutes(1));
                
            if (!result.TimedOut && Program.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                UserTimeZone newUserTimeZone = new UserTimeZone
                {
                    UserId = context.Message.Author.Id, 
                    TimeZoneId = result.Result.Content, 
                    OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                };
                DataAccessProvider.AddUserTimeZone(newUserTimeZone);
                await context.RespondAsync($"I set your timezone as { result.Result.Content } in all guilds I am a member of.");
            }
            else
            {
                await context.RespondAsync(
                    "You either waited too long to respond or gave me invalid input for the timezone.");
            }
        }

        [Command("update"), Description("Perform a time zone update process for the user who called update.")]
        // ReSharper disable once UnusedMember.Global
        public async Task UpdateTimeZone(CommandContext context)
        {
            
            await context.RespondAsync(
                "Please navigate to https://kevinnovak.github.io/Time-Zone-Picker/ and select your timezone. After you do please hit the copy button and paste the contents into the chat.");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(msg => msg.Author.Equals(context.Message.Author));
                
            if (!result.TimedOut && Program.SystemTimeZones.ContainsKey(result.Result.Content))
            {
                UserTimeZone updatedUserTimeZone = DataAccessProvider.GetUsersTimeZone(context.Message.Author.Id);
                updatedUserTimeZone.TimeZoneId = result.Result.Content;
                DataAccessProvider.UpdateUserTimeZone(updatedUserTimeZone);
                await context.RespondAsync($"I set your timezone as { result.Result.Content } in all guilds I am a member of.");
            }
            else
            {
                await context.RespondAsync(
                    "You either waited too long to respond or gave me invalid input for the timezone.");
            }
        }
    }
}
