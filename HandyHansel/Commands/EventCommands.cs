using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using HandyHansel.Models;

namespace HandyHansel.Commands
{
    [Group("event"), Description("Randomly choose an event")]
    public class EventCommands : BaseCommandModule
    {
        private static readonly Random _random = new Random();
        private IDataAccessProvider DataAccessProvider { get; }

        public EventCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DataAccessProvider = dataAccessProvider;
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            List<GuildEvent> guildEvents = DataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id);
            GuildEvent selectedEvent = guildEvents[_random.Next(guildEvents.Count)];
            DiscordEmbedBuilder eventEmbedBuilder = new DiscordEmbedBuilder();
            eventEmbedBuilder
                .WithTitle(selectedEvent.EventName)
                .WithDescription(selectedEvent.EventDesc);
            await context.RespondAsync(embed: eventEmbedBuilder.Build());
        }

        [Command("schedule")]
        public async Task ScheduleGuildEvent(CommandContext context, DateTime datetime)
        {
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to schedule an event for {datetime.ToString("g")} UTC?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                return;
            }

            await context.RespondAsync("Ok, which event do you want to schedule?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, interactivity), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                timeoutoverride: TimeSpan.FromMinutes(1));

            if (result.TimedOut) return;

            GuildEvent selectedEvent =
                DataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id)[
                    (int.Parse(result.Result.Content) - 1)];
            ScheduledEvent newEvent = new ScheduledEvent()
            {
                GuildEventId = selectedEvent.Id,
                ScheduledDate = datetime,
                ChannelId = context.Channel.Id,
            };

            DataAccessProvider.AddScheduledEvent(newEvent);
        }

        [Command("add")]
        public async Task AddGuildEvent(CommandContext context)
        {
            DiscordMessage msg = await context.RespondAsync($":wave: Hi, {context.User.Mention}! You wanted to create a new event?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                return;
            }

            await context.RespondAsync("Ok, what do you want the event name to be?");
            InteractivityResult<DiscordMessage> result = await
                interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User));

            if (result.TimedOut) return;

            string eventName = result.Result.Content;
            
            await context.RespondAsync("What do you want the event description to be?");
            result = await
                interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User));

            if (result.TimedOut) return;

            string eventDesc = result.Result.Content;

            GuildEvent newEvent = new GuildEvent()
            {
                EventName = eventName,
                EventDesc = eventDesc,
                GuildId = context.Guild.Id,
            };

            DataAccessProvider.AddGuildEvent(newEvent);
        }

        [Command("remove")]
        public async Task RemoveGuildEvent(CommandContext context)
        {
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to remove an event from your guild list?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Name.Equals("regional_indicator_y"))
            {
                return;
            }

            await context.RespondAsync("Ok, which event do you want to remove?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, interactivity), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                timeoutoverride: TimeSpan.FromMinutes(1));

            if (result.TimedOut) return;

            GuildEvent selectedEvent =
                DataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id)[
                    (int.Parse(result.Result.Content) - 1)];
            
            DataAccessProvider.DeleteGuildEvent(selectedEvent);
        }

        [Command("show"), RequireUserPermissions(Permissions.Administrator)]
        public async Task ShowGuildEvents(CommandContext context)
        {
            await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, context.Client.GetInteractivity()),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));
        }
        private Page[] GetGuildEventsPages(ulong guildId, InteractivityExtension interactivity)
        {
            StringBuilder guildEventsStringBuilder = new StringBuilder();
            
            List<GuildEvent> guildEvents = DataAccessProvider.GetAllAssociatedGuildEvents(guildId);

            int count = 1;
            foreach (GuildEvent guildEvent in guildEvents)
            {
                guildEventsStringBuilder.AppendLine($"{count}. {guildEvent.EventName}");
            }

            return interactivity.GeneratePagesInEmbed(guildEventsStringBuilder.ToString(), SplitType.Line);
        }    
    }
}