using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HandyHansel.BotDatabase;
using Microsoft.EntityFrameworkCore;
using Streamx.Linq.SQL;
using Streamx.Linq.SQL.EFCore;
using static Streamx.Linq.SQL.SQL;
using static Streamx.Linq.SQL.PostgreSQL.SQL;
using static Streamx.Linq.SQL.Directives;

namespace HandyHansel.Models
{
    public class BotAccessPostgreSqlProvider : IBotAccessProvider
    {
        /// <summary>
        ///     The context that allows access to the different tables in the database.
        /// </summary>
        private readonly PostgreSqlContext _mContext;

        /// <summary>
        ///     Creates a DataAccessPostgreSqlProvider for use by the programmer
        /// </summary>
        /// <param name="context">The PostgreSqlContext to use for this Provider</param>
        public BotAccessPostgreSqlProvider(PostgreSqlContext context)
        {
            _mContext = context;
        }

        public void Dispose()
        {
            _mContext?.Dispose();
        }

        #region UserTimeZones

        public void AddUserTimeZone(ulong userId, string timeZoneId)
        {
            _mContext.UserTimeZones.Add(new UserTimeZone { UserId = userId, TimeZoneId = timeZoneId, OperatingSystem = RuntimeInformation.OSDescription });
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

        public void AddGuildEvent(ulong guildId, string eventName, string eventDesc)
        {
            _mContext.GuildEvents.Add(new GuildEvent { GuildId = guildId, EventName = eventName, EventDesc = eventDesc});
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

        #region Guild Prefixes

        public void AddGuildPrefix(ulong guildId, string prefix)
        {
            _mContext.GuildPrefixes.Add(new GuildPrefix { GuildId = guildId, Prefix = prefix});
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

        #region Guild Background Jobs
        public void AddGuildBackgroundJob(string hangfireJobId, ulong guildId, string jobName, DateTime scheduledTime, GuildJobType guildJobType)
        {
            _mContext.GuildBackgroundJobs.Add(new GuildBackgroundJob
            {
                HangfireJobId = hangfireJobId,
                GuildId = guildId,
                JobName = jobName,
                ScheduledTime = scheduledTime,
                GuildJobType = guildJobType,
            });
            _mContext.SaveChanges();
        }

        public void DeleteGuildBackgroundJob(GuildBackgroundJob job)
        {
            _mContext.GuildBackgroundJobs.Remove(job);
            _mContext.SaveChanges();
        }

        public IEnumerable<GuildBackgroundJob> GetAllAssociatedGuildBackgroundJobs(ulong guildId)
        {
            return _mContext.GuildBackgroundJobs.Where(x => x.GuildId == guildId && x.ScheduledTime > DateTime.Now);
        }
        #endregion

        #region Karma Records

        public void BulkUpdateKarma(IEnumerable<GuildKarmaRecord> karmaRecords)
        {
            if (!karmaRecords.Any()) return;
            _mContext.Database.Execute((GuildKarmaRecord records) =>
            {
                var set = records.@using((records.Id, records.UserId, records.GuildId, records.CurrentKarma));
                INSERT().INTO(set);
                VALUES(set.RowsFrom(karmaRecords));
                ON_CONFLICT(records.Id).DO_UPDATE().SET(() => records.CurrentKarma = EXCLUDED<GuildKarmaRecord>().CurrentKarma);
            });
        }

        public void AddKarma(ulong userId, ulong guildId, ulong karma)
        {
            GuildKarmaRecord guildKarmaRecord = GetUsersGuildKarmaRecord(userId, guildId);
            guildKarmaRecord.CurrentKarma += karma;
            _mContext.GuildKarmaRecords.Update(guildKarmaRecord);
            _mContext.SaveChanges();
        }

        public GuildKarmaRecord GetUsersGuildKarmaRecord(ulong userId, ulong guildId)
        {
            GuildKarmaRecord record = _mContext.GuildKarmaRecords.FirstOrDefault(record => record.UserId == userId && record.GuildId == guildId);
            if (record == null)
            {
                record = new GuildKarmaRecord
                {
                    GuildId = guildId,
                    UserId = userId,
                    CurrentKarma = 0,
                };

                _mContext.GuildKarmaRecords.Add(record);
                _mContext.SaveChanges();
            }    
            return record;
        }
        #endregion

        #region User Cards

        /// <summary>
        /// Retrieves the User's user card info and creates a user card record for them if they don't currently have one.
        /// </summary>
        /// <param name="userId">The user to fetch the user card for.</param>
        /// <returns></returns>
        public UserCard GetUsersUserCard(ulong userId)
        {
            UserCard userCard = _mContext.UserCards.First(e => e.UserId == userId);
            if (userCard == null)
            {
                UserTimeZone timeZone = _mContext.UserTimeZones.First(x => x.UserId == userId);
                userCard = new UserCard
                {
                    UserId = userId,
                    UserTimeZoneId = timeZone.Id,
                    UserTimeZone = timeZone
                };

                _mContext.UserCards.Add(userCard);
                _mContext.SaveChanges();
            }

            return userCard;
        }
        #endregion
    }
}