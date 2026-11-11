namespace GeekPass.Web.Cms.DemoContent;

/// <summary>
/// Stable aliases and keys of the GeekPass content model.
/// Keys are fixed so that every environment gets the same IDs (see ADR-0004).
/// <see cref="Documents.CatanChampionship"/> is the stable event ID that the Booking module will use later.
/// </summary>
public static class ContentModel
{
    /// <summary>
    /// Version of the content model created by <see cref="DemoContentSeeder"/>.
    /// Bump it whenever an existing type changes; the seeder then warns that the database must be reset (ADR-0004).
    /// </summary>
    public const int Version = 3;

    public static class DocumentTypes
    {
        public static readonly Guid Home = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0001");
        public static readonly Guid Programme = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0002");
        public static readonly Guid FestivalEvent = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0003");
    }

    /// <summary>Element types used as blocks in the Home Block Grid.</summary>
    public static class ElementTypes
    {
        public static readonly Guid HeroBlock = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0101");
        public static readonly Guid FeaturedEventsBlock = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0102");
        public static readonly Guid TrackCardBlock = new("6a0f4a6e-2f43-4c43-9d55-1f2d7a9c0103");
    }

    public static class DataTypes
    {
        public static readonly Guid HomeBlockGrid = new("4d9e8c7b-1a2f-4b3c-9d8e-7f6a5b4c0001");
        public static readonly Guid Track = new("4d9e8c7b-1a2f-4b3c-9d8e-7f6a5b4c0002");
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
        public static readonly Guid WorldbuildingTalk = new("c3a1e7d4-5b2f-4e8a-9c6d-7f1e2a3b0004");
        public static readonly Guid FirstContactPanel = new("c3a1e7d4-5b2f-4e8a-9c6d-7f1e2a3b0005");
        public static readonly Guid LearnToPlayCorner = new("c3a1e7d4-5b2f-4e8a-9c6d-7f1e2a3b0006");
    }

    public static class Aliases
    {
        public const string Home = "home";
        public const string Programme = "programme";
        public const string FestivalEvent = "festivalEvent";
        public const string HeroBlock = "heroBlock";
        public const string FeaturedEventsBlock = "featuredEventsBlock";
        public const string TrackCardBlock = "trackCardBlock";
    }

    /// <summary>Property aliases, shared by the seeder and the Razor views.</summary>
    public static class Properties
    {
        // Home
        public const string Blocks = "blocks";

        // Blocks
        public const string Title = "title";
        public const string Text = "text";
        public const string CtaLabel = "ctaLabel";
        public const string CtaUrl = "ctaUrl";
        public const string Heading = "heading";
        public const string MaxItems = "maxItems";

        // Programme
        public const string Intro = "intro";

        // FestivalEvent
        public const string Subtitle = "subtitle";
        public const string Summary = "summary";
        public const string Description = "description";
        public const string StartsAt = "startsAt";
        public const string EndsAt = "endsAt";
        public const string Venue = "venue";
        public const string Track = "track";
        public const string Bookable = "bookable";
        public const string RequestedCapacity = "requestedCapacity";
    }

    /// <summary>Festival tracks: the values of the Track dropdown.</summary>
    public static class Tracks
    {
        public const string Fantasy = "Fantasy";
        public const string SciFi = "Sci-Fi";
        public const string Tabletop = "Tabletop";

        public static readonly string[] All = [Fantasy, SciFi, Tabletop];
    }
}
