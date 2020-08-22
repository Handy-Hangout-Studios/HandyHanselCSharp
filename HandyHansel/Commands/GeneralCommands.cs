using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using HandyHansel.Models;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    public class GeneralCommands : BaseCommandModule
    {
        private IDataAccessProvider DataAccessProvider { get; }

        public GeneralCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DataAccessProvider = dataAccessProvider;
        }

        [Command("hi"), Description("A basic \"Hello, World!\" command for D#+")]
        public async Task hi(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}!");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == context.User.Id && xm.Content.ToLower() == "how are you?", TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
                await context.RespondAsync($"I'm fine, thank you!");
        }
    }
}
