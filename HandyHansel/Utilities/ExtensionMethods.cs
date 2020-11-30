using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HandyHansel.Utilities
{
    public static class ExtensionMethods
    {
        private static TaskCompletionSource<DiscordEventArgs> DiscordEventSubscriber { get; set; }

        public static async Task<CustomResult<T>> WaitForMessageAndPaginateOnMsg<T>(
               this CommandContext context,
               IEnumerable<Page> pages,
               Func<MessageCreateEventArgs, Task<(bool, T)>> messageValidationAndReturn,
               PaginationEmojis paginationEmojis = null,
               PaginationBehaviour behaviour = PaginationBehaviour.WrapAround,
               PaginationDeletion deletion = PaginationDeletion.DeleteEmojis,
               DiscordMessage msg = null)
        {
            List<Page> pagesList = pages.ToList();
            paginationEmojis ??= new PaginationEmojis();
            paginationEmojis.SkipLeft ??= DiscordEmoji.FromName(context.Client, ":track_previous:");
            paginationEmojis.Left ??= DiscordEmoji.FromName(context.Client, ":arrow_backward:");
            paginationEmojis.Right ??= DiscordEmoji.FromName(context.Client, ":arrow_forward:");
            paginationEmojis.SkipRight ??= DiscordEmoji.FromName(context.Client, ":track_next:");
            paginationEmojis.Stop ??= DiscordEmoji.FromName(context.Client, ":stop_button:");

            int currentPage = 0;
            if (msg == null)
            {
                msg = await context.RespondAsync(content: pagesList[currentPage].Content, embed: pagesList[currentPage].Embed);
            }
            else
            {
                await msg.ModifyAsync(content: pagesList[currentPage].Content, embed: pagesList[currentPage].Embed);
            }

            await msg.CreateReactionAsync(paginationEmojis.SkipLeft);
            await msg.CreateReactionAsync(paginationEmojis.Left);
            await msg.CreateReactionAsync(paginationEmojis.Right);
            await msg.CreateReactionAsync(paginationEmojis.SkipRight);
            await msg.CreateReactionAsync(paginationEmojis.Stop);


            async Task messageCreated(DiscordClient c, MessageCreateEventArgs a)
            {
                await Task.Run(() =>
                {
                    if (a.Channel.Id == context.Channel.Id && a.Author.Id == context.Member.Id)
                    {
                        DiscordEventSubscriber?.TrySetResult(a);
                    }
                });
            }

            async Task reactionAdded(DiscordClient c, MessageReactionAddEventArgs a)
            {
                await Task.Run(() =>
                {
                    if (a.Message.Id == msg.Id && a.User.Id == context.Member.Id)
                    {
                        DiscordEventSubscriber?.TrySetResult(a);
                    }
                });
            }

            async Task reactionRemoved(DiscordClient c, MessageReactionRemoveEventArgs a)
            {
                await Task.Run(() =>
                {
                    if (a.Message.Id == msg.Id && a.User.Id == context.Member.Id)
                    {
                        DiscordEventSubscriber?.TrySetResult(a);
                    }
                });
            }

            while (true)
            {
                DiscordEventSubscriber = new TaskCompletionSource<DiscordEventArgs>();
                context.Client.MessageCreated += messageCreated;
                context.Client.MessageReactionAdded += reactionAdded;
                context.Client.MessageReactionRemoved += reactionRemoved;

                await Task.WhenAny(DiscordEventSubscriber.Task, Task.Delay(60000));

                context.Client.MessageCreated -= messageCreated;
                context.Client.MessageReactionAdded -= reactionAdded;
                context.Client.MessageReactionRemoved -= reactionRemoved;

                if (!DiscordEventSubscriber.Task.IsCompleted)
                {
                    return new CustomResult<T>(timedOut: true);
                }

                DiscordEventArgs discordEvent = DiscordEventSubscriber.Task.Result;
                DiscordEventSubscriber = null;

                if (discordEvent is MessageCreateEventArgs messageEvent)
                {

                    (bool success, T messageCreateFuncResult) = await messageValidationAndReturn(messageEvent);

                    if (success)
                    {
                        switch (deletion)
                        {
                            case PaginationDeletion.DeleteEmojis:
                                await msg.DeleteAllReactionsAsync();
                                break;
                            case PaginationDeletion.DeleteMessage:
                                await msg.DeleteAsync();
                                break;
                            default:
                                break;
                        }
                        await messageEvent.Message.DeleteAsync();
                        return new CustomResult<T>(result: messageCreateFuncResult);
                    }
                    else
                    {
                        _ = Task.Run(async () =>
                        {
                            DiscordMessage invalid = await messageEvent.Channel.SendMessageAsync("Invalid Input");
                            Thread.Sleep(5000);
                            await messageEvent.Channel.DeleteMessagesAsync(new List<DiscordMessage> { messageEvent.Message, invalid });
                        });
                    }
                }

                if (discordEvent is MessageReactionAddEventArgs || discordEvent is MessageReactionRemoveEventArgs)
                {
                    DiscordEmoji reactEmoji = discordEvent switch
                    {
                        MessageReactionAddEventArgs addReact => addReact.Emoji,
                        MessageReactionRemoveEventArgs deleteReact => deleteReact.Emoji,
                        _ => throw new Exception("Somehow, something happened that caused an event that I know to be a reaction add or remove to suddenly stop being that. XD.")
                    };


                    if (reactEmoji.Equals(paginationEmojis.SkipLeft))
                    {
                        currentPage = 0;
                    }
                    else if (reactEmoji.Equals(paginationEmojis.Left))
                    {
                        currentPage = (--currentPage < 0, behaviour) switch
                        {
                            (true, PaginationBehaviour.Ignore) => 0,
                            (true, PaginationBehaviour.WrapAround) => pagesList.Count - 1,
                            _ => currentPage
                        };
                    }
                    else if (reactEmoji.Equals(paginationEmojis.Right))
                    {
                        int count = pagesList.Count;
                        currentPage = (++currentPage == pagesList.Count, behaviour) switch
                        {
                            (true, PaginationBehaviour.Ignore) => pagesList.Count - 1,
                            (true, PaginationBehaviour.WrapAround) => 0,
                            _ => currentPage
                        };
                    }
                    else if (reactEmoji.Equals(paginationEmojis.SkipRight))
                    {
                        currentPage = pagesList.Count - 1;
                    }

                    if (reactEmoji.Equals(paginationEmojis.Stop))
                    {
                        switch (deletion)
                        {
                            case PaginationDeletion.DeleteEmojis:
                                await msg.DeleteAllReactionsAsync();
                                break;
                            case PaginationDeletion.DeleteMessage:
                                await msg.DeleteAsync();
                                break;
                            default:
                                break;
                        }
                        return new CustomResult<T>(cancelled: true);
                    }
                    else
                    {
                        await msg.ModifyAsync(embed: pagesList[currentPage].Embed);
                    }
                }
            }
        }

        //public static string WithDiscordMarkdownStripped(this string content)
        //{
        //    Regex discordMarkdownCharacters = new Regex("");

        //    MatchEvaluator evaluator = new MatchEvaluator((match) => "\\" + match.Value);

        //    return discordMarkdownCharacters.Replace(content, evaluator);
        //}

        public static string AsHumanReadableString(this Period period)
        {
            StringBuilder humanReadableString = new StringBuilder();

            Period normalizedPeriod = period.Normalize();

            if (normalizedPeriod.Years > 0)
            {
                humanReadableString.Append($"{normalizedPeriod.Years} years");
            }

            if (normalizedPeriod.Weeks > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Weeks} weeks");
            }

            if (normalizedPeriod.Days > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Days} days");
            }

            if (normalizedPeriod.Hours > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Hours} hours");
            }

            if (normalizedPeriod.Minutes > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Minutes} minutes");
            }

            if (normalizedPeriod.Seconds > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Seconds} seconds");
            }

            if (normalizedPeriod.Milliseconds > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Milliseconds} milliseconds");
            }

            if (normalizedPeriod.Nanoseconds > 0)
            {
                humanReadableString.Append(humanReadableString.Length > 0 ? ", " : "").Append($"{normalizedPeriod.Nanoseconds} nanoseconds");
            }

            return humanReadableString.ToString();
        }
    }

    public static class PaginationMessageFunction
    {
        /// <summary>
        /// Creates a function that will return a bool stating whether the message was accepted and a int that was parsed from the message.
        /// </summary>
        /// <param name="user">The user's whose messages to watch</param>
        /// <param name="channel">The channel to watch the messages from</param>
        /// <param name="min">The inclusive minimum the result should be</param>
        /// <param name="max">The exclusive maximum the result should be</param>
        /// <returns>The function to be used for the custom pagination solution</returns>
        public static Func<MessageCreateEventArgs, Task<(bool, int)>> CreateWaitForMessageWithIntInRange(DiscordUser user, DiscordChannel channel, int min, int max)
        {
            return new Func<MessageCreateEventArgs, Task<(bool, int)>>((eventArgs) =>
            {
                if (eventArgs.Channel.Equals(channel) && eventArgs.Author.Equals(user) && int.TryParse(eventArgs.Message.Content, out int eventToChoose) && eventToChoose >= min && eventToChoose < max)
                {
                    return Task.FromResult((true, eventToChoose));
                }
                else
                {
                    return Task.FromResult((false, -1));
                }
            });
        }
    }
}
