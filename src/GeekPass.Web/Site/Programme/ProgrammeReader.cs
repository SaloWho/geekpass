using GeekPass.Web.Cms.DemoContent;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace GeekPass.Web.Site.Programme;

/// <summary>Reads published festival events from Umbraco and maps them to <see cref="ProgrammeEvent"/>.</summary>
public sealed class ProgrammeReader(FestivalClock clock)
{
    /// <summary>All published events of the site that <paramref name="anyPage"/> belongs to.</summary>
    public IReadOnlyList<ProgrammeEvent> GetEvents(IPublishedContent anyPage) =>
        anyPage.Root().Children()
            .Where(x => x.ContentType.Alias == ContentModel.Aliases.Programme)
            .SelectMany(programme => programme.Children())
            .Where(x => x.ContentType.Alias == ContentModel.Aliases.FestivalEvent)
            .Select(Map)
            .ToList();

    public IReadOnlyList<ProgrammeDay> GetDays(IPublishedContent anyPage) =>
        ProgrammeSchedule.GroupByDay(GetEvents(anyPage));

    public IReadOnlyList<ProgrammeEvent> GetUpcoming(IPublishedContent anyPage, int take) =>
        ProgrammeSchedule.Upcoming(GetEvents(anyPage), clock.Now, take);

    public ProgrammeEvent Map(IPublishedContent item) => new(
        item.Key,
        item.Name,
        item.Url(),
        item.Value<string>(ContentModel.Properties.Track),
        item.Value<string>(ContentModel.Properties.Summary),
        item.Value<string>(ContentModel.Properties.Venue),
        clock.FromFestivalLocal(item.Value<DateTime>(ContentModel.Properties.StartsAt)),
        clock.FromFestivalLocal(item.Value<DateTime>(ContentModel.Properties.EndsAt)));
}
