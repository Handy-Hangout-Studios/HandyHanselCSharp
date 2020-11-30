using HandyHansel.BotDatabase;
using HandyHansel.BotDatabase.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Streamx.Linq.SQL;
using Streamx.Linq.SQL.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static Streamx.Linq.SQL.Directives;
using static Streamx.Linq.SQL.PostgreSQL.SQL;
using static Streamx.Linq.SQL.SQL;

namespace HandyHansel.Models
{
    public class BotAccessPostgreSqlProvider : IBotAccessProvider
    {
        /// <summary>
        ///     The context that allows access to the different tables in the database.
        /// </summary>
        private readonly PostgreSqlContext context;

        private readonly IClock clock;

        /// <summary>
        ///     Creates a DataAccessPostgreSqlProvider for use by the programmer
        /// </summary>
        /// <param name="context">The PostgreSqlContext to use for this Provider</param>
        public BotAccessPostgreSqlProvider(PostgreSqlContext context, IClock clock)
        {
            this.context = context;
            this.clock = clock;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.context?.Dispose();
        }

        #region Database Maintenance
        public void Migrate()
        {
            context.Database.Migrate();
        }
        #endregion

        #region UserTimeZones

        public void AddUserTimeZone(ulong userId, string timeZoneId)
        {
            this.context.UserTimeZones.Add(new UserTimeZone { UserId = userId, TimeZoneId = timeZoneId });
            this.context.SaveChanges();
        }

        public void UpdateUserTimeZone(UserTimeZone userTimeZone)
        {
            this.context.UserTimeZones.Update(userTimeZone);
            this.context.SaveChanges();
        }

        public void DeleteUserTimeZone(UserTimeZone userTimeZone)
        {
            UserTimeZone entity = this.context.UserTimeZones.Find(userTimeZone.Id);
            this.context.UserTimeZones.Remove(entity);
            this.context.SaveChanges();
        }

        public IEnumerable<UserTimeZone> GetUserTimeZones()
        {
            return this.context.UserTimeZones.ToList();
        }

        public UserTimeZone GetUsersTimeZone(ulong userId)
        {
            return this.context.UserTimeZones.FirstOrDefault(utz => utz.UserId == userId);
        }

        #endregion

        #region GuildEvents

        public void AddGuildEvent(ulong guildId, string eventName, string eventDesc)
        {
            this.context.GuildEvents.Add(new GuildEvent { GuildId = guildId, EventName = eventName, EventDesc = eventDesc });
            this.context.SaveChanges();
        }

        public void DeleteGuildEvent(GuildEvent guildEvent)
        {
            GuildEvent entity = this.context.GuildEvents.Find(guildEvent.Id);
            this.context.GuildEvents.Remove(entity);
            this.context.SaveChanges();
        }

        public IEnumerable<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId)
        {
            return this.context.GuildEvents.Where(ge => ge.GuildId == guildId).ToList();
        }

        #endregion

        #region Guild Prefixes

        public void AddGuildPrefix(ulong guildId, string prefix)
        {
            this.context.GuildPrefixes.Add(new GuildPrefix { GuildId = guildId, Prefix = prefix });
            this.context.SaveChanges();
        }

        public void DeleteGuildPrefix(GuildPrefix prefix)
        {
            GuildPrefix delete = this.context.GuildPrefixes.Find(prefix.Id);
            if (delete == null)
            {
                return;
            }

            this.context.Remove(delete);
            this.context.SaveChanges();
        }

        public IEnumerable<GuildPrefix> GetAllAssociatedGuildPrefixes(ulong guildId)
        {
            return this.context.GuildPrefixes.Where(prefix => prefix.GuildId == guildId);
        }

        #endregion

        #region Guild Background Jobs
        public void AddGuildBackgroundJob(string hangfireJobId, ulong guildId, string jobName, Instant scheduledTime, GuildJobType guildJobType)
        {
            this.context.GuildBackgroundJobs.Add(new GuildBackgroundJob
            {
                HangfireJobId = hangfireJobId,
                GuildId = guildId,
                JobName = jobName,
                ScheduledTime = scheduledTime,
                GuildJobType = guildJobType,
            });
            this.context.SaveChanges();
        }

        public void DeleteGuildBackgroundJob(GuildBackgroundJob job)
        {
            this.context.GuildBackgroundJobs.Remove(job);
            this.context.SaveChanges();
        }

        public IEnumerable<GuildBackgroundJob> GetAllAssociatedGuildBackgroundJobs(ulong guildId)
        {
            return this.context.GuildBackgroundJobs.Where(x => x.GuildId == guildId && x.ScheduledTime > this.clock.GetCurrentInstant());
        }
        #endregion

        #region Karma Records

