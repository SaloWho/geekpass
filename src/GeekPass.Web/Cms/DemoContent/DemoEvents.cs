using static GeekPass.Web.Cms.DemoContent.ContentModel;

namespace GeekPass.Web.Cms.DemoContent;

/// <summary>
/// Synthetic programme. Times are festival local time (Asia/Tbilisi), the way an editor types them.
/// </summary>
internal static class DemoEvents
{
    public sealed record DemoEvent(
        Guid Key,
        string Name,
        string Subtitle,
        string Track,
        string Summary,
        string DescriptionHtml,
        string StartsAt,
        string EndsAt,
        string Venue,
        bool Bookable,
        int Capacity,
        bool PublishOnSeed);

    public static readonly DemoEvent[] All =
    [
        // Draft on purpose: publishing it is the editor demo (README).
        new(Documents.CatanChampionship, "Catan Championship", "Settlers Cup", Tracks.Tabletop,
            "Four-player Swiss rounds and a final table. Bring your best trading face.",
            "<p>Trade, build and race to 10 victory points. Three Swiss rounds decide who plays the final table.</p>" +
            "<ul><li>Base game with the tournament variant</li><li>Boards and pieces are provided</li><li>Beginners welcome</li></ul>",
            "2027-05-15 11:00:00", "2027-05-15 17:00:00", "Hall B, Table Zone", Bookable: true, Capacity: 4, PublishOnSeed: false),

        new(Documents.WorldbuildingTalk, "Worldbuilding for Fantasy Writers", "Maps, myths and magic systems", Tracks.Fantasy,
            "How to build a world that readers believe in, from the first map to the last rule of magic.",
            "<p>A practical talk with examples from tabletop campaigns and novels. Bring a notebook.</p>",
            "2027-05-15 14:00:00", "2027-05-15 15:00:00", "Main Stage", Bookable: false, Capacity: 0, PublishOnSeed: true),

        new(Documents.FirstContactPanel, "First Contact", "A sci-fi panel", Tracks.SciFi,
            "Panelists debate how first contact would really go, and what science fiction got right.",
            "<p>Moderated panel with audience questions in the last 20 minutes.</p>",
            "2027-05-16 12:00:00", "2027-05-16 13:30:00", "Main Stage", Bookable: false, Capacity: 0, PublishOnSeed: true),

        new(Documents.LearnToPlayCorner, "Learn-to-Play Corner", "Modern board games in 20 minutes", Tracks.Tabletop,
            "Volunteers teach a different modern board game every half hour. Drop in, no booking needed.",
            "<p>Games rotate every 30 minutes. Tables for up to 6 players.</p>",
            "2027-05-16 10:00:00", "2027-05-16 13:00:00", "Hall B, Table Zone", Bookable: false, Capacity: 0, PublishOnSeed: true),
    ];
}
