# Closet UI Refresh — Design Spec

**Date:** 2026-04-13  
**Branch:** feature/economy-system-docs  
**Status:** Approved

---

## Overview

Refresh the `ClosetScreen` from text-only buttons to a responsive icon-card grid with `Clothing.Icon` thumbnails and hover-reveal names. Simultaneously introduce a shared SCSS design system (`_theme.scss` + `_mixins.scss`) that unifies design tokens across the project and can be adopted by future screens incrementally.

Also fixes a category name mismatch bug causing zero items in Pants, Shirt, and Shoes tabs.

---

## Files Changed

| File | Change |
|---|---|
| `code/UI/_theme.scss` | **New** — design tokens |
| `code/UI/_mixins.scss` | **New** — reusable SCSS mixins |
| `code/UI/ClosetScreen.razor.scss` | **New** — closet component styles |
| `code/UI/ClosetScreen.razor` | **Modified** — grid, thumbnails, category fix |

---

## 1. Design System (`_theme.scss` + `_mixins.scss`)

### `_theme.scss`

SCSS variables covering both design contexts present in the project:

```scss
// Accent (dark game UI — closet, ATM, HUD)
$accent-start:    #667eea;
$accent-end:      #764ba2;

// Surfaces (dark game UI)
$panel-bg-top:    rgba(20, 20, 30, 0.95);
$panel-bg-bottom: rgba(15, 15, 25, 0.98);
$surface:         rgba(255, 255, 255, 0.05);
$surface-hover:   rgba(255, 255, 255, 0.08);

// Semantic colors
$success:         #4CAF50;
$danger:          #f44336;
$text-primary:    rgba(255, 255, 255, 1.0);
$text-secondary:  rgba(255, 255, 255, 0.6);
$text-muted:      rgba(255, 255, 255, 0.35);

// Spacing
$gap-sm:   6px;
$gap-md:   12px;
$gap-lg:   24px;

// Shape
$radius-sm:   8px;
$radius-md:   12px;
$radius-lg:   20px;
$radius-pill: 20px;

// Motion
$transition: 0.15s ease;
```

### `_mixins.scss`

Four mixins covering patterns used across closet, HUD, and future screens. Each mixin imports `_theme` internally so callers only need `@use '_mixins'`.

**`dark-panel`** — glass dark panel background (closet left panel, ATM, interaction panels):
```scss
@mixin dark-panel {
    background: linear-gradient(180deg, $panel-bg-top 0%, $panel-bg-bottom 100%);
}
```

**`pill-tab` / `pill-tab--active`** — category filter tabs:
```scss
@mixin pill-tab {
    padding: 8px 14px;
    border-radius: $radius-pill;
    cursor: pointer;
    font-size: 12px;
    font-weight: 600;
    color: $text-secondary;
    background: $surface-hover;
}
@mixin pill-tab--active {
    color: $text-primary;
    background: linear-gradient(135deg, $accent-start, $accent-end);
}
```

**`icon-card` / `icon-card--selected`** — square thumbnail cards (clothing items, could extend to inventory, app icons):
```scss
@mixin icon-card {
    aspect-ratio: 1;
    border-radius: $radius-sm;
    border: 2px solid transparent;
    background: $surface;
    cursor: pointer;
    overflow: hidden;
    transition: border-color $transition;
    position: relative;

    &:hover {
        border-color: rgba($accent-start, 0.6);
    }
}
@mixin icon-card--selected {
    background: rgba($accent-start, 0.2);
    border-color: $accent-start;
}
```

**`btn-primary` / `btn-secondary`** — action buttons (Apply, Next, Close):
```scss
@mixin btn-primary {
    padding: 16px;
    border-radius: $radius-md;
    cursor: pointer;
    font-size: 16px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 1px;
    color: $text-primary;
    background: linear-gradient(135deg, $accent-start, $accent-end);
    text-align: center;
}
@mixin btn-secondary {
    padding: 16px;
    border-radius: $radius-md;
    cursor: pointer;
    font-size: 16px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 1px;
    color: $text-primary;
    background: rgba(255, 255, 255, 0.1);
    border: 2px solid rgba(255, 255, 255, 0.2);
    text-align: center;
}
```

