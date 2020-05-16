using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using HandyHansel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyHansel.Commands
{
    [Group("time")]
    public class TimeCommands : BaseCommandModule
    {
        public PostgreSqlContext DbContext { get; }
        public IDataAccessProvider DataAccessProvider { get; }

        public TimeCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DbContext = sqlContext;
            DataAccessProvider = dataAccessProvider;
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}! What timezone do you want the time for?");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            List<TimeZoneInfo> correctResponses = getListOfTimeZones(context.Guild.Id.ToString());
            
            string description = "";
            for (int i = 0; i < correctResponses.Count; i++)
            {
                description += $"{i + 1}. {correctResponses[i].Id}\n";
            }

            Page[] correctResponsesPages = interactivity.GeneratePagesInEmbed(description, SplitType.Line);
            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User, correctResponsesPages, behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(5));
            await context.RespondAsync($"Choose a timezone by typing: ^time <timezone number>");
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Content.StartsWith("^time"), timeoutoverride: TimeSpan.FromMinutes(5));

            if (!result.TimedOut)
            {
                DateTime currentTime = DateTime.Now;
                TimeZoneInfo requestedTimeZone = correctResponses[(int.Parse(result.Result.Content.Substring("^time".Length)) - 1)];
                DateTime requestedTime = TimeZoneInfo.ConvertTime(currentTime, TimeZoneInfo.Local, requestedTimeZone);
                DiscordEmbed timeEmbed = new DiscordEmbedBuilder
                {
                    Title = "Current Time: " + requestedTimeZone.Id,
                    Description = $"{requestedTime}",
                };
                await context.RespondAsync(embed: timeEmbed);
            }
        }

        List<TimeZoneInfo> getListOfTimeZones(string guildId)
        {
            List<TimeZoneInfo> timeZoneInfos = new List<TimeZoneInfo>();
            System.Collections.ObjectModel.ReadOnlyCollection<TimeZoneInfo> allTimeZones = TimeZoneInfo.GetSystemTimeZones();
            foreach (TimeZoneInfo timezone in TimeZoneInfo.GetSystemTimeZones())
            {
                if (DataAccessProvider.GetAllAssociatedGuildTimeZones(guildId).Select(gtz => gtz.TimeZoneId).Contains(timezone.Id))
                    timeZoneInfos.Add(timezone);
            }
            return timeZoneInfos;
        }

        [Command("addTime")]
        public async Task AddTimeZoneToDb(CommandContext context)
        {
            try
            {
                InteractivityExtension interactivity = context.Client.GetInteractivity();

                // Builds a GuildTimeZone object and adds it into the guild time zone database.
                List<TimeZoneInfo> allPossibleTimes = TimeZoneInfo.GetSystemTimeZones().ToList();
                string allPossibleTimesString = "";
                for (int i = 0; i < allPossibleTimes.Count; i++)
                {
                    allPossibleTimesString += $"{i+1}. {allPossibleTimes[i].Id}\n";
                }

                DiscordEmbedBuilder baseDiscordEmbed = new DiscordEmbedBuilder
                {
                    Title = "Choose a time!",
                };
                Page[] allPossibleTimesPages = interactivity.GeneratePagesInEmbed(allPossibleTimesString, SplitType.Line, baseDiscordEmbed);
                _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User, allPossibleTimesPages, behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(5));
                await context.RespondAsync($"Choose a timezone by typing: ^addTimeZone <timezone number>");
                InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync(xm => xm.Content.StartsWith("^addTimeZone"), timeoutoverride: TimeSpan.FromMinutes(5));
                
                if (!msg.TimedOut)
                {
                    string operating_system = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                    string guild = context.Guild.Id.ToString();
                    string timeZoneId = allPossibleTimes[(int.Parse(msg.Result.Content.Substring("^addTimeZone".Length))-1)].Id;
                    GuildTimeZone newGuildTimeZone = new GuildTimeZone { Guild = guild, TimeZoneId = timeZoneId, OperatingSystem = operating_system };
                    DataAccessProvider.AddGuildTimeZone(newGuildTimeZone);
                    await context.RespondAsync($"I added { timeZoneId } to the timezone options for this guild.");
                } 
            }
            catch
            {
                await context.RespondAsync($"I failed to add any timezone to to the timezone options for this guild.");
            }
        }

        [Command("allTimes"), RequireUserPermissions(Permissions.Administrator), Hidden]
        public async Task AllTimesOnSystem(CommandContext context)
        {
            List<TimeZoneInfo> correctResponses = TimeZoneInfo.GetSystemTimeZones().ToList();
            string description = "";
            for (int i = 0; i < correctResponses.Count; i++)
            {
                description += correctResponses[i].Id + (i == correctResponses.Count - 1 ? " " : "\n\n");
            }

            System.IO.File.WriteAllText(@"AllTimes.txt", description);

            await context.RespondWithFileAsync(@"AllTimes.txt");
        }

        [Command("currentDbTimes"), RequireUserPermissions(Permissions.Administrator), Hidden]
        public async Task CurrentTimeZonesOnDb(CommandContext context)
        {
            try
            {
                List<GuildTimeZone> allGuildTimeZones = DataAccessProvider.GetAllGuildsTimeZones();
                string description = "Guild,TimeZoneId,OperatingSystem\n";
                for (int i = 0; i < allGuildTimeZones.Count; i++)
                {
                    description += allGuildTimeZones[i].Guild + "," + allGuildTimeZones[i].TimeZoneId + "," + allGuildTimeZones[i].OperatingSystem + (i == allGuildTimeZones.Count - 1 ? " " : "\n");
                }

                System.IO.File.WriteAllText(@"CurrentDbTimes.csv", description);

                await context.RespondWithFileAsync(@"CurrentDbTimes.csv");
            }
            catch (Exception e)
            {
                await context.RespondAsync($"Exception Thrown: { e.ToString() }");
            }
        }
    }
}
