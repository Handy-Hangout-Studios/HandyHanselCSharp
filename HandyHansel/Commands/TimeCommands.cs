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
    [Group("time"), Description("All commands associated with current time functionalities.")]
    public class TimeCommands : BaseCommandModule
    {
        private static readonly List<TimeZoneInfo> systemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToList();

        public PostgreSqlContext DbContext { get; }
        public IDataAccessProvider DataAccessProvider { get; }

        public TimeCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DbContext = sqlContext;
            DataAccessProvider = dataAccessProvider;
        }

        private static Boolean currentlyPresentingGuildTimes = false;

        [GroupCommand, Description("Allows a user to select a time zone and Handy Hansel will say what time it is there.")]
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            if (!currentlyPresentingGuildTimes)
            {
                await context.RespondAsync($":wave: Hi, {context.User.Mention}! What timezone do you want the time for?");
                List<TimeZoneInfo> associatedTimeZones = getListOfTimeZones(context.Guild.Id.ToString());
                InteractivityExtension interactivity = context.Client.GetInteractivity();
                _ = PresentAssociatedTimeZones(associatedTimeZones, interactivity, context.Channel, context.User);
                await context.RespondAsync($"Choose a timezone by typing: ^time <timezone number>");
                InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => xm.Content.StartsWith("^time"), timeoutoverride: TimeSpan.FromMinutes(5));

                if (!result.TimedOut)
                {
                    DateTime currentTime = DateTime.Now;
                    TimeZoneInfo requestedTimeZone = associatedTimeZones[(int.Parse(result.Result.Content.Substring("^time".Length)) - 1)];
                    DateTime requestedTime = TimeZoneInfo.ConvertTime(currentTime, TimeZoneInfo.Local, requestedTimeZone);
                    DiscordEmbed timeEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Current Time: " + requestedTimeZone.Id,
                        Description = $"{requestedTime}",
                    };
                    await context.RespondAsync(embed: timeEmbed);
                }
            }
            else
            {
                await context.RespondAsync($"Before you attempt to ask for another time please press the :stop_button: button on the embed above.");
            }
        }

        private async Task PresentAssociatedTimeZones(List<TimeZoneInfo> associatedTimeZones, InteractivityExtension interactivity, DiscordChannel channel, DiscordUser user)
        {
            currentlyPresentingGuildTimes = true;

            string description = "";
            for (int i = 0; i < associatedTimeZones.Count; i++)
            {
                description += $"{i + 1}. {associatedTimeZones[i].Id}\n";
            }

            Page[] correctResponsesPages = interactivity.GeneratePagesInEmbed(description, SplitType.Line);
            await interactivity.SendPaginatedMessageAsync(channel, user, correctResponsesPages, behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(5));
            currentlyPresentingGuildTimes = false;
        }

        List<TimeZoneInfo> getListOfTimeZones(string guildId)
        {
            List<TimeZoneInfo> timeZoneInfos = new List<TimeZoneInfo>();
            List<string> guildAssociatedTimeZones = DataAccessProvider.GetAllAssociatedGuildTimeZones(guildId).Select(gtz => gtz.TimeZoneId).ToList();
            foreach (TimeZoneInfo timezone in systemTimeZones)
            {
                if (guildAssociatedTimeZones.Contains(timezone.Id))
                    timeZoneInfos.Add(timezone);
            }
            return timeZoneInfos;
        }

        private static Boolean currentlyPresentingSystemTimes = false;

        [Command("addTime"), Description("Allows a user to add a new time zone to the time zone options for the guild")]
        public async Task AddTimeZoneToDb(CommandContext context)
        {
            try
            {
                InteractivityExtension interactivity = context.Client.GetInteractivity();

                if (!currentlyPresentingSystemTimes)
                {
                    _ = PresentSystemTimes(interactivity, context.Channel, context.User);
                }
                await context.RespondAsync($"Choose a timezone by typing: ^addtime <timezone number>");
                InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync(xm => xm.Content.StartsWith("^addtime"), timeoutoverride: TimeSpan.FromMinutes(5));
                
                if (!msg.TimedOut)
                {
                    string operating_system = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                    string guild = context.Guild.Id.ToString();
                    string timeZoneId = systemTimeZones[(int.Parse(msg.Result.Content.Substring("^addtime".Length))-1)].Id;
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

        private async Task PresentSystemTimes(InteractivityExtension interactivity, DiscordChannel channel, DiscordUser user)
        {
            string allPossibleTimesString = "";
            for (int i = 0; i < systemTimeZones.Count; i++)
            {
                allPossibleTimesString += $"{i + 1}. {systemTimeZones[i].Id}\n";
            }

            DiscordEmbedBuilder baseDiscordEmbed = new DiscordEmbedBuilder
            {
                Title = "Choose a time!",
            };

            Page[] allPossibleTimesPages = interactivity.GeneratePagesInEmbed(allPossibleTimesString, SplitType.Line, baseDiscordEmbed);
            currentlyPresentingSystemTimes = true;
            await interactivity.SendPaginatedMessageAsync(channel, user, allPossibleTimesPages, behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(5));
            currentlyPresentingSystemTimes = false;
        }

        [Command("allTimes"), RequireUserPermissions(Permissions.Administrator), Hidden]
        public async Task AllTimesOnSystem(CommandContext context)
        {
            string description = "";
            for (int i = 0; i < systemTimeZones.Count; i++)
            {
                description += systemTimeZones[i].Id + (i == systemTimeZones.Count - 1 ? " " : "\n\n");
            }

            System.IO.File.WriteAllText(@"AllTimes.txt", description);

            await context.RespondWithFileAsync(@"AllTimes.txt");
        }

        [Command("currentDbTimes"), RequireUserPermissions(Permissions.Administrator), Hidden]
        public async Task CurrentTimeZonesOnDb(CommandContext context)
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
    }
}
