using GeekPass.Web.Site;
using Microsoft.Extensions.Options;

namespace GeekPass.Web.UnitTests;

public class FestivalClockTests
{
    private static FestivalClock CreateClock(DateTimeOffset utcNow) =>
        new(new FixedTimeProvider(utcNow), Options.Create(new FestivalOptions { TimeZone = "Asia/Tbilisi" }));

    [Fact]
    public void FromFestivalLocal_treats_the_editor_value_as_Tbilisi_time()
    {
        FestivalClock clock = CreateClock(DateTimeOffset.UnixEpoch);

        DateTimeOffset startsAt = clock.FromFestivalLocal(new DateTime(2027, 5, 15, 11, 0, 0));

        Assert.Equal(TimeSpan.FromHours(4), startsAt.Offset);
        Assert.Equal(new DateTimeOffset(2027, 5, 15, 7, 0, 0, TimeSpan.Zero), startsAt.ToUniversalTime());
    }

    [Fact]
    public void FromFestivalLocal_ignores_the_DateTimeKind_of_the_stored_value()
    {
        FestivalClock clock = CreateClock(DateTimeOffset.UnixEpoch);
        var wallClock = new DateTime(2027, 5, 15, 11, 0, 0);

        DateTimeOffset fromLocal = clock.FromFestivalLocal(DateTime.SpecifyKind(wallClock, DateTimeKind.Local));
        DateTimeOffset fromUtc = clock.FromFestivalLocal(DateTime.SpecifyKind(wallClock, DateTimeKind.Utc));

        Assert.Equal(fromLocal, fromUtc);
        Assert.Equal(11, fromUtc.Hour);
    }

    [Fact]
    public void Now_is_the_current_UTC_instant_shown_in_festival_time()
    {
        FestivalClock clock = CreateClock(new DateTimeOffset(2027, 5, 15, 21, 30, 0, TimeSpan.Zero));

        DateTimeOffset now = clock.Now;

        Assert.Equal(new DateTime(2027, 5, 16, 1, 30, 0), now.DateTime); // already the next day in Tbilisi
        Assert.Equal(TimeSpan.FromHours(4), now.Offset);
    }

    [Fact]
    public void OffsetLabel_formats_whole_hours()
    {
        FestivalClock clock = CreateClock(DateTimeOffset.UnixEpoch);

        Assert.Equal("GMT+4", clock.OffsetLabel(new DateTimeOffset(2027, 5, 15, 7, 0, 0, TimeSpan.Zero)));
    }
}