---

## 2. ClosetScreen Grid Redesign

### Item Grid

Replace the current 2-column text-button layout with a responsive icon-card grid:

```scss
.item-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(72px, 1fr));
    gap: $gap-sm;
    overflow-y: scroll;
    align-content: flex-start;
}
```

- `auto-fill` + `minmax(72px, 1fr)`: columns fill available width, reflow automatically as the panel width changes — no JS required
- `align-content: flex-start`: cards pack to the top, not stretched to fill the container

### Item Card Structure

Each clothing card is a square with the icon filling it and the name revealed on hover:

```scss
.item-card {
    @include icon-card;

    .card-thumb {
        width: 100%;
        height: 100%;
        background-size: cover;
        background-position: center;
    }

    .card-name {
        position: absolute;
        bottom: 0; left: 0; right: 0;
        padding: 14px 4px 4px;
        background: linear-gradient(0deg, rgba(0,0,0,0.85), transparent);
        font-size: 9px;
        font-weight: 600;
        color: $text-primary;
        text-align: center;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        opacity: 0;
        transition: opacity $transition;
    }

    &:hover .card-name { opacity: 1; }

    &.selected {
        @include icon-card--selected;
    }
}
```

**Thumbnail binding:** `Clothing.Icon` is a `Texture`. In the Razor template, bind it as an inline `background-image` style on `.card-thumb`:

```razor
<div class="card-thumb" style="background-image: url(@item.Icon);"></div>
```

If `item.Icon` is null (no asset thumbnail), the card still renders correctly — empty background, name on hover.

### "None" Card

Always shows its label (no hover needed since there's no image to obscure it):

```scss
.item-card--none {
    @include icon-card;
    color: $text-muted;

    .card-name { opacity: 1; background: none; }
}
```

---

## 3. Category Name Bug Fix

The `_categories` array uses display names that don't match S&Box's `Clothing.ClothingCategory` enum, causing `GetItemsForCategory()` to return zero items for three tabs.

**Fix:** rename the three mismatched entries in `_categories` and update `_selectedCategory`'s default:

| Before | After (matches enum) |
|---|---|
| `"Tops"` | `"Shirt"` |
| `"Bottoms"` | `"Pants"` |
| `"Footwear"` | `"Shoes"` |

```csharp
// Before
private readonly string[] _categories = new[]
{
    "Hat", "Hair", "Facial", "Tops", "Gloves", "Bottoms", "Footwear", "Skin"
};

// After
private readonly string[] _categories = new[]
{
    "Hat", "Hair", "Facial", "Shirt", "Gloves", "Pants", "Shoes", "Skin"
};
```

No other logic changes required — `BuildClothingMap`, `GetItemsForCategory`, and `_previewClothing` all key off `item.Category.ToString()`, which now matches `_selectedCategory` correctly end-to-end.

---

## 4. Razor Template Changes

The inline `style="..."` strings on every element in `ClosetScreen.razor` are replaced with CSS classes defined in `ClosetScreen.razor.scss`. The logic (`@code` block) is unchanged except for the `_categories` fix above.

Key structural changes in the template:

- Left panel: `style="position: absolute; ..."` → `class="closet-panel"`
- Category tabs: inline style string per tab → `class="pill-tab @(isActive ? "active" : "")"`
- Item grid container: inline flex → `class="item-grid"`
- Each item: inline style string → `class="item-card @(isEquipped ? "selected" : "")"`  with inner `.card-thumb` and `.card-name` divs
- "None" card: → `class="item-card item-card--none"`
- Action buttons: inline style strings → `class="btn-primary"` / `class="btn-secondary"`

---

## Out of Scope

- Refactoring other screens (ShopScreen, AtmScreen, etc.) to use the new design system — they can adopt `_theme` and `_mixins` incrementally in future passes
- Live 3D ScenePanel thumbnails — using `Clothing.Icon` (static pre-rendered texture)
- Any changes to the `@code` block logic beyond the category rename
