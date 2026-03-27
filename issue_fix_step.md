# Current Fix Step

This file contains only the current round of instructions for the AI agent.

Current goals:

1. Add an in-app way to create new app entries so users do not have to edit `apps_repository.json` manually.
2. Add a small settings gear icon to the left of the theme toggle button and hide the app-creation feature inside Settings.
3. Fix the current bug where the theme toggle icon disappears after switching to dark theme.
4. After making changes, append a summary to `modification_log.md`.

## 1. Add a Settings entry point

### Requirement

The new app-creation workflow must not be exposed directly in the main app list area.

It should be hidden behind a Settings entry point:

- add a small gear icon button
- place it immediately to the left of the theme toggle button
- keep both buttons in the same top-right action area

### UI requirement

Current layout already has a top-right action button inside the presets card area.

For this round:

- keep the theme toggle on the far right
- add the settings gear button immediately to its left
- make both buttons visually consistent
- ensure both buttons remain fully clickable in both light and dark themes

### Recommended implementation

Do not overbuild this.

A first version is acceptable as either:

- a modal settings dialog
- a flyout / content dialog
- a dedicated settings panel opened from the gear button

Prefer the simplest stable implementation that fits the current codebase.

## 2. Add an in-app "Add App" editor

### Problem to solve

Users should not have to open `apps_repository.json`, understand the schema manually, and risk breaking JSON formatting or structure.

The app should provide a guided form-based workflow for creating new app entries safely.

### Scope for this round

The required minimum is:

- a form in Settings that allows creating a new app entry
- validation before saving
- safe write-back to `apps_repository.json`
- UI refresh after save so the newly added app becomes visible without manual JSON editing

This round does not need a full "edit every existing app" administration suite unless it comes naturally from the implementation.

Focus on safe app creation first.

## 3. Required app-creation workflow

### UX goal

The common case should be easy:

- add a normal Winget app
- assign a name
- choose where it should appear
- save safely

Do not force users to deal with every low-level JSON detail up front.

### Recommended form structure

Create a form with two levels:

#### Basic fields

- `Id`
- `Name`
- `Description`
- `IconPath` (optional in first version)
- `SourceType`
- `Source`
- `InstallerType`
- `RequiresAdmin`

#### Placement fields

- allow selecting one or more existing group sections
- or at minimum allow choosing whether the app should only appear in `All Apps`

Important:

- if the app is added only to `Apps`, it will still appear in `All Apps`
- but users should ideally be able to place it into existing groups/sections without editing JSON manually

#### Advanced section

Hide advanced fields behind an expandable "Advanced" section:

- `DeploymentType`
- `InstallArgs`
- `Dependencies`
- `RetryCount`
- `InstallCheck`
- `PortableTargetFolder`
- `PortableEntryRelativePath`
- `PortableExtractSubfolder`

This keeps the common Winget path simple while still supporting future portable scenarios.

## 4. Save and validation requirements

### Current limitation

`ConfigService` currently loads configuration but does not provide a safe save workflow.

This round must add save support.

### Required validation

Before saving:

- `Id` must not be empty
- `Name` must not be empty
- `Source` must not be empty
- `Id` must be unique across `Apps`
- selected enum values must be valid
- if `DeploymentType == Portable`, required portable fields must be validated
- if `InstallCheck.Type == Path`, the path value must not be empty

### Required save behavior

Do not write JSON unsafely.

Use a safe save strategy:

1. load current repository
2. add the new app entry to `Apps`
3. update selected group sections by adding the new `AppId`
4. serialize using the same repository model
5. write to a temporary file first
6. keep a backup of the previous JSON if practical
7. replace the main file only after successful serialization

### Encoding and formatting

Save the repository in UTF-8 and keep JSON formatting readable.

The app should reduce the chance of invalid manual edits, not introduce a new way to corrupt the file.

## 5. Refresh behavior after save

After the user saves a new app:

- the app should appear immediately in the UI
- `All Apps` must show it
- any selected group/section placements must show it too
- the user should not need to restart the application

Recommended approach:

- reload the repository after a successful save
- rebuild the ViewModel collections cleanly

Do not leave the UI in a stale state after save.

## 6. Suggested Settings content for this round

The Settings surface does not need to be large yet.

For this round, it is enough to include:

- `Add App`
- optional placeholder section for future settings

Recommended first layout:

- header: `Settings`
- tab / section 1: `Add App`
- optional future section: `General`

The main goal is to hide the advanced management feature behind Settings while still keeping it usable.

## 7. Fix the dark-theme icon disappearance bug

### Current confirmed behavior

Theme switching appears to work now because the app can enter dark mode.

However, after switching to dark mode:

- the theme toggle button is still present
- but the icon becomes invisible / blank

This is no longer a "button does not work" problem.
It is now an icon rendering / icon-state problem.

### Most likely causes

The strongest current suspects are:

1. The dark-theme icon symbol name is invalid for the installed WPF-UI / Fluent symbol set.
2. The icon foreground is not explicitly bound to a theme-safe brush, so it blends into the button background in dark mode.

Current code uses a string-based symbol binding and sets the dark-theme icon to:

- `WeatherSun24`

That must be verified, not assumed.

An empty icon after theme switch strongly suggests one of these:

- the symbol token does not exist
- the symbol binding fails silently
- the icon color becomes unreadable in dark mode

### Required fix sequence

#### Step 1: Verify the symbol name

Do not assume `WeatherSun24` is valid.

Confirm that the selected icon token actually exists in the installed WPF-UI symbol set.

If it does not exist:

- replace it with a confirmed valid sun/day icon token
- keep the moon icon for light mode if that token is already working

#### Step 2: Make the icon visible in both themes

Set the icon foreground explicitly using a theme-aware dynamic brush.

Do not rely on implicit inheritance if it is unstable.

The icon must remain visible in:

- light theme
- dark theme
- hover state
- pressed state

#### Step 3: Keep icon state aligned with theme state

After toggling:

- light theme should show the moon icon
- dark theme should show the sun icon

The icon must not disappear, lag behind, or show the wrong state.

### Acceptance criteria for the icon bug

1. The theme toggle icon remains visible after switching to dark theme.
2. The icon remains visible after switching back to light theme.
3. The icon state correctly reflects the current theme.
4. The settings gear icon is also clearly visible in both themes.

## 8. Recommended implementation order

1. Add the settings gear button to the left of the theme toggle.
2. Fix the dark-theme icon disappearance bug first, because both top-right icons must remain visible.
3. Add a simple Settings surface.
4. Implement the in-app Add App form.
5. Add safe JSON save support in `ConfigService`.
6. Reload the repository after save and refresh the UI.
7. Append the implementation summary to `modification_log.md`.

## 9. Acceptance criteria for this round

### Settings and app creation

1. A settings gear icon exists to the left of the theme toggle.
2. Clicking the gear opens a Settings UI.
3. The Settings UI includes an `Add App` workflow.
4. Users can add a normal app without editing raw JSON manually.
5. Validation prevents broken or incomplete entries from being saved.
6. The new app appears in the UI immediately after save.

### Theme / icon behavior

1. The theme toggle still works.
2. The theme icon no longer disappears in dark mode.
3. The gear icon and theme icon are both readable in light and dark themes.
