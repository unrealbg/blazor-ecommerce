using BuildingBlocks.Infrastructure.Modules;
using Catalog.Application.Products;
using Catalog.Application.Products.CreateProduct;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Seeding;

internal sealed class CatalogReleaseSeeder(
    ISender sender,
    IProductRepository productRepository,
    ILogger<CatalogReleaseSeeder> logger)
{
    private static readonly Guid FinishOptionId = Guid.Parse("80d25691-43d2-4f41-8a47-f804f34fd4d0");
    private static readonly Guid GraphiteValueId = Guid.Parse("ebde404d-b0da-4c6d-8e47-fb0ba4be1908");
    private static readonly Guid SilverValueId = Guid.Parse("f9c2f479-3614-4b1f-91db-5ea0f84b22ef");

    private static readonly CatalogSeedProductDefinition MechanicalKeyboardDefinition = new(
        "mechanical-keyboard",
        publishedAtUtc => new CreateProductCommand(
            Name: "Mechanical Keyboard",
            ShortDescription: "Low-profile mechanical input tuned for focused desk setups.",
            Description: "Precision aluminum keyboard with damped switches, warm backlight control, and a calmer industrial finish for modern workstations.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "all-products",
            CategoryName: "All Products",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: true,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 249.00m,
            Sku: "AF-KEYBOARD-001",
            CompareAtAmount: 279.00m,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("Design", "Layout", "ANSI 80%", 0),
                CreateAttribute("Materials", "Top case", "CNC aluminum", 1, false),
                CreateAttribute("Connectivity", "Wireless", "Bluetooth 5.3 / USB-C", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition MotionDeskMatDefinition = new(
        "motion-desk-mat",
        publishedAtUtc => new CreateProductCommand(
            Name: "Motion Desk Mat",
            ShortDescription: "Layered felt and silicone surface for quieter premium workstations.",
            Description: "A restrained desk surface designed to soften acoustics, organize tools, and keep a premium workspace visually calm.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "all-products",
            CategoryName: "All Products",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: false,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 89.00m,
            Sku: "AF-DESKMAT-001",
            CompareAtAmount: null,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("Surface", "Top layer", "Wool blend felt", 0),
                CreateAttribute("Surface", "Base layer", "Anti-slip silicone", 1),
                CreateAttribute("Dimensions", "Footprint", "900 x 400 mm", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition WorkspaceMonitorLightDefinition = new(
        "workspace-monitor-light",
        publishedAtUtc => new CreateProductCommand(
            Name: "Workspace Monitor Light",
            ShortDescription: "Precision task lighting for modern desks and dual-screen environments.",
            Description: "An asymmetrical optical bar that improves contrast on the workspace surface while keeping glare away from the display.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "workspace",
            CategoryName: "Workspace",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: false,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 159.00m,
            Sku: "AF-LIGHT-001",
            CompareAtAmount: 179.00m,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("Lighting", "Color temperature", "2700K - 6500K", 0),
                CreateAttribute("Lighting", "CRI", "95+", 1),
                CreateAttribute("Power", "Input", "USB-C PD", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition WorkspaceDockDefinition = new(
        "workspace-dock",
        publishedAtUtc => new CreateProductCommand(
            Name: "Workspace Dock",
            ShortDescription: "A compact desktop hub for power, display, and storage expansion.",
            Description: "Machined aluminum dock engineered for premium workspace builds with clean cable routing, silent thermals, and a product-first silhouette.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "workspace",
            CategoryName: "Workspace",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: true,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 299.00m,
            Sku: "AF-DOCK-001",
            CompareAtAmount: null,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("I/O", "Display output", "2x 4K 60Hz", 0),
                CreateAttribute("I/O", "Host connection", "Thunderbolt 4", 1),
                CreateAttribute("Power", "Pass-through", "96W", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition StudioSpeakerPairDefinition = new(
        "studio-speaker-pair",
        publishedAtUtc => new CreateProductCommand(
            Name: "Studio Speaker Pair",
            ShortDescription: "Compact stereo monitors with a disciplined acoustic profile.",
            Description: "Balanced near-field speakers tuned for smaller rooms, studio desks, and high-clarity everyday listening without visual excess.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "audio",
            CategoryName: "Audio",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: false,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 429.00m,
            Sku: "AF-SPEAKER-001",
            CompareAtAmount: null,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("Acoustics", "Amplification", "2 x 50W Class D", 0),
                CreateAttribute("Acoustics", "Frequency range", "48Hz - 22kHz", 1),
                CreateAttribute("Inputs", "Wireless", "Wi-Fi / Bluetooth 5.3", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition AdaptiveHeadphonesDefinition = new(
        "adaptive-headphones",
        publishedAtUtc => new CreateProductCommand(
            Name: "Adaptive Headphones",
            ShortDescription: "Wireless over-ear headphones with cleaner materials and quieter industrial detail.",
            Description: "Adaptive noise control, a lightweight frame, and premium materials designed for travel, focus, and long sessions.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "audio",
            CategoryName: "Audio",
            Status: "Active",
            ProductType: "VariantParent",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: true,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 329.00m,
            Sku: null,
            CompareAtAmount: 359.00m,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options:
            [
                new CreateProductOptionModel(
                    FinishOptionId,
                    "Finish",
                    0,
                    [
                        new CreateProductOptionValueModel(GraphiteValueId, "Graphite", 0),
                        new CreateProductOptionValueModel(SilverValueId, "Silver", 1),
                    ]),
            ],
            Variants:
            [
                new CreateProductVariantModel(
                    Id: null,
                    Sku: "AF-HEAD-GRAPHITE",
                    Name: "Graphite",
                    Slug: "adaptive-headphones-graphite",
                    Barcode: null,
                    PriceAmount: 329.00m,
                    Currency: "EUR",
                    CompareAtPriceAmount: 359.00m,
                    WeightKg: 0.28m,
                    IsActive: true,
                    Position: 0,
                    OptionAssignments:
                    [
                        new CreateProductVariantOptionAssignmentModel(FinishOptionId, GraphiteValueId),
                    ]),
                new CreateProductVariantModel(
                    Id: null,
                    Sku: "AF-HEAD-SILVER",
                    Name: "Silver",
                    Slug: "adaptive-headphones-silver",
                    Barcode: null,
                    PriceAmount: 339.00m,
                    Currency: "EUR",
                    CompareAtPriceAmount: 369.00m,
                    WeightKg: 0.28m,
                    IsActive: true,
                    Position: 1,
                    OptionAssignments:
                    [
                        new CreateProductVariantOptionAssignmentModel(FinishOptionId, SilverValueId),
                    ]),
            ],
            Attributes:
            [
                CreateAttribute("Audio", "Driver", "40 mm custom dynamic", 0),
                CreateAttribute("Audio", "Battery life", "38 hours", 1),
                CreateAttribute("Connectivity", "Codec support", "AAC / aptX Adaptive", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition PulseSmartBandDefinition = new(
        "pulse-smart-band",
        publishedAtUtc => new CreateProductCommand(
            Name: "Pulse Smart Band",
            ShortDescription: "A slim health and performance wearable with premium restraint.",
            Description: "Minimal daily wearable built around recovery tracking, understated materials, and a cleaner interface language.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "wearables",
            CategoryName: "Wearables",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: false,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 189.00m,
            Sku: "AF-BAND-001",
            CompareAtAmount: null,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("Sensors", "Biometrics", "Heart rate / SpO2 / skin temp", 0),
                CreateAttribute("Battery", "Runtime", "7 days", 1),
                CreateAttribute("Durability", "Water resistance", "5 ATM", 2),
            ],
            Images: [],
            Relations: []));

    private static readonly CatalogSeedProductDefinition TravelPowerModuleDefinition = new(
        "travel-power-module",
        publishedAtUtc => new CreateProductCommand(
            Name: "Travel Power Module",
            ShortDescription: "Multi-port GaN charging block for cleaner travel and desk carry.",
            Description: "A compact power module that combines 140W output, thermal control, and a premium matte shell for travel kits and daily carry.",
            BrandId: null,
            BrandName: "Astra Flux",
            DefaultCategoryId: null,
            CategorySlug: "accessories",
            CategoryName: "Accessories",
            Status: "Active",
            ProductType: "Simple",
            SeoTitle: null,
            SeoDescription: null,
            CanonicalUrl: null,
            IsFeatured: false,
            PublishedAtUtc: publishedAtUtc,
            Currency: "EUR",
            Amount: 139.00m,
            Sku: "AF-POWER-001",
            CompareAtAmount: null,
            WeightKg: null,
            ImageUrl: "/images/og-default.jpg",
            IsInStock: true,
            Categories: [],
            Options: [],
            Variants: [],
            Attributes:
            [
                CreateAttribute("Power", "Total output", "140W", 0),
                CreateAttribute("Ports", "Configuration", "3x USB-C, 1x USB-A", 1),
                CreateAttribute("Travel", "Weight", "285 g", 2),
            ],
            Images: [],
            Relations: []));

    private static IReadOnlyCollection<CatalogSeedProductDefinition> MinimalDefinitions =>
    [
        MechanicalKeyboardDefinition,
        WorkspaceDockDefinition,
        AdaptiveHeadphonesDefinition,
    ];

    private static IReadOnlyCollection<CatalogSeedProductDefinition> AllDefinitions =>
    [
        MechanicalKeyboardDefinition,
        MotionDeskMatDefinition,
        WorkspaceMonitorLightDefinition,
        WorkspaceDockDefinition,
        StudioSpeakerPairDefinition,
        AdaptiveHeadphonesDefinition,
        PulseSmartBandDefinition,
        TravelPowerModuleDefinition,
    ];

    private static IReadOnlyCollection<CatalogSeedProductDefinition> GetDefinitions(string seedMode)
    {
        return seedMode switch
        {
            ReleaseSeedModes.Minimal or ReleaseSeedModes.Test => MinimalDefinitions,
            ReleaseSeedModes.Demo => AllDefinitions,
            _ => [],
        };
    }

    public async Task SeedAsync(string seedMode, CancellationToken cancellationToken)
    {
        var normalizedSeedMode = ReleaseSeedModes.Normalize(seedMode);
        var definitions = GetDefinitions(normalizedSeedMode);
        if (definitions.Count == 0)
        {
            logger.LogInformation("Catalog release seeding skipped. SeedMode={SeedMode}", normalizedSeedMode);
            return;
        }

        var publishedAtUtc = DateTime.UtcNow;
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await productRepository.SlugExistsAsync(definition.ExpectedSlug, null, cancellationToken))
            {
                skippedCount++;
                continue;
            }

            var result = await sender.Send(definition.BuildCommand(publishedAtUtc), cancellationToken);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Unable to seed catalog product '{definition.ExpectedSlug}'. Code: {result.Error.Code}. Message: {result.Error.Message}");
            }

            createdCount++;
        }

        logger.LogInformation(
            "Catalog release seeding finished. SeedMode={SeedMode} Created={CreatedCount} Skipped={SkippedCount}",
            normalizedSeedMode,
            createdCount,
            skippedCount);
    }

    private static CreateProductAttributeModel CreateAttribute(
        string groupName,
        string name,
        string value,
        int position,
        bool isFilterable = true)
    {
        return new CreateProductAttributeModel(
            Id: null,
            GroupName: groupName,
            Name: name,
            Value: value,
            Position: position,
            IsFilterable: isFilterable);
    }

    private sealed record CatalogSeedProductDefinition(
        string ExpectedSlug,
        Func<DateTime, CreateProductCommand> BuildCommand);
}
