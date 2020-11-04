using System;
using System.Threading;

namespace HandyHansel.Models
{
    public class GuildBackgroundJob
    {
        public string JobName;
        public DateTime ScheduledTime;
        public GuildJobType GuildJobType;
        public CancellationTokenSource CancellationTokenSource;

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
