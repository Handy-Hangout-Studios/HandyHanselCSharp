using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

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

        #region UserTimeZones

        public void AddUserTimeZone(UserTimeZone userTimeZone)
        {
            m_context.UserTimeZones.Add(userTimeZone);
            m_context.SaveChanges();
        }

        public void UpdateUserTimeZone(UserTimeZone userTimeZone)
        {
            m_context.UserTimeZones.Update(userTimeZone);
            m_context.SaveChanges();
        }

        public void DeleteUserTimeZone(UserTimeZone userTimeZone)
        {
            UserTimeZone entity = m_context.UserTimeZones.First(e => e.UserId == userTimeZone.UserId);
            m_context.UserTimeZones.Remove(entity);
            m_context.SaveChanges();
        }

        public List<UserTimeZone> GetUserTimeZones()
        {
            return m_context.UserTimeZones.ToList();
        }

        public UserTimeZone GetUsersTimeZone(ulong userId)
        {
            return m_context.UserTimeZones.FirstOrDefault(utz => utz.UserId == userId);
        }

        #endregion

        #region GuildEvents

        public void AddGuildEvent(GuildEvent guildEvent)
        {
            m_context.GuildEvents.Add(guildEvent);
            m_context.SaveChanges();
        }

        public void DeleteGuildEvent(GuildEvent guildEvent)
        {
            GuildEvent entity = m_context.GuildEvents.First(e => e.Id == guildEvent.Id);
            m_context.GuildEvents.Remove(entity);
            m_context.SaveChanges();
        }

        public List<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId)
        {
            return m_context.GuildEvents.Where(ge => ge.GuildId == guildId).ToList();
        }

        #endregion

        #region ScheduledEvents

        public void AddScheduledEvent(ScheduledEvent scheduledEvent)
        {
            m_context.ScheduledEvents.Add(scheduledEvent);
            m_context.SaveChanges();
        }

        public void DeleteScheduledEvent(ScheduledEvent scheduledEvent)
        {
            ScheduledEvent delete = m_context.ScheduledEvents.First(e => e.Id == scheduledEvent.Id);
            m_context.ScheduledEvents.Remove(delete);
            m_context.SaveChanges();
        }

        public List<ScheduledEvent> GetAllPastScheduledEvents()
        {
            return m_context.ScheduledEvents.Where(se => se.ScheduledDate < DateTime.Now).Include(se => se.Event).ToList();
        }

        public List<ScheduledEvent> GetAllScheduledEventsForGuild(ulong guildId)
        {
            return m_context.ScheduledEvents.Where(se => se.Event.GuildId.Equals(guildId)).Include(se => se.Event).ToList();
        }

        #endregion
    }
}
