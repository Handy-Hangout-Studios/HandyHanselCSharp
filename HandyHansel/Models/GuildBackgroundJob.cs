using System;
using System.Collections.Generic;
using System.Text;
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
            ScheduledTime = TimeZoneInfo.ConvertTimeFromUtc(ScheduledTime, timezone);
        }
    }

    public enum GuildJobType
    {
        SCHEDULED_EVENT
    }
}
