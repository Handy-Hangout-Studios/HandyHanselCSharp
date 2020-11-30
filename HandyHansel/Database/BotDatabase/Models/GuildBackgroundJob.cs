using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.Models
{
    [Table("all_guild_background_jobs")]
    public class GuildBackgroundJob
    {
        [Key]
        [Column("hangfire_job_id")]
        public string HangfireJobId { get; set; }

        [Column("guild_id")]
        public ulong GuildId { get; set; }

        [Column("job_name")]
        public string JobName { get; set; }

        [Column("scheduled_time")]
        public Instant ScheduledTime { get; set; }

        [NotMapped]
        public ZonedDateTime ScheduledTimeInTimeZone { get; set; }

        [Column("job_type")]
        public GuildJobType GuildJobType { get; set; }

        public GuildBackgroundJob WithTimeZoneConvertedTo(DateTimeZone timezone)
        {
            return new GuildBackgroundJob
            {
                HangfireJobId = this.HangfireJobId,
                GuildId = this.GuildId,
                JobName = this.JobName,
                ScheduledTimeInTimeZone = this.ScheduledTime.InZone(timezone),
                GuildJobType = this.GuildJobType,
            };
        }
    }

    public enum GuildJobType
    {
        SCHEDULED_EVENT,
        TEMP_BAN,
        TEMP_MUTE,
    }
}
