using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.ContentTypeEditing;
using Umbraco.Cms.Core.Services.OperationStatus;
using static GeekPass.Web.Cms.DemoContent.ContentModel;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace GeekPass.Web.Cms.DemoContent;

/// <summary>
/// Creates the GeekPass content model (templates + document types) and synthetic demo content.
/// Runs once Umbraco has started; every step is idempotent (looked up by fixed key first),
/// so restarting the app or running it on an existing database is safe.
/// </summary>
/// <remarks>
/// It uses the same editing services the backoffice uses, so Umbraco validates everything
/// exactly as if an editor had clicked it together. See ADR-0004 for why this is code and not uSync.
/// </remarks>
public sealed class DemoContentSeeder(
    IRuntimeState runtimeState,
    IOptions<DemoContentOptions> options,
    IWebHostEnvironment environment,
    ITemplateService templateService,
    IContentTypeService contentTypeService,
    IContentTypeEditingService contentTypeEditingService,
    IContentEditingService contentEditingService,
    IContentPublishingService contentPublishingService,
    ILogger<DemoContentSeeder> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Seeding runs as the built-in administrator created by the (unattended) install.
    private static readonly Guid SuperUserKey = UmbracoConstants.Security.SuperUserKey;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        logger.LogInformation("Seeding GeekPass demo content model and content");

        await EnsureTemplateAsync(Templates.Home, "Home", Aliases.Home);
        await EnsureTemplateAsync(Templates.Programme, "Programme", Aliases.Programme);
        await EnsureTemplateAsync(Templates.FestivalEvent, "Festival Event", Aliases.FestivalEvent);

        // Children first, because a parent type lists its allowed child types.
        await EnsureFestivalEventTypeAsync();
        await EnsureProgrammeTypeAsync();
        await EnsureHomeTypeAsync();

        await EnsureHomeContentAsync();
        await EnsureProgrammeContentAsync();
        await EnsureCatanEventAsync();

        logger.LogInformation("GeekPass demo content is ready");
    }

    // ---------- Templates ----------

    private async Task EnsureTemplateAsync(Guid key, string name, string alias)
    {
        if (await templateService.GetAsync(key) is not null)
        {
            return;
        }

        // Umbraco writes the template content to Views/{alias}.cshtml when it creates a template.
        // Passing the content of our committed view keeps that file unchanged.
        var viewPath = Path.Combine(environment.ContentRootPath, "Views", $"{alias}.cshtml");
        var content = System.IO.File.Exists(viewPath) ? await System.IO.File.ReadAllTextAsync(viewPath) : null;

        Attempt<ITemplate, TemplateOperationStatus> result =
            await templateService.CreateAsync(name, alias, content, SuperUserKey, key);
        EnsureSuccess(result.Success, $"template '{alias}'", result.Status);
    }

    // ---------- Document types ----------

    private Task EnsureFestivalEventTypeAsync()
    {
        var details = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30001");
        var booking = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30002");

        return EnsureDocumentTypeAsync(new ContentTypeCreateModel
        {
            Key = DocumentTypes.FestivalEvent,
            Alias = Aliases.FestivalEvent,
            Name = "Festival Event",
            Description = "A session, tournament or talk in the festival programme.",
            Icon = "icon-calendar-alt",
            AllowedTemplateKeys = [Templates.FestivalEvent],
            DefaultTemplateKey = Templates.FestivalEvent,
            Containers =
            [
                Group(details, "Details", 0),
                Group(booking, "Booking", 1),
            ],
            Properties =
            [
                Property(details, Properties.Subtitle, "Subtitle", UmbracoConstants.DataTypes.Guids.TextstringGuid, 0),
                Property(details, Properties.Summary, "Summary", UmbracoConstants.DataTypes.Guids.TextareaGuid, 1, mandatory: true,
                    description: "One or two sentences for programme cards."),
                Property(details, Properties.Description, "Description", UmbracoConstants.DataTypes.Guids.TextareaGuid, 2),
                Property(details, Properties.StartsAt, "Starts at", UmbracoConstants.DataTypes.Guids.DatePickerWithTimeGuid, 3, mandatory: true),
                Property(details, Properties.EndsAt, "Ends at", UmbracoConstants.DataTypes.Guids.DatePickerWithTimeGuid, 4, mandatory: true),
                Property(details, Properties.Venue, "Venue", UmbracoConstants.DataTypes.Guids.TextstringGuid, 5, mandatory: true),
                Property(booking, Properties.Bookable, "Bookable", UmbracoConstants.DataTypes.Guids.CheckboxGuid, 0,
                    description: "Guests can book a seat once the Booking module is live (Stage 2)."),
                Property(booking, Properties.RequestedCapacity, "Requested capacity", UmbracoConstants.DataTypes.Guids.NumericGuid, 1,
                    description: "Input for the Booking module. Booking owns the effective capacity (ADR-0002)."),
            ],
        });
    }

    private Task EnsureProgrammeTypeAsync()
    {
        var content = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30003");

        return EnsureDocumentTypeAsync(new ContentTypeCreateModel
        {
            Key = DocumentTypes.Programme,
            Alias = Aliases.Programme,
            Name = "Programme",
            Icon = "icon-calendar",
            AllowedTemplateKeys = [Templates.Programme],
            DefaultTemplateKey = Templates.Programme,
            AllowedContentTypes = [new ContentTypeSort(DocumentTypes.FestivalEvent, 0, Aliases.FestivalEvent)],
            Containers = [Group(content, "Content", 0)],
            Properties =
            [
                Property(content, Properties.Intro, "Intro", UmbracoConstants.DataTypes.Guids.TextareaGuid, 0),
            ],
        });
    }

    private Task EnsureHomeTypeAsync()
    {
        var hero = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30004");

        return EnsureDocumentTypeAsync(new ContentTypeCreateModel
        {
            Key = DocumentTypes.Home,
            Alias = Aliases.Home,
            Name = "Home",
            Icon = "icon-home",
            AllowedAsRoot = true,
            AllowedTemplateKeys = [Templates.Home],
            DefaultTemplateKey = Templates.Home,
            AllowedContentTypes = [new ContentTypeSort(DocumentTypes.Programme, 0, Aliases.Programme)],
            Containers = [Group(hero, "Hero", 0)],
            Properties =
            [
                Property(hero, Properties.HeroTitle, "Hero title", UmbracoConstants.DataTypes.Guids.TextstringGuid, 0, mandatory: true),
                Property(hero, Properties.HeroSubtitle, "Hero subtitle", UmbracoConstants.DataTypes.Guids.TextareaGuid, 1),
            ],
        });
    }

    private async Task EnsureDocumentTypeAsync(ContentTypeCreateModel model)
    {
        if (await contentTypeService.GetAsync(model.Key!.Value) is not null)
        {
            return;
        }

        Attempt<IContentType?, ContentTypeOperationStatus> result =
            await contentTypeEditingService.CreateAsync(model, SuperUserKey);
        EnsureSuccess(result.Success, $"document type '{model.Alias}'", result.Status);
    }

    private static ContentTypePropertyContainerModel Group(Guid key, string name, int sortOrder) => new()
    {
        Key = key,
        Name = name,
        Type = nameof(PropertyGroupType.Group),
        SortOrder = sortOrder,
    };

    private static ContentTypePropertyTypeModel Property(
        Guid containerKey,
        string alias,
        string name,
        Guid dataTypeKey,
        int sortOrder,
        bool mandatory = false,
        string? description = null) => new()
    {
        Key = Guid.NewGuid(),
        ContainerKey = containerKey,
        Alias = alias,
        Name = name,
        Description = description,
        DataTypeKey = dataTypeKey,
        SortOrder = sortOrder,
        Validation = new PropertyTypeValidation { Mandatory = mandatory },
    };

    // ---------- Content ----------

    private async Task EnsureHomeContentAsync()
    {
        if (await contentEditingService.GetAsync(Documents.Home) is not null)
        {
            return;
        }

        await CreateContentAsync(new ContentCreateModel
        {
            Key = Documents.Home,
            ContentTypeKey = DocumentTypes.Home,
            TemplateKey = Templates.Home,
            Variants = [new VariantModel { Name = "GeekPass Fest 2027" }],
            Properties =
            [
                Value(Properties.HeroTitle, "GeekPass Fest 2027"),
                Value(Properties.HeroSubtitle, "Three days of fantasy, sci-fi and tabletop games in Tbilisi. Synthetic demo data."),
            ],
        });
        await PublishAsync(Documents.Home, "Home");
    }

    private async Task EnsureProgrammeContentAsync()
    {
        if (await contentEditingService.GetAsync(Documents.Programme) is not null)
        {
            return;
        }

        await CreateContentAsync(new ContentCreateModel
        {
            Key = Documents.Programme,
            ParentKey = Documents.Home,
            ContentTypeKey = DocumentTypes.Programme,
            TemplateKey = Templates.Programme,
            Variants = [new VariantModel { Name = "Programme" }],
            Properties =
            [
                Value(Properties.Intro, "Tournaments, talks and workshops. Seats are limited, so book early."),
            ],
        });
        await PublishAsync(Documents.Programme, "Programme");
    }

    private async Task EnsureCatanEventAsync()
    {
        if (await contentEditingService.GetAsync(Documents.CatanChampionship) is null)
        {
            // Created as a draft on purpose: publishing it is step 2 of the editor demo (README).
            await CreateContentAsync(new ContentCreateModel
            {
                Key = Documents.CatanChampionship,
                ParentKey = Documents.Programme,
                ContentTypeKey = DocumentTypes.FestivalEvent,
                TemplateKey = Templates.FestivalEvent,
                Variants = [new VariantModel { Name = "Catan Championship" }],
                Properties =
                [
                    Value(Properties.Subtitle, "Settlers Cup"),
                    Value(Properties.Summary, "Four-player Swiss rounds and a final table. Bring your best trading face."),
                    Value(Properties.Description,
                        "Trade, build and race to 10 victory points. Three Swiss rounds decide who plays the final table. " +
                        "Base game rules with the official tournament variant. Boards and pieces are provided. " +
                        "All names and data on this site are synthetic."),
                    Value(Properties.StartsAt, "2027-05-15 11:00:00"),
                    Value(Properties.EndsAt, "2027-05-15 17:00:00"),
                    Value(Properties.Venue, "Hall B, Table Zone"),
                    Value(Properties.Bookable, true),
                    Value(Properties.RequestedCapacity, 4),
                ],
            });
        }

        if (options.Value.PublishCatanEvent)
        {
            await PublishAsync(Documents.CatanChampionship, "Catan Championship");
        }
    }

    private async Task CreateContentAsync(ContentCreateModel model)
    {
        Attempt<ContentCreateResult, ContentEditingOperationStatus> result =
            await contentEditingService.CreateAsync(model, SuperUserKey);
        EnsureSuccess(
            result.Success && result.Status == ContentEditingOperationStatus.Success,
            $"content '{model.Variants.First().Name}'",
            result.Status);
    }

    private async Task PublishAsync(Guid key, string name)
    {
        Attempt<ContentPublishingResult, ContentPublishingOperationStatus> result =
            await contentPublishingService.PublishAsync(
                key,
                [new CulturePublishScheduleModel { Culture = UmbracoConstants.System.InvariantCulture }],
                SuperUserKey);
        EnsureSuccess(result.Success, $"publish of '{name}'", result.Status);
    }

    private static PropertyValueModel Value(string alias, object? value) => new() { Alias = alias, Value = value };

    private static void EnsureSuccess<TStatus>(bool success, string what, TStatus status)
    {
        if (!success)
        {
            // Fail loudly: a half-seeded demo is worse than a clear startup error.
            throw new InvalidOperationException($"GeekPass demo content: creating {what} failed with status '{status}'.");
        }
    }
}
