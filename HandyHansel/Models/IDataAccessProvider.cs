using System;
using System.Collections.Generic;

namespace HandyHansel.Models
{
    public interface IDataAccessProvider : IDisposable
    {
        #region User Time Zones
        
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
        /// Delete a specified user time zone from the database
        /// </summary>
        /// <param name="userTimeZone">An object that stores a user id, time zone id, and operating system that this user time zone will belong to</param>
        void DeleteUserTimeZone(UserTimeZone userTimeZone);

        /// <summary>
        /// Get every user time zone currently in the database
        /// </summary>
        /// <returns>List of user time zones</returns>
        IEnumerable<UserTimeZone> GetUserTimeZones();

        /// <summary>
        /// Get user time zone that is associated with a specified user using the user id.
        /// </summary>
        /// <param name="userId">The user id that the time zone should be associated with</param>
        /// <returns>users time zone</returns>
        UserTimeZone GetUsersTimeZone(ulong userId);
        
        #endregion
        
        #region Guild Events
        
        /// <summary>
        /// Add a new guild event to the database
        /// </summary>
        /// <param name="guildEvent">Guild event object that has the guild id, the event name, and the event desc</param>
        void AddGuildEvent(GuildEvent guildEvent);

        /// <summary>
        /// Delete a guild event from the database
        /// </summary>
        /// <param name="guildEvent">Guild event object with at minimum the guild event id</param>
        void DeleteGuildEvent(GuildEvent guildEvent);

        /// <summary>
        /// Gets all guild events associated with the guild id passed in.
        /// </summary>
        /// <param name="guildId">Guild ID to search for associations</param>
        /// <returns>Guild events associated with Guild ID</returns>
        IEnumerable<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId);

        #endregion
        
        #region Scheduled Events
        
        /// <summary>
        /// Adds an event to the scheduling database to be used by consumers for determining when an event should be
        /// fired off. 
        /// </summary>
        /// <param name="scheduledEvent">Scheduled Event object containing the scheduled date, guild event id, and channel id.</param>
        void AddScheduledEvent(ScheduledEvent scheduledEvent);

        void SetEventAnnounced(ScheduledEvent scheduledEvent);

        void SaveAnnouncedEvents();

        /// <summary>
        /// Permanently deletes a scheduled event from the database 
        /// </summary>
        /// <param name="scheduledEvent">The scheduled event object with at minimum the scheduled event id.</param>
        void DeleteScheduledEvent(ScheduledEvent scheduledEvent);

        /// <summary>
        /// Gets all possible scheduled events associated with some specified guild id.
        /// </summary>
        /// <param name="guildId">Guild ID</param>
        /// <returns>List of possible scheduled events</returns>
        IEnumerable<ScheduledEvent> GetAllScheduledEventsForGuild(ulong guildId);

        /// <summary>
        /// Gets all scheduled events that have a scheduled date specified in the past according to the
        /// amountOfTimeInFuture passed in.
        ///
        /// Example: amountOfTimeInFuture = 10 mins
        ///          Current Time = 12 noon
        /// Returns: All events that are scheduled for 12:10 in the afternoon or earlier on this particular day.
        /// </summary>
        /// <param name="amountOfTimeInFuture">Amount of time in the future you want to view</param>
        /// <returns></returns>
        IEnumerable<ScheduledEvent> GetAllPastScheduledEvents(TimeSpan? amountOfTimeInFuture = null);
        
        #endregion
        
        #region Guild Prefixes

        /// <summary>
        /// Add a guild prefix to the database
        /// </summary>
        /// <param name="prefix">A guild prefix object with prefix and guild id</param>
        void AddGuildPrefix(GuildPrefix prefix);

        /// <summary>
        /// Delete a guild prefix from the database
        /// </summary>
        /// <param name="prefix">A guild prefix object with at minimum guild prefix id.</param>
        void DeleteGuildPrefix(GuildPrefix prefix);

        /// <summary>
        /// Gets all guild prefixes associated with a specific guild id.
        /// </summary>
        /// <param name="guildId">Guild id</param>
        /// <returns>enumerable of all guild prefixes associated with guild id</returns>
        IEnumerable<GuildPrefix> GetAllAssociatedGuildPrefixes(ulong guildId);

        #endregion
    }
}
