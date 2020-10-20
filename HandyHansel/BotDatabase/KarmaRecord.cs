using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HandyHansel.BotDatabase
{
    [Table("all_user_guild_karma_records")]
    public class KarmaRecord
    {
        [Key, Column("id")] public int Id { get; set; }
        
        [Column("user_id")] public ulong UserId { get; set; }

        [Column("guild_id")] public ulong GuildId { get; set; }

        [Column("current_karma_amount")] public ulong CurrentKarma { get; set; }
    }
}
