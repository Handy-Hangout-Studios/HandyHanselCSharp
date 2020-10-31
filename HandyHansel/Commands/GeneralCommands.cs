using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace HandyHansel.Commands
{
    public class GeneralCommands : BaseCommandModule
    {
        [Command("hi")]
        [Description("A basic \"Hello, World!\" command for D#+")]
        public async Task Hi(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}!");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => xm.Author.Id == context.User.Id && xm.Content.ToLower() == "how are you?",
                TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
                await context.RespondAsync("I'm fine, thank you!");
        }

        [Command("break")]
        [Description("Purposefully throw an error for testing purposes")]
        [RequireOwner]
        [Hidden]
        public async Task Break(CommandContext context)
        {
            await context.RespondAsync("Throwing an exception now");
            throw new Exception();
        }
    }
}