using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Attributes;
using HandyHansel.Models;
using HandyHansel.Utilities;
using Hangfire;
using Microsoft.Extensions.Logging;
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

        private readonly BotService _bot;

        public EventCommands(IBotAccessProviderBuilder builder, BotService bot)
        {
            this.accessBuilder = builder;
            this._bot = bot;
        }

        [Command("random")]
        [Description("The bot will randomly choose an event and announce it to `@everyone`!")]
        [RequirePermissions(Permissions.MentionEveryone)]
        public async Task RandomEvent(CommandContext context)
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
            await context.RespondAsync("@everyone", embed: eventEmbedBuilder.Build());
        }

        [Command("schedule")]
        [Description("Schedule an event for the time passed in.")]
        [RequirePermissions(Permissions.MentionEveryone)]
        [BotCategory("Scheduling sub-commands")]
        public async Task ScheduleGuildEvent(
            CommandContext context,
            [Description("The channel to announce the event in")]
            DiscordChannel announcementChannel,
            [Description("Current event or new event: (current | c | curr) / (new | n)")]
            string currentOrNewEvent,
            [Description("The date to schedule the event for")]
            [RemainingText]
            string datetimeString
        )
        {
            Dictionary<string, bool> current = new Dictionary<string, bool>
            {
                { "current", true },
                { "c", true },
                { "curr", true },
                { "new", false },
                { "n", false },
            };

            if (!current.ContainsKey(currentOrNewEvent))
            {
                await context.RespondAsync("You have not provided an available event kind. Use <prefix>help event schedule to see possible event kinds.");
                return;
            }

            using IBotAccessProvider provider = this.accessBuilder.Build();
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

            TimeZoneInfo schedulerTimeZoneInfo = this._bot.SystemTimeZones[userTimeZone.TimeZoneId];

            DateTime datetime = Parser.DateTimeV2DateTimeParse(datetimeString, schedulerTimeZoneInfo);
            DiscordMessage msg = await context.RespondAsync($":wave: Hi, {context.User.Mention}! You want to schedule an event for {datetime:g} in your timezone?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                DiscordMessage snark = await context.RespondAsync($"{context.User.Mention}, well then why did you get my attention! Thanks for wasting my time. Let me clean up now. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return;
            }

            await msg.DeleteAllReactionsAsync();

            DiscordEmbedBuilder scheduleEmbedBase = new DiscordEmbedBuilder()
                .WithTitle("Select an event by typing: <event number>")
                .WithColor(context.Member.Color);

            GuildEvent selectedEvent;
            if (current[currentOrNewEvent])
            {
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
                IEnumerable<Page> pages = GetGuildEventsPages(guildEvents, interactivity, scheduleEmbedBase);
                CustomResult<int> result = await context.WaitForMessageAndPaginateOnMsg(
                    pages,
                    messageValidationAndReturn,
                    msg: msg
                );

                if (result.TimedOut || result.Cancelled)
                {
                    DiscordMessage snark = await context.RespondAsync("You never gave me a valid input. Thanks for wasting my time. :triumph:");
                    await Task.Delay(5000);
                    await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                    return;
                }

                selectedEvent = guildEvents[result.Result - 1];
            }
            else
            {
                await this.AddGuildEventInteractive(context, interactivity, msg);
                selectedEvent = provider.GetAllAssociatedGuildEvents(context.Guild.Id).OrderByDescending(e => e.Id).First();
            }

            DateTime eventDateTime = TimeZoneInfo.ConvertTimeToUtc(datetime, schedulerTimeZoneInfo);
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Member.DisplayName, iconUrl: context.Member.AvatarUrl)
                .WithDescription(selectedEvent.EventDesc)
                .WithTitle(selectedEvent.EventName)
                .Build();
            await msg.ModifyAsync($"You have scheduled the following event for {datetime:g} in your time zone to be output in the {announcementChannel.Mention} channel.", embed: embed);

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
        [BotCategory("Scheduling sub-commands")]
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
                DiscordMessage snark = await context.RespondAsync($"{context.User.Mention}, well then why did you get my attention! Thanks for wasting my time. Let me clean up now. :triumph:");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark, context.Message });
                return;
            }

            await context.Message.DeleteAsync();
            await msg.DeleteAllReactionsAsync();

            TimeZoneInfo memberTimeZone = this._bot.SystemTimeZones[provider.GetUsersTimeZone(context.User.Id).TimeZoneId];

            List<GuildBackgroundJob> guildEventJobs = provider.GetAllAssociatedGuildBackgroundJobs(context.Guild.Id)
                .Where(x => x.GuildJobType == GuildJobType.SCHEDULED_EVENT)
                .OrderBy(x => x.ScheduledTime)
                .ToList();

            DiscordEmbedBuilder removeEventEmbed = new DiscordEmbedBuilder()
                .WithTitle("Select an event to unschedule by typing: <event number>")
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

            CustomResult<int> result = await context.WaitForMessageAndPaginateOnMsg(
                GetScheduledEventsPages(guildEventJobs.Select(x => x.WithTimeZoneConvertedTo(memberTimeZone)), interactivity, removeEventEmbed),
                messageValidationAndReturn,
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
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut ||
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                DiscordMessage snark = await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                await Task.Delay(5000);
                await context.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return;
            }

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

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User));

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
            result = await interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User), timeoutoverride: TimeSpan.FromMinutes(3));

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
                guildEventsStringBuilder.AppendLine($"{count}. {job.JobName} - {job.ScheduledTime:f}");
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