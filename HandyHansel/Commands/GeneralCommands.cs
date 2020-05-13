using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
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
        [Command("hi")]
        public async Task Hi(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}!");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            InteractivityResult<DSharpPlus.Entities.DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == context.User.Id && xm.Content.ToLower() == "how are you?", TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
                await context.RespondAsync($"I'm fine, thank you!");
        }

        [Command("time")]
        public async Task CurrentTime(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}! What timezone do you want the time for?");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            List<TimeZoneInfo> correctResponses = getListOfTimeZones();

            IDictionary<int, DiscordEmoji> discordEmojiNumbers = new Dictionary<int, DiscordEmoji>
            {
                { 0, DiscordEmoji.FromName(context.Client, ":zero:") },
                { 1, DiscordEmoji.FromName(context.Client, ":one:") },
                { 2, DiscordEmoji.FromName(context.Client, ":two:") },
                { 3, DiscordEmoji.FromName(context.Client, ":three:") },
                { 4, DiscordEmoji.FromName(context.Client, ":four:") },
                { 5, DiscordEmoji.FromName(context.Client, ":five:") },
                { 6, DiscordEmoji.FromName(context.Client, ":six:") },
                { 7, DiscordEmoji.FromName(context.Client, ":seven:") },
                { 8, DiscordEmoji.FromName(context.Client, ":eight:") },
                { 9, DiscordEmoji.FromName(context.Client, ":nine:") },
                { 10, DiscordEmoji.FromName(context.Client, ":keycap_ten:") },
            };

            discordEmojiNumbers = discordEmojiNumbers.Where(keyValuePair => keyValuePair.Key < correctResponses.Count).ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            string description = "";
            for (int i = 0; i < correctResponses.Count; i++)
            {
                description += discordEmojiNumbers[i].ToString() + " " + correctResponses[i].Id + (i == correctResponses.Count - 1 ? " " : "\n\n");
            }

            DiscordEmbed embed = new DiscordEmbedBuilder
            {
                Title = "Choose a time!",
                Description = description,
            };

            DiscordMessage msg = await context.RespondAsync(embed: embed);

            for (int i = 0; i < correctResponses.Count && discordEmojiNumbers.ContainsKey(i); i++)
            {
                await msg.CreateReactionAsync(discordEmojiNumbers[i]);
            }

            InteractivityResult<DSharpPlus.EventArgs.MessageReactionAddEventArgs> result =
                await interactivity.WaitForReactionAsync(reaction =>
                    discordEmojiNumbers.Any(keyValuePair =>
                    {
                        return keyValuePair.Value.Equals(reaction.Emoji) && !reaction.User.IsBot && reaction.Message.Id == msg.Id;
                    }
                    )
                );

            if (!result.TimedOut)
            {
                int discordEmojiValue = discordEmojiNumbers.First(keyValuePair => keyValuePair.Value.Equals(result.Result.Emoji)).Key;
                DateTime currentTime = DateTime.Now;
                TimeZoneInfo requestedTimeZone = correctResponses[discordEmojiValue];
                DateTime requestedTime = TimeZoneInfo.ConvertTime(currentTime, TimeZoneInfo.Local, requestedTimeZone);
                DiscordEmbed timeEmbed = new DiscordEmbedBuilder
                {
                    Title = "Current Time: " + requestedTimeZone.Id,
                    Description = $"{requestedTime}",
                };
                await context.RespondAsync(embed: timeEmbed);
            }
        }

        [Command("allTimes")]
        public async Task AllTimesOnSystem(CommandContext context)
        {
            if (context.User.Username != "ProfDoof") return;
            List<TimeZoneInfo> correctResponses = getListOfTimeZones();
            string description = "";
            for (int i = 0; i < correctResponses.Count; i++)
            {
                description += correctResponses[i].Id + (i == correctResponses.Count - 1 ? " " : "\n\n");
            }
            DiscordEmbed embed = new DiscordEmbedBuilder
            {
                Title = "All System Times",
                Description = description,
            };

            await context.RespondAsync(embed: embed);
        }

        [Command("test")]
        public async Task TestingCICD(CommandContext context)
        {
            await context.RespondAsync($"{context.User.Mention}! It WORKED! IT WORKED! IT REALLY REALLY WORKED!");
        }

        List<TimeZoneInfo> getListOfTimeZones()
        {
            List<TimeZoneInfo> timeZoneInfos = new List<TimeZoneInfo>();
            System.Collections.ObjectModel.ReadOnlyCollection<TimeZoneInfo> allTimeZones = TimeZoneInfo.GetSystemTimeZones();
            foreach (TimeZoneInfo timezone in TimeZoneInfo.GetSystemTimeZones())
            {
                if (timezone.Id.Equals("Central Standard Time") )
                    timeZoneInfos.Add(timezone);
                else if (timezone.Id.Equals("AUS Eastern Standard Time"))
                    timeZoneInfos.Add(timezone);
            }
            return timeZoneInfos;
        }
    }
}
