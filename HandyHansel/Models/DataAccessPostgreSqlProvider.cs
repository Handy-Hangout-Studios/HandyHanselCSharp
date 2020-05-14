using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandyHansel.Models
{
    class DataAccessPostgreSqlProvider : IDataAccessProvider
    {
        private readonly PostgreSqlContext m_context;

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
    }
}
