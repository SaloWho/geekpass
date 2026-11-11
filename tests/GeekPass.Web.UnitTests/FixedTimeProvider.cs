namespace GeekPass.Web.UnitTests;

/// <summary>A clock that always returns the same instant. Enough for these tests; no extra package needed.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
