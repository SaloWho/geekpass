using System.Text.Json.Nodes;
using static GeekPass.Web.Cms.DemoContent.ContentModel;

namespace GeekPass.Web.Cms.DemoContent;

/// <summary>
/// The demo Home page as a Block Grid value, in the same JSON format the backoffice sends when an editor saves.
/// </summary>
/// <remarks>
/// Format (Umbraco 15+): <c>layout</c> places blocks in the grid, <c>contentData</c> holds each block's values,
/// <c>expose</c> marks blocks as visible for a culture/segment (null = invariant).
/// </remarks>
internal static class HomeBlocks
{
    private const int GridColumns = 12;

    public static string Create()
    {
        Block[] blocks =
        [
            new(new Guid("b10c0000-0000-4000-8000-000000000001"), ElementTypes.HeroBlock, GridColumns,
            [
                (Properties.Title, "GeekPass Fest 2027"),
                (Properties.Text, "Three days of fantasy, sci-fi and tabletop games in Tbilisi. Tournaments, talks and workshops. Synthetic demo data."),
                (Properties.CtaLabel, "See the programme"),
                (Properties.CtaUrl, "/programme/"),
            ]),
            new(new Guid("b10c0000-0000-4000-8000-000000000002"), ElementTypes.FeaturedEventsBlock, GridColumns,
            [
                (Properties.Heading, "Coming up"),
                (Properties.MaxItems, 3),
            ]),
            new(new Guid("b10c0000-0000-4000-8000-000000000003"), ElementTypes.TrackCardBlock, 4,
            [
                (Properties.Title, "Fantasy"),
                (Properties.Text, "Worldbuilding talks, cosplay and dragons of every size."),
            ]),
            new(new Guid("b10c0000-0000-4000-8000-000000000004"), ElementTypes.TrackCardBlock, 4,
            [
                (Properties.Title, "Sci-Fi"),
                (Properties.Text, "Starships, AI ethics panels and retro-futurism."),
            ]),
            new(new Guid("b10c0000-0000-4000-8000-000000000005"), ElementTypes.TrackCardBlock, 4,
            [
                (Properties.Title, "Tabletop"),
                (Properties.Text, "Board game tournaments, RPG one-shots and a learn-to-play corner."),
            ]),
        ];

        var value = new JsonObject
        {
            ["layout"] = new JsonObject
            {
                [Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.BlockGrid] = new JsonArray(blocks.Select(b => (JsonNode)new JsonObject
                {
                    ["contentKey"] = b.Key,
                    ["settingsKey"] = null,
                    ["columnSpan"] = b.ColumnSpan,
                    ["rowSpan"] = 1,
                    ["areas"] = new JsonArray(),
                }).ToArray()),
            },
            ["contentData"] = new JsonArray(blocks.Select(b => (JsonNode)new JsonObject
            {
                ["key"] = b.Key,
                ["contentTypeKey"] = b.ElementTypeKey,
                ["values"] = new JsonArray(b.Values.Select(v => (JsonNode)new JsonObject
                {
                    ["alias"] = v.Alias,
                    ["value"] = v.Value switch
                    {
                        int number => JsonValue.Create(number),
                        string text => JsonValue.Create(text),
                        _ => throw new NotSupportedException($"Unsupported demo block value type {v.Value.GetType()}"),
                    },
                    ["culture"] = null,
                    ["segment"] = null,
                }).ToArray()),
            }).ToArray()),
            ["settingsData"] = new JsonArray(),
            ["expose"] = new JsonArray(blocks.Select(b => (JsonNode)new JsonObject
            {
                ["contentKey"] = b.Key,
                ["culture"] = null,
                ["segment"] = null,
            }).ToArray()),
        };

        return value.ToJsonString();
    }

    private sealed record Block(Guid Key, Guid ElementTypeKey, int ColumnSpan, (string Alias, object Value)[] Values);
}
