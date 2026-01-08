// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Provides time and calendar utilities for telemetry.
    /// </summary>
    internal class TelemetryClock {
        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        public virtual DateTimeOffset UtcNow
            => DateTimeOffset.UtcNow;

        /// <summary>
        /// Computes the current ISO week string (YYYY-Www).
        /// </summary>
        public virtual string GetCurrentIsoWeek() {
            var now = UtcNow.UtcDateTime;

            // ISO 8601: week starts Monday, first week has at least 4 days
            var calendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            var week = calendar.GetWeekOfYear(
                now,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);

            var year = now.Year;

            // Handle edge case: last days of December belonging to week 1 of next year
            if (week == 1 && now.Month == 12) {
                year++;
            }

            return $"{year}-W{week:D2}";
        }
    }
}
