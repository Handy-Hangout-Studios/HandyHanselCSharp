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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("event")]
    [Description(
        "The event functionality's submodule.")]
    public class EventCommands : BaseCommandModule
    {
        private static readonly Random Random = new Random();

        private readonly IBotAccessProviderBuilder dapBuilder;

        private readonly Parser parser;

        private readonly BotService _bot;

        public EventCommands(IBotAccessProviderBuilder builder, BotService bot, ILoggerFactory loggerFactory)
        {
            this.dapBuilder = builder;
            this.parser = new Parser(loggerFactory.CreateLogger("Event Commands Date Parser"), parserTypes: Parser.ParserType.DateTime);
            this._bot = bot;
        }

        [Command("random")]
        [Description("The bot will randomly choose an event and announce it to `@everyone`!")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task RandomEvent(CommandContext context)
        {
            // TODO: Add in an are you sure prompt.
            using IBotAccessProvider dataAccessProvider = this.dapBuilder.Build();
            List<GuildEvent> guildEvents = dataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList();
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
            using IBotAccessProvider dataAccessProvider = this.dapBuilder.Build();

            UserTimeZone userTimeZone = dataAccessProvider.GetUsersTimeZone(context.User.Id);
            if (userTimeZone == null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, you don't have your time set up... I'm not much use without your timezone set up. Please set up your timezone by typing ^time before you waste my time anymore.");
                return;
            }

            TimeZoneInfo schedulerTimeZoneInfo = this._bot.SystemTimeZones[userTimeZone.TimeZoneId];

            DateTime datetime = this.parser.DateTimeV2DateTimeParse(datetimeString, schedulerTimeZoneInfo);
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
                this.GetGuildEventsPages(context.Guild.Id, interactivity, dataAccessProvider),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm
                    => int.TryParse(xm.Content, out _)
                       && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut)
            {
                await context.RespondAsync("You didn't respond in time to select an event. I didn't schedule anything.");
            }

            GuildEvent selectedEvent = dataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList()[int.Parse(result.Result.Content) - 1];


            DateTime eventDateTime = TimeZoneInfo.ConvertTimeToUtc(datetime, schedulerTimeZoneInfo);

            await context.RespondAsync($"You have scheduled the following event for {datetime:g} in your time zone to be output in the {announcementChannel.Mention} channel.");
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Member.DisplayName, iconUrl: context.Member.AvatarUrl)
                .WithDescription(selectedEvent.EventDesc)
                .WithTitle(selectedEvent.EventName)
                .Build();

            await context.RespondAsync(embed: embed);
            GuildBackgroundJob job = new GuildBackgroundJob
            {
                JobName = selectedEvent.EventName,
                ScheduledTime = eventDateTime,
                CancellationTokenSource = new CancellationTokenSource(),
                GuildJobType = GuildJobType.SCHEDULED_EVENT
            };

            if (!this._bot.guildBackgroundJobs.ContainsKey(context.Guild.Id))
            {
                this._bot.guildBackgroundJobs[context.Guild.Id] = new Dictionary<int, GuildBackgroundJob>();
            }

            this._bot.guildBackgroundJobs[context.Guild.Id].Add(job.GetHashCode(), job);

            BackgroundJob.Schedule<BotService>(
                bot =>
                    bot.SendEmbedWithMessageToChannelAsUser(
                            job.CancellationTokenSource.Token,
                            context.Guild.Id,
                            context.Member.Id,
                            announcementChannel.Id,
                            "@everyone, this event is starting now!",
                            selectedEvent.EventName,
                            selectedEvent.EventDesc),
                eventDateTime - DateTime.UtcNow);


            BackgroundJob.Schedule<BotService>(
                bot
                    => bot.SendEmbedWithMessageToChannelAsUser(
                            job.CancellationTokenSource.Token,
                            context.Guild.Id,
                            context.Member.Id,
                            announcementChannel.Id,
                            "@everyone, this event is starting in 10 minutes!",
                            selectedEvent.EventName,
                            selectedEvent.EventDesc
                        ),
                eventDateTime - DateTime.UtcNow - TimeSpan.FromMinutes(10));

            BackgroundJob.Schedule<BotService>(
                bot =>
                    bot.RemoveGuildBackgroundJob(context.Guild.Id, job.GetHashCode()),
                eventDateTime - DateTime.UtcNow + TimeSpan.FromSeconds(5));
        }

        [Command("unschedule")]
        [Description("Start the interactive unscheduling prompt.")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task UnscheduleGuildEvent(
            CommandContext context
        )
        {
            using IBotAccessProvider dap = this.dapBuilder.Build();
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

            TimeZoneInfo memberTimeZone = this._bot.SystemTimeZones[dap.GetUsersTimeZone(context.User.Id).TimeZoneId];

            List<GuildBackgroundJob> guildEventJobs = this._bot.guildBackgroundJobs[context.Guild.Id]
                .Select(x => x.Value)
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

            if (result.TimedOut)
            {
                return;
            }

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

            await this._bot.RemoveGuildBackgroundJob(context.Guild.Id, job.GetHashCode());
            await context.RespondAsync("Ok, it's been done");
        }

        [Command("add")]
        [Description("Starts the set-up process for a new event to be added to the guild events for this server.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task AddGuildEvent(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = this.dapBuilder.Build();
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

            if (result.TimedOut)
            {
                return;
            }

            string eventName = result.Result.Content;

            await context.RespondAsync("What do you want the event description to be?");
            result = await
                interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User));

            if (result.TimedOut)
            {
                return;
            }

            string eventDesc = result.Result.Content;

            GuildEvent newEvent = new GuildEvent
            {
                EventName = eventName,
                EventDesc = eventDesc,
                GuildId = context.Guild.Id,
            };

            dataAccessProvider.AddGuildEvent(newEvent);
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Client.CurrentUser.Username, iconUrl: context.Client.CurrentUser.AvatarUrl)
                .WithDescription(newEvent.EventDesc)
                .WithTitle(newEvent.EventName)
                .Build();

            await context.RespondAsync("You have added the following event to your guild:");
            await context.RespondAsync(embed: embed);
        }

        [Command("remove")]
        [Description("Removes an event from the guild's events.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveGuildEvent(CommandContext context)
        {
            using IBotAccessProvider dataAccessProvider = this.dapBuilder.Build();
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
                this.GetGuildEventsPages(context.Guild.Id, interactivity, dataAccessProvider),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut)
            {
                return;
            }

            GuildEvent selectedEvent =
                dataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id)
                    .ToList()[int.Parse(result.Result.Content) - 1];

            dataAccessProvider.DeleteGuildEvent(selectedEvent);
            await context.RespondAsync($"You have deleted the \"{selectedEvent.EventName}\" event from the guild");
        }

        [Command("show")]
        [Description("Shows a listing of all events currently available for this guild.")]
        public async Task ShowGuildEvents(CommandContext context)
        {
            await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User,
                this.GetGuildEventsPages(context.Guild.Id, context.Client.GetInteractivity(),
                    this.dapBuilder.Build()),
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