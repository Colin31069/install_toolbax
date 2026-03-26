# Windows 軟體安裝管理工具 MVP 開發計畫

我們將根據先前的架構設計，著手開發此工具的第一階段 MVP 版本。此計畫將確保核心的批次安裝流程與配置檔讀取能順利運作。

## User Review Required

> [!WARNING]
> **MVP 階段權限提升策略**
> 為了加快第一版的開發速度，本計畫中 **MVP 版本將採用全域提權**（亦即啟動應用程式時跳出一次 UAC 要求管理員權限）。
> 您是否同意第一階段先做全域提權，後續的進階版本（V2）再實作複雜的 Named Pipes 雙進程（UI + Worker）架構？

> [!NOTE]
> **主題與外觀 (UI Aesthetic)**
> 我計畫使用原生 WPF 搭配基礎極簡樣式作為 MVP，若您強烈希望一開始就擁有「圖吧工具箱」那種酷炫的深色風格，我可以一併導入如 `ModernWpfUI` 或 `MaterialDesignInXAML`。請在回覆中告知您的偏好。

## Proposed Changes

我們將在目前的工作區 (`install_toolbax`) 初始化一個全新的 .NET WPF 應用程式專案。

### Project Structure (WPF Application)

#### [NEW] `InstallToolbox.csproj`
建立基於 .NET 8 / 10 的 WPF 專案，指定 `net8.0-windows` 做為目標框架，並配置所需的 NuGet 套件（MvvmToolkit, System.Text.Json）。

### Core Models & State Management

#### [NEW] `Models/AppItem.cs`
定義軟體項目的核心資料模型，包含 Id, 顯示名稱, 狀態 (Pending, Downloading, Installing, Success, Failed 等)，以及 Winget/DirectUri 等來源設定。

#### [NEW] `Models/AppCategory.cs`
與 `apps_repository.json` 對應的分類結構。

### Configuration Services

#### [NEW] `Services/ConfigService.cs`
負責讀取本地的 `apps_repository.json` 並反序列化為 `AppItem` 集合，供 UI 層綁定。

#### [NEW] `apps_repository.json`
提供一組基礎配置檔（包含 Notepad++ 或 .NET 等範例），以便能夠直接測試與預覽。

### Implementation Engine

#### [NEW] `Services/InstallEngine.cs`
實作安裝與下載邏輯：
*   **Downloader**: 實作 `HttpClient` 抓取 Direct URL 的檔案至快取目錄。
*   **Winget Executer**: 使用 `Process.Start("winget", "install ...")` 執行安裝任務。
*   **Exe/Msi Executer**: 執行本地安裝包且帶入靜默參數 (如 `/S`)。

### UI Layer

#### [NEW] `ViewModels/MainViewModel.cs`
基於 MVVM 模式管理狀態，實作「批次開始」、「分類切換」、「狀態更新 (進度)」等邏輯。

#### [NEW] `MainWindow.xaml`
實作主介面：
1. **左側分類導航**: 列出所有設定檔中的群組。
2. **右側軟體清單**: 列出軟體並提供 CheckBox 供使用者勾選。
3. **底部狀態列/進度儀表板**: 顯示全域執行進度。

## Open Questions

1. **架構分期**：是否確認以「全域提權 (整支程式啟動要求管理員)」作為 MVP 首要目標？
2. **快取目錄**：安裝的快取目錄預設將建置在所在目錄的 `Cache` 資料夾或是 `%USERPROFILE%\AppData\Local\InstallToolbox` 中，您偏好哪一種？
3. **UI 風格**：是否需要先導入 `ModernWpfUI` 來增加美觀程度？

## Verification Plan

### Automated Tests
*   執行 `dotnet build` 確保專案無編譯錯誤。

### Manual Verification
*   請您執行程式 `dotnet run`，驗證 UI 介面是否正確展現基礎軟體清單。
*   嘗試勾選一項輕量工具 (例如 Notepad++) 並觀察其經歷狀態變遷 (等待 -> 下載 -> 安裝 -> 完成)。
