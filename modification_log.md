# Code Modification Log

此文件記錄 Antigravity 在專案內實作的每一次核心檔案修改，以便後續與 ChatGPT 進行錯誤排除與分析。

> **注意：每一次對專案源碼層進行 Tool Calls (`replace_file_content` 等) 都會更新在這裡。**

## [Phase 3] GUI Modernization - WPF-UI (Option B)

### `InstallToolbox.csproj`
- **目的**: 導入 WPF-UI 框架
- **操作**: 透過 `dotnet add package WPF-UI` 進行修改。

### `App.xaml`
- **目的**: 匯入 WPF-UI 的全域資源字典 (Theme & Controls)。
- **操作**: 新增 `xmlns:ui` 命名空間，並將 `ui:ThemesDictionary` 與 `ui:ControlsDictionary` 加入 `Application.Resources`。

### `MainWindow.xaml.cs`
- **目的**: 升級視窗為 `FluentWindow` (支援 Mica 毛玻璃與系統主題連動)。
- **操作**: 繼承自 `Wpf.Ui.Controls.FluentWindow`，並於建構子中加入 `SystemThemeWatcher.Watch(this);` 以追蹤系統佈景變化。

### `MainViewModel.cs`
- **目的**: 支援按鈕切換明暗主題。
- **操作**: 新增 `ToggleThemeCommand` 使用 `ApplicationThemeManager` 切換主題。

### `MainWindow.xaml`
- **目的**: 套用 Windows 11 Fluent 外觀與相容深淺色系。
- **操作**: 
  - 根元素替換為 `<ui:FluentWindow>`，加入 `WindowBackdropType="Mica"`。
  - 移除舊版藍色 Border，替換成微軟標準的 `<ui:TitleBar>`。
  - 在 TitleBar 旁疊加了一個 `<ui:Button>` 用作主題切換開關。
  - 將所有硬編碼 (Hardcoded) 的背景色與文字色 `#F0F0F0` / `White` / `Gray`，全面替換為 `DynamicResource`，採用例如 `CardBackgroundFillColorDefaultBrush` 與 `TextFillColorSecondaryBrush`。
  - 傳統 `<Button>` 轉換為 `<ui:Button Appearance="Primary">`。

*(後續變更皆會依序記錄於此處)*
