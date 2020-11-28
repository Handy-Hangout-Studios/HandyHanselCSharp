using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.BotDatabase
{
    [Obsolete("This system has been deactivated")]
    [Table("all_user_guild_karma_records")]
    public class GuildKarmaRecord
    {
        [Key, Column("id")] public int Id { get; set; }

        [Column("user_id")] public ulong UserId { get; set; }

        [Column("guild_id")] public ulong GuildId { get; set; }

        [Column("current_karma_amount")] public ulong CurrentKarma { get; set; }
    }
}
