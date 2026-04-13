# Closet UI Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace ClosetScreen's text-only item buttons with a responsive icon-card grid using `Clothing.Icon` thumbnails and hover-reveal names, fix the category name bug (zero pants/shirts/shoes), and introduce a shared SCSS design-token system.

**Architecture:** Four changes in order — category bug fix first (fastest win, isolated), then the shared SCSS partials (`_theme.scss`, `_mixins.scss`), then `ClosetScreen.razor.scss` that imports them, then the Razor template refactor that swaps inline styles for CSS classes and restructures the grid. No logic changes outside the category rename.

**Tech Stack:** S&Box Razor UI (Blazor-like syntax), Dart Sass / S&Box SCSS compiler, C# gamemode code.

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `code/UI/_theme.scss` | **Create** | Design tokens — colors, spacing, radii, transitions |
| `code/UI/_mixins.scss` | **Create** | Reusable SCSS mixins: `dark-panel`, `pill-tab`, `icon-card`, `btn-*` |
| `code/UI/ClosetScreen.razor.scss` | **Create** | Closet-specific styles, imports `_theme` and `_mixins` |
| `code/UI/ClosetScreen.razor` | **Modify** | Category rename + grid restructure + class-based markup |

---

## Task 1: Fix category name bug

**Files:**
- Modify: `code/UI/ClosetScreen.razor` (lines 161–164, `_categories` array)

This is a one-liner fix isolated to the `_categories` string array. `GetItemsForCategory()` filters by `c.Category.ToString() == _selectedCategory`, and `BuildClothingMap` also keys off `item.Category.ToString()`. The array values must exactly match `Clothing.ClothingCategory` enum names.

- [ ] **Step 1: Update `_categories` array**

In `code/UI/ClosetScreen.razor`, find the `_categories` field (around line 161) and replace:

```csharp
// Before
private readonly string[] _categories = new[]
{
    "Hat", "Hair", "Facial", "Tops", "Gloves", "Bottoms", "Footwear", "Skin"
};
```

With:

```csharp
// After — names match Clothing.ClothingCategory enum exactly
private readonly string[] _categories = new[]
{
    "Hat", "Hair", "Facial", "Shirt", "Gloves", "Pants", "Shoes", "Skin"
};
```

- [ ] **Step 2: Verify in S&Box**

Open the scene in S&Box, interact with the in-world closet entity. Click the **Pants** tab — you should now see trouser items listed. Click **Shirt** and **Shoes** and confirm items appear. Previously all three tabs returned zero items.

- [ ] **Step 3: Commit**

```bash
git add code/UI/ClosetScreen.razor
git commit -m "fix: rename Tops/Bottoms/Footwear categories to match Clothing.ClothingCategory enum"
```

---

## Task 2: Create `_theme.scss`

**Files:**
- Create: `code/UI/_theme.scss`

> **S&Box SCSS import note:** No existing `.scss` partials exist in this project — all SCSS files are `.razor.scss` component pairs. S&Box uses Dart Sass and _should_ support `@import` for underscore-prefixed partials in the same directory. Task 3 verifies this before building further. If imports fail, inline the variables at the top of `ClosetScreen.razor.scss` instead.

- [ ] **Step 1: Create `code/UI/_theme.scss`**

```scss
// =====================================================
// GameRP Design Tokens
// Import: @import '../_theme'; (from a sibling .razor.scss)
// =====================================================

// --- Accent (dark game UI: closet, ATM, HUD) ---
$accent-start:    #667eea;
$accent-end:      #764ba2;
$accent-gradient: linear-gradient(135deg, $accent-start 0%, $accent-end 100%);

// --- Surfaces (dark game UI) ---
$panel-bg-top:    rgba(20, 20, 30, 0.95);
$panel-bg-bottom: rgba(15, 15, 25, 0.98);
$surface:         rgba(255, 255, 255, 0.05);
$surface-hover:   rgba(255, 255, 255, 0.08);

// --- Semantic ---
$success:         #4CAF50;
$danger:          #f44336;

// --- Text ---
$text-primary:    rgba(255, 255, 255, 1.0);
$text-secondary:  rgba(255, 255, 255, 0.6);
$text-muted:      rgba(255, 255, 255, 0.35);

// --- Spacing ---
$gap-sm:   6px;
$gap-md:   12px;
$gap-lg:   24px;
$padding:  24px;

// --- Shape ---
$radius-sm:   8px;
$radius-md:   12px;
$radius-lg:   20px;
$radius-pill: 20px;

// --- Motion ---
$transition: 0.15s ease;
```

