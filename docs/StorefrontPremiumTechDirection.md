# Premium High-Tech Storefront Package

## 1. High-level design direction summary

The storefront direction is now a premium high-tech commerce system built around product-first presentation, SSR-friendly semantic structure, calmer chrome, larger visual blocks, and stronger dark/light rhythm.

The visual identity aims for:

- premium consumer electronics positioning
- oversized but controlled typography
- monochrome-first palettes with restrained blue-silver accents
- fewer, larger, more cinematic content blocks
- higher trust through shipping, warranty, and checkout clarity
- backend-driven commerce surfaces presented with flagship-launch cadence

The live storefront implementation uses this direction in:

- [Home.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Home.razor#L1)
- [Category.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Category.razor#L1)
- [Product.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Product.razor#L1)
- [Cart.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Cart.razor#L1)
- [Checkout.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Checkout.razor#L1)
- [app.css](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/wwwroot/app.css#L2019)

## 2. Full premium storefront HTML + Tailwind template for the homepage

The full Tailwind prototype lives in:

- [tech-premium-storefront-template.html](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/wwwroot/prototypes/tech-premium-storefront-template.html)

This file already contains the homepage shell and design language for:

- top announcement strip
- premium sticky header
- cinematic hero
- featured categories
- signature products
- value/spec row
- split promo
- ecosystem/editorial block
- social proof
- newsletter block
- premium footer

Live Razor homepage implementation:

- [Home.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Home.razor#L14)

## 3. Product listing page template

Premium listing direction is implemented in:

- [Category.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Category.razor#L19)

The listing template includes:

- breadcrumb
- H1 and intro copy
- active filter chips
- sidebar facets
- sort dropdown
- premium product grid
- empty/unavailable state
- pagination

Supporting reusable parts:

- [Breadcrumbs.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/Breadcrumbs.razor)
- [ActiveFilterChips.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/ActiveFilterChips.razor)
- [FacetSidebar.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/FacetSidebar.razor)
- [SortDropdown.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/SortDropdown.razor)
- [ProductGrid.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/ProductGrid.razor)
- [Pagination.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/Pagination.razor)

## 4. Product details page template

Premium PDP direction is implemented in:

- [Product.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Product.razor#L16)

The page structure supports:

- media gallery
- product title and category/brand framing
- rating and review proof
- price and compare-at pricing
- stock and availability states
- SKU/meta area
- variant selectors
- add to cart
- trust/spec tiles
- grouped specifications
- related products
- reviews and Q&A surfaces

Supporting reusable parts:

- [AddToCartButton.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/AddToCartButton.razor)
- [RatingStars.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/RatingStars.razor)
- [ResponsiveImage.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Shared/ResponsiveImage.razor)

## 5. Mini cart drawer template

Reusable mini cart drawer:

- [MiniCartDrawer.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Cart/MiniCartDrawer.razor#L1)

The component already supports:

- title and eyebrow copy
- line items
- thumbnails
- subtotal
- view cart / checkout CTA hierarchy
- optional close action

Expected later data:

- item name
- variant/meta summary
- line amount
- currency
- image
- subtotal

## 6. Checkout progress header template

Reusable checkout progress header:

- [CheckoutProgressHeader.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Checkout/CheckoutProgressHeader.razor#L1)

Live usage:

- [Cart.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Cart.razor#L16)
- [Checkout.razor](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/Components/Pages/Checkout.razor#L17)

It supports:

- current step
- ordered step labels
- active and completed states

## 7. Style guide / design token section

Primary storefront token implementation is currently expressed in:

- [app.css](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/wwwroot/app.css#L2019)
- [tech-premium-storefront-template-notes.md](e:/repos/blazor-ecommerce/src/Storefront/Storefront.Web/wwwroot/prototypes/tech-premium-storefront-template-notes.md)

Recommended token baseline:

- Colors
  - `--color-bg-page: #f4f7fb`
  - `--color-bg-surface: #ffffff`
  - `--color-bg-panel: #11161d`
  - `--color-border-soft: #cdd6e2`
  - `--color-text-primary: #0a0d12`
  - `--color-text-secondary: #5d6674`
  - `--color-accent-primary: #7a93ff`
  - `--color-accent-soft: #dfe6ff`
- Typography
  - `--font-family-display: "Sora", sans-serif`
  - `--font-family-body: "Manrope", sans-serif`
  - `--font-size-hero: clamp(3rem, 7vw, 4.75rem)`
  - `--font-size-section-title: clamp(2rem, 4vw, 3.25rem)`
  - `--letter-spacing-eyebrow: 0.2em`
- Spacing and radius
  - `--space-section-y-mobile: 4rem`
  - `--space-section-y-desktop: 6rem`
  - `--radius-card: 2rem`
  - `--radius-media: 1.75rem`
  - `--radius-pill: 9999px`
- Reusable UI rules
  - `tech-button` for primary, light, and ghost CTAs
  - `tech-chip` for featured/stock/status chips
  - `tech-detail-card` for premium surface blocks
  - `tech-home`, `tech-listing-page`, `tech-product-page`, `tech-cart-page`, `tech-checkout-page` for page shells

## 8. Blazor component split proposal

### Components/Layout

- `AnnouncementBar.razor`
  - responsibility: top trust strip and premium shipping/support messaging
  - later data: announcement message, utility links, locale/currency metadata
- `MainHeader.razor`
  - responsibility: sticky brand header with nav, search, account, cart
  - later data: nav items, search state, cart count, auth state
- `MainFooter.razor`
  - responsibility: structured footer information architecture
  - later data: footer columns, support links, locale/currency, copyright

### Components/Shared

- `SearchBox.razor`
  - responsibility: search entry with SSR-friendly form behavior
  - later data: placeholder, query, action path
- `CategoryCard.razor`
  - responsibility: curated category tile
  - later data: title, slug/href, image, description, CTA label
- `ProductCard.razor`
  - responsibility: reusable premium product tile
  - later data: name, slug, image, price, compare-at price, badge, rating, review count, stock state, brand/category label
- `RatingStars.razor`
  - responsibility: review visualization
  - later data: rating value, optional count
- `ProductGrid.razor`
  - responsibility: listing/card composition and empty state
  - later data: `IReadOnlyCollection<StoreSearchProductItem>` or `IReadOnlyCollection<StoreProduct>`
- `FacetSidebar.razor`
  - responsibility: brand/price/stock filters
  - later data: facet options, applied state, URLs or commands
- `SortDropdown.razor`
  - responsibility: sort UI
  - later data: sort options and selected value

### Components/Catalog

- `Home.razor`
  - responsibility: homepage orchestration
  - later data: featured products, hero product, curated categories, editorial content
- `Category.razor`
  - responsibility: listing page orchestration
  - later data: search response, breadcrumbs, filter state
- `Product.razor`
  - responsibility: PDP orchestration
  - later data: product details, reviews, questions, related products, variant state

### Components/Cart

- `MiniCartDrawer.razor`
  - responsibility: compact order summary shell
  - later data: line items, subtotal, CTA labels, close behavior

### Components/Checkout

- `CheckoutProgressHeader.razor`
  - responsibility: progress visualization across purchase steps
  - later data: current step, step labels

## 9. Suggested file/folder structure for Storefront UI

- `Components/Layout`
- `Components/Shared`
- `Components/Cart`
- `Components/Checkout`
- `Components/Pages`
- `Components/Pages/Admin`
- `wwwroot/app.css`
- `wwwroot/prototypes/tech-premium-storefront-template.html`
- `wwwroot/prototypes/tech-premium-storefront-template-notes.md`

If the storefront grows further, a good next split is:

- `Components/Catalog`
- `Components/Content`
- `Components/Commerce`

## 10. Migration notes for integrating into Storefront.Web

The integration path should stay incremental and SSR-first.

Recommended path:

1. Keep the current Razor page routes as the composition layer.
2. Continue extracting page sections into reusable components only when the data contract is clear.
3. Keep content crawlable with semantic headings and descriptive paragraphs before adding any progressive enhancement.
4. Keep image handling inside `ResponsiveImage` to avoid CLS regressions.
5. Use typed view models instead of anonymous payloads for hero, category, and card surfaces.
6. Treat Tailwind prototype files as design references and `app.css` plus Razor components as the production surface.
7. Keep JS minimal and isolated to true interaction shells only.
8. Preserve existing API-driven flows for catalog, search, cart, reviews, checkout, and customer state.

## 11. Optional future polish ideas

- add SSR-safe CSS stagger reveals for hero stats, categories, and product cards
- add subtle hover lift and shadow movement on premium cards
- add slightly richer image treatment with controlled object-position tuning per category
- add a mobile filter sheet variant to complement the current desktop sidebar rhythm
- add a more editorial homepage collection block fed by CMS-backed campaign content
- add restrained ambient glow treatment in dark sections with `prefers-reduced-motion` support
