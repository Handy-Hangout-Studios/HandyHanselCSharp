using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHangoutStudios.Parsers;
using HandyHansel.Attributes;
using HandyHansel.Models;
using HandyHansel.Services;
using HandyHansel.Utilities;
using Hangfire;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("event")]
    [Description(
        "The event functionality's submodule.")]
    public class EventCommands : BaseCommandModule
    {
        private static readonly Random Random = new Random();

        private readonly IBotAccessProviderBuilder accessBuilder;

        private readonly IDateTimeZoneProvider timeZoneProvider;

        private readonly IClock clock;

        public EventCommands(IBotAccessProviderBuilder builder, IDateTimeZoneProvider timeZoneProvider, IClock clock)
        {
            this.accessBuilder = builder;
            this.timeZoneProvider = timeZoneProvider;
            this.clock = clock;
        }

        [Command("random")]
        [Description("The bot will randomly choose an event and announce it to the specified role or `@everyone`!")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task RandomEventForRole(CommandContext context,
            [Description("Role to mention")]
            DiscordRole role)
        {
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to `@everyone` and announce a random event?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult =
                await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(
                    DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                DiscordMessage snark = await context.RespondAsync($"{context.User.Mention}, well then why did you get my attention! Thanks for wasting my time. Now I have to clean up your mess.");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark, context.Message });
                return;
            }

            await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { context.Message, msg });
            using IBotAccessProvider provider = this.accessBuilder.Build();
            List<GuildEvent> guildEvents = provider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList();
            GuildEvent selectedEvent = guildEvents[Random.Next(guildEvents.Count)];
            DiscordEmbedBuilder eventEmbedBuilder = new DiscordEmbedBuilder();
            eventEmbedBuilder
                .WithTitle(selectedEvent.EventName)
                .WithDescription(selectedEvent.EventDesc);
            await context.RespondAsync(role.Mention, embed: eventEmbedBuilder.Build());
        }

        [Command("random")]
        public async Task RandomEvent(CommandContext context)
        {
            await this.RandomEventForRole(context, context.Guild.EveryoneRole);
        }

        [Command("cschedule")]
        [Description("Create and schedule an event for the time given announced to the role given.")]
        [RequirePermissions(Permissions.MentionEveryone)]
        [BotCategory("Scheduling sub-commands")]
        public async Task CreateAndScheduleGuildEvent(
            CommandContext context,
            [Description("The channel to announce the event in")]
            DiscordChannel announcementChannel,
            [Description("The role to announce the event to")]
            DiscordRole role,
            [Description("The date to schedule the event for")]
            [RemainingText]
            string datetimeString
        )
        {
            using IBotAccessProvider provider = this.accessBuilder.Build();

            if (!context.User.TryGetDateTimeZone(provider, this.timeZoneProvider, out DateTimeZone schedulerTimeZone))
            {
                await context.RespondAsync(StringConstants.NoTimeZoneErrorMessage);
                return;
            }

            DiscordMember botMember = await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id);
            if (!announcementChannel.PermissionsFor(botMember).HasPermission(Permissions.SendMessages | Permissions.MentionEveryone))
            {
                await context.RespondAsync($"{context.Member.Mention}, I don't have permission to send messages and mention `@everyone` in that channel.");
                return;
            }

            LocalDateTime datetime = Recognizers.RecognizeDateTime(datetimeString, DateTimeV2Type.DateTime)
                .First().Values.Select(value => (LocalDateTime)value.Value).OrderBy(key => key).First();
            DiscordMessage msg = await context.RespondAsync($":wave: Hi, {context.User.Mention}! You want to schedule an event for {datetime:g} in your timezone?");

            InteractivityExtension interactivity = context.Client.GetInteractivity();
            Reaction reaction = await interactivity.AddAndWaitForYesNoReaction(msg, context.User);
            if (reaction != Reaction.Yes)
            {
                return;
            }

            await this.AddGuildEventInteractive(context, interactivity, msg);
            GuildEvent selectedEvent = provider.GetAllAssociatedGuildEvents(context.Guild.Id).OrderByDescending(e => e.Id).First();

            Instant eventDateTime = datetime.InZoneStrictly(schedulerTimeZone).ToInstant();
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Member.DisplayName, iconUrl: context.Member.AvatarUrl)
                .WithDescription(selectedEvent.EventDesc)
                .WithTitle(selectedEvent.EventName)
                .Build();
            await msg.ModifyAsync($"You have scheduled the following event for {datetime:g} in your time zone to be output in the {announcementChannel.Mention} channel.", embed: embed);
            this.ScheduleEventsForRole(context, announcementChannel, provider, selectedEvent, eventDateTime, role);
        }

        [Command("cschedule")]
        public async Task CreateAndScheduleGuildEventNoRole(
            CommandContext context,
            [Description("The channel to announce the event in")]
            DiscordChannel announcementChannel,
            [Description("The date to schedule the event for")]
            [RemainingText]
            string datetimeString
        )
        {
            await this.CreateAndScheduleGuildEvent(context, announcementChannel, context.Guild.EveryoneRole, datetimeString);
        }

        [Command("schedule")]
        [Description("Schedule an event from the list of events defined for this guild that will be announced to the role given.")]
        [RequirePermissions(Permissions.MentionEveryone)]
        [BotCategory("Scheduling sub-commands")]
        public async Task ScheduleGuildEvent(
            CommandContext context,
            [Description("The channel to announce the event in")]
            DiscordChannel announcementChannel,
            [Description("The role to announce the event to")]
            DiscordRole role,
            [Description("The date to schedule the event for")]
            [RemainingText]
            string datetimeString
        )
        {
            using IBotAccessProvider provider = this.accessBuilder.Build();

            if (!context.User.TryGetDateTimeZone(provider, this.timeZoneProvider, out DateTimeZone schedulerTimeZone))
            {
                await context.RespondAsync(StringConstants.NoTimeZoneErrorMessage);
                return;
            }

            DiscordMember botMember = await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id);
            if (!announcementChannel.PermissionsFor(botMember).HasPermission(Permissions.SendMessages | Permissions.MentionEveryone))
            {
                await context.RespondAsync($"{context.Member.Mention}, I don't have permission to send messages and mention `@everyone` in that channel.");
                return;
            }

            LocalDateTime datetime = Recognizers.RecognizeDateTime(datetimeString, DateTimeV2Type.DateTime)
                .First().Values.Select(value => (LocalDateTime)value.Value).OrderBy(key => key).First();
            DiscordMessage msg = await context.RespondAsync($":wave: Hi, {context.User.Mention}! You want to schedule an event for {datetime:g} in your timezone?");

            InteractivityExtension interactivity = context.Client.GetInteractivity();
            Reaction reaction = await interactivity.AddAndWaitForYesNoReaction(msg, context.User);
            if (reaction != Reaction.Yes)
            {
                return;
            }

            DiscordEmbedBuilder scheduleEmbedBase = new DiscordEmbedBuilder()
                .WithTitle("Select an event by typing: <event number>")
                .WithColor(context.Member.Color);

            GuildEvent selectedEvent = await SelectPredefinedEvent(context, provider, msg, interactivity, scheduleEmbedBase);

            Instant eventDateTime = datetime.InZoneStrictly(schedulerTimeZone).ToInstant();
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Member.DisplayName, iconUrl: context.Member.AvatarUrl)
                .WithDescription(selectedEvent.EventDesc)
                .WithTitle(selectedEvent.EventName)
                .Build();
            await msg.ModifyAsync($"You have scheduled the following event for {datetime:g} in your time zone to be output in the {announcementChannel.Mention} channel.", embed: embed);
            this.ScheduleEventsForRole(context, announcementChannel, provider, selectedEvent, eventDateTime, role);
        }

        [Command("schedule")]
        public async Task ScheduleGuildEventNoRole(
            CommandContext context,
            [Description("The channel to announce the event in")]
            DiscordChannel announcementChannel,
            [Description("The date to schedule the event for")]
            [RemainingText]
            string datetimeString
        )
        {
            await this.ScheduleGuildEvent(context, announcementChannel, context.Guild.EveryoneRole, datetimeString);
        }

        private void ScheduleEventsForRole(CommandContext context, DiscordChannel announcementChannel, IBotAccessProvider provider, GuildEvent selectedEvent, Instant eventDateTime, DiscordRole role)
        {
            Duration eventScheduleDuration = eventDateTime - this.clock.GetCurrentInstant();
            string scheduledJobId = BackgroundJob.Schedule<EventService>(
                eventService =>
                    eventService.SendEmbedWithMessageToChannelAsUser(
                            context.Guild.Id,
                            context.Member.Id,
                            announcementChannel.Id,
                            $"{role.Mention}, this event is starting now!",
                            selectedEvent.EventName,
                            selectedEvent.EventDesc),
                eventScheduleDuration.ToTimeSpan());

            provider.AddGuildBackgroundJob(scheduledJobId, context.Guild.Id, $"{selectedEvent.EventName} - Announcement", eventDateTime, GuildJobType.SCHEDULED_EVENT);

            scheduledJobId = BackgroundJob.Schedule<EventService>(
                eventService
                    => eventService.SendEmbedWithMessageToChannelAsUser(
                            context.Guild.Id,
                            context.Member.Id,
                            announcementChannel.Id,
                            $"{role.Mention}, this event is starting in 10 minutes!",
                            selectedEvent.EventName,
                            selectedEvent.EventDesc
                        ),
                (eventScheduleDuration - Duration.FromMinutes(10)).ToTimeSpan());

            provider.AddGuildBackgroundJob(scheduledJobId, context.Guild.Id, $"{selectedEvent.EventName} - 10 Min Announcement", eventDateTime - Duration.FromMinutes(10), GuildJobType.SCHEDULED_EVENT);
        }

        private static async Task<GuildEvent> SelectPredefinedEvent(CommandContext context, IBotAccessProvider provider, DiscordMessage msg, InteractivityExtension interactivity, DiscordEmbedBuilder scheduleEmbedBase)
        {
            List<GuildEvent> guildEvents = provider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList();
            IEnumerable<Page> pages = GetGuildEventsPages(guildEvents, interactivity, scheduleEmbedBase);
            CustomResult<int> result = await context.WaitForMessageAndPaginateOnMsg(pages,
                PaginationMessageFunction.CreateWaitForMessageWithInt(context.User, context.Channel),
                msg: msg
            );
            if (result.TimedOut || result.Cancelled)
            {
                DiscordMessage snark = await context.RespondAsync("You never gave me a valid input. Thanks for wasting my time. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return null;
            }

            return guildEvents[result.Result];
        }

        [Command("unschedule")]
        [Description("Start the interactive unscheduling prompt.")]
        [BotCategory("Scheduling sub-commands")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task UnscheduleGuildEvent(CommandContext context)
        {
            using IBotAccessProvider provider = this.accessBuilder.Build();
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to unschedule an event for your guild?");
            Reaction reaction = await interactivity.AddAndWaitForYesNoReaction(msg, context.User);

            if (reaction != Reaction.Yes)
            {
                return;
            }

            await context.Message.DeleteAsync();

            DateTimeZone memberTimeZone = this.timeZoneProvider[provider.GetUsersTimeZone(context.User.Id).TimeZoneId];

            List<GuildBackgroundJob> guildEventJobs = provider.GetAllAssociatedGuildBackgroundJobs(context.Guild.Id)
                .Where(x => x.GuildJobType == GuildJobType.SCHEDULED_EVENT)
                .OrderBy(x => x.ScheduledTime)
                .ToList();

            DiscordEmbedBuilder removeEventEmbed = new DiscordEmbedBuilder()
                .WithTitle("Select an event to unschedule by typing: <event number>")
                .WithColor(context.Member.Color);

            CustomResult<int> result = await context.WaitForMessageAndPaginateOnMsg(
                GetScheduledEventsPages(guildEventJobs.Select(x => x.WithTimeZoneConvertedTo(memberTimeZone)), interactivity, removeEventEmbed),
                PaginationMessageFunction.CreateWaitForMessageWithInt(context.User, context.Channel),
                msg: msg);

            if (result.TimedOut || result.Cancelled)
            {
                DiscordMessage snark = await context.RespondAsync("You never gave me a valid input. Thanks for wasting my time. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return;
            }

            GuildBackgroundJob job = guildEventJobs[result.Result - 1];

            msg = await msg.ModifyAsync($"{context.User.Mention}, are you sure you want to unschedule this event?", embed: null);
            reaction = await interactivity.AddAndWaitForYesNoReaction(msg, context.User);

            BackgroundJob.Delete(job.HangfireJobId);
            provider.DeleteGuildBackgroundJob(job);
            await msg.DeleteAllReactionsAsync();
            await msg.ModifyAsync("Ok, I've unscheduled that event!", embed: null);
        }

        [Command("add")]
        [Description("Starts the set-up process for a new event to be added to the guild events for this server.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
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
                DiscordMessage snark = await context.RespondAsync("Well, thanks for wasting my time. Have a good day.");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark, context.Message });
                return;
            }

            await context.Message.DeleteAsync();
            await msg.DeleteAllReactionsAsync();
            await this.AddGuildEventInteractive(context, interactivity, msg);
        }

        private async Task AddGuildEventInteractive(CommandContext context, InteractivityExtension interactivity, DiscordMessage msg)
        {
            using IBotAccessProvider provider = this.accessBuilder.Build();

            if (msg == null)
            {
                await context.RespondAsync(content: "Ok, what do you want the event name to be?");
            }
            else
            {
                await msg.ModifyAsync(content: "Ok, what do you want the event name to be?");
            }

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User) && xm.Channel.Equals(context.Channel));

            if (result.TimedOut)
            {
                DiscordMessage snark = await context.RespondAsync(
                    content: "You failed to provide a valid event title within the time limit, so thanks for wasting a minute of myyyy time. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, result.Result, snark });
                return;
            }

            string eventName = result.Result.Content;

            await result.Result.DeleteAsync();
            await msg.ModifyAsync("What do you want the event description to be?");
            result = await interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User) && xm.Channel.Equals(context.Channel), timeoutoverride: TimeSpan.FromMinutes(3));

            if (result.TimedOut)
            {
                DiscordMessage snark = await context.RespondAsync(
                    content: "You failed to provide a valid event description within the time limit, so thanks for wasting a minute of myyyy time. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, result.Result, snark });
                return;
            }

            string eventDesc = result.Result.Content;
            await result.Result.DeleteAsync();

            provider.AddGuildEvent(context.Guild.Id, eventName, eventDesc);
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Client.CurrentUser.Username, iconUrl: context.Client.CurrentUser.AvatarUrl)
                .WithDescription(eventDesc)
                .WithTitle(eventName)
                .Build();

            await msg.ModifyAsync("You have added the following event to your guild:", embed: embed);
        }

        [Command("remove")]
        [Description("Removes an event from the guild's events.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveGuildEvent(CommandContext context)
        {
            using IBotAccessProvider provider = this.accessBuilder.Build();
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
                DiscordMessage snark = await context.RespondAsync("Well, thanks for wasting my time. Have a good day.");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark, context.Message });
                return;
            }

            DiscordEmbedBuilder removeEventEmbed = new DiscordEmbedBuilder()
                .WithTitle("Select an event to remove by typing: <event number>")
                .WithColor(context.Member.Color);

            Task<(bool, int)> messageValidationAndReturn(MessageCreateEventArgs messageE)
            {
                if (messageE.Author.Equals(context.User) && int.TryParse(messageE.Message.Content, out int eventToChoose))
                {
                    return Task.FromResult((true, eventToChoose));
                }
                else
                {
                    return Task.FromResult((false, -1));
                }
            }

            List<GuildEvent> guildEvents = provider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList();

            await msg.DeleteAllReactionsAsync();

            CustomResult<int> result = await context.WaitForMessageAndPaginateOnMsg(
                GetGuildEventsPages(guildEvents, interactivity, removeEventEmbed),
                messageValidationAndReturn,
                msg: msg);

            if (result.TimedOut || result.Cancelled)
            {
                DiscordMessage snark = await context.RespondAsync("You never gave me a valid input. Thanks for wasting my time. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return;
            }

            GuildEvent selectedEvent = guildEvents[result.Result - 1];

            provider.DeleteGuildEvent(selectedEvent);
            await msg.ModifyAsync($"You have deleted the \"{selectedEvent.EventName}\" event from the guild", embed: null);
        }

        [Command("show")]
        [Description("Shows a listing of all events currently available for this guild.")]
        public async Task ShowGuildEvents(CommandContext context)
        {
            await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(this.accessBuilder.Build().GetAllAssociatedGuildEvents(context.Guild.Id), context.Client.GetInteractivity()),
                behaviour: PaginationBehaviour.WrapAround, deletion: PaginationDeletion.DeleteMessage, timeoutoverride: TimeSpan.FromMinutes(1));
            await context.Message.DeleteAsync();
        }

        private static IEnumerable<Page> GetGuildEventsPages(IEnumerable<GuildEvent> guildEvents, InteractivityExtension interactivity, DiscordEmbedBuilder pageEmbedBase = null)
        {
            StringBuilder guildEventsStringBuilder = new StringBuilder();

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

            return interactivity.GeneratePagesInEmbed(guildEventsStringBuilder.ToString(), SplitType.Line, embedbase: pageEmbedBase);
        }

        private static IEnumerable<Page> GetScheduledEventsPages(IEnumerable<GuildBackgroundJob> guildEventJobs, InteractivityExtension interactivity, DiscordEmbedBuilder pageEmbedBase = null)
        {
            StringBuilder guildEventsStringBuilder = new StringBuilder();

            int count = 1;
            foreach (GuildBackgroundJob job in guildEventJobs)
            {
                guildEventsStringBuilder.AppendLine($"{count}. {job.JobName} - {job.ScheduledTimeInTimeZone.LocalDateTime:f}");
                count++;
            }

            if (!guildEventJobs.Any())
            {
                guildEventsStringBuilder.AppendLine("This guild doesn't have any scheduled events.");
            }

            return interactivity.GeneratePagesInEmbed(guildEventsStringBuilder.ToString(), SplitType.Line, embedbase: pageEmbedBase);
        }
    }
}