- [ ] **Step 2: Commit**

```bash
git add code/UI/_theme.scss
git commit -m "feat: add _theme.scss design tokens"
```

---

## Task 3: Create `_mixins.scss` and verify SCSS imports work

**Files:**
- Create: `code/UI/_mixins.scss`
- Create (temp test): `code/UI/ClosetScreen.razor.scss` — minimal import test, replaced fully in Task 4

- [ ] **Step 1: Create `code/UI/_mixins.scss`**

```scss
@import 'theme';

// =====================================================
// dark-panel
// Usage: @include dark-panel;
// Dark glass panel background for in-world UI screens.
// =====================================================
@mixin dark-panel {
    background: linear-gradient(180deg, $panel-bg-top 0%, $panel-bg-bottom 100%);
}

// =====================================================
// pill-tab / pill-tab--active
// Usage: apply .pill-tab class, add .active for selected state
// =====================================================
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
    background: $accent-gradient;
}

// =====================================================
// icon-card / icon-card--selected
// Usage: square thumbnail cards (clothing, inventory, etc.)
// =====================================================
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
        border-color: rgba(102, 126, 234, 0.6); // $accent-start with opacity
    }
}

@mixin icon-card--selected {
    background: rgba(102, 126, 234, 0.2);
    border-color: $accent-start;
}

// =====================================================
// btn-primary / btn-secondary / btn-danger
// Usage: action buttons (Apply, Next, Close, Confirm)
// =====================================================
@mixin btn-primary {
    flex: 1;
    padding: 16px;
    border-radius: $radius-md;
    cursor: pointer;
    font-size: 16px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 1px;
    color: $text-primary;
    background: $accent-gradient;
    text-align: center;
}

@mixin btn-secondary {
    flex: 1;
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

@mixin btn-danger {
    flex: 1;
    padding: 14px;
    border-radius: $radius-sm;
    cursor: pointer;
    font-size: 14px;
    font-weight: 700;
    color: $text-primary;
    background: linear-gradient(135deg, #f44336 0%, #d32f2f 100%);
    text-align: center;
}

@mixin btn-success {
    flex: 1;
    padding: 16px;
    border-radius: $radius-md;
    cursor: pointer;
    font-size: 16px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 1px;
    color: $text-primary;
    background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%);
    text-align: center;
}
```

- [ ] **Step 2: Create a minimal `ClosetScreen.razor.scss` to test imports**

```scss
@import 'theme';
@import 'mixins';

// import test — remove this comment once verified working
ClosetScreen {
    display: contents;
}
```

- [ ] **Step 3: Load project in S&Box and verify no compile errors**

Open the S&Box editor and load the scene. Check the console for SCSS compile errors. If you see an error like "file not found: theme" or similar:

**Fallback:** Copy the contents of `_theme.scss` variables directly to the top of `ClosetScreen.razor.scss`, and copy the mixin bodies inline below them (no `@import` needed). Then proceed to Task 4.

- [ ] **Step 4: Commit**

```bash
git add code/UI/_mixins.scss code/UI/ClosetScreen.razor.scss
git commit -m "feat: add _mixins.scss and verify SCSS partial imports work"
```

---

## Task 4: Write `ClosetScreen.razor.scss`

**Files:**
- Modify: `code/UI/ClosetScreen.razor.scss` (replace the test stub from Task 3)

- [ ] **Step 1: Replace `ClosetScreen.razor.scss` with full styles**

