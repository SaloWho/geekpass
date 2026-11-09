namespace GeekPass.Web.Cms.DemoContent;

/// <summary>
/// Stable aliases and keys of the GeekPass content model.
/// Keys are fixed so that every environment gets the same IDs (see ADR-0004).
/// <see cref="Documents.CatanChampionship"/> is the stable event ID that the Booking module will use later.
/// </summary>
public static class ContentModel
{
    public static class DocumentTypes
    {
        public static readonly Guid Home = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0001");
        public static readonly Guid Programme = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0002");
        public static readonly Guid FestivalEvent = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0003");
    }

    public static class Templates
    {
        public static readonly Guid Home = new("8b7d2f19-6c1e-4a3b-8f0e-3c4d5e6f0001");
        public static readonly Guid Programme = new("8b7d2f19-6c1e-4a3b-8f0e-3c4d5e6f0002");
        public static readonly Guid FestivalEvent = new("8b7d2f19-6c1e-4a3b-8f0e-3c4d5e6f0003");
    }

    public static class Documents
    {
        public static readonly Guid Home = new("c3a1e7d4-5b2f-4e8a-9c6d-7f1e2a3b0001");
        public static readonly Guid Programme = new("c3a1e7d4-5b2f-4e8a-9c6d-7f1e2a3b0002");
        public static readonly Guid CatanChampionship = new("c3a1e7d4-5b2f-4e8a-9c6d-7f1e2a3b0003");
    }

    public static class Aliases
    {
        public const string Home = "home";
        public const string Programme = "programme";
        public const string FestivalEvent = "festivalEvent";
    }

    /// <summary>Property aliases, shared by the seeder and the Razor views.</summary>
    public static class Properties
    {
        // Home
        public const string HeroTitle = "heroTitle";
        public const string HeroSubtitle = "heroSubtitle";

        // Programme
        public const string Intro = "intro";

        // FestivalEvent
        public const string Subtitle = "subtitle";
        public const string Summary = "summary";
        public const string Description = "description";
        public const string StartsAt = "startsAt";
        public const string EndsAt = "endsAt";
        public const string Venue = "venue";
        public const string Bookable = "bookable";
        public const string RequestedCapacity = "requestedCapacity";
    }
}