        [Obsolete("This system has been deactivated")]
        public void BulkUpdateKarma(IEnumerable<GuildKarmaRecord> karmaRecords)
        {
            if (!karmaRecords.Any())
            {
                return;
            }

            this.context.Database.Execute((GuildKarmaRecord records) =>
            {
                IProjection<GuildKarmaRecord, (int Id, ulong UserId, ulong GuildId, ulong CurrentKarma)> set = records.@using((records.Id, records.UserId, records.GuildId, records.CurrentKarma));
                INSERT().INTO(set);
                VALUES(set.RowsFrom(karmaRecords));
                ON_CONFLICT(records.Id).DO_UPDATE().SET(() => records.CurrentKarma = EXCLUDED<GuildKarmaRecord>().CurrentKarma);
            });
        }

        [Obsolete("This system has been deactivated")]
        public void AddKarma(ulong userId, ulong guildId, ulong karma)
        {
            GuildKarmaRecord guildKarmaRecord = this.GetUsersGuildKarmaRecord(userId, guildId);
            guildKarmaRecord.CurrentKarma += karma;
            this.context.GuildKarmaRecords.Update(guildKarmaRecord);
            this.context.SaveChanges();
        }


        [Obsolete("This system has been deactivated")]
        public GuildKarmaRecord GetUsersGuildKarmaRecord(ulong userId, ulong guildId)
        {
            GuildKarmaRecord record = this.context.GuildKarmaRecords.FirstOrDefault(record => record.UserId == userId && record.GuildId == guildId);
            if (record == null)
            {
                record = new GuildKarmaRecord
                {
                    GuildId = guildId,
                    UserId = userId,
                    CurrentKarma = 0,
                };

                this.context.GuildKarmaRecords.Add(record);
                this.context.SaveChanges();
            }
            return record;
        }


        [Obsolete("This system has been deactivated")]
        public IEnumerable<GuildKarmaRecord> GetGuildKarmaRecords(ulong guildId)
        {
            return this.context.GuildKarmaRecords.Where(record => record.GuildId == guildId).OrderByDescending(record => record.CurrentKarma);
        }

        [Obsolete("This system has been deactivated")]
        public void RemoveUsersGuildKarmaRecord(ulong guildId, ulong userId)
        {
            GuildKarmaRecord deletion = this.context.GuildKarmaRecords.FirstOrDefault(record => record.GuildId == guildId && record.UserId == userId);
            this.context.Remove(deletion);
            this.context.SaveChanges();
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
            UserCard userCard = this.context.UserCards.First(e => e.UserId == userId);
            if (userCard == null)
            {
                UserTimeZone timeZone = this.context.UserTimeZones.First(x => x.UserId == userId);
                userCard = new UserCard
                {
                    UserId = userId,
                    UserTimeZoneId = timeZone.Id,
                    UserTimeZone = timeZone
                };

                this.context.UserCards.Add(userCard);
                this.context.SaveChanges();
            }

            return userCard;
        }
        #endregion

        #region Guild Logs Channel
        public void AddOrUpdateGuildLogChannel(ulong guildId, ulong channelId)
        {
            GuildLogsChannel logsChannel = this.context.GuildLogsChannels.FirstOrDefault(channel => channel.GuildId == guildId);
            if (logsChannel == null)
            {
                this.context.GuildLogsChannels.Add(new GuildLogsChannel { GuildId = guildId, ChannelId = channelId });
            }
            else
            {
                logsChannel.ChannelId = channelId;
                this.context.Update(logsChannel);
            }

            this.context.SaveChanges();

        }

        public void RemoveGuildLogChannel(ulong guildId)
        {
            GuildLogsChannel logsChannel = this.context.GuildLogsChannels.FirstOrDefault(channel => channel.GuildId == guildId);
            if (logsChannel == null)
            {
                return;
            }

            this.context.GuildLogsChannels.Remove(logsChannel);
            this.context.SaveChanges();
        }

        public GuildLogsChannel GetGuildLogChannel(ulong guildId)
        {
            return this.context.GuildLogsChannels.AsNoTracking().FirstOrDefault(channel => channel.GuildId == guildId);
        }
        #endregion

        #region Guild Moderation Audit Records
        public void AddModerationAuditRecord(ulong guildId, ulong modUserId, ulong userId, ModerationActionType action, string reason)
        {
            GuildModerationAuditRecord record = new GuildModerationAuditRecord()
            {
                GuildId = guildId,
                ModeratorUserId = modUserId,
                UserId = userId,
                ModerationAction = action,
                Reason = reason,
                Timestamp = this.clock.GetCurrentInstant()
            };
            this.context.GuildModerationAuditRecords.Add(record);
            this.context.SaveChanges();
        }

        public IEnumerable<GuildModerationAuditRecord> GetGuildModerationAuditRecords(ulong guildId, ulong? modUserId = null, ulong? userId = null, ModerationActionType? action = null)
        {
            return this.context.GuildModerationAuditRecords.AsNoTracking().Where(record =>
                record.GuildId == guildId &&
                (modUserId == null || record.ModeratorUserId == modUserId) &&
                (userId == null || record.UserId == userId) &&
                (action == ModerationActionType.NONE || record.ModerationAction == action));
        }
        #endregion
    }
}