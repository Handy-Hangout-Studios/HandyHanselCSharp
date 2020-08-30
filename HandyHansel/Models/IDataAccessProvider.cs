using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel.Models
{
    public interface IDataAccessProvider
    {
        /// <summary>
        /// Adds a new user time zone to the database
        /// </summary>
        /// <param name="userTimeZone">An object that stores a user id, time zone id, and operating system that this guildTimeZone will belong to</param>
        void AddUserTimeZone(UserTimeZone userTimeZone);

        /// <summary>
        /// Update a users time zone
        /// </summary>
        /// <param name="userTimeZone"></param>
        void UpdateUserTimeZone(UserTimeZone userTimeZone);

        /// <summary>
        /// Delete a specified guild time zone from the database
        /// </summary>
        /// <param name="userTimeZone">An object that stores a guild id, time zone id, and operating system that this guildTimeZone will belong to</param>
        void DeleteUserTimeZone(UserTimeZone userTimeZone);

        /// <summary>
        /// Get every guild time zone currently in the database
        /// </summary>
        /// <returns>List of guild time zones</returns>
        List<UserTimeZone> GetUserTimeZones();

        /// <summary>
        /// Get every guild time zone that is associated with a specified guild using the guild id.
        /// </summary>
        /// <param name="userId">The guild id that the time zones should be associated with</param>
        /// <returns>List of guild time zones</returns>
        UserTimeZone GetUsersTimeZone(ulong userId);
        
        void AddGuildEvent(GuildEvent guildEvent);

        void DeleteGuildEvent(GuildEvent guildEvent);

        List<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId);

        void AddScheduledEvent(ScheduledEvent scheduledEvent);

        void DeleteScheduledEvent(ScheduledEvent scheduledEvent);

        List<ScheduledEvent> GetAllScheduledEventsForGuild(ulong guildId);

        List<ScheduledEvent> GetAllPastScheduledEvents();
    }
}
