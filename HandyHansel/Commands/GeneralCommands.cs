using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Attributes;
using System;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    public class GeneralCommands : BaseCommandModule
    {
        [Command("hi")]
        [Description("A basic \"Hello, World!\" command for D#+")]
        [BotCategory("General")]
#pragma warning disable CA1822 // Mark members as static
        public async Task Hi(CommandContext context)
#pragma warning restore CA1822 // Mark members as static
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}!");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => xm.Author.Id == context.User.Id && xm.Content.ToLower() == "how are you?",
                TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
            {
                await context.RespondAsync("I'm fine, thank you!");
            }
        }

        [Command("break")]
        [Description("Purposefully throw an error for testing purposes")]
        [RequireOwner]
        [Hidden]
        [BotCategory("General")]
#pragma warning disable CA1822 // Mark members as static
        public async Task Break(CommandContext context)
#pragma warning restore CA1822 // Mark members as static
        {
            await context.RespondAsync("Throwing an exception now");
            throw new Exception();
        }
    }
}