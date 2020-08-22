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
        public int Id { get; set; }
        
        [Column("scheduled_date")] 
        public DateTime ScheduledDate { get; set; }
        
        [Column("guild_event_id")] 
        public int GuildEventId { get; set; }

        [Column("channel_id")]
        public ulong ChannelId { get; set; }
        
        [ForeignKey("GuildEventId")]
        public GuildEvent Event { get; set; }
    }
}