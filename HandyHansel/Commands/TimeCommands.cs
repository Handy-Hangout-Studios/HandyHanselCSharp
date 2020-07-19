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
    [Group("time"), Description("All commands associated with current time functionality.")]
    public class TimeCommands : BaseCommandModule
    {
        // The list of all system time zones
        private readonly List<TimeZoneInfo> _systemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToList();

        // Singleton of systemTimeZonePages
        private Page[] _systemTimeZonePages = null;

        private IEnumerable<Page> GetSystemTimeZonePages(InteractivityExtension interactivity)
        {
            // If the singleton is not null then we should just return it.
            if (!(_systemTimeZonePages is null)) return _systemTimeZonePages;

            string allPossibleTimesString = "";
            for (int i = 0; i < _systemTimeZones.Count; i++)
            {
                allPossibleTimesString += $"{i + 1}. {_systemTimeZones[i].Id}\n";
            }

            DiscordEmbedBuilder baseDiscordEmbed = new DiscordEmbedBuilder
            {
                Title = "Choose a time!",
            };

            _systemTimeZonePages =
                interactivity.GeneratePagesInEmbed(allPossibleTimesString, SplitType.Line, baseDiscordEmbed);

            return _systemTimeZonePages;
        }

        // Singleton map of Guild Time Zone Infos
        
        
        // Singleton map of Guild Times
        private readonly Dictionary<ulong, Page[]> _guildTimeZonePages;

        private IEnumerable<Page> GetGuildTimeZonePages(ulong guildId, InteractivityExtension interactivity)
        {
            if (_guildTimeZonePages.ContainsKey(guildId)) return _guildTimeZonePages[guildId];
            string description = "";
            int count = 1;
            List<string> guildAssociatedTimeZones = DataAccessProvider.GetAllAssociatedGuildTimeZones(guildId.ToString()).Select(gtz => gtz.TimeZoneId).ToList();
            foreach (TimeZoneInfo timezone in _systemTimeZones.Where(timezone => guildAssociatedTimeZones.Contains(timezone.Id)))
            {
                description += $"{count}. {timezone.Id}\n";
                count++;
            }
            
            _guildTimeZonePages[guildId] = interactivity.GeneratePagesInEmbed(description, SplitType.Line);
            return _guildTimeZonePages[guildId];
        }
        

        private IDataAccessProvider DataAccessProvider { get; }

        public TimeCommands(PostgreSqlContext sqlContext, IDataAccessProvider dataAccessProvider)
        {
            DataAccessProvider = dataAccessProvider;
            _guildTimeZonePages = new Dictionary<ulong, Page[]>();
        }

        [GroupCommand, Description("Allows a user to select a time zone and Handy Hansel will say what time it is there.")]
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            await context.RespondAsync($":wave: Hi, {context.User.Mention}! What timezone do you want the time for?");
            InteractivityExtension interactivity = context.Client.GetInteractivity();
            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User, GetGuildTimeZonePages(context.Guild.Id, interactivity), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(5));
            await context.RespondAsync($"Choose a timezone by typing: <timezone number>");
            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User), timeoutoverride: TimeSpan.FromMinutes(5));

            if (!result.TimedOut)
            {
                DateTime currentTime = DateTime.Now;
                TimeZoneInfo requestedTimeZone = GetListOfTimeZones(context.Guild.Id)[(int.Parse(result.Result.Content) - 1)];
                DateTime requestedTime = TimeZoneInfo.ConvertTime(currentTime, TimeZoneInfo.Local, requestedTimeZone);
                DiscordEmbed timeEmbed = new DiscordEmbedBuilder
                {
                    Title = "Current Time: " + requestedTimeZone.Id,
                    Description = $"{requestedTime}",
                };
                await context.RespondAsync(embed: timeEmbed);
            }

        }
        

        /// <summary>
        /// Get list of all guild time zones as a list of TimeZoneInfo's that can then be used to calculate times.
        /// </summary>
        /// <param name="guildId">The guild id of the guild time zones we are pulling</param>
        /// <returns>The list of Time Zone Infos associated with the passed guild id.</returns>
        List<TimeZoneInfo> GetListOfTimeZones(ulong guildId)
        {
            List<TimeZoneInfo> timeZoneInfos = new List<TimeZoneInfo>();
            List<string> guildAssociatedTimeZones = DataAccessProvider.GetAllAssociatedGuildTimeZones(guildId.ToString()).Select(gtz => gtz.TimeZoneId).ToList();
            foreach (TimeZoneInfo timezone in _systemTimeZones)
            {
                if (guildAssociatedTimeZones.Contains(timezone.Id))
                    timeZoneInfos.Add(timezone);
            }
            return timeZoneInfos;
        }

        [Command("addtime"), Description("Allows a user to add a new time zone to the time zone options for the guild")]
        public async Task AddTimeZoneToDb(CommandContext context)
        {
            try
            {
                InteractivityExtension interactivity = context.Client.GetInteractivity();
                
                _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User, GetSystemTimeZonePages(interactivity), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(5));
                await context.RespondAsync($"Choose a timezone by typing: <timezone number>");
                InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync(xm => int.TryParse(xm.Content, out _), timeoutoverride: TimeSpan.FromMinutes(5));
                
                if (!msg.TimedOut)
                {
                    string operatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                    string guild = context.Guild.Id.ToString();
                    string timeZoneId = _systemTimeZones[(int.Parse(msg.Result.Content)-1)].Id;
                    GuildTimeZone newGuildTimeZone = new GuildTimeZone { Guild = guild, TimeZoneId = timeZoneId, OperatingSystem = operatingSystem };
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
            string description = "";
            for (int i = 0; i < _systemTimeZones.Count; i++)
            {
                description += _systemTimeZones[i].Id + (i == _systemTimeZones.Count - 1 ? " " : "\n\n");
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
