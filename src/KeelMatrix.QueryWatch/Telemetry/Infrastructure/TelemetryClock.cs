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
            // Use date component only; ISO week is date-based, not time-of-day-based.
            var date = UtcNow.UtcDateTime.Date;

#if NET8_0_OR_GREATER
            int isoYear = System.Globalization.ISOWeek.GetYear(date);
            int isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(date);
            return $"{isoYear}-W{isoWeek:D2}";
#else
    // ISO 8601 week/year algorithm:
    // - Week starts Monday.
    // - Week 1 is the week containing Jan 4 (equivalently: the week with the year's first Thursday).
    // - ISO year is the year of the Thursday of the current week.

    // Map DayOfWeek to Monday=0 .. Sunday=6
    int dow = ((int)date.DayOfWeek + 6) % 7;

    // Thursday of the current ISO week
    DateTime thursday = date.AddDays(3 - dow);

    int isoYear = thursday.Year;

    // Jan 4 is always in ISO week 1
    DateTime jan4 = new(isoYear, 1, 4, 0, 0, 0, DateTimeKind.Utc);

    int jan4Dow = ((int)jan4.DayOfWeek + 6) % 7;

    // Thursday of ISO week 1 for isoYear
    DateTime week1Thursday = jan4.AddDays(3 - jan4Dow);

    int isoWeek = 1 + (int)((thursday - week1Thursday).TotalDays / 7);
    return $"{isoYear}-W{isoWeek:D2}";
#endif
        }
    }
}
