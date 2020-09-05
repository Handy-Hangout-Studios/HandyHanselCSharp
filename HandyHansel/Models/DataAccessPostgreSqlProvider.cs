using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HandyHansel.Models
{
    public class DataAccessPostgreSqlProvider : IDataAccessProvider
    {
        /// <summary>
        /// The context that allows access to the different tables in the database.
        /// </summary>
        private readonly PostgreSqlContext _mContext;

        /// <summary>
        /// Creates a DataAccessPostgreSqlProvider for use by the programmer
        /// </summary>
        /// <param name="context">The PostgreSqlContext to use for this Provider</param>
        public DataAccessPostgreSqlProvider(PostgreSqlContext context)
        {
            _mContext = context;
        }

        #region UserTimeZones

        public void AddUserTimeZone(UserTimeZone userTimeZone)
        {
            _mContext.UserTimeZones.Add(userTimeZone);
            _mContext.SaveChanges();
        }

        public void UpdateUserTimeZone(UserTimeZone userTimeZone)
        {
            _mContext.UserTimeZones.Update(userTimeZone);
            _mContext.SaveChanges();
        }

        public void DeleteUserTimeZone(UserTimeZone userTimeZone)
        {
            UserTimeZone entity = _mContext.UserTimeZones.First(e => e.UserId == userTimeZone.UserId);
            _mContext.UserTimeZones.Remove(entity);
            _mContext.SaveChanges();
        }

        public List<UserTimeZone> GetUserTimeZones()
        {
            return _mContext.UserTimeZones.ToList();
        }

        public UserTimeZone GetUsersTimeZone(ulong userId)
        {
            return _mContext.UserTimeZones.FirstOrDefault(utz => utz.UserId == userId);
        }

        #endregion

        #region GuildEvents

        public void AddGuildEvent(GuildEvent guildEvent)
        {
            _mContext.GuildEvents.Add(guildEvent);
            _mContext.SaveChanges();
        }

        public void DeleteGuildEvent(GuildEvent guildEvent)
        {
            GuildEvent entity = _mContext.GuildEvents.First(e => e.Id == guildEvent.Id);
            _mContext.GuildEvents.Remove(entity);
            _mContext.SaveChanges();
        }

        public List<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId)
        {
            return _mContext.GuildEvents.Where(ge => ge.GuildId == guildId).ToList();
        }

        #endregion

        #region ScheduledEvents

        public void AddScheduledEvent(ScheduledEvent scheduledEvent)
        {
            _mContext.ScheduledEvents.Add(scheduledEvent);
            _mContext.SaveChanges();
        }

        public void DeleteScheduledEvent(ScheduledEvent scheduledEvent)
        {
            ScheduledEvent delete = _mContext.ScheduledEvents.First(e => e.Id == scheduledEvent.Id);
            _mContext.ScheduledEvents.Remove(delete);
            _mContext.SaveChanges();
        }

        public IEnumerable<ScheduledEvent> GetAllPastScheduledEvents()
        {
            return _mContext.ScheduledEvents.Where(se => se.ScheduledDate < DateTime.Now).Include(se => se.Event).ToList();
        }

        public List<ScheduledEvent> GetAllScheduledEventsForGuild(ulong guildId)
        {
            return _mContext.ScheduledEvents.Where(se => se.Event.GuildId.Equals(guildId)).Include(se => se.Event).ToList();
        }

        #endregion
    }
}
