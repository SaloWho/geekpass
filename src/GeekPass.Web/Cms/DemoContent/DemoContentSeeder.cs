using System.Globalization;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
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
    IDataTypeService dataTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer,
    IKeyValueService keyValueService,
    ILogger<DemoContentSeeder> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Seeding runs as the built-in administrator created by the (unattended) install.
    private static readonly Guid SuperUserKey = UmbracoConstants.Security.SuperUserKey;

    private const string ModelVersionKey = "GeekPass.DemoContent.ModelVersion";

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        if (!await IsModelVersionCurrentAsync())
        {
            return;
        }

        logger.LogInformation("Seeding GeekPass demo content model and content");

        await EnsureTemplateAsync(Templates.Home, "Home", Aliases.Home);
        await EnsureTemplateAsync(Templates.Programme, "Programme", Aliases.Programme);
        await EnsureTemplateAsync(Templates.FestivalEvent, "Festival Event", Aliases.FestivalEvent);

        // Blocks before the data type that lists them, and the data type before Home, which uses it.
        await EnsureHeroBlockTypeAsync();
        await EnsureFeaturedEventsBlockTypeAsync();
        await EnsureTrackCardBlockTypeAsync();
        await EnsureHomeBlockGridDataTypeAsync();

        await EnsureTrackDataTypeAsync();

        // Children first, because a parent type lists its allowed child types.
        await EnsureFestivalEventTypeAsync();
        await EnsureProgrammeTypeAsync();
        await EnsureHomeTypeAsync();

        await EnsureHomeContentAsync();
        await EnsureProgrammeContentAsync();
        foreach (DemoEvents.DemoEvent demoEvent in DemoEvents.All)
        {
            await EnsureEventAsync(demoEvent);
        }

        keyValueService.SetValue(ModelVersionKey, ContentModel.Version.ToString(CultureInfo.InvariantCulture));
        logger.LogInformation("GeekPass demo content is ready");
    }

    // ---------- Model version ----------

    /// <summary>
    /// The seeder only creates; it never changes existing types (ADR-0004).
    /// So when the model changed since this database was seeded, stop and tell the developer to reset it.
    /// </summary>
    private async Task<bool> IsModelVersionCurrentAsync()
    {
        var stored = keyValueService.GetValue(ModelVersionKey);
        var isFreshDatabase = await contentTypeService.GetAsync(DocumentTypes.Home) is null;

        if (isFreshDatabase || stored == ContentModel.Version.ToString(CultureInfo.InvariantCulture))
        {
            return true;
        }

        logger.LogError(
            "GeekPass demo content model is outdated (database: {StoredVersion}, code: {CodeVersion}). " +
            "Reset the local database with 'docker compose down -v && docker compose up -d' and restart the site.",
            stored ?? "1",
            ContentModel.Version);
        return false;
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
                Property(details, Properties.Track, "Track", DataTypes.Track, 2, mandatory: true),
                Property(details, Properties.Description, "Description", UmbracoConstants.DataTypes.Guids.RichtextEditorGuid, 3),
                Property(details, Properties.StartsAt, "Starts at", UmbracoConstants.DataTypes.Guids.DatePickerWithTimeGuid, 4, mandatory: true,
                    description: "Festival local time (Tbilisi)."),
                Property(details, Properties.EndsAt, "Ends at", UmbracoConstants.DataTypes.Guids.DatePickerWithTimeGuid, 5, mandatory: true,
                    description: "Festival local time (Tbilisi)."),
                Property(details, Properties.Venue, "Venue", UmbracoConstants.DataTypes.Guids.TextstringGuid, 6, mandatory: true),
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
        var content = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30004");

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
            Containers = [Group(content, "Content", 0)],
            Properties =
            [
                Property(content, Properties.Blocks, "Blocks", DataTypes.HomeBlockGrid, 0,
                    description: "Compose the Home page from blocks."),
            ],
        });
    }

    private async Task EnsureTrackDataTypeAsync()
    {
        if (await dataTypeService.GetAsync(DataTypes.Track) is not null)
        {
            return;
        }

        IDataEditor editor = propertyEditors[UmbracoConstants.PropertyEditors.Aliases.DropDownListFlexible]
            ?? throw new InvalidOperationException("The dropdown property editor is not registered.");

        var dataType = new DataType(editor, configurationSerializer)
        {
            Key = DataTypes.Track,
            Name = "GeekPass - Track",
            DatabaseType = ValueStorageType.Nvarchar,
            ConfigurationData = new Dictionary<string, object>
            {
                ["items"] = ContentModel.Tracks.All,
                ["multiple"] = false,
            },
        };

        Attempt<IDataType, DataTypeOperationStatus> result = await dataTypeService.CreateAsync(dataType, SuperUserKey);
        EnsureSuccess(result.Success, "data type 'GeekPass - Track'", result.Status);
    }

    // ---------- Block Grid: element types and data type ----------

    private Task EnsureHeroBlockTypeAsync()
    {
        var group = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30101");
        return EnsureDocumentTypeAsync(new ContentTypeCreateModel
        {
            Key = ElementTypes.HeroBlock,
            Alias = Aliases.HeroBlock,
            Name = "Hero",
            Icon = "icon-picture",
            IsElement = true,
            Containers = [Group(group, "Content", 0)],
            Properties =
            [
                Property(group, Properties.Title, "Title", UmbracoConstants.DataTypes.Guids.TextstringGuid, 0, mandatory: true),
                Property(group, Properties.Text, "Text", UmbracoConstants.DataTypes.Guids.TextareaGuid, 1),
                Property(group, Properties.CtaLabel, "Button label", UmbracoConstants.DataTypes.Guids.TextstringGuid, 2),
                Property(group, Properties.CtaUrl, "Button URL", UmbracoConstants.DataTypes.Guids.TextstringGuid, 3,
                    description: "Relative URL, e.g. /programme/"),
            ],
        });
    }

    private Task EnsureFeaturedEventsBlockTypeAsync()
    {
        var group = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30102");
        return EnsureDocumentTypeAsync(new ContentTypeCreateModel
        {
            Key = ElementTypes.FeaturedEventsBlock,
            Alias = Aliases.FeaturedEventsBlock,
            Name = "Featured Events",
            Icon = "icon-calendar-alt",
            IsElement = true,
            Containers = [Group(group, "Content", 0)],
            Properties =
            [
                Property(group, Properties.Heading, "Heading", UmbracoConstants.DataTypes.Guids.TextstringGuid, 0, mandatory: true),
                Property(group, Properties.MaxItems, "Number of events", UmbracoConstants.DataTypes.Guids.NumericGuid, 1,
                    description: "Shows the next upcoming published events from the programme."),
            ],
        });
    }

    private Task EnsureTrackCardBlockTypeAsync()
    {
        var group = Guid.Parse("0d2e4f60-1a2b-4c3d-8e9f-a0b1c2d30103");
        return EnsureDocumentTypeAsync(new ContentTypeCreateModel
        {
            Key = ElementTypes.TrackCardBlock,
            Alias = Aliases.TrackCardBlock,
            Name = "Track Card",
            Icon = "icon-tag",
            IsElement = true,
            Containers = [Group(group, "Content", 0)],
            Properties =
            [
                Property(group, Properties.Title, "Title", UmbracoConstants.DataTypes.Guids.TextstringGuid, 0, mandatory: true),
                Property(group, Properties.Text, "Text", UmbracoConstants.DataTypes.Guids.TextareaGuid, 1),
            ],
        });
    }

    private async Task EnsureHomeBlockGridDataTypeAsync()
    {
        if (await dataTypeService.GetAsync(DataTypes.HomeBlockGrid) is not null)
        {
            return;
        }

        IDataEditor editor = propertyEditors[UmbracoConstants.PropertyEditors.Aliases.BlockGrid]
            ?? throw new InvalidOperationException("The Block Grid property editor is not registered.");

        // Same shape the backoffice stores when you configure a Block Grid by hand.
        static Dictionary<string, object> Block(Guid elementTypeKey, params int[] columnSpans) => new()
        {
            ["contentElementTypeKey"] = elementTypeKey,
            ["allowAtRoot"] = true,
            ["allowInAreas"] = false,
            ["columnSpanOptions"] = columnSpans.Select(span => new Dictionary<string, object> { ["columnSpan"] = span }).ToArray(),
            ["rowMinSpan"] = 1,
            ["rowMaxSpan"] = 1,
            ["areas"] = Array.Empty<object>(),
        };

        var dataType = new DataType(editor, configurationSerializer)
        {
            Key = DataTypes.HomeBlockGrid,
            Name = "GeekPass - Home Blocks",
            DatabaseType = ValueStorageType.Ntext,
            ConfigurationData = new Dictionary<string, object>
            {
                ["gridColumns"] = 12,
                ["blocks"] = new[]
                {
                    Block(ElementTypes.HeroBlock, 12),
                    Block(ElementTypes.FeaturedEventsBlock, 12),
                    Block(ElementTypes.TrackCardBlock, 4, 6, 12),
                },
            },
        };

        Attempt<IDataType, DataTypeOperationStatus> result = await dataTypeService.CreateAsync(dataType, SuperUserKey);
        EnsureSuccess(result.Success, "data type 'GeekPass - Home Blocks'", result.Status);
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
            Properties = [Value(Properties.Blocks, HomeBlocks.Create())],
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

    private async Task EnsureEventAsync(DemoEvents.DemoEvent demoEvent)
    {
        if (await contentEditingService.GetAsync(demoEvent.Key) is null)
        {
            await CreateContentAsync(new ContentCreateModel
            {
                Key = demoEvent.Key,
                ParentKey = Documents.Programme,
                ContentTypeKey = DocumentTypes.FestivalEvent,
                TemplateKey = Templates.FestivalEvent,
                Variants = [new VariantModel { Name = demoEvent.Name }],
                Properties =
                [
                    Value(Properties.Subtitle, demoEvent.Subtitle),
                    Value(Properties.Track, new[] { demoEvent.Track }), // dropdowns take a list of values
                    Value(Properties.Summary, demoEvent.Summary),
                    Value(Properties.Description, demoEvent.DescriptionHtml), // raw markup is accepted by the RTE
                    Value(Properties.StartsAt, demoEvent.StartsAt),
                    Value(Properties.EndsAt, demoEvent.EndsAt),
                    Value(Properties.Venue, demoEvent.Venue),
                    Value(Properties.Bookable, demoEvent.Bookable),
                    Value(Properties.RequestedCapacity, demoEvent.Capacity),
                ],
            });

            if (demoEvent.PublishOnSeed)
            {
                await PublishAsync(demoEvent.Key, demoEvent.Name);
            }
        }

        // CI has no editor to click Publish, so it publishes the Catan draft too.
        if (!demoEvent.PublishOnSeed && options.Value.PublishCatanEvent)
        {
            await PublishAsync(demoEvent.Key, demoEvent.Name);
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
