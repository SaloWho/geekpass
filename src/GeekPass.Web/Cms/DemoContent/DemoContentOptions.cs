namespace GeekPass.Web.Cms.DemoContent;

public sealed class DemoContentOptions
{
    public const string SectionName = "GeekPass:DemoContent";

    /// <summary>Create the content model and synthetic demo content on startup (idempotent).</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Publish the Catan event right after seeding. Off by default: in the demo an editor publishes it.
    /// CI turns it on so the smoke test can request the event page.
    /// </summary>
    public bool PublishCatanEvent { get; set; }
}
