# Current Fix Step

This file contains only the current round of instructions for the AI agent.

## Goal

Add in-app management for apps that already exist in the repository.

Users can already create new app entries, but they still have to open `apps_repository.json` manually if they want to:

- change an existing app
- replace or add an icon for an existing app
- remove an app that is no longer needed

This round must remove that dependency on manual JSON editing.

## Fixed Product Decisions

- Do not require users to edit `apps_repository.json` manually for normal maintenance.
- Keep app management inside Settings.
- Add UI support for editing existing apps.
- Add UI support for deleting existing apps.
- Existing app icon changes must use the same managed icon approach as the Add App flow.
- Deleting an app must also clean up references to that app in groups, sections, and presets.
- Prefer a safe, explicit workflow over a fast but risky one.

## Scope

### In scope

- browse and select an existing app
- load the selected app into an editable form
- modify existing app fields
- change or reset the icon of an existing app
- save changes back to `apps_repository.json`
- delete an existing app with confirmation
- remove all references to the deleted app from the repository structure
- refresh the UI after edit or delete

### Out of scope

- bulk edit
- multi-select delete
- undo history
- automatic icon scraping
- a separate admin window outside Settings

## Required UX

### 1. Add an Existing Apps management surface in Settings

Settings must include a dedicated management area for existing apps.

A simple recommended layout is:

- `Add App`
- `Manage Existing Apps`
- `General`

The new management area must include:

- a list, combo box, or searchable selector for existing apps
- a form that loads the selected app's current values
- a save action for updates
- a delete action for removal

The user must not need to open raw JSON for normal edit or delete tasks.

### 2. Reuse the same field model as much as possible

The edit form should reuse the same field structure already used for Add App wherever practical.

At minimum, the existing app editor must support:

- `Id`
- `Name`
- `Description`
- `IconPath` through managed icon controls
- `SourceType`
- `Source`
- `InstallerType`
- `RequiresAdmin`
- `DeploymentType`
- `InstallArgs`

If the current codebase already has additional supported fields in the model, the edit flow should preserve them and avoid silently dropping them.

### 3. Existing app icon management

The edit flow must support icon updates for apps that are already in the repository.

Required behavior:

- show the current icon preview
- allow browsing for a replacement icon
- allow resetting the icon to the default fallback icon
- use the same accepted file types as the Add App flow:
  - `.png`
  - `.jpg`
  - `.jpeg`
  - `.ico`
- use the same managed icon storage rules as Add App:
  - copy user-selected icons into `UserAssets/Icons/`
  - persist only the managed relative path
  - do not persist the original external absolute path

If the edited app already uses a managed icon and the user replaces it, the managed icon file should be overwritten or replaced safely.

## Required Edit Behavior

### 4. Loading an existing app

When the user selects an existing app:

- load the app's current values into the edit form
- populate icon preview from the current persisted icon path
- load the app's current group and section memberships
- keep the original app id available internally for reference updates

The UI must make it clear that the user is editing an existing record, not creating a new one.

### 5. Saving edits

The save flow for existing apps must update the repository entry instead of appending a new one.

Required behavior:

1. load the current repository
2. identify the selected existing app by its original id
3. validate edited values
4. update the matching app record
5. update group/section references if needed
6. update preset references if needed
7. save the repository safely
8. refresh the UI so changes appear immediately

### 6. Editing the app id

Editing `Id` is allowed in this round.

If the app id changes:

- the new id must still be unique across all apps
- every matching id reference in every group section must be updated
- every matching id reference in every preset must be updated
- if the app has a managed custom icon named from the old id, the managed icon file should be renamed to match the new id when practical
- if rename is not practical because of the current implementation path, the save flow must still leave the app with a valid icon path and must not break the icon reference

This is a required consistency rule. No stale old ids may remain in the repository after a successful save.

### 7. Group and section membership editing

The edit workflow must allow updating where an existing app appears.

Required behavior:

- show current group/section memberships
- allow adding memberships
- allow removing memberships
- leave `All Apps` implicit as it is today

