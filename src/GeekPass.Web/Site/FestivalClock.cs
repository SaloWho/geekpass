using Microsoft.Extensions.Options;

namespace GeekPass.Web.Site;

public sealed class FestivalOptions
{
    public const string SectionName = "GeekPass:Festival";

    /// <summary>IANA time zone of the festival venue.</summary>
    public string TimeZone { get; set; } = "Asia/Tbilisi";
}

/// <summary>
/// The one place where festival times are converted.
/// </summary>
/// <remarks>
/// Editors type times as the festival's local wall-clock time (that is how a programme is written),
/// and Umbraco's date picker stores them without a time zone. This class attaches the festival time zone,
/// so the rest of the code works with <see cref="DateTimeOffset"/> and can always get UTC
/// (the Booking module will receive UTC). <see cref="TimeProvider"/> makes "now" testable.
/// </remarks>
public sealed class FestivalClock(TimeProvider timeProvider, IOptions<FestivalOptions> options)
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public TimeZoneInfo TimeZone => _timeZone;

    /// <summary>Current time at the festival.</summary>
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone);

    /// <summary>Interprets a stored editor value as festival local time.</summary>
    public DateTimeOffset FromFestivalLocal(DateTime wallClock)
    {
        var unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, _timeZone.GetUtcOffset(unspecified));
    }

    /// <summary>Short label for the UI, e.g. "GMT+4".</summary>
    public string OffsetLabel(DateTimeOffset at)
    {
        TimeSpan offset = _timeZone.GetUtcOffset(at);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        offset = offset.Duration();
        return offset.Minutes == 0 ? $"GMT{sign}{offset.Hours}" : $"GMT{sign}{offset.Hours}:{offset.Minutes:00}";
    }
}