```scss
@import 'theme';
@import 'mixins';

// =====================================================
// Root
// =====================================================
ClosetScreen {
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    z-index: 100;
    pointer-events: none;
}

// =====================================================
// Left panel
// =====================================================
.closet-panel {
    @include dark-panel;
    position: absolute;
    left: 0;
    top: 0;
    width: 38%;
    height: 100%;
    display: flex;
    flex-direction: column;
    padding: $padding;
    gap: $gap-lg;
    pointer-events: all;
    overflow: hidden;
}

// =====================================================
// Header
// =====================================================
.closet-header {
    display: flex;
    justify-content: space-between;
    align-items: center;

    .closet-title {
        font-size: 28px;
        font-weight: 700;
        color: $text-primary;
    }
}

// =====================================================
// Wizard sections (gender + body sliders)
// =====================================================
.wizard-section {
    display: flex;
    flex-direction: column;
    gap: 6px;

    .section-label {
        font-size: 12px;
        font-weight: 600;
        color: $text-muted;
        text-transform: uppercase;
        letter-spacing: 1px;
    }
}

.gender-buttons {
    display: flex;
    gap: 8px;
}

.gender-btn {
    flex: 1;
    padding: 10px;
    border-radius: $radius-sm;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    color: $text-secondary;
    background: $surface-hover;
    text-align: center;

    &.active {
        font-weight: 600;
        color: $text-primary;
        background: $accent-gradient;
    }
}

.body-sliders {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.slider-row {
    display: flex;
    flex-direction: column;
    gap: 4px;

    .slider-label {
        display: flex;
        justify-content: space-between;
        font-size: 13px;
        color: $text-secondary;
    }

    input[type="range"] {
        width: 100%;
        accent-color: $accent-start;
    }
}

// =====================================================
// Category tabs
// =====================================================
.category-tabs {
    display: flex;
    flex-wrap: wrap;
    gap: $gap-sm;
}

.pill-tab {
    @include pill-tab;

    &.active {
        @include pill-tab--active;
    }
}

// =====================================================
// Item grid
// =====================================================
.item-grid {
    flex: 1;
    overflow: scroll;
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(72px, 1fr));
    gap: $gap-sm;
    align-content: flex-start;
}

// =====================================================
// Item cards
// =====================================================
.item-card {
    @include icon-card;

    &.selected {
        @include icon-card--selected;
    }

    .card-thumb {
        width: 100%;
        height: 100%;
        background-size: cover;
        background-position: center;
        background-repeat: no-repeat;
    }

    .card-name {
        position: absolute;
        bottom: 0;
        left: 0;
        right: 0;
        padding: 14px 4px 4px;
        background: linear-gradient(0deg, rgba(0, 0, 0, 0.85) 0%, transparent 100%);
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

    &:hover .card-name {
        opacity: 1;
    }
}

// "None" card — name always visible, no gradient needed
.item-card--none {
    @include icon-card;

    color: $text-muted;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 18px;

    .card-name {
        opacity: 1;
        background: none;
        color: $text-muted;
        font-size: 9px;
    }

    &.selected {
        @include icon-card--selected;
        color: $text-primary;

        .card-name {
            color: $text-primary;
        }
    }
}

// =====================================================
// Action buttons
// =====================================================
.action-row {
    display: flex;
    gap: $gap-md;
}

.btn-primary   { @include btn-primary; }
.btn-secondary { @include btn-secondary; }
.btn-danger    { @include btn-danger; }
.btn-success   { @include btn-success; }

// =====================================================
// Unsaved changes confirmation
// =====================================================
.confirm-close {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: $gap-md;

    .confirm-message {
        font-size: 14px;
        font-weight: 600;
        color: $danger;
        text-align: center;
    }

    .confirm-buttons {
        display: flex;
        gap: $gap-md;
    }
}
```

- [ ] **Step 2: Verify in S&Box — no compile errors, panel still renders**

