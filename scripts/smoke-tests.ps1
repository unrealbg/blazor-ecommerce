param(
    [string]$AppHostBaseUrl = "http://localhost:8080",
    [string]$StorefrontBaseUrl = "http://localhost:5100",
    [string]$ProductSlug = "mechanical-keyboard",
    [switch]$SkipAdminPath,
    [switch]$SkipCartPath
)

$ErrorActionPreference = "Stop"

function Invoke-SmokeCheck {
    param(
        [string]$Name,
        [string]$Url,
        [int[]]$AllowedStatusCodes = @(200)
    )

    Write-Host "Checking $Name -> $Url"
    $response = Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 5 -SkipHttpErrorCheck

    if ($AllowedStatusCodes -notcontains [int]$response.StatusCode) {
        throw "Smoke check failed for $Name. Expected one of [$($AllowedStatusCodes -join ', ')], got $([int]$response.StatusCode)."
    }
}

Invoke-SmokeCheck -Name "apphost version" -Url "$AppHostBaseUrl/version"
Invoke-SmokeCheck -Name "health live" -Url "$AppHostBaseUrl/health/live"
Invoke-SmokeCheck -Name "health ready" -Url "$AppHostBaseUrl/health/ready" -AllowedStatusCodes @(200, 503)
Invoke-SmokeCheck -Name "storefront home" -Url "$StorefrontBaseUrl/"
Invoke-SmokeCheck -Name "storefront product" -Url "$StorefrontBaseUrl/product/$ProductSlug"
Invoke-SmokeCheck -Name "storefront sitemap" -Url "$StorefrontBaseUrl/sitemap.xml"
Invoke-SmokeCheck -Name "storefront login page" -Url "$StorefrontBaseUrl/account/login"

if (-not $SkipAdminPath) {
    Invoke-SmokeCheck -Name "storefront admin login shell" -Url "$StorefrontBaseUrl/admin/reviews"
}

if (-not $SkipCartPath) {
    Invoke-SmokeCheck -Name "storefront cart shell" -Url "$StorefrontBaseUrl/cart"
}

Write-Host "Smoke checks completed successfully."