using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace GeekPass.Web.Cms.DemoContent;

public sealed class DemoContentComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<DemoContentOptions>(builder.Config.GetSection(DemoContentOptions.SectionName));
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DemoContentSeeder>();
    }
}
