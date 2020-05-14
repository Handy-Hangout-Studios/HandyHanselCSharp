using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel.Models
{
    public interface IDataAccessProvider
    {
        void AddGuildTimeZone(GuildTimeZone guildTimeZone);
        void DeleteGuildTimeZone(GuildTimeZone guildTimeZone);
        List<GuildTimeZone> GetAllGuildsTimeZones();
    }
}
