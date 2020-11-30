using DSharpPlus.Entities;
using HandyHansel.Models;
using NodaTime;
using NodaTime.TimeZones;
using System;
using System.Runtime.Serialization;

namespace HandyHansel.Utilities
{
    public static class DiscordObjectExtensionMethods
    {
        /// <summary>
        /// Verifies that this DiscordUser has a timezone registered in our database and throws a PreExecutionException if they don't
        /// </summary>
        /// <param name="user">The user who should have a timezone</param>
        /// <param name="provider">The database access provider</param>
        /// <param name="timeZoneProvider"></param>
        /// <returns></returns>
        public static bool TryGetDateTimeZone(this DiscordUser user, IBotAccessProvider provider, IDateTimeZoneProvider timeZoneProvider, out DateTimeZone timeZone)
        {
            UserTimeZone userTimeZone = provider.GetUsersTimeZone(user.Id);
            try
            {
                if (userTimeZone != null)
                {
                    timeZone = timeZoneProvider[userTimeZone.TimeZoneId];
                    return true;
                }
            }
            catch (DateTimeZoneNotFoundException)
            {
            }

            timeZone = default;
            return false;
        }
    }

    [Serializable]
    internal class PreExecutionException : Exception
    {
        public PreExecutionException()
        {
        }

        public PreExecutionException(string message) : base(message)
        {
        }

        public PreExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PreExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
