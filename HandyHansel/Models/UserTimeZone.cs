using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.Models
{
    [Table("all_user_time_zones")]
    public class UserTimeZone
    {
        [Key]
        [Column("id")]
        // ReSharper disable once UnusedMember.Global
        public int Id { get; set;  }

        [Column("user_id")]
        public ulong UserId { get; set; }
        
        [Column("timezone_id")]
        public string TimeZoneId { get; set; }

        [Column("operating_system")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string OperatingSystem { get; set; }
    }
}