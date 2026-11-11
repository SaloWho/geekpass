namespace GeekPass.Web.Site.Programme;

/// <summary>An event as the website shows it. Built from Umbraco content by <see cref="ProgrammeReader"/>.</summary>
public sealed record ProgrammeEvent(
    Guid Key,
    string Name,
    string Url,
    string? Track,
    string? Summary,
    string? Venue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

/// <summary>One day of the programme.</summary>
public sealed record ProgrammeDay(DateOnly Date, IReadOnlyList<ProgrammeEvent> Events);

/// <summary>Pure programme rules, kept free of Umbraco so they are easy to unit test.</summary>
public static class ProgrammeSchedule
{
    /// <summary>Groups events by their local festival date; days and events are in chronological order.</summary>
    public static IReadOnlyList<ProgrammeDay> GroupByDay(IEnumerable<ProgrammeEvent> events) =>
        events
            .OrderBy(e => e.StartsAt)
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .GroupBy(e => DateOnly.FromDateTime(e.StartsAt.DateTime)) // DateTime of a DateTimeOffset = its local wall clock
            .Select(day => new ProgrammeDay(day.Key, day.ToList()))
            .ToList();

    /// <summary>Events that have not ended yet, soonest first.</summary>
    public static IReadOnlyList<ProgrammeEvent> Upcoming(IEnumerable<ProgrammeEvent> events, DateTimeOffset now, int take) =>
        events
            .Where(e => e.EndsAt > now)
            .OrderBy(e => e.StartsAt)
            .Take(take)
            .ToList();
}
