using HandyHansel.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HandyHansel.BotDatabase
{
    [Table("all_user_cards")]
    class UserCard
    {
        [Key, Column("id")] public int Id { get; set; }

        [Column("user_id")] public ulong UserId { get; set; }

        [ForeignKey("KarmaRecord"), Column("karma_record_id")] public List<int> KarmaRecordIds { get; set; }

        [ForeignKey("UserTimeZone"), Column("user_timezone_id")] public int UserTimeZoneId { get; set; }

        public List<KarmaRecord> KarmaRecords;

        public UserTimeZone UserTimeZone;
    }
}
