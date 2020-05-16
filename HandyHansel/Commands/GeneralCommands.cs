using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using HandyHansel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    public class GeneralCommands : BaseCommandModule
    {
        public PostgreSqlContext DbContext { get;  }
        public IDataAccessProvider DataAccessProvider { get; }

        public GeneralCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DbContext = sqlContext;
            DataAccessProvider = dataAccessProvider;
        }

        [Command("hi"), Description("A basic \"Hello, World!\" command for D#+")]
        public async Task Hi(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}!");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DSharpPlus.Entities.DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == context.User.Id && xm.Content.ToLower() == "how are you?", TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
                await context.RespondAsync($"I'm fine, thank you!");
        }
    }
}
