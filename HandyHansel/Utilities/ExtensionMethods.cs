using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Emzi0767.Utilities;
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
        public static TaskCompletionSource<DiscordEventArgs> discordEventSubscriber;

        public static async Task<CustomResult<T>> WaitForMessagePaginationOnMsg<T>(
               this InteractivityExtension interactivity,
               CommandContext context,
               IEnumerable<Page> pages,
               Func<MessageCreateEventArgs, Task<(bool, T)> > messageValidationAndReturn,
               PaginationEmojis paginationEmojis = null,
               PaginationBehaviour behaviour = PaginationBehaviour.WrapAround,
               PaginationDeletion deletion = PaginationDeletion.DeleteEmojis,
               DiscordMessage msg = null)
        {
            List<Page> pagesList = pages.ToList();
            paginationEmojis ??= new PaginationEmojis
            {
                SkipLeft = DiscordEmoji.FromName(context.Client, ":track_previous:"),
                Left = DiscordEmoji.FromName(context.Client, ":arrow_backward:"),
                Right = DiscordEmoji.FromName(context.Client, ":arrow_forward:"),
                SkipRight = DiscordEmoji.FromName(context.Client, ":track_next:"),
                Stop = DiscordEmoji.FromName(context.Client, ":stop_button:")
            };

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
                if (a.Channel.Id == context.Channel.Id && a.Author.Id == context.Member.Id)
                    discordEventSubscriber?.TrySetResult(a);
            }

            async Task reactionAdded(DiscordClient c, MessageReactionAddEventArgs a)
            {
                if (a.Message.Id == msg.Id && a.User.Id == context.Member.Id)
                    discordEventSubscriber?.TrySetResult(a);
            }

            async Task reactionRemoved(DiscordClient c, MessageReactionRemoveEventArgs a)
            {
                if (a.Message.Id == msg.Id && a.User.Id == context.Member.Id)
                    discordEventSubscriber?.TrySetResult(a);
            }

            while (true)
            {
                discordEventSubscriber = new TaskCompletionSource<DiscordEventArgs>();
                interactivity.Client.MessageCreated += messageCreated;
                interactivity.Client.MessageReactionAdded += reactionAdded;
                interactivity.Client.MessageReactionRemoved += reactionRemoved;

                await Task.WhenAny(discordEventSubscriber.Task, Task.Delay(60000));

                interactivity.Client.MessageCreated -= messageCreated;
                interactivity.Client.MessageReactionAdded -= reactionAdded;
                interactivity.Client.MessageReactionRemoved -= reactionRemoved;

                if (!discordEventSubscriber.Task.IsCompleted)
                {
                    return new CustomResult<T>(timedOut: true);
                }

                DiscordEventArgs discordEvent = discordEventSubscriber.Task.Result;
                discordEventSubscriber = null;

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
                            await messageEvent.Channel.DeleteMessagesAsync(new List<DiscordMessage> { messageEvent.Message, invalid});
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
                        switch(deletion)
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
    }
}
