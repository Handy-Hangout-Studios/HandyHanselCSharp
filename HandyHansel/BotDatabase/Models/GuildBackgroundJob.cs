using System;
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
        public DateTime ScheduledTime { get; set; }

        [Column("job_type")]
        public GuildJobType GuildJobType { get; set; }

        public void ConvertTimeZoneTo(TimeZoneInfo timezone)
        {
            this.ScheduledTime = TimeZoneInfo.ConvertTimeFromUtc(this.ScheduledTime, timezone);
        }
    }

    public enum GuildJobType
    {
        SCHEDULED_EVENT
    }
}
