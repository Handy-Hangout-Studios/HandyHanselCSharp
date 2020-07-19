using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandyHansel.Models
{
    class DataAccessPostgreSqlProvider : IDataAccessProvider
    {
        /// <summary>
        /// The context that allows access to the different tables in the database.
        /// </summary>
        private readonly PostgreSqlContext m_context;

        /// <summary>
        /// Creates a DataAccessPostgreSqlProvider for use by the programmer
        /// </summary>
        /// <param name="context">The PostgreSqlContext to use for this Provider</param>
        public DataAccessPostgreSqlProvider(PostgreSqlContext context)
        {
            m_context = context;
        }

        public void AddGuildTimeZone(GuildTimeZone guildTimeZone)
        {
            m_context.guildTimeZones.Add(guildTimeZone);
            m_context.SaveChanges();
        }

        public void DeleteGuildTimeZone(GuildTimeZone guildTimeZone)
        {
            GuildTimeZone entity = m_context.guildTimeZones.First(e => e.Guild == guildTimeZone.Guild && e.TimeZoneId == guildTimeZone.TimeZoneId);
            m_context.guildTimeZones.Remove(entity);
            m_context.SaveChanges();
        }

        public List<GuildTimeZone> GetAllGuildsTimeZones()
        {
            return m_context.guildTimeZones.ToList();
        }

        public List<GuildTimeZone> GetAllAssociatedGuildTimeZones(string guild_id)
        {
            return m_context.guildTimeZones.Where(gtz => gtz.Guild == guild_id).ToList();
        }
    }
}
