﻿using System;
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
            UserTimeZone entity = _mContext.UserTimeZones.Find(userTimeZone.Id);
            _mContext.UserTimeZones.Remove(entity);
            _mContext.SaveChanges();
        }

        public IEnumerable<UserTimeZone> GetUserTimeZones()
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
            GuildEvent entity = _mContext.GuildEvents.Find(guildEvent.Id);
            _mContext.GuildEvents.Remove(entity);
            _mContext.SaveChanges();
        }

        public IEnumerable<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId)
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

        public void SetEventAnnounced(ScheduledEvent scheduledEvent)
        {
            scheduledEvent.Announced = true;
            _mContext.ScheduledEvents.Update(scheduledEvent);
        }

        public void SaveAnnouncedEvents()
        {
            _mContext.SaveChanges();
        }
        
        public void DeleteScheduledEvent(ScheduledEvent scheduledEvent)
        {
            ScheduledEvent delete = _mContext.ScheduledEvents.Find(scheduledEvent.Id);
            if (delete == null) return;
            _mContext.ScheduledEvents.Remove(delete);
            _mContext.SaveChanges();
        }
        
        public IEnumerable<ScheduledEvent> GetAllPastScheduledEvents(TimeSpan? amountOfTimeInFuture = null)
        {
            TimeSpan nonNull = amountOfTimeInFuture ?? new TimeSpan(0);
            DateTime currentTimeMinusSpan = DateTime.Now.Add(nonNull);
            return _mContext.ScheduledEvents.Where(se => se.ScheduledDate < currentTimeMinusSpan && !se.Announced).Include(se => se.Event).ToList();
        }

        public IEnumerable<ScheduledEvent> GetAllScheduledEventsForGuild(ulong guildId)
        {
            return _mContext.ScheduledEvents.Where(se => se.Event.GuildId.Equals(guildId) && !se.Announced).Include(se => se.Event).ToList();
        }

        #endregion
        
        #region Guild Prefixes

        public void AddGuildPrefix(GuildPrefix prefix)
        {
            _mContext.GuildPrefixes.Add(prefix);
            _mContext.SaveChanges();
        }

        public void DeleteGuildPrefix(GuildPrefix prefix)
        {
            GuildPrefix delete = _mContext.GuildPrefixes.Find(prefix.Id);
            if (delete == null) return;
            _mContext.Remove(delete);
            _mContext.SaveChanges();
        }

        public IEnumerable<GuildPrefix> GetAllAssociatedGuildPrefixes(ulong guildId)
        {
            return _mContext.GuildPrefixes.Where(prefix => prefix.GuildId == guildId);
        }
        
        #endregion

        public void Dispose()
        {
            _mContext?.Dispose();
        }
    }
}
