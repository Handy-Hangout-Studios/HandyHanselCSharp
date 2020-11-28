using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using HandyHangoutStudios.Parsers;
using HandyHangoutStudios.Parsers.Models;
using HandyHangoutStudios.Parsers.Resolutions;
using HandyHansel.Commands;
using HandyHansel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandyHansel
{
    public partial class BotService
    {
        private readonly ulong _devUserId;

        public DiscordShardedClient Client { get; }
        private readonly CommandsNextConfiguration commandsConfig;
        private readonly InteractivityConfiguration interactivityConfig;
        private IReadOnlyDictionary<int, CommandsNextExtension> commands;
#pragma warning disable IDE0052 // Remove unread private members
        private IReadOnlyDictionary<int, InteractivityExtension> interactivity;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly IBotAccessProviderBuilder accessBuilder;
        private readonly IDateTimeZoneProvider timeZoneProvider;
        private readonly ILogger logger;

        //private readonly ISet<GuildKarmaRecord> userKarmaAddition = new HashSet<GuildKarmaRecord>();
        //private readonly object karmaLock = new object();
        //private readonly Random rng = new Random();

        private DiscordEmoji clockEmoji;

        public BotService(
            ILoggerFactory loggerFactory,
            IOptions<BotConfig> botConfig,
            IBotAccessProviderBuilder dataAccessProviderBuilder,
            IDateTimeZoneProvider timeZoneProvider,
            IServiceProvider services)
        {
            DiscordConfiguration ClientConfig = new DiscordConfiguration
            {
                Token = botConfig.Value.BotToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                LoggerFactory = loggerFactory,
                Intents = DiscordIntents.All,
            };

            this.commandsConfig = new CommandsNextConfiguration
            {
                PrefixResolver = PrefixResolver,
                Services = services,
                EnableDms = true,
                EnableMentionPrefix = true,
            };

            this.interactivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2),
            };


            this.Client = new DiscordShardedClient(ClientConfig);
            this._devUserId = botConfig.Value.DevId;
            this.logger = loggerFactory.CreateLogger("BotService");
            this.accessBuilder = dataAccessProviderBuilder;
            this.timeZoneProvider = timeZoneProvider;
        }

        public async Task StartAsync()
        {
            this.commands = await this.Client.UseCommandsNextAsync(this.commandsConfig);
            this.interactivity = await this.Client.UseInteractivityAsync(this.interactivityConfig);
            IReadOnlyDictionary<int, VoiceNextExtension> test = await this.Client.UseVoiceNextAsync(new VoiceNextConfiguration());

            foreach (KeyValuePair<int, CommandsNextExtension> pair in this.commands)
            {
                pair.Value.RegisterCommands<GeneralCommands>();
                pair.Value.RegisterCommands<TimeCommands>();
                pair.Value.RegisterCommands<EventCommands>();
                pair.Value.RegisterCommands<PrefixCommands>();
                // This command module has been deactivated as it has not served the purpose for which it was originally designed
                // If there comes a time in which a new design is implemented which is able to help foster a positive community
                // of people helping people rather than a community of simps or a simple popularity contest then it will be
                // reactivated
                // pair.Value.RegisterCommands<KarmaCommands>();
                pair.Value.RegisterCommands<ModerationCommands>();
                pair.Value.CommandErrored += this.ChecksFailedError;
                pair.Value.CommandErrored += this.CheckCommandExistsError;
                pair.Value.CommandErrored += this.LogExceptions;
                pair.Value.SetHelpFormatter<CategoryHelpFormatter>();
            }

            //this.Client.MessageCreated += this.EarnKarma;
            this.Client.MessageCreated += this.CheckForDate;
            this.Client.MessageReactionAdded += this.SendAdjustedDate;
            this.Client.Ready += this.UpdateDiscordStatus;

            await this.Client.StartAsync();
            this.clockEmoji = DiscordEmoji.FromName(this.Client.ShardClients[0], ":clock:");

            //RecurringJob.AddOrUpdate<BotService>(bot =>
            //bot.UpdateKarmas(), "0/1 * * * *");
        }

        public async Task StopAsync()
        {
            await this.Client.StopAsync();
        }

        private async Task UpdateDiscordStatus(DiscordClient sender, ReadyEventArgs e)
        {
            await this.Client.UpdateStatusAsync(new DiscordActivity("all the users in anticipation", ActivityType.Watching));
        }

        private async Task ChecksFailedError(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            if (e.Exception is ChecksFailedException checksFailed)
            {
                IReadOnlyList<CheckBaseAttribute> failedChecks = checksFailed.FailedChecks;

                string DetermineMessage()
                {
                    if (failedChecks.Any(x => x is RequireBotPermissionsAttribute))
                    {
                        return "I don't have the permissions necessary";
                    }
                    if (failedChecks.Any(x => x is RequireUserPermissionsAttribute))
                    {
                        return "you don't have the permissions necessary";
                    }
                    if (failedChecks.Any(x => x is CooldownAttribute))
                    {
                        CooldownAttribute cooldown = failedChecks.First(x => x is CooldownAttribute) as CooldownAttribute;
                        return $"this command is on cooldown for {cooldown.GetRemainingCooldown(e.Context):hh\\:mm\\:ss}";
                    }
                    if (failedChecks.Any(x => x is RequireOwnerAttribute))
                    {
                        return "this command can only be used by the Bot's owner";
                    }

                    return "The check failed is unknown";
                }

                await e.Context.RespondAsync($"You can't use `{e.Command.QualifiedName}` because {DetermineMessage()}.");
                e.Handled = true;
            }
        }

        private async Task CheckCommandExistsError(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            if (e.Exception is CommandNotFoundException)
            {
                await e.Context.RespondAsync("The given command doesn't exist");
                e.Handled = true;
            }
            else if (e.Exception is InvalidOperationException invalid)
            {
                await e.Context.RespondAsync(invalid.Message);
                e.Handled = true;
            }
            else if (e.Exception is ArgumentException)
            {
                await e.Context.RespondAsync($"Missing or invalid arguments. Call `help {e.Command.QualifiedName}` for the proper usage.");
                e.Handled = true;
            }
        }

        private async Task LogExceptions(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            try
            {
                DiscordEmbedBuilder commandErrorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Command Exception");

                if (e.Exception.Message != null)
                {
                    commandErrorEmbed.AddField("Message", e.Exception.Message);
                }

                if (e.Exception.StackTrace != null)
                {
                    int stackTraceLength = e.Exception?.StackTrace.Length > 1024 ? 1024 : e.Exception.StackTrace.Length;
                    commandErrorEmbed.AddField("StackTrace", e.Exception.StackTrace.Substring(0, stackTraceLength));
                }

                if (e.Exception.GetType() != null)
                {
                    commandErrorEmbed.AddField("ExceptionType", e.Exception.GetType().FullName);
                }

                await e.Context.Guild.Members[this._devUserId].SendMessageAsync(embed: commandErrorEmbed);
                this.logger.LogError(e.Exception, "Exception from Command Errored");
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "An error occurred in sending the exception to the Dev");
            }
        }

        private async Task CheckForDate(DiscordClient c, MessageCreateEventArgs e)
        {

            if (e.Author.IsBot)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                IEnumerable<DateTimeV2ModelResult> parserList = DateTimeRecognizer.RecognizeDateTime(e.Message.Content, culture: Culture.English)
                .Select(x => x.ToDateTimeV2ModelResult()).Where(x => x.TypeName is DateTimeV2Type.Time or DateTimeV2Type.DateTime);

                if (parserList.Any())
                {
                    await e.Message.CreateReactionAsync(this.clockEmoji);
                }
            });
        }

        private async Task SendAdjustedDate(DiscordClient c, MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot)
            {
                return;
            }

            DiscordChannel channel = await c.GetChannelAsync(e.Channel.Id);
            _ = Task.Run(async () =>
            {
                if (e.Emoji.Equals(this.clockEmoji))
                {
                    try
                    {
                        using IBotAccessProvider database = this.accessBuilder.Build();
                        DiscordMember reactor = (DiscordMember)e.User;
                        DiscordMessage msg = await channel.GetMessageAsync(e.Message.Id);
                        IEnumerable<DateTimeV2ModelResult> parserList = DateTimeRecognizer.RecognizeDateTime(msg.Content, culture: Culture.English)
                            .Select(x => x.ToDateTimeV2ModelResult()).Where(x => x.TypeName is DateTimeV2Type.Time or DateTimeV2Type.DateTime);

                        if (!parserList.Any())
                        {
                            await reactor.SendMessageAsync("Hey, you're stupid, stop trying to react to messages that don't have times in them.");
                            return;
                        }

                        DiscordEmbedBuilder reactorTimeEmbed = new DiscordEmbedBuilder().WithTitle("You requested a timezone conversion");

                        string opTimeZoneId = database.GetUsersTimeZone(msg.Author.Id)?.TimeZoneId;
                        string reactorTimeZoneId = database.GetUsersTimeZone(e.User.Id)?.TimeZoneId;

                        if (opTimeZoneId is null)
                        {
                            await reactor.SendMessageAsync("The original poster has not set up a time zone yet.");
                            return;
                        }

                        if (reactorTimeZoneId is null)
                        {
                            await channel.SendMessageAsync("You have not set up a time zone yet.");
                            return;
                        }

                        DateTimeZone opTimeZone = this.timeZoneProvider.GetZoneOrNull(opTimeZoneId);
                        DateTimeZone reactorTimeZone = this.timeZoneProvider.GetZoneOrNull(reactorTimeZoneId);
                        if (opTimeZone == null || reactorTimeZone == null)
                        {
                            await reactor.SendMessageAsync("There was a problem, please reach out to your bot developer.");
                            return;
                        }

                        IEnumerable<(string, DateTimeV2Value)> results = parserList.SelectMany(x => x.Values.Select(y => (x.Text, y)));
                        foreach ((string parsedText, DateTimeV2Value result) in results)
                        {
                            string outputString;
                            if (result.Type is DateTimeV2Type.Time)
                            {
                                DateTimeOffset messageDateTime = msg.Timestamp;
                                ZonedDateTime zonedMessageDateTime = ZonedDateTime.FromDateTimeOffset(messageDateTime);
                                LocalTime localParsedTime = (LocalTime)result.Value;
                                LocalDateTime localParsedDateTime = localParsedTime.On(zonedMessageDateTime.LocalDateTime.Date);
                                ZonedDateTime zonedOpDateTime = localParsedDateTime.InZoneStrictly(opTimeZone);
                                ZonedDateTime zonedReactorDateTime = zonedOpDateTime.WithZone(reactorTimeZone);
                                outputString = zonedReactorDateTime.LocalDateTime.TimeOfDay.ToString("t", null);
                            }
                            else
                            {
                                LocalDateTime localParsedDateTime = (LocalDateTime)result.Value;
                                ZonedDateTime zonedOpDateTime = localParsedDateTime.InZoneStrictly(opTimeZone);
                                ZonedDateTime zonedReactorDateTime = zonedOpDateTime.WithZone(reactorTimeZone);
                                outputString = zonedReactorDateTime.LocalDateTime.ToString("g", null);
                            }

                            reactorTimeEmbed
                                .AddField("Poster's Time", $"\"{parsedText}\"")
                                .AddField("Your time", $"{outputString}");
                        }
                        await reactor.SendMessageAsync(embed: reactorTimeEmbed);
                    }
                    catch (Exception exception)
                    {
                        this.logger.Log(LogLevel.Error, exception, "Error in sending reactor the DM");
                    }
                }
            });
        }

        public async Task SendEmbedWithMessageToChannelAsUser(ulong guildId, ulong userId, ulong channelId, string message, string title, string description)
        {
            try
            {
                DiscordClient shardClient = this.Client.GetShard(guildId);
                DiscordChannel channel = await shardClient.GetChannelAsync(channelId);
                DiscordUser poster = await shardClient.GetUserAsync(userId);
                this.Client.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                        .WithTitle(title)
                        .WithAuthor(poster.Username, iconUrl: poster.AvatarUrl)
                        .WithDescription(description)
                        .Build();
                await shardClient.SendMessageAsync(channel, content: message, embed: embed);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error in Sending Embed", guildId, userId, message, title, description);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private async Task<int> PrefixResolver(DiscordMessage msg)

        {
            using IBotAccessProvider dataAccessProvider = this.accessBuilder.Build();
            List<GuildPrefix> guildPrefixes = dataAccessProvider.GetAllAssociatedGuildPrefixes(msg.Channel.GuildId).ToList();

            if (!guildPrefixes.Any())
            {
                return msg.GetStringPrefixLength("^");
            }

            foreach (int length in guildPrefixes.Select(prefix => msg.GetStringPrefixLength(prefix.Prefix)).Where(length => length != -1))
            {
                return length;
            }

            return -1;
        }

        //private async Task EarnKarma(DiscordClient client, MessageCreateEventArgs e)
        //{
        //    if (e.Author.IsBot || e.Channel.IsPrivate)
        //    {
        //        return;
        //    }

        //    using IBotAccessProvider provider = this.accessBuilder.Build();
        //    GuildKarmaRecord userGuildKarmaRecord = provider.GetUsersGuildKarmaRecord(e.Author.Id, e.Guild.Id);
        //    lock (this.karmaLock)
        //    {
        //        if (!this.userKarmaAddition.Any(item => item.UserId == e.Author.Id && item.GuildId == e.Guild.Id))
        //        {
        //            userGuildKarmaRecord.CurrentKarma += (ulong)this.rng.Next(1, 4);
        //            this.userKarmaAddition.Add(userGuildKarmaRecord);
        //        }
        //    }
        //}

        //public async Task UpdateKarmas()
        //{
        //    using IBotAccessProvider provider = this.accessBuilder.Build();
        //    ISet<GuildKarmaRecord> bulkUpdate;
        //    lock (this.karmaLock)
        //    {
        //        bulkUpdate = new HashSet<GuildKarmaRecord>(this.userKarmaAddition);
        //        this.userKarmaAddition.Clear();
        //    }
        //    provider.BulkUpdateKarma(bulkUpdate);
        //}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}

// Possible candidate for testing async vs. sync ef core stuff.
//this.Client.MessageCreated += async (client, args) =>
//{
//    if (!args.Author.Id.Equals(this._devUserId))
//    {
//        return;
//    }

//    if (!args.Message.Content.StartsWith("async"))
//    {
//        return;
//    }

//    string argsString = args.Message.Content["async".Length..];

//    if (!int.TryParse(argsString, out int numIterations))
//    {
//        return;
//    }
//    args.Handled = true;
//    _ = Task.Run(async () =>
//    {
//        Random random = new Random();
//        Stopwatch stopwatch = new Stopwatch();
//        CommandsNextExtension cnext = client.GetCommandsNext();
//        List<Task> allTestTasks = new List<Task>();

//        string[] cmdStrings = new string[numIterations];
//        for (int i = 0; i < numIterations; i++)
//        {
//            ulong ulongRand;
//            byte[] buf = new byte[8];
//            random.NextBytes(buf);
//            ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
//            cmdStrings[i] = $"async true {ulongRand}";
//        }

//        string prefix = "c*";
//        stopwatch.Start();
//        for (int i = 0; i < numIterations; i++)
//        {
//            Command command = cnext.FindCommand(cmdStrings[i], out string rawArguments);
//            CommandContext context = cnext.CreateFakeContext(args.Author, args.Channel, prefix + cmdStrings[i], prefix, command, rawArguments);
//            allTestTasks.Add(Task.Run(async () => await cnext.ExecuteCommandAsync(context)));
//        }
//        await Task.WhenAll(allTestTasks);
//        stopwatch.Stop();
//        long asyncElapsedTime = stopwatch.ElapsedMilliseconds;

//        stopwatch.Reset();

//        cmdStrings = new string[numIterations];
//        for (int i = 0; i < numIterations; i++)
//        {
//            ulong ulongRand;
//            byte[] buf = new byte[8];
//            random.NextBytes(buf);
//            ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
//            cmdStrings[i] = $"async false {ulongRand}";
//        }

//        stopwatch.Start();
//        for (int i = 0; i < numIterations; i++)
//        {
//            Command command = cnext.FindCommand(cmdStrings[i], out string rawArguments);
//            CommandContext context = cnext.CreateFakeContext(args.Author, args.Channel, prefix + cmdStrings[i], prefix, command, rawArguments);
//            allTestTasks.Add(Task.Run(async () => await cnext.ExecuteCommandAsync(context)));
//        }
//        await Task.WhenAll(allTestTasks);
//        stopwatch.Stop();
//        long syncElapsedTime = stopwatch.ElapsedMilliseconds;
//        StringBuilder sb = new StringBuilder();
//        sb.Append("Async vs. Sync Database Calls");
//        sb.Append("\n");
//        sb.Append(new string('-', 20));
//        sb.Append("\n");
//        sb.Append($"Async Time: {asyncElapsedTime}");
//        sb.Append("\n");
//        sb.Append($"Sync Time: {syncElapsedTime}");
//        sb.Append("\n");
//        sb.Append(new string('-', 20));
//        sb.Append("\n");
//        sb.Append($"{(double)asyncElapsedTime / syncElapsedTime} async/sync ratio");
//        await args.Channel.SendMessageAsync(sb.ToString());
//    });
//};