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

## [Phase 4] GUI Refinement (Glassmorphism & Bug Fixes)

### `MainWindow.xaml.cs`
- **目的**: 修正手動切換深淺色主題失效的問題。
- **操作**: 移除了 `SystemThemeWatcher.Watch(this)`，因為該機制會即時強制套用系統主題，導致 `ToggleThemeCommand` 的手動切換被覆蓋而毫無反應。現在應用程式將尊崇使用者的手動切換。

### `MainWindow.xaml`
- **目的**: 修復 DataGrid 勾選框被裁切問題，並全域升級為 Glassmorphism (半透明玻璃卡片) 風格。
- **操作**: 
  - **CheckBox 修正**: 將勾選框所在的 `DataGridTemplateColumn` 寬度提升至 `64`。不再讓 `CheckBox` 裸放，而是用 `<Grid Margin="4,0">` 包覆，並將 `RowHeight` 加大為 `50` 確保足夠留白。
  - **Glassmorphism 特效**: 於 `Window.Resources` 宣告了 `GlassCardStyle`，運用 `CardBackgroundFillColorDefaultBrush` 搭配 `DropShadowEffect` 產生輕柔的玻璃卡片與漂浮陰影。
  - **深度與氛圍光**: 在主要的排版層最底層加入了兩個 `IsHitTestVisible="False"` 的巨大 `<Ellipse>` (綠色與藍色)，並賦予極大的 `BlurEffect` (半徑 120-150)。這讓整體的 Mica 背景之上多了一層如 iOS / macOS 般的毛玻璃氛圍感色塊，在不同主題切換下都能透出高質感的層次。
  - **應用設計套用**: 將左側 Group 列、上方 Presets 列、右側主區域表單、底部狀態列皆套用 `Style="{StaticResource GlassCardStyle}"`，取代生硬的原理底色與框線。

## [Phase 5] Theme Observability & Portable Toolkit Engine

### Theme Toggle 改良 (`ViewModels/MainViewModel.cs` & `MainWindow.xaml`)
- **目的**: 確保切換系統時不被原生的 TitleBar 點擊區塊覆蓋，以及提供明顯的視覺回饋。
- **操作**:
  - 將 ThemeToggle 按鈕從 `<ui:TitleBar>` 疊層內拔除，搬移到右側「快速預設清單」區段的右上角，確保 100% 獨立的點擊區域。
  - 在 `MainViewModel` 中新增了 `Debug.WriteLine` 用於追蹤觸發狀態。
  - 新增 `ThemeIcon` 屬性綁定到按鈕上的 `<ui:SymbolIcon>`，當主題切換時，動態展示 `WeatherMoon24` (暗色系) 或是 `WeatherSun24` (亮色系)。

### Portable Toolkit 免安裝引擎建置 (`Models/Enums.cs`, `Models/AppItem.cs`, `Services/InstallEngine.cs`)
- **目的**: 免安裝包不再作為執行檔強行跑 Process 開安裝流程，而是提供下載、解壓、集中放置的標準化自動化管理。
- **操作**:
  - 新增 `DeploymentType.Portable` (與原來的 `Installed` 區隔)。
  - `AppItem` 中新增了 `PortableTargetFolder`, `PortableEntryRelativePath`, `PortableExtractSubfolder` 三個欄位供未來擴充彈性。
  - 修改 `InstallEngine.cs`，當遇到 `DeploymentType == Portable` 時，走入不跑 Process 的新分支 `HandlePortableDeploymentAsync`。
  - 新分支會自動在 `%UserProfile%\Tools\Portable\` 目錄底下根據欄位建立對應的獨立資料夾，若來源是 `InstallerType.Zip` 就走 `System.IO.Compression.ZipFile.ExtractToDirectory` 直接解開；若是 `InstallerType.Exe` 則只作 Copy 放入資料夾內。
  - 完善了 `InstallCheckType.Path` 檢查邏輯，自動使用 `Environment.ExpandEnvironmentVariables` 置換環境變數後確認 `File.Exists`。若發現主檔案已存在，則在下次開啟時將直接亮綠燈（Skipped），再也不會重複下載。

### `apps_repository.json` 擴充
- **目的**: 提供免安裝區塊驗證剛修好的新設計。
- **操作**:
  - 把 5 款常見的實用工具實體化為 `app_repository` 中的項目（包含 WizTree, CrystalDiskInfo, Process Explorer, SumatraPDF, PuTTY），它們皆使用了 Mock/Real 的 Zip/Exe 連結與 `DeploymentType: Portable` 屬性。
  - 新增名為 `Portable Toolkit` 的全新大分類群組。

*(後續變更皆會依序記錄於此處)*

## [Phase 6] Settings UI, App Creation Workflow, and Theme Icon Fix

### `MainWindow.xaml` & `MainViewModel.cs`
- **目的**: 修正 Dark Mode 佈景切換時圖示消失的問題，並新增「設定」按鈕。
- **操作**: 
  - 主題切換的 `ui:SymbolIcon` 補上 `Foreground="{DynamicResource TextFillColorPrimaryBrush}"` 明確綁定。
  - 將不穩定的圖標 `WeatherSun24` 替換為 `WeatherSunny24` 以提高相容性。
  - 於主畫面右上角 `ThemeToggle` 左側新增「設定」的齒輪按鈕 (`Settings24`)，並連結至 `OpenSettingsCommand`。
  - `LoadData()` 方法內部加入 `Groups.Clear()` 與 `Presets.Clear()` 防止重載配置檔時資料重複疊加。

### `SettingsWindow.xaml` & `SettingsWindow.xaml.cs` & `SettingsViewModel.cs` (新增)
- **目的**: 提供不必手動編輯 `.json` 的圖形化應用程式新增介面。
- **操作**: 
  - 建立專屬的 `SettingsWindow` 視窗，並套用 `FluentWindow` / `Mica` 樣式。
  - 新增包含基本設定區（Id、名稱、描述、來源類型、安裝檔類型）與進階設定區（部署類型、安裝參數）的 `SettingsViewModel`。
  - 表單填寫具備簡單的空值檢查與 ID 唯一性驗證。
  - 允許使用者透過 CheckBox 在現有的 `AppGroup` 與 `AppSection` 勾選並安置該軟體。

### `ConfigService.cs`
- **目的**: 安全地將修改後的 `AppsRepository` 寫回 `apps_repository.json`，並保留備份機制。
- **操作**: 
  - 新增 `SaveConfig(AppsRepository repo)` 方法。
  - 寫入前若目標存在，自動利用 `File.Copy` 產出 `.bak` 備份檔。
  - 使用 `JsonSerializerOptions`（如 `WriteIndented`、`UnsafeRelaxedJsonEscaping`）來保留 UTF-8 可讀性排版。