Saving must produce the final membership state exactly as selected in the form.

### 8. Preserve non-edited data safely

If a field is not exposed in the editor UI yet, the save flow must not accidentally erase or reinitialize it.

The agent must be careful not to overwrite supported repository data with defaults unless the user explicitly changed that field.

## Required Delete Behavior

### 9. Delete an existing app safely

The management UI must provide a delete action for the currently selected existing app.

Delete must require explicit confirmation.

The confirmation should clearly warn that the action will:

- remove the app from the main app catalog
- remove the app from all group sections
- remove the app from all presets

### 10. Delete cleanup rules

When an app is deleted:

- remove the app record from `Apps`
- remove its id from every `Group -> Section -> AppIds`
- remove its id from every `Preset -> AppIds`

If the app uses a managed custom icon under `UserAssets/Icons/`:

- delete that managed icon file if it belongs only to the deleted app
- do not delete built-in assets under `/Assets/Icons/`

The delete flow must not leave dangling references in the repository.

## Validation Rules

- an edited app cannot be saved with an empty `Id`
- an edited app cannot be saved with an empty `Name`
- an edited app cannot be saved with an empty `Source`
- the edited `Id` must be unique across all apps except the currently edited record
- if a custom icon is selected, only allowed file extensions may be accepted
- if icon copy or replacement fails, do not persist a broken icon path
- deleting an app must require user confirmation

## Data and Save Safety

- keep using `apps_repository.json` as the single source of truth for app catalog data
- do not introduce a second catalog file
- save in UTF-8
- keep readable JSON formatting
- create or keep a backup before replacing the main repository file if the current implementation already supports that pattern
- do not partially save only part of the repository structure

The repository update must be treated as one logical operation.

## UI Refresh Requirements

After a successful edit:

- the updated app must appear immediately in the main UI
- the updated icon must appear immediately if it changed
- updated group/section placement must appear immediately
- updated preset targeting must be reflected immediately

After a successful delete:

- the removed app must disappear immediately from the main UI
- it must disappear from all affected sections
- it must no longer be targeted by presets

The user must not need to restart the application.

## Recommended Implementation Order

1. Add the existing app selector and edit mode UI in Settings.
2. Load selected app data into editable state.
3. Reuse icon preview and managed icon replacement behavior for existing apps.
4. Implement safe update logic for existing apps.
5. Implement id-change propagation across groups, sections, and presets.
6. Implement delete with confirmation and repository cleanup.
7. Reload the repository after save or delete and refresh the main UI.

## Acceptance Tests

### Edit existing app

1. Select an existing app and change its name. Save and verify the new name appears immediately in the main UI.
2. Select an existing app and change its icon. Save and verify the new icon appears immediately and still appears after restarting the app.
3. Select an existing app and reset its icon to the default icon. Save and verify the fallback icon is shown.
4. Select an existing app and change its source-related fields. Save and verify the repository entry updates instead of creating a duplicate.

### Edit existing app id

1. Select an existing app and change its `Id` to a new unique value.
2. Save and verify the app record uses the new id.
3. Verify every matching group/section reference now points to the new id.
4. Verify every matching preset reference now points to the new id.
5. Verify no old id references remain in the repository after save.

### Delete existing app

1. Select an existing app and trigger delete.
2. Confirm the delete action.
3. Verify the app is removed from `Apps`.
4. Verify the app id is removed from all sections.
5. Verify the app id is removed from all presets.
6. Verify the app no longer appears in the UI without restarting.

### Validation and safety

1. Try to save an edit with an empty `Name` and verify validation blocks the save.
2. Try to save an edit with a duplicate `Id` and verify validation blocks the save.
3. Try to delete an app and cancel the confirmation dialog; verify nothing changes.
4. Verify built-in icon assets are never deleted during app deletion.

## Non-goals for this round

- Do not build a full power-user admin console.
- Do not add batch operations.
- Do not introduce direct JSON editing into the UI.
- Do not require the user to understand repository internals for ordinary maintenance.
