using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.Models
{
    [Table("all_guild_events")]
    public class GuildEvent
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("guild")] public ulong GuildId { get; set; }

        [Column("event_name")] public string EventName { get; set; }

        [Column("event_description")] public string EventDesc { get; set; }
    }
}