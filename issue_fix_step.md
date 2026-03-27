# Current Fix Step

This file should contain only the current round of instructions for the AI agent.

Current goals:

1. Fix the top-right theme toggle button because it still has no visible effect.
2. Add support for a new "Portable Toolkit" area.
3. After making changes, append a summary to `modification_log.md`.

## 1. Fix the theme toggle button

### Current situation

The previous hypothesis is no longer enough:

- `SystemThemeWatcher.Watch(this)` has already been removed.
- `MainViewModel.ToggleTheme()` still exists.
- The top-right button in `MainWindow.xaml` is still bound to `ToggleThemeCommand`.
- The user tested again and the UI still shows no visible response when clicking the button.

This means the problem is not solved by removing the system theme watcher alone.

### Most likely causes now

Do not guess. Validate these two possibilities first:

1. The button command is not firing at all.
2. The command is firing, but `ApplicationThemeManager.Apply(...)` is not producing an actual visible UI theme change.

The first possibility is currently the most suspicious because the button is still visually layered on top of the custom `TitleBar` instead of being placed in a clearly safe interactive region. That can easily cause:

- the button to look clickable
- but click input to be swallowed by title bar drag behavior
- or unstable hit testing / focus behavior

### Do not repeat these mistakes

- Do not blame `SystemThemeWatcher` again without new evidence.
- Do not only tweak `Apply()` parameters without confirming whether the command is actually firing.
- Do not rely only on visual guessing.

### Required debugging sequence

#### Step 1: Prove whether `ToggleTheme()` is being executed

This is mandatory.

Add minimal diagnostics:

- a debug log, temporary status message, or `Debug.WriteLine` at the start of `ToggleTheme()`
- a listener for `ApplicationThemeManager.Changed`

You must confirm all of the following:

- whether clicking the button enters `ToggleTheme()`
- whether `ApplicationThemeManager.Changed` fires

Interpretation:

- If `ToggleTheme()` is not entered, the problem is the button click chain.
- If `ToggleTheme()` is entered but `Changed` never fires, the problem is the theme manager call.
- If both happen but the UI still does not visibly change, the problem is that the theme resources are not actually affecting the current visual layer.

#### Step 2: Stop placing the theme button directly on top of the TitleBar

The current placement is high risk.

Use one of these approaches:

Option A:

- move the theme button out of the TitleBar overlay
- place it in a dedicated toolbar / header area in the main content region
- verify behavior first, then refine layout later

Option B:

- if the current WPF-UI `TitleBar` supports an official action-content / right-content slot, use that supported API instead of manual overlay

For this round, prefer Option A because stability matters more than keeping the exact current visual placement.

#### Step 3: Make theme switching observable

Right now the user cannot tell whether anything happened.

Add an observable theme state:

- update the icon when the theme changes
  - show moon when the app is in light theme
  - show sun when the app is in dark theme
- if useful, expose the current theme in a tooltip or temporary status text

This is not only a UX improvement. It is also a debugging aid.

#### Step 4: Use the most appropriate theme apply call for the current WPF-UI version

Current code uses:

```csharp
ApplicationThemeManager.Apply(newTheme);
```

Review the available overloads for the installed WPF-UI version and prefer the one that more fully updates:

- application theme
- backdrop behavior
- accent update

Do not randomly change APIs. Use the overload only if it matches the installed version and improves consistency.

#### Step 5: Verify that the current glass-style UI actually reacts to theme changes

The current UI now uses:

- `DynamicResource`
- a custom `GlassCardStyle`
- Mica / glass-like backgrounds

Verify that these elements visibly change between light and dark themes:

- main background
- left group card
- preset card
- bottom status card
- DataGrid background / text / grid lines

If these do not change, then the theme manager may be working partially while the custom visual layer remains effectively static.

### Acceptance criteria

1. Clicking the theme button must definitely execute `ToggleTheme()`.
2. `ApplicationThemeManager.Changed` must become observable during testing.
3. The UI must immediately show a clear light/dark difference.
4. The icon must update with the current theme state.
5. The app must no longer feel like "nothing happened" after clicking.

## 2. Add support for a Portable Toolkit area

### Requirement summary

Add a new area for portable tools that do not require a traditional installation.

This is not just a new visual group. It must also address:

- how portable apps are represented in the data model
- how the install engine handles them
- how the UI distinguishes them from normal installable packages

### Scope for this round

At minimum, the app should support:

- a new top-level group named `Portable Toolkit`
- child sections inside that group
- selectable portable items in the UI
- batch execution that performs download / extract / prepare instead of normal installation

### Reference tool list from the user

#### System / file tools

