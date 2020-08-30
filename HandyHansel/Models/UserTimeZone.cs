using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HandyHansel.Models
{
    [Table("all_user_time_zones")]
    public class UserTimeZone
    {
        [Key]
        [Column("id")]
        public int Id { get; set;  }

        [Column("user_id")]
        public ulong UserId { get; set; }
        
        [Column("timezone_id")]
        public string TimeZoneId { get; set; }

        [Column("operating_system")]
        public string OperatingSystem { get; set; }
    }
}