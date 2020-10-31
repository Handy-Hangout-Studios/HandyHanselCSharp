using HandyHansel.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.BotDatabase
{
    [Table("all_user_cards")]
    public class UserCard
    {
        [Key, Column("id")] public int Id { get; set; }

        [Column("user_id")] public ulong UserId { get; set; }

        [ForeignKey("UserTimeZone"), Column("user_timezone_id")] public int UserTimeZoneId { get; set; }

        public UserTimeZone UserTimeZone;

        public List<GuildKarmaRecord> KarmaRecords;
    }
}
