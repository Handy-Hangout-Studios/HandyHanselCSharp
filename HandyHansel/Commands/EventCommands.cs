using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Models;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace HandyHansel.Commands
{
    [Group("event")]
    [Description(
        "The event functionality's submodule.")]
    public class EventCommands : BaseCommandModule
    {
        private static readonly Random Random = new Random();

        private readonly IBotAccessProviderBuilder accessBuilder;

        private readonly Parser parser;

        private readonly BotService _bot;

        public EventCommands(IBotAccessProviderBuilder builder, BotService bot, ILoggerFactory loggerFactory)
        {
            accessBuilder = builder;
            parser = new Parser(loggerFactory.CreateLogger("Event Commands Date Parser"), parserTypes: Parser.ParserType.DateTime);
            _bot = bot;
        }

        [Command("random")]
        [Description("The bot will randomly choose an event and announce it to `@everyone`!")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task RandomEvent(CommandContext context)
        {
            // TODO: Add in an are you sure prompt.
            using IBotAccessProvider provider = accessBuilder.Build();
            List<GuildEvent> guildEvents = provider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList();
            GuildEvent selectedEvent = guildEvents[Random.Next(guildEvents.Count)];
            DiscordEmbedBuilder eventEmbedBuilder = new DiscordEmbedBuilder();
            eventEmbedBuilder
                .WithTitle(selectedEvent.EventName)
                .WithDescription(selectedEvent.EventDesc);
            await context.RespondAsync("@everyone");
            await context.RespondAsync(embed: eventEmbedBuilder.Build());
        }

        [Command("schedule")]
        [Description("Schedule an event for the time passed in.")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task ScheduleGuildEvent(
            CommandContext context,
            [Description("The channel to announce the event in")]
            DiscordChannel announcementChannel,
            [Description("The date to schedule the event for")] 
            [RemainingText]
            string datetimeString
        )
        {
            using IBotAccessProvider provider = accessBuilder.Build();

            UserTimeZone userTimeZone = provider.GetUsersTimeZone(context.User.Id);
            if (userTimeZone == null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, you don't have your time set up... I'm not much use without your timezone set up. Please set up your timezone by typing ^time before you waste my time anymore.");
                return;
            }
            DiscordMember botMember = await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id);
            if (!announcementChannel.PermissionsFor(botMember).HasPermission(Permissions.MentionEveryone | Permissions.SendMessages))
            {
                await context.RespondAsync($"{context.User.Mention}, I don't have permission to send messages and mention `@everyone` in that channel.");
                return;
            }

            TimeZoneInfo schedulerTimeZoneInfo = _bot.SystemTimeZones[userTimeZone.TimeZoneId];

            DateTime datetime = parser.DateTimeV2DateTimeParse(datetimeString, schedulerTimeZoneInfo);
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to schedule an event for {datetime:g} in your timezone?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync($"{context.User.Mention}, well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            await context.RespondAsync("Ok, which event do you want to schedule?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, interactivity, provider),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm
                    => int.TryParse(xm.Content, out _)
                       && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut)
                await context.RespondAsync("You didn't respond in time to select an event. I didn't schedule anything.");

            GuildEvent selectedEvent = provider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList()[int.Parse(result.Result.Content) - 1];

            
            DateTime eventDateTime = TimeZoneInfo.ConvertTimeToUtc(datetime, schedulerTimeZoneInfo);

            await context.RespondAsync($"You have scheduled the following event for {datetime:g} in your time zone to be output in the {announcementChannel.Mention} channel.");
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Member.DisplayName, iconUrl: context.Member.AvatarUrl)
                .WithDescription(selectedEvent.EventDesc)
                .WithTitle(selectedEvent.EventName)
                .Build();

            await context.RespondAsync(embed: embed);

            string scheduledJobId = BackgroundJob.Schedule<BotService>(
                bot =>
                    bot.SendEmbedWithMessageToChannelAsUser(
                            context.Guild.Id,
                            context.Member.Id,
                            announcementChannel.Id,
                            "@everyone, this event is starting now!",
                            selectedEvent.EventName,
                            selectedEvent.EventDesc), 
                eventDateTime - DateTime.UtcNow);

            provider.AddGuildBackgroundJob(scheduledJobId, context.Guild.Id, $"{selectedEvent.EventName} - Announcement", eventDateTime, GuildJobType.SCHEDULED_EVENT);

            scheduledJobId = BackgroundJob.Schedule<BotService>(
                bot
                    => bot.SendEmbedWithMessageToChannelAsUser(
                            context.Guild.Id,
                            context.Member.Id,
                            announcementChannel.Id,
                            "@everyone, this event is starting in 10 minutes!",
                            selectedEvent.EventName,
                            selectedEvent.EventDesc
                        ),
                eventDateTime - DateTime.UtcNow - TimeSpan.FromMinutes(10));

            provider.AddGuildBackgroundJob(scheduledJobId, context.Guild.Id, $"{selectedEvent.EventName} - 10 Min Announcement", eventDateTime - TimeSpan.FromMinutes(10), GuildJobType.SCHEDULED_EVENT);
        }

        [Command("unschedule")]
        [Description("Start the interactive unscheduling prompt.")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task UnscheduleGuildEvent(
            CommandContext context
        )
        {
            using IBotAccessProvider provider = this.accessBuilder.Build();
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to unschedule an event for your guild?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            TimeZoneInfo memberTimeZone = _bot.SystemTimeZones[provider.GetUsersTimeZone(context.User.Id).TimeZoneId];

            List<GuildBackgroundJob> guildEventJobs = provider.GetAllAssociatedGuildBackgroundJobs(context.Guild.Id)
                .Where(x => x.GuildJobType == GuildJobType.SCHEDULED_EVENT)
                .ToList();

            guildEventJobs.ForEach(x => x.ConvertTimeZoneTo(memberTimeZone));

            await context.RespondAsync("Ok, which event do you want to remove?");

            _ = interactivity.SendPaginatedMessageAsync(
                context.Channel,
                context.User,
                this.GetScheduledEventsPages(guildEventJobs, interactivity),
                behaviour: PaginationBehaviour.Ignore,
                timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync($"{context.User.Mention}, choose an event to unschedule by typing <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut) return;

            GuildBackgroundJob job = guildEventJobs[int.Parse(result.Result.Content) - 1];

            msg = await context.RespondAsync($"{context.User.Mention}, are you sure you want to unschedule this event?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")
                    )
                )
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }
            BackgroundJob.Delete(job.HangfireJobId);
            provider.DeleteGuildBackgroundJob(job);
            await context.RespondAsync("Ok, it's been done");
        }

        [Command("add")]
        [Description("Starts the set-up process for a new event to be added to the guild events for this server.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task AddGuildEvent(CommandContext context)
        {
            using IBotAccessProvider provider = accessBuilder.Build();
            DiscordMessage msg =
                await context.RespondAsync($":wave: Hi, {context.User.Mention}! You wanted to create a new event?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well, thanks for wasting my time. Have a good day.");
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

            provider.AddGuildEvent(context.Guild.Id, eventName, eventDesc);
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Client.CurrentUser.Username, iconUrl: context.Client.CurrentUser.AvatarUrl)
                .WithDescription(eventDesc)
                .WithTitle(eventName)
                .Build();

            await context.RespondAsync("You have added the following event to your guild:");
            await context.RespondAsync(embed: embed);
        }

        [Command("remove")]
        [Description("Removes an event from the guild's events.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveGuildEvent(CommandContext context)
        {
            using IBotAccessProvider provider = accessBuilder.Build();
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to remove an event from your guild list?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            await context.RespondAsync("Ok, which event do you want to remove?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, interactivity, provider),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut) return;

            GuildEvent selectedEvent =
                provider.GetAllAssociatedGuildEvents(context.Guild.Id)
                    .ToList()[int.Parse(result.Result.Content) - 1];

            provider.DeleteGuildEvent(selectedEvent);
            await context.RespondAsync($"You have deleted the \"{selectedEvent.EventName}\" event from the guild");
        }

        [Command("show")]
        [Description("Shows a listing of all events currently available for this guild.")]
        public async Task ShowGuildEvents(CommandContext context)
        {
            await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, context.Client.GetInteractivity(),
                    accessBuilder.Build()),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));
        }

        private IEnumerable<Page> GetGuildEventsPages(ulong guildId, InteractivityExtension interactivity,
            IBotAccessProvider dataAccessProvider)
        {
            StringBuilder guildEventsStringBuilder = new StringBuilder();
            List<GuildEvent> guildEvents = dataAccessProvider.GetAllAssociatedGuildEvents(guildId).ToList();

            int count = 1;
            foreach (GuildEvent guildEvent in guildEvents)
            {
                guildEventsStringBuilder.AppendLine($"{count}. {guildEvent.EventName}");
                count++;
            }

            if (!guildEvents.Any())
            {
                guildEventsStringBuilder.AppendLine("This guild doesn't have any defined events.");
            }

            return interactivity.GeneratePagesInEmbed(guildEventsStringBuilder.ToString(), SplitType.Line);
        }

        private IEnumerable<Page> GetScheduledEventsPages(IEnumerable<GuildBackgroundJob> guildEventJobs, InteractivityExtension interactivity)
        {
            StringBuilder guildEventsStringBuilder = new StringBuilder();

            int count = 1;
            foreach (GuildBackgroundJob job in guildEventJobs)
            {
                guildEventsStringBuilder.AppendLine($"{count}. {job.JobName} - {job.ScheduledTime:f}");
                count++;
            }

            return interactivity.GeneratePagesInEmbed(guildEventsStringBuilder.ToString(), SplitType.Line);
        }
    }
}