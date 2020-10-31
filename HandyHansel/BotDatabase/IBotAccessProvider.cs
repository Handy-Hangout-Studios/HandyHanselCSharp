using HandyHansel.BotDatabase;
using System;
using System.Collections.Generic;

namespace HandyHansel.Models
{
    public interface IBotAccessProvider : IDisposable
    {
        #region User Time Zones

        /// <summary>
        ///     Creates and adds a new user time zone to the database.
        /// </summary>
        /// <param name="userId">The Discord User Id</param>
        /// <param name="timeZoneId">The Users Timezone Id</param>
        void AddUserTimeZone(ulong userId, string timeZoneId);

        /// <summary>
        ///     Update a users time zone
        /// </summary>
        /// <param name="userTimeZone"></param>
        void UpdateUserTimeZone(UserTimeZone userTimeZone);

        /// <summary>
        ///     Delete a specified user time zone from the database
        /// </summary>
        /// <param name="userTimeZone">
        ///     An object that stores a user id, time zone id, and operating system that this user time zone
        ///     will belong to
        /// </param>
        void DeleteUserTimeZone(UserTimeZone userTimeZone);

        /// <summary>
        ///     Get every user time zone currently in the database
        /// </summary>
        /// <returns>List of user time zones</returns>
        IEnumerable<UserTimeZone> GetUserTimeZones();

        /// <summary>
        ///     Get user time zone that is associated with a specified user using the user id.
        /// </summary>
        /// <param name="userId">The user id that the time zone should be associated with</param>
        /// <returns>users time zone</returns>
        UserTimeZone GetUsersTimeZone(ulong userId);

        #endregion

        #region Guild Events

        /// <summary>
        ///     Add a new guild event to the database
        /// </summary>
        /// <param name="guildEvent">Guild event object that has the guild id, the event name, and the event desc</param>
        void AddGuildEvent(ulong guildId, string eventName, string eventDesc);

        /// <summary>
        ///     Delete a guild event from the database
        /// </summary>
        /// <param name="guildEvent">Guild event object with at minimum the guild event id</param>
        void DeleteGuildEvent(GuildEvent guildEvent);

        /// <summary>
        ///     Gets all guild events associated with the guild id passed in.
        /// </summary>
        /// <param name="guildId">Guild ID to search for associations</param>
        /// <returns>Guild events associated with Guild ID</returns>
        IEnumerable<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId);

        #endregion

        #region Guild Prefixes

        /// <summary>
        ///     Add a guild prefix to the database
        /// </summary>
        /// <param name="prefix">A guild prefix object with prefix and guild id</param>
        void AddGuildPrefix(ulong guildId, string prefix);

        /// <summary>
        ///     Delete a guild prefix from the database
        /// </summary>
        /// <param name="prefix">A guild prefix object with at minimum guild prefix id.</param>
        void DeleteGuildPrefix(GuildPrefix prefix);

        /// <summary>
        ///     Gets all guild prefixes associated with a specific guild id.
        /// </summary>
        /// <param name="guildId">Guild id</param>
        /// <returns>enumerable of all guild prefixes associated with guild id</returns>
        IEnumerable<GuildPrefix> GetAllAssociatedGuildPrefixes(ulong guildId);

        #endregion

        #region Guild Background Jobs

        /// <summary>
        ///  Adds a Guild Background job for use in deleting scheduled jobs.
        /// </summary>
        /// <param name="hangfireJobId">The job id created by Hangfire when scheduling a job</param>
        /// <param name="guildId">The guild id associated with the job</param>
        /// <param name="jobName">The name associated with the job</param>
        /// <param name="scheduledTime">The time the job is scheduled to run</param>
        /// <param name="guildJobType">The kind of job that's being scheduled</param>
        void AddGuildBackgroundJob(string hangfireJobId, ulong guildId, string jobName, DateTime scheduledTime, GuildJobType guildJobType);

        /// <summary>
        /// Deletes the specified Guild Background Job
        /// </summary>
        /// <param name="job">The Guild Background Job to delete</param>
        void DeleteGuildBackgroundJob(GuildBackgroundJob job);

        /// <summary>
        /// Retrieves a list of all Guild Background Jobs associated with a guild
        /// </summary>
        /// <param name="guildId">The Guild ID for the Jobs to fetch</param>
        /// <returns></returns>
        IEnumerable<GuildBackgroundJob> GetAllAssociatedGuildBackgroundJobs(ulong guildId);
        #endregion

        #region Karma Records

        /// <summary>
        ///  Update all Karma Records in the list and create new ones for any that don't currently exist in the database.
        /// </summary>
        /// <param name="karmaRecords">List of Karma Records to Add and Update</param>
        void BulkUpdateKarma(IEnumerable<GuildKarmaRecord> karmaRecords);

        /// <summary>
        /// Add Karma to Users Guild Karma Record
        /// </summary>
        /// <param name="userId">User to Update</param>
        /// <param name="guildId">Guild Record to Update</param>
        /// <param name="karma">Karma to Add</param>
        void AddKarma(ulong userId, ulong guildId, ulong karma);

        /// <summary>
        /// Get users Guild Karma Record
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="guildId">Guild ID</param>
        /// <returns></returns>
        GuildKarmaRecord GetUsersGuildKarmaRecord(ulong userId, ulong guildId);
        #endregion

        #region User Cards

        /// <summary>
        /// Retrieves the User's user card info and creates a user card record for them if they don't currently have one.
        /// </summary>
        /// <param name="userId">The user to fetch the user card for.</param>
        /// <returns></returns>
        UserCard GetUsersUserCard(ulong userId);
        #endregion
    }
}