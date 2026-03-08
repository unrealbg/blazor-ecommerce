# Premium Storefront Notes

## A. Component Split Proposal For Blazor

Split the static prototype into layout, page, and reusable commerce primitives:

- Layout shell: announcement bar, premium header, desktop navigation, mobile menu shell, footer.
- Home composition: hero, trust strip, category tiles, signature products, split editorial block, featured collection, manifesto, testimonials, newsletter.
- Commerce flows: listing template, product details template, mini cart drawer, checkout progress header.
- Reusable UI primitives: section heading, product card, category card, button set, chip or badge, form field, trust item, pricing block.

Recommended rendering approach:

- Keep home, listing, and PDP server-rendered first.
- Use small interactive islands only for the mobile menu, mini cart open state, gallery image switching, and quantity changes.
- Keep campaign copy and editorial zones CMS-backed rather than hard-coded into product DTOs.

## B. Suggested Razor Component Names

Suggested structure aligned to `Storefront.Web/Components`:

- `Components/Layout/AnnouncementBar.razor`
- `Components/Layout/MainHeader.razor`
- `Components/Layout/DesktopNavigation.razor`
- `Components/Layout/MobileMenuShell.razor`
- `Components/Layout/MainFooter.razor`
- `Components/Shared/SectionHeading.razor`
- `Components/Shared/PremiumButton.razor`
- `Components/Shared/ChipBadge.razor`
- `Components/Shared/PremiumInput.razor`
- `Components/Shared/ProductCard.razor`
- `Components/Shared/CategoryCard.razor`
- `Components/Shared/TrustItem.razor`
- `Components/Home/EditorialHero.razor`
- `Components/Home/CuratedCategoryTiles.razor`
- `Components/Home/SignatureProducts.razor`
- `Components/Home/EditorialSplitPromo.razor`
- `Components/Home/FeaturedCollection.razor`
- `Components/Home/BrandManifesto.razor`
- `Components/Home/TestimonialsTrust.razor`
- `Components/Home/NewsletterSignup.razor`
- `Components/Pages/CategoryListing.razor`
- `Components/Pages/ProductDetails.razor`
- `Components/Cart/MiniCartDrawer.razor`
- `Components/Checkout/CheckoutProgressHeader.razor`

## C. Design Token Recommendations

Use tokens once in Tailwind config and mirror them as CSS variables for component-level fallback:

- Color tokens:
  - `luxe.canvas = #f6f1e8`
  - `luxe.surface = #fbf8f3`
  - `luxe.surfaceAlt = #f0e8dc`
  - `luxe.line = #ddd2c5`
  - `luxe.ink = #161311`
  - `luxe.soft = #6d645b`
  - `luxe.accent = #b79a66`
  - `luxe.accentSoft = #efe1c2`
  - `luxe.smoke = #2a2420`
- Typography tokens:
  - `font.display = Cormorant Garamond`
  - `font.body = Manrope`
  - `tracking.eyebrow = 0.32em`
- Radius tokens:
  - `radius.card = 2rem`
  - `radius.panel = 1.5rem`
  - `radius.pill = 9999px`
- Shadow tokens:
  - `shadow.soft = 0 14px 32px rgba(22, 19, 17, 0.05)`
  - `shadow.luxe = 0 24px 60px rgba(22, 19, 17, 0.08)`
- Layout tokens:
  - `maxWidth.shell = 84rem`
  - `space.sectionY = 4rem / 5rem / 6rem by breakpoint`

## D. Notes For Migrating Static Template Into `Storefront.Web` Razor Components

- Keep the prototype in `wwwroot/prototypes` as the visual source of truth while splitting components incrementally.
- Move the repeated utility bundles from the inline Tailwind layer into the app styling system once the production Tailwind pipeline is in place.
- Map the existing `StoreProduct` data into a thin `ProductCardViewModel` so the new card layout is isolated from API churn.
- Do the same for categories, testimonials, hero campaigns, and footer link groups.
- Treat the hero, manifesto, editorial split, and newsletter copy as CMS content rather than catalog content.
- Keep the listing page URL-driven: filters, sort, and pagination should round-trip through query string state.
- Keep product detail page content server-rendered first, then progressively enhance gallery switching and quantity controls.
- Drive the mini cart from `CartState` or a cart summary endpoint, but render the shell so it can load without layout shift.
- Move the checkout progress header into a small component that takes a current-step enum and optional order summary state.
- Once the prototype is componentized, replace prototype image URLs with media service or CMS-managed assets.
