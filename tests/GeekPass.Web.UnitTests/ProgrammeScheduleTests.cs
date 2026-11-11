using GeekPass.Web.Site.Programme;

namespace GeekPass.Web.UnitTests;

public class ProgrammeScheduleTests
{
    private static readonly TimeSpan Tbilisi = TimeSpan.FromHours(4);

    private static ProgrammeEvent Event(string name, DateTimeOffset startsAt, int hours = 1) =>
        new(Guid.NewGuid(), name, $"/programme/{name.ToLowerInvariant()}/", "Tabletop", null, null, startsAt, startsAt.AddHours(hours));

    [Fact]
    public void GroupByDay_orders_days_and_events_chronologically()
    {
        ProgrammeEvent late = Event("Late", new DateTimeOffset(2027, 5, 15, 18, 0, 0, Tbilisi));
        ProgrammeEvent dayTwo = Event("DayTwo", new DateTimeOffset(2027, 5, 16, 10, 0, 0, Tbilisi));
        ProgrammeEvent early = Event("Early", new DateTimeOffset(2027, 5, 15, 9, 0, 0, Tbilisi));

        IReadOnlyList<ProgrammeDay> days = ProgrammeSchedule.GroupByDay([late, dayTwo, early]);

        Assert.Equal(new[] { new DateOnly(2027, 5, 15), new DateOnly(2027, 5, 16) }, days.Select(d => d.Date));
        Assert.Equal(new[] { "Early", "Late" }, days[0].Events.Select(e => e.Name));
    }

    [Fact]
    public void GroupByDay_uses_the_festival_date_not_the_UTC_date()
    {
        // 01:00 in Tbilisi on 16 May is still 15 May in UTC.
        ProgrammeEvent nightOwl = Event("NightOwl", new DateTimeOffset(2027, 5, 16, 1, 0, 0, Tbilisi));

        IReadOnlyList<ProgrammeDay> days = ProgrammeSchedule.GroupByDay([nightOwl]);

        Assert.Equal(new DateOnly(2027, 5, 15), DateOnly.FromDateTime(nightOwl.StartsAt.UtcDateTime));
        Assert.Equal(new DateOnly(2027, 5, 16), Assert.Single(days).Date);
    }

    [Fact]
    public void Upcoming_skips_ended_events_keeps_running_ones_and_respects_take()
    {
        var now = new DateTimeOffset(2027, 5, 15, 12, 0, 0, Tbilisi);
        ProgrammeEvent ended = Event("Ended", now.AddHours(-3), hours: 1);
        ProgrammeEvent running = Event("Running", now.AddHours(-1), hours: 2);
        ProgrammeEvent next = Event("Next", now.AddHours(1));
        ProgrammeEvent later = Event("Later", now.AddHours(5));

        IReadOnlyList<ProgrammeEvent> upcoming = ProgrammeSchedule.Upcoming([later, ended, next, running], now, take: 2);

        Assert.Equal(new[] { "Running", "Next" }, upcoming.Select(e => e.Name));
    }
}
