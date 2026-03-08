param(
    [string]$AppHostBaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Test-ProductExists {
    param(
        [string]$Slug
    )

    $response = Invoke-WebRequest `
        -Uri "$AppHostBaseUrl/api/v1/catalog/products/by-slug/$Slug" `
        -Method Get `
        -UseBasicParsing `
        -SkipHttpErrorCheck

    return [int]$response.StatusCode -eq 200
}

function New-Attribute {
    param(
        [string]$GroupName,
        [string]$Name,
        [string]$Value,
        [int]$Position,
        [bool]$IsFilterable = $true
    )

    return @{
        GroupName = $GroupName
        Name = $Name
        Value = $Value
        Position = $Position
        IsFilterable = $IsFilterable
    }
}

$publishedAtUtc = [DateTime]::UtcNow.ToString("O")
$defaultImageUrl = "/images/og-default.jpg"

$finishOptionId = "80d25691-43d2-4f41-8a47-f804f34fd4d0"
$graphiteValueId = "ebde404d-b0da-4c6d-8e47-fb0ba4be1908"
$silverValueId = "f9c2f479-3614-4b1f-91db-5ea0f84b22ef"

$products = @(
    @{
        Slug = "mechanical-keyboard"
        Payload = @{
            Name = "Mechanical Keyboard"
            ShortDescription = "Low-profile mechanical input tuned for focused desk setups."
            Description = "Precision aluminum keyboard with damped switches, warm backlight control, and a calmer industrial finish for modern workstations."
            Currency = "EUR"
            Amount = 249.00
            CompareAtAmount = 279.00
            IsActive = $true
            IsFeatured = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "all-products"
            CategoryName = "All Products"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "Design" -Name "Layout" -Value "ANSI 80%" -Position 0),
                (New-Attribute -GroupName "Materials" -Name "Top case" -Value "CNC aluminum" -Position 1 -IsFilterable $false),
                (New-Attribute -GroupName "Connectivity" -Name "Wireless" -Value "Bluetooth 5.3 / USB-C" -Position 2)
            )
        }
    },
    @{
        Slug = "motion-desk-mat"
        Payload = @{
            Name = "Motion Desk Mat"
            ShortDescription = "Layered felt and silicone surface for quieter premium workstations."
            Description = "A restrained desk surface designed to soften acoustics, organize tools, and keep a premium workspace visually calm."
            Currency = "EUR"
            Amount = 89.00
            IsActive = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "all-products"
            CategoryName = "All Products"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "Surface" -Name "Top layer" -Value "Wool blend felt" -Position 0),
                (New-Attribute -GroupName "Surface" -Name "Base layer" -Value "Anti-slip silicone" -Position 1),
                (New-Attribute -GroupName "Dimensions" -Name "Footprint" -Value "900 x 400 mm" -Position 2)
            )
        }
    },
    @{
        Slug = "workspace-monitor-light"
        Payload = @{
            Name = "Workspace Monitor Light"
            ShortDescription = "Precision task lighting for modern desks and dual-screen environments."
            Description = "An asymmetrical optical bar that improves contrast on the workspace surface while keeping glare away from the display."
            Currency = "EUR"
            Amount = 159.00
            CompareAtAmount = 179.00
            IsActive = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "workspace"
            CategoryName = "Workspace"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "Lighting" -Name "Color temperature" -Value "2700K - 6500K" -Position 0),
                (New-Attribute -GroupName "Lighting" -Name "CRI" -Value "95+" -Position 1),
                (New-Attribute -GroupName "Power" -Name "Input" -Value "USB-C PD" -Position 2)
            )
        }
    },
    @{
        Slug = "workspace-dock"
        Payload = @{
            Name = "Workspace Dock"
            ShortDescription = "A compact desktop hub for power, display, and storage expansion."
            Description = "Machined aluminum dock engineered for premium workspace builds with clean cable routing, silent thermals, and a product-first silhouette."
            Currency = "EUR"
            Amount = 299.00
            IsActive = $true
            IsFeatured = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "workspace"
            CategoryName = "Workspace"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "I/O" -Name "Display output" -Value "2x 4K 60Hz" -Position 0),
                (New-Attribute -GroupName "I/O" -Name "Host connection" -Value "Thunderbolt 4" -Position 1),
                (New-Attribute -GroupName "Power" -Name "Pass-through" -Value "96W" -Position 2)
            )
        }
    },
    @{
        Slug = "studio-speaker-pair"
        Payload = @{
            Name = "Studio Speaker Pair"
            ShortDescription = "Compact stereo monitors with a disciplined acoustic profile."
            Description = "Balanced near-field speakers tuned for smaller rooms, studio desks, and high-clarity everyday listening without visual excess."
            Currency = "EUR"
            Amount = 429.00
            IsActive = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "audio"
            CategoryName = "Audio"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "Acoustics" -Name "Amplification" -Value "2 x 50W Class D" -Position 0),
                (New-Attribute -GroupName "Acoustics" -Name "Frequency range" -Value "48Hz - 22kHz" -Position 1),
                (New-Attribute -GroupName "Inputs" -Name "Wireless" -Value "Wi-Fi / Bluetooth 5.3" -Position 2)
            )
        }
    },
    @{
        Slug = "adaptive-headphones"
        Payload = @{
            Name = "Adaptive Headphones"
            ShortDescription = "Wireless over-ear headphones with cleaner materials and quieter industrial detail."
            Description = "Adaptive noise control, a lightweight frame, and premium materials designed for travel, focus, and long sessions."
            Currency = "EUR"
            Amount = 329.00
            CompareAtAmount = 359.00
            IsActive = $true
            IsFeatured = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "audio"
            CategoryName = "Audio"
            PublishedAtUtc = $publishedAtUtc
            Options = @(
                @{
                    Id = $finishOptionId
                    Name = "Finish"
                    Position = 0
                    Values = @(
                        @{
                            Id = $graphiteValueId
                            Value = "Graphite"
                            Position = 0
                        },
                        @{
                            Id = $silverValueId
                            Value = "Silver"
                            Position = 1
                        }
                    )
                }
            )
            Variants = @(
                @{
                    Sku = "AF-HEAD-GRAPHITE"
                    Name = "Graphite"
                    Slug = "adaptive-headphones-graphite"
                    PriceAmount = 329.00
                    Currency = "EUR"
                    CompareAtPriceAmount = 359.00
                    WeightKg = 0.28
                    IsActive = $true
                    Position = 0
                    OptionAssignments = @(
                        @{
                            ProductOptionId = $finishOptionId
                            ProductOptionValueId = $graphiteValueId
                        }
                    )
                },
                @{
                    Sku = "AF-HEAD-SILVER"
                    Name = "Silver"
                    Slug = "adaptive-headphones-silver"
                    PriceAmount = 339.00
                    Currency = "EUR"
                    CompareAtPriceAmount = 369.00
                    WeightKg = 0.28
                    IsActive = $true
                    Position = 1
                    OptionAssignments = @(
                        @{
                            ProductOptionId = $finishOptionId
                            ProductOptionValueId = $silverValueId
                        }
                    )
                }
            )
            Attributes = @(
                (New-Attribute -GroupName "Audio" -Name "Driver" -Value "40 mm custom dynamic" -Position 0),
                (New-Attribute -GroupName "Audio" -Name "Battery life" -Value "38 hours" -Position 1),
                (New-Attribute -GroupName "Connectivity" -Name "Codec support" -Value "AAC / aptX Adaptive" -Position 2)
            )
        }
    },
    @{
        Slug = "pulse-smart-band"
        Payload = @{
            Name = "Pulse Smart Band"
            ShortDescription = "A slim health and performance wearable with premium restraint."
            Description = "Minimal daily wearable built around recovery tracking, understated materials, and a cleaner interface language."
            Currency = "EUR"
            Amount = 189.00
            IsActive = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "wearables"
            CategoryName = "Wearables"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "Sensors" -Name "Biometrics" -Value "Heart rate / SpO2 / skin temp" -Position 0),
                (New-Attribute -GroupName "Battery" -Name "Runtime" -Value "7 days" -Position 1),
                (New-Attribute -GroupName "Durability" -Name "Water resistance" -Value "5 ATM" -Position 2)
            )
        }
    },
    @{
        Slug = "travel-power-module"
        Payload = @{
            Name = "Travel Power Module"
            ShortDescription = "Multi-port GaN charging block for cleaner travel and desk carry."
            Description = "A compact power module that combines 140W output, thermal control, and a premium matte shell for travel kits and daily carry."
            Currency = "EUR"
            Amount = 139.00
            IsActive = $true
            Brand = "Astra Flux"
            ImageUrl = $defaultImageUrl
            IsInStock = $true
            CategorySlug = "accessories"
            CategoryName = "Accessories"
            PublishedAtUtc = $publishedAtUtc
            Attributes = @(
                (New-Attribute -GroupName "Power" -Name "Total output" -Value "140W" -Position 0),
                (New-Attribute -GroupName "Ports" -Name "Configuration" -Value "3x USB-C, 1x USB-A" -Position 1),
                (New-Attribute -GroupName "Travel" -Name "Weight" -Value "285 g" -Position 2)
            )
        }
    }
)

$created = [System.Collections.Generic.List[string]]::new()
$skipped = [System.Collections.Generic.List[string]]::new()

foreach ($product in $products) {
    if (Test-ProductExists -Slug $product.Slug) {
        $skipped.Add($product.Slug)
        Write-Host "Skipping existing product $($product.Slug)"
        continue
    }

    $body = $product.Payload | ConvertTo-Json -Depth 10
    $response = Invoke-RestMethod `
        -Uri "$AppHostBaseUrl/api/v1/catalog/products" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    $created.Add($product.Slug)
    Write-Host "Created $($product.Slug) -> $($response.id)"
}

$catalog = Invoke-RestMethod -Uri "$AppHostBaseUrl/api/v1/catalog/products" -Method Get

Write-Host ""
Write-Host "Created: $($created.Count)"
Write-Host "Skipped: $($skipped.Count)"
Write-Host "Catalog count: $($catalog.Count)"

if ($created.Count -gt 0) {
    Write-Host "New products:" $created
}
