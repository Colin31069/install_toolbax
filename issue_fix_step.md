# Current Fix Step

This file contains only the current round of instructions for the AI agent.

## Goal

Redesign the main browsing model so the left sidebar is driven by child categories instead of parent groups, keep `All Apps` and `Portable Toolkit` as special entries, move former parent-group use cases into the top quick preset area, and make the app default to dark theme.

This round is a navigation and information architecture redesign. The current UI still uses parent groups on the left and child sections as tabs on the right. The new UX should make the left side more directly useful and reduce overlap between parent groups and top presets.

## Fixed Product Decisions

- Replace left-side parent-group navigation with child-category navigation.
- Keep `All Apps` as a special always-available entry.
- Visually separate `All Apps` from the normal categories below it with a divider or an equally clear visual separator.
- Keep `Portable Toolkit` as a special preserved entry in the left sidebar.
- Do not keep parent groups such as `New PC Essentials` as left-side navigation entries.
- Move the use case of parent groups like `New PC Essentials`, `Coding`, and similar bundles into the top quick preset area instead.
- Plan for many presets in the top area. The preset bar must support hiding overflow and user-controlled ordering.
- Users must be able to drag and reorder the top presets.
- Default the application theme to dark mode.

## Scope

### In scope

- redesign the left sidebar navigation model
- keep `All Apps` as a separated special entry
- keep `Portable Toolkit` as a separated special entry
- flatten normal browsing into child categories such as:
  - browsers
  - system tools
  - communication / daily
  - other section-level categories already present in the repository
- move former parent-group use cases into the top quick preset area
- add preset overflow handling
- add preset drag-and-drop reordering
- persist the preset order locally
- make dark theme the default theme

### Out of scope

- changing the installer engine
- rewriting the app repository schema from scratch unless required
- removing `All Apps`
- removing `Portable Toolkit`
- building a full preset editor in this round

## Required Navigation Model

### 1. Left sidebar becomes section-driven

The left sidebar must stop showing parent groups such as:

- `New PC Essentials`
- `Coding`
- `Gaming`

Instead, the left sidebar must show child categories directly.

Examples of the intended left-side category style:

- `Browsers`
- `System Tools`
- `Communication / Daily`
- other section-level categories derived from the current repository

### 2. Keep `All Apps` as a special top entry

`All Apps` must remain available.

Required behavior:

- place `All Apps` at the top of the left sidebar
- keep it visually separated from the normal category list below it
- use a horizontal divider, spacing block, or another equally clear separator

The user must immediately understand that `All Apps` is a global view and not just another category.

### 3. Keep `Portable Toolkit` as a special entry

`Portable Toolkit` must remain available in the left sidebar.

It must not be removed or converted into a normal quick preset.

`Portable Toolkit` may remain visually separated from the standard flattened categories if that produces a clearer UX.

### 4. Remove redundant right-side child tabs for normal categories

Once the left sidebar already shows child categories, the extra right-side tab layer for normal categories becomes redundant.

Required behavior:

- when the selected left-side item is a normal child category, show its app list directly
- do not require the user to click an additional child tab for the normal browsing flow

### 5. Preserve a special internal structure for `Portable Toolkit` if needed

`Portable Toolkit` may keep its own internal sub-sections if that still improves usability.

This is the one special case where an extra inner tab or segmented view is acceptable.

## Data Strategy

### 6. Derive sidebar categories from existing section data

Prefer deriving the new left-side categories from existing section-level repository data instead of inventing a brand-new schema immediately.

Recommended approach:

- treat `All Apps` as a special synthetic entry
- treat normal non-portable sections as sidebar categories
- treat `Portable Toolkit` as a special synthetic entry backed by its current group

The implementation should minimize repository migration risk for this round.

### 7. Treat former parent groups as quick presets

Former parent groups such as `New PC Essentials` should no longer be left-side navigation entries.

Instead, they should be represented in the top quick preset area because their purpose overlaps heavily with presets.

The agent may implement this in one of these ways, whichever is simpler and safer in the current codebase:

- migrate those group bundles into real presets
- or derive quick presets from those groups at runtime

The important product rule is:

- they appear in the top quick preset area
- they do not appear as primary left-side categories anymore

## Quick Preset Area Requirements

