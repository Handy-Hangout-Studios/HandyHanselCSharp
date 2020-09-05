using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.Models
{
    [Table("all_guild_scheduled_events")]
    public class ScheduledEvent
    {
        [Key] 
        [Column("id")] 
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Id { get; set; }
        
        [Column("scheduled_date")] 
        public DateTime ScheduledDate { get; set; }
        
        [Column("guild_event_id")] 
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int GuildEventId { get; set; }

        [Column("channel_id")]
        public ulong ChannelId { get; set; }
        
        [ForeignKey("GuildEventId")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public GuildEvent Event { get; set; }
    }
}