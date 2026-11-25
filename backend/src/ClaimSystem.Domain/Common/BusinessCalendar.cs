namespace ClaimSystem.Domain.Common;

/// <summary>
/// The business runs in Thailand, so date rules ("not in the future", "due date not in the past", claim-number year)
/// use the Bangkok calendar date. Stored timestamps stay UTC; the frontend converts them for display.
/// </summary>
public static class BusinessCalendar
{
    public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    /// <summary>The calendar date in Bangkok at the given UTC instant (e.g. 2026-06-15 20:00 UTC is 16 June).</summary>
    public static DateOnly Today(DateTime utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZone));
}
