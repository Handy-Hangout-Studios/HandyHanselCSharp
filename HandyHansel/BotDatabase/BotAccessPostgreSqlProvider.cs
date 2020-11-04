using System.Collections.Generic;
using System.Linq;

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
            this._mContext = context;
        }

        public void Dispose()
        {
            this._mContext?.Dispose();
        }

        #region UserTimeZones

        public void AddUserTimeZone(UserTimeZone userTimeZone)
        {
            this._mContext.UserTimeZones.Add(userTimeZone);
            this._mContext.SaveChanges();
        }

        public void UpdateUserTimeZone(UserTimeZone userTimeZone)
        {
            this._mContext.UserTimeZones.Update(userTimeZone);
            this._mContext.SaveChanges();
        }

        public void DeleteUserTimeZone(UserTimeZone userTimeZone)
        {
            UserTimeZone entity = this._mContext.UserTimeZones.Find(userTimeZone.Id);
            this._mContext.UserTimeZones.Remove(entity);
            this._mContext.SaveChanges();
        }

        public IEnumerable<UserTimeZone> GetUserTimeZones()
        {
            return this._mContext.UserTimeZones.ToList();
        }

        public UserTimeZone GetUsersTimeZone(ulong userId)
        {
            return this._mContext.UserTimeZones.FirstOrDefault(utz => utz.UserId == userId);
        }

        #endregion

        #region GuildEvents

        public void AddGuildEvent(GuildEvent guildEvent)
        {
            this._mContext.GuildEvents.Add(guildEvent);
            this._mContext.SaveChanges();
        }

        public void DeleteGuildEvent(GuildEvent guildEvent)
        {
            GuildEvent entity = this._mContext.GuildEvents.Find(guildEvent.Id);
            this._mContext.GuildEvents.Remove(entity);
            this._mContext.SaveChanges();
        }

        public IEnumerable<GuildEvent> GetAllAssociatedGuildEvents(ulong guildId)
        {
            return this._mContext.GuildEvents.Where(ge => ge.GuildId == guildId).ToList();
        }

        #endregion

        #region Guild Prefixes

        public void AddGuildPrefix(GuildPrefix prefix)
        {
            this._mContext.GuildPrefixes.Add(prefix);
            this._mContext.SaveChanges();
        }

        public void DeleteGuildPrefix(GuildPrefix prefix)
        {
            GuildPrefix delete = this._mContext.GuildPrefixes.Find(prefix.Id);
            if (delete == null)
            {
                return;
            }

            this._mContext.Remove(delete);
            this._mContext.SaveChanges();
        }

        public IEnumerable<GuildPrefix> GetAllAssociatedGuildPrefixes(ulong guildId)
        {
            return this._mContext.GuildPrefixes.Where(prefix => prefix.GuildId == guildId);
        }

        #endregion
    }
}