Load the scene. The closet should still open and look identical to before (styles not wired up in the Razor yet — that's Task 5). No console errors.

- [ ] **Step 3: Commit**

```bash
git add code/UI/ClosetScreen.razor.scss
git commit -m "feat: add ClosetScreen.razor.scss with design system classes"
```

---

## Task 5: Refactor ClosetScreen.razor — classes and grid

**Files:**
- Modify: `code/UI/ClosetScreen.razor` (template section only — `@code` block unchanged except category fix already done in Task 1)

This task replaces every inline `style="..."` string in the template with the CSS classes defined in Task 4, and restructures the item grid from text buttons to icon cards.

- [ ] **Step 1: Replace the root style and left panel**

Find and replace the `<root>` and left panel `<div>` opening (around lines 11–15):

```razor
@* Before *@
<root style="@RootStyle">
    @if (_isOpen)
    {
        <div style="position: absolute; left: 0; top: 0; width: 38%; height: 100%; background: linear-gradient(180deg, rgba(20,20,30,0.95) 0%, rgba(15,15,25,0.98) 100%); display: flex; flex-direction: column; padding: 24px; gap: 16px; pointer-events: all; overflow: hidden;">
```

```razor
@* After *@
<root>
    @if (_isOpen)
    {
        <div class="closet-panel">
```

Also delete the `RootStyle` property from `@code` (around line 176):
```csharp
// Delete this line entirely:
private string RootStyle => "position: absolute; left: 0; top: 0; width: 100%; height: 100%; z-index: 100; pointer-events: none;";
```

- [ ] **Step 2: Replace the header**

```razor
@* Before *@
<div style="display: flex; justify-content: space-between; align-items: center;">
    <div style="font-size: 28px; font-weight: 700; color: white;">@(_wizardMode ? "Create Your Look" : "Closet")</div>
</div>
```

```razor
@* After *@
<div class="closet-header">
    <div class="closet-title">@(_wizardMode ? "Create Your Look" : "Closet")</div>
</div>
```

- [ ] **Step 3: Replace wizard gender selector**

```razor
@* Before *@
<div style="display: flex; flex-direction: column; gap: 6px;">
    <div style="font-size: 12px; font-weight: 600; color: rgba(255,255,255,0.5); text-transform: uppercase; letter-spacing: 1px;">Gender</div>
    <div style="display: flex; gap: 8px;">
        @foreach (var g in _genderOptions)
        {
            var isActive = g == _selectedGender;
            var gStyle = isActive
                ? "flex: 1; padding: 10px; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 600; color: white; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); text-align: center;"
                : "flex: 1; padding: 10px; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 500; color: rgba(255,255,255,0.6); background-color: rgba(255,255,255,0.08); text-align: center;";
            <div style="@gStyle" @onclick="@(() => SelectGender(g))">@g</div>
        }
    </div>
</div>
```

```razor
@* After *@
<div class="wizard-section">
    <div class="section-label">Gender</div>
    <div class="gender-buttons">
        @foreach (var g in _genderOptions)
        {
            var isActive = g == _selectedGender;
            <div class="gender-btn @(isActive ? "active" : "")" @onclick="@(() => SelectGender(g))">@g</div>
        }
    </div>
</div>
```

- [ ] **Step 4: Replace wizard body sliders**

```razor
@* Before *@
<div style="display: flex; flex-direction: column; gap: 10px;">
    <div style="font-size: 12px; font-weight: 600; color: rgba(255,255,255,0.5); text-transform: uppercase; letter-spacing: 1px;">Body</div>

    <div style="display: flex; flex-direction: column; gap: 4px;">
        <div style="display: flex; justify-content: space-between; font-size: 13px; color: rgba(255,255,255,0.7);">
            <span>Skin Tone</span>
            <span>@(_previewSkinTone.ToString("F2"))</span>
        </div>
        <input type="range" min="0" max="1" step="0.01" value="@_previewSkinTone"
               @oninput="@(e => OnSkinToneChanged(e))"
               style="width: 100%; accent-color: #667eea;" />
    </div>

    <div style="display: flex; flex-direction: column; gap: 4px;">
        <div style="display: flex; justify-content: space-between; font-size: 13px; color: rgba(255,255,255,0.7);">
            <span>Height</span>
            <span>@(_previewHeight.ToString("F2"))</span>
        </div>
        <input type="range" min="0" max="1" step="0.01" value="@_previewHeight"
               @oninput="@(e => OnHeightChanged(e))"
               style="width: 100%; accent-color: #667eea;" />
    </div>

    <div style="display: flex; flex-direction: column; gap: 4px;">
        <div style="display: flex; justify-content: space-between; font-size: 13px; color: rgba(255,255,255,0.7);">
            <span>Age</span>
            <span>@(_previewAge.ToString("F2"))</span>
        </div>
        <input type="range" min="0" max="1" step="0.01" value="@_previewAge"
               @oninput="@(e => OnAgeChanged(e))"
               style="width: 100%; accent-color: #667eea;" />
    </div>
</div>
```

```razor
@* After *@
<div class="wizard-section">
    <div class="section-label">Body</div>
    <div class="body-sliders">
        <div class="slider-row">
            <div class="slider-label"><span>Skin Tone</span><span>@(_previewSkinTone.ToString("F2"))</span></div>
            <input type="range" min="0" max="1" step="0.01" value="@_previewSkinTone" @oninput="@(e => OnSkinToneChanged(e))" />
        </div>
        <div class="slider-row">
            <div class="slider-label"><span>Height</span><span>@(_previewHeight.ToString("F2"))</span></div>
            <input type="range" min="0" max="1" step="0.01" value="@_previewHeight" @oninput="@(e => OnHeightChanged(e))" />
        </div>
        <div class="slider-row">
            <div class="slider-label"><span>Age</span><span>@(_previewAge.ToString("F2"))</span></div>
            <input type="range" min="0" max="1" step="0.01" value="@_previewAge" @oninput="@(e => OnAgeChanged(e))" />
        </div>
    </div>
</div>
```

- [ ] **Step 5: Replace category tabs**

```razor
@* Before *@
<div style="display: flex; flex-wrap: wrap; gap: 6px;">
    @foreach (var category in _categories)
    {
        var isActive = category == _selectedCategory;
        var tabStyle = isActive
            ? "padding: 8px 14px; border-radius: 20px; cursor: pointer; font-size: 12px; font-weight: 600; color: white; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);"
            : "padding: 8px 14px; border-radius: 20px; cursor: pointer; font-size: 12px; font-weight: 600; color: rgba(255,255,255,0.6); background-color: rgba(255,255,255,0.08);";
        <div style="@tabStyle" @onclick="@(() => SelectCategory(category))">@category</div>
    }
</div>
```

```razor
@* After *@
<div class="category-tabs">
    @foreach (var category in _categories)
    {
        var isActive = category == _selectedCategory;
        <div class="pill-tab @(isActive ? "active" : "")" @onclick="@(() => SelectCategory(category))">@category</div>
    }
</div>
```

- [ ] **Step 6: Replace item grid — the main structural change**

Replace the entire item grid section (the `<div style="flex: 1; overflow: scroll; ...">` block containing "None" and the item foreach):

```razor
@* Before *@
<div style="flex: 1; overflow: scroll; display: flex; flex-wrap: wrap; gap: 8px; align-content: flex-start;">
    @{
        var noneActive = !_previewClothing.ContainsKey(_selectedCategory);
        var noneStyle = noneActive
            ? "width: calc(50% - 4px); padding: 12px; border-radius: 10px; cursor: pointer; font-size: 13px; font-weight: 600; color: white; background-color: rgba(102,126,234,0.3); border: 2px solid #667eea; text-align: center;"
            : "width: calc(50% - 4px); padding: 12px; border-radius: 10px; cursor: pointer; font-size: 13px; font-weight: 500; color: rgba(255,255,255,0.7); background-color: rgba(255,255,255,0.05); border: 2px solid transparent; text-align: center;";
    }
    <div style="@noneStyle" @onclick="@(() => PreviewItem(null))">None</div>

    @foreach (var item in GetItemsForCategory())
    {
        var isEquipped = _previewClothing.ContainsKey(_selectedCategory) && _previewClothing[_selectedCategory] == item.ResourcePath;
        var itemStyle = isEquipped
            ? "width: calc(50% - 4px); padding: 12px; border-radius: 10px; cursor: pointer; font-size: 13px; font-weight: 600; color: white; background-color: rgba(102,126,234,0.3); border: 2px solid #667eea; text-align: center; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;"
            : "width: calc(50% - 4px); padding: 12px; border-radius: 10px; cursor: pointer; font-size: 13px; font-weight: 500; color: rgba(255,255,255,0.7); background-color: rgba(255,255,255,0.05); border: 2px solid transparent; text-align: center; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;";
        <div style="@itemStyle" @onclick="@(() => PreviewItem(item.ResourcePath))">@item.Title</div>
    }
</div>
```

```razor
@* After *@
<div class="item-grid">
    @{
        var noneSelected = !_previewClothing.ContainsKey(_selectedCategory);
    }
    <div class="item-card--none @(noneSelected ? "selected" : "")" @onclick="@(() => PreviewItem(null))">
        ✕
        <div class="card-name">None</div>
    </div>

    @foreach (var item in GetItemsForCategory())
    {
        var isEquipped = _previewClothing.ContainsKey(_selectedCategory) && _previewClothing[_selectedCategory] == item.ResourcePath;
        <div class="item-card @(isEquipped ? "selected" : "")" @onclick="@(() => PreviewItem(item.ResourcePath))">
            @if (item.Icon != null)
            {
                <div class="card-thumb" style="background-image: url(@item.Icon.ResourcePath);"></div>
            }
            <div class="card-name">@item.Title</div>
        </div>
    }
</div>
```

- [ ] **Step 7: Replace action buttons**

```razor
@* Before *@
<div style="display: flex; gap: 12px;">
    @if (_wizardMode)
    {
        <div style="flex: 1; padding: 16px; border-radius: 12px; cursor: pointer; font-size: 16px; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; color: white; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); text-align: center;" @onclick="@OnNextClicked">Next</div>
    }
    else if (!_showConfirmClose)
    {
        <div style="flex: 1; padding: 16px; border-radius: 12px; cursor: pointer; font-size: 16px; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; color: white; background-color: rgba(255,255,255,0.1); border: 2px solid rgba(255,255,255,0.2); text-align: center;" @onclick="@OnCloseClicked">Close</div>
        @if (HasUnsavedChanges)
        {
            <div style="flex: 1; padding: 16px; border-radius: 12px; cursor: pointer; font-size: 16px; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; color: white; background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); text-align: center;" @onclick="@ApplyChanges">Apply</div>
        }
    }
    else
    {
        <div style="flex: 1; display: flex; flex-direction: column; gap: 12px;">
            <div style="font-size: 14px; font-weight: 600; color: #f44336; text-align: center;">Your changes will not be saved. Are you sure?</div>
            <div style="display: flex; gap: 12px;">
                <div style="flex: 1; padding: 14px; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 700; color: white; background-color: rgba(255,255,255,0.1); border: 2px solid rgba(255,255,255,0.2); text-align: center;" @onclick="@CancelClose">No</div>
                <div style="flex: 1; padding: 14px; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 700; color: white; background: linear-gradient(135deg, #f44336 0%, #d32f2f 100%); text-align: center;" @onclick="@ConfirmClose">Yes</div>
            </div>
        </div>
    }
</div>
```

```razor
@* After *@
<div class="action-row">
    @if (_wizardMode)
    {
        <div class="btn-primary" @onclick="@OnNextClicked">Next</div>
    }
    else if (!_showConfirmClose)
    {
        <div class="btn-secondary" @onclick="@OnCloseClicked">Close</div>
        @if (HasUnsavedChanges)
        {
            <div class="btn-success" @onclick="@ApplyChanges">Apply</div>
        }
    }
    else
    {
        <div class="confirm-close">
            <div class="confirm-message">Your changes will not be saved. Are you sure?</div>
            <div class="confirm-buttons">
                <div class="btn-secondary" @onclick="@CancelClose">No</div>
                <div class="btn-danger" @onclick="@ConfirmClose">Yes</div>
            </div>
        </div>
    }
</div>
```

- [ ] **Step 8: Verify in S&Box — full visual check**

Open the scene and interact with the closet. Confirm:
- [ ] Panel renders correctly (dark glass background, correct layout)
- [ ] Category tabs are pill-shaped; active tab has purple gradient
- [ ] Pants, Shirt, Shoes tabs show items (from Task 1 fix)
- [ ] Item grid fills width responsively — more columns on wider panel
- [ ] Each item card is square with the icon thumbnail filling it
- [ ] Hovering a card fades in the item name
- [ ] "None" card always shows its label
- [ ] Selecting an item highlights its card with purple border
- [ ] Wizard mode: gender buttons, body sliders, Next button all render
- [ ] Normal mode: Close / Apply buttons render; unsaved-changes confirmation renders

- [ ] **Step 9: Commit**

```bash
git add code/UI/ClosetScreen.razor
git commit -m "feat: refactor ClosetScreen to CSS classes and responsive icon-card grid"
```

---

## Self-Review

**Spec coverage check:**
- ✅ Responsive auto-fill grid — Task 5 Step 6 (`grid-template-columns: repeat(auto-fill, minmax(72px, 1fr))`)
- ✅ Square cards — `aspect-ratio: 1` via `icon-card` mixin in Task 4
- ✅ `Clothing.Icon` thumbnail — Task 5 Step 6, `background-image: url(@item.Icon.ResourcePath)`
- ✅ Name on hover — `.card-name` with `opacity: 0` / `:hover opacity: 1` in Task 4
- ✅ "None" card always shows label — `.item-card--none .card-name { opacity: 1; }` in Task 4
- ✅ Category bug fix — Task 1
- ✅ `_theme.scss` tokens — Task 2
- ✅ `_mixins.scss` — Task 3
- ✅ `ClosetScreen.razor.scss` — Task 4
- ✅ Inline styles → CSS classes — Task 5
- ✅ SCSS import risk documented — Task 3 Step 3 fallback

**Placeholder scan:** None found.

**Type consistency:** `item.Icon.ResourcePath` used in Task 5 Step 6, consistent with `Sandbox.Clothing.Icon` returning a `Texture` which has `.ResourcePath`. `_categories` array renamed in Task 1, `GetItemsForCategory()` and `BuildClothingMap` both use `item.Category.ToString()` — no further changes needed. All CSS class names defined in Task 4 match their usage in Task 5.
