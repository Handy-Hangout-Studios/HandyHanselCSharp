using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.BotDatabase.Models
{
    [Table("all_guild_log_channels")]
    public class GuildLogsChannel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("guild_id")]
        public ulong GuildId { get; set; }

        [Column("log_channel_id")]
        public ulong ChannelId { get; set; }
    }
}