### 8. Support many presets gracefully

The top quick preset area must be designed for growth.

Do not assume the preset count will remain small.

Required behavior:

- the first row of presets must remain readable and uncluttered
- extra presets must be hideable when there are too many
- the UI must provide an explicit expand or show-more mechanism

Recommended behavior:

- show a single-row preset bar by default
- if presets exceed available space, show a `More` or `Expand` control
- when expanded, show the full preset list in a wrapped or expanded layout

### 9. Preset reordering

Users must be able to drag and reorder quick presets.

Required behavior:

- support drag-and-drop reordering in the quick preset area
- persist the resulting order locally in user settings
- preserve the custom order across restarts

### 10. Preset persistence

Add local persistence for preset UI state as needed.

At minimum, local settings should support:

- custom preset order
- optionally the expanded or collapsed state of the preset area if the implementation uses one

This state belongs in local user settings, not in the shared app catalog repository.

## Theme Requirement

### 11. Default to dark theme

The application theme must default to dark mode.

Required behavior:

- on first run or when no saved theme preference exists, use dark theme
- do not force-reset users who already have an explicit saved theme preference
- keep the existing theme toggle functional

The UX goal is:

- dark mode is the product default
- explicit user choice still wins after it has been saved

## UI and Behavior Rules

### 12. Category selection behavior

When the user clicks a normal child category in the left sidebar:

- show the apps for that category directly
- show a single direct app list view
- do not require another category-selection step on the right

### 13. `All Apps` behavior

When the user clicks `All Apps`:

- show the complete app list
- keep this visually distinct from the normal categories in the left sidebar

### 14. `Portable Toolkit` behavior

When the user clicks `Portable Toolkit`:

- show the portable-specific view
- retain its internal structure if needed for usability

### 15. Preset application behavior

Quick presets must continue selecting app bundles in the main list as they do today.

Moving former parent groups into the preset area must not remove the ability to apply those bundles quickly.

## Validation and Migration Rules

- do not break existing `apps_repository.json` loading unless migration is absolutely required
- do not remove `All Apps`
- do not remove `Portable Toolkit`
- do not leave both old parent-group navigation and new child-category navigation active at the same time
- do not require the user to understand repository internals to use the new navigation model

## Recommended Implementation Order

1. Refactor the view-model navigation model so the left sidebar can be driven by child categories instead of parent groups.
2. Keep `All Apps` as a synthetic top entry and add a clear separator below it.
3. Preserve `Portable Toolkit` as a special sidebar entry.
4. Remove redundant right-side child tabs for normal browsing categories.
5. Move former parent-group use cases into the top quick preset area.
6. Add a scalable preset presentation model with overflow hiding and an expand control.
7. Add drag-and-drop preset reordering and persist the custom order locally.
8. Make dark theme the default when no saved preference exists.
9. Verify the final UX against the target layout and interaction model.

## Acceptance Tests

### Sidebar structure

1. The left sidebar shows `All Apps` at the top.
2. `All Apps` is visually separated from the normal categories below it.
3. Normal left-side entries are child categories such as `Browsers`, `System Tools`, and `Communication / Daily`.
4. Former parent groups like `New PC Essentials` no longer appear as left-side primary navigation entries.
5. `Portable Toolkit` still appears in the left sidebar.

### Main content behavior

1. Clicking a normal left-side child category shows its app list directly without an extra normal-category tab step.
2. Clicking `All Apps` shows the complete app list.
3. Clicking `Portable Toolkit` still gives access to its portable-specific content and internal sections if retained.

### Quick preset area

1. Former parent-group bundles such as `New PC Essentials` appear in the top quick preset area.
2. The quick preset area remains usable when the preset count grows.
3. Extra presets can be hidden and then revealed through an explicit UI control.
4. Users can drag and reorder presets.
5. Preset order remains the same after restarting the app.

### Theme

1. On first run with no saved preference, the app starts in dark mode.
2. If the user later chooses light mode, that preference is preserved.
3. The theme toggle still works after the default-dark change.

## Non-goals for this round

- Do not redesign the install workflow itself.
- Do not remove support for the current preset concept.
- Do not build a full preset authoring UI.
- Do not let the preset bar grow without an overflow strategy.
