using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using HandyHansel.Models;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GeneralCommands : BaseCommandModule
    {
        // ReSharper disable twice UnusedParameter.Local
        public GeneralCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
        }

        [Command("hi"), Description("A basic \"Hello, World!\" command for D#+")]
        // ReSharper disable once UnusedMember.Global
        public async Task Hi(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}!");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == context.User.Id && xm.Content.ToLower() == "how are you?", TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
                await context.RespondAsync("I'm fine, thank you!");
        }
    }
}
