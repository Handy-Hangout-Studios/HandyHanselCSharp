using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel.Models
{
    public interface IDataAccessProvider
    {
        /// <summary>
        /// Adds a new guild time zone to the database
        /// </summary>
        /// <param name="guildTimeZone">An object that stores a guild id, time zone id, and operating system that this guildTimeZone will belong to</param>
        void AddGuildTimeZone(GuildTimeZone guildTimeZone);

        /// <summary>
        /// Delete a specified guild time zone from the database
        /// </summary>
        /// <param name="guildTimeZone">An object that stores a guild id, time zone id, and operating system that this guildTimeZone will belong to</param>
        void DeleteGuildTimeZone(GuildTimeZone guildTimeZone);

        /// <summary>
        /// Get every guild time zone currently in the database
        /// </summary>
        /// <returns>List of guild time zones</returns>
        List<GuildTimeZone> GetAllGuildsTimeZones();

        /// <summary>
        /// Get every guild time zone that is associated with a specified guild using the guild id.
        /// </summary>
        /// <param name="guild_id">The guild id that the time zones should be associated with</param>
        /// <returns>List of guild time zones</returns>
        List<GuildTimeZone> GetAllAssociatedGuildTimeZones(string guild_id);
    }
}
