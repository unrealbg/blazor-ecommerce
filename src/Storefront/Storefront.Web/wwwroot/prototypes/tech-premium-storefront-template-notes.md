# Tech Premium Storefront Notes

## A. Blazor component split proposal

Split the prototype into a thin page composition layer and a reusable storefront design system layer.

Recommended page-level composition:

- Home page
  - Announcement strip
  - Sticky header
  - Flagship hero
  - Specs row
  - Featured categories
  - Signature products
  - Split promo
  - Featured collection
  - Editorial ecosystem
  - Review proof
  - Launch signup
  - Footer
- Category listing page
  - Listing header
  - Filter sidebar / mobile filter sheet
  - Product grid
  - Sort control
- Product details page
  - Media gallery
  - Product summary
  - Variant selector
  - Quantity control
  - Sticky purchase actions
  - Technical specs
  - Delivery and warranty details
- Checkout shell
  - Checkout progress header
  - Order summary
  - Address form
  - Payment step
- Shared overlays
  - Mobile navigation shell
  - Mini cart drawer

Recommended reusable design-system components:

- Section header
- Product card
- Category card
- Spec tile row
- CTA button set
- Badge / chip
- Search input
- Select / filter controls
- Newsletter form
- Review quote card

## B. Suggested Razor component names

Use consistent naming between page sections and reusable parts.

Suggested section components:

- `StorefrontAnnouncementStrip.razor`
- `StorefrontHeader.razor`
- `StorefrontMobileMenu.razor`
- `FlagshipHeroSection.razor`
- `SpecsValueRow.razor`
- `FeaturedCategoriesSection.razor`
- `SignatureProductsSection.razor`
- `SplitPromoSection.razor`
- `FeaturedCollectionSection.razor`
- `EcosystemEditorialSection.razor`
- `ReviewProofSection.razor`
- `LaunchSignupSection.razor`
- `StorefrontFooter.razor`
- `ProductListingShell.razor`
- `ProductDetailsShell.razor`
- `CheckoutProgressHeader.razor`
- `MiniCartDrawer.razor`
- `StorefrontStyleGuideSection.razor`

Suggested shared UI components:

- `SectionHeader.razor`
- `ProductCard.razor`
- `CategoryCard.razor`
- `SpecTile.razor`
- `PrimaryButton.razor`
- `SecondaryButton.razor`
- `BadgeChip.razor`
- `StoreInput.razor`
- `StoreSelect.razor`
- `QuantityStepper.razor`

## C. Design token recommendations

Promote the visual system into strongly named design tokens before migrating the template.

Suggested color tokens:

- `--color-bg-page: #f4f7fb`
- `--color-bg-surface: #ffffff`
- `--color-bg-panel: #11161d`
- `--color-bg-panel-soft: #1a212b`
- `--color-border-soft: #cdd6e2`
- `--color-text-primary: #0a0d12`
- `--color-text-secondary: #5d6674`
- `--color-text-inverse: #ffffff`
- `--color-accent-primary: #7a93ff`
- `--color-accent-soft: #dfe6ff`
- `--color-neutral-silver: #aeb8c6`

Suggested typography tokens:

- `--font-family-display: "Sora", sans-serif`
- `--font-family-body: "Manrope", sans-serif`
- `--font-size-hero: clamp(3rem, 7vw, 4.75rem)`
- `--font-size-section-title: clamp(2rem, 4vw, 3.25rem)`
- `--font-size-body: 1rem`
- `--letter-spacing-eyebrow: 0.2em`

Suggested spacing and radius tokens:

- `--space-section-y-mobile: 4rem`
- `--space-section-y-desktop: 6rem`
- `--space-card-padding: 1.5rem`
- `--radius-card: 2rem`
- `--radius-media: 1.75rem`
- `--radius-pill: 9999px`

Suggested shadow and effect tokens:

- `--shadow-surface: 0 24px 70px rgba(10, 13, 18, 0.14)`
- `--shadow-soft: 0 20px 48px rgba(10, 13, 18, 0.12)`
- `--blur-glass: 24px`

## D. Notes for migrating to `Storefront.Web`

Keep the migration incremental. Do not move everything into one large Razor page.

Recommended approach:

1. Create a `DesignSystem` or `Shared/Storefront` folder for the reusable UI pieces first.
2. Extract color, typography, radius, and spacing tokens into the Tailwind config or a shared CSS variables file.
3. Convert the static hero, product card, category card, and section header into isolated Razor components before composing the home page.
4. Feed cards with typed view models instead of anonymous dictionaries or ad hoc `dynamic` payloads.
5. Keep content sections parameterized with small, serializable models so CMS-backed content can replace hardcoded copy later.
6. Move the listing and PDP templates into separate route pages, but keep their sub-parts shared with the home page to avoid duplicate card logic.
7. Implement `MiniCartDrawer` and `StorefrontMobileMenu` as progressively enhanced interactive shells so SSR remains intact without heavy client code.
8. Use image components that preserve aspect ratios to prevent layout shift once real media is introduced.

Suggested folder direction:

- `Components/Storefront/Layout`
- `Components/Storefront/Sections`
- `Components/Storefront/Cards`
- `Components/Storefront/Forms`
- `Components/Storefront/Commerce`
- `Pages/Store`

## E. Homepage animation suggestions that keep SSR safe

Use motion as a progressive enhancement layer after the static SSR markup is already complete.

Safe additions:

- Fade and slight upward reveal on section entry using CSS `@keyframes` plus `prefers-reduced-motion` handling.
- Very subtle hero image scale-in on page load.
- Staggered reveal of spec tiles and product cards via CSS animation delay classes.
- Gentle hover lift and shadow shift on product cards and category cards.
- Soft underline or color transitions on navigation and CTA focus states.
- Lightweight parallax offset on the hero glow or background halo driven by CSS only.
- Drawer slide-in for mini cart and mobile nav using class toggles rather than client-side rendering replacements.

Avoid:

- Layout-shifting entrance animations
- Long autoplay video hero treatments by default
- JS-dependent first paint effects
- Heavy scroll libraries that replace native rendering behavior
