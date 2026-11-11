using GeekPass.Web.Site.Programme;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace GeekPass.Web.Site;

/// <summary>Registers the website's own services (not the CMS seeding, see DemoContentComposer).</summary>
public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<FestivalOptions>(builder.Config.GetSection(FestivalOptions.SectionName));
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<FestivalClock>();
        builder.Services.AddSingleton<ProgrammeReader>();
    }
}