- Everything
- WizTree
- Process Explorer
- Autoruns
- 7-Zip Portable
- CrystalDiskInfo

#### Documents / PDF / office

- SumatraPDF
- PDF-XChange Editor Portable
- LibreOffice Portable
- Notepad++ Portable

#### Network / transfer / remote

- FileZilla Portable
- PuTTY Portable
- TeamViewer Portable
- Google Chrome Portable

#### Multimedia

- VLC Media Player Portable
- GIMP Portable
- IrfanView Portable

### Do not implement the wrong thing

- Do not only add a new group in the UI.
- Do not push portable tools into the normal Winget installation flow.
- Do not assume the current `InstallEngine` already supports portable tools.

### Why the current engine is not enough

Right now the engine behavior for:

- `SourceType == DirectUrl`
- `InstallerType == Zip`

is effectively "download the file and then try to execute it".

That means:

- a `.zip` portable package will not be extracted
- it will not be organized into a stable tool folder
- the app will not prepare a ready-to-use portable executable

Therefore, supporting portable tools requires a real download/extract workflow, not only new JSON entries.

## 3. Portable data model requirements

### Minimum acceptable model change

Extend the existing app model enough to represent portable packages clearly.

Preferred approach:

- add `PackageKind` or `DeploymentType`
  - `Installed`
  - `Portable`
- add `PortableTargetFolder`
- add `PortableEntryRelativePath`
- add `PortableExtractSubfolder` when needed

If a new enum is not added, there still must be a clear and explicit way to distinguish portable items. Do not infer everything only from `InstallerType == Zip`.

### Install check requirements

Normal apps currently rely mostly on Winget checks.

Portable tools should not use that approach.

Portable items should support at least:

- `InstallCheckType.Path`

Checks should verify:

- whether the target portable executable exists
- or whether the portable tool directory exists

If `InstallCheckType.Path` is not fully implemented yet, implement it in this round.

## 4. Portable execution workflow requirements

### Acceptable first version

When a user selects a portable tool, the batch workflow should:

1. download the package
2. extract it if it is a zip archive
3. store a single-file portable executable in a stable tool folder if extraction is not needed
4. mark the item as ready/successful
5. allow future runs to detect that the tool is already prepared

### Recommended target location

Use one stable strategy for portable tools, for example:

- `%LocalAppData%/InstallToolbox/PortableTools/<ToolName>`

or

- `%UserProfile%/Tools/Portable/<ToolName>`

Pick one consistent location and keep it predictable.

### Status wording

For portable items, avoid misleading "Installing" wording if possible.

Preferred wording includes:

- `Downloading`
- `Extracting`
- `Ready`

If new status enum values are too large a change for this round, at least make the visible status text understandable for portable workflows.

## 5. UI and grouping requirements

### Add a new group

Add:

- `Portable Toolkit`

Recommended sections:

1. System / file tools
2. Documents / PDF / office
3. Network / transfer / remote
4. Multimedia

### Make portable items visually distinguishable

Portable items should not look identical to normal Winget install items.

At minimum, do one of the following:

- show a `Portable` tag
- include `Portable` / `No installation required` in the description
- add a visible type column or badge

### Presets

If time allows, add a preset such as:

- `Common Portable Toolkit`

This is optional for this round. The core requirement is a working portable flow.

## 6. Recommended first portable set for implementation

Do not try to support the full portable list immediately.

For this round, start with 3 to 5 representative items to validate the workflow:

1. CrystalDiskInfo
2. WizTree
3. Process Explorer
4. SumatraPDF
5. PuTTY Portable

Why:

- they cover common real-world use cases
- they help validate zip/exe/path-check scenarios
- they are useful enough to prove the concept

After the workflow is stable, expand the catalog.

## 7. Execution order for this round

1. First make theme switching observable and debuggable.
2. Ensure the button definitely triggers and the UI definitely changes theme.
3. Then add the portable data model and workflow support.
4. Then add the `Portable Toolkit` group and initial sample entries.
5. Finally append this round's summary to `modification_log.md`.

## 8. Acceptance criteria for this round

### Theme

1. Clicking the top-right button must immediately switch the visible app theme.
2. The user must be able to tell the current theme from both the UI and the icon.
3. The app must no longer behave as if the button does nothing.

### Portable toolkit

1. The UI contains a `Portable Toolkit` group.
2. At least 3 to 5 portable tools are present as initial entries.
3. Selecting a portable item triggers a real download/extract/prepare workflow instead of trying to execute a zip file as an installer.
4. Re-running the app must correctly detect already-prepared portable tools and avoid unnecessary repeated work.
