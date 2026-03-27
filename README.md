# InstallToolbox

InstallToolbox 是一個 Windows WPF 桌面工具，目標是在重灌、換新電腦或建立新環境時，快速批次安裝常用軟體。

目前專案正從舊版的單層分類模式，逐步演進成更實用的分組式流程：

- `All Apps`
- 情境分組，例如 `新電腦必裝`、`寫程式`、`打電動`
- 每個分組底下再切 `Sections`
- 透過 `Presets` 一鍵勾選常用組合

目前 UI 採用 `WPF-UI`，並已導入 Fluent / Glass 風格的現代化介面。

## 目前狀態

目前已完成：

- 建立於 `.NET 8` 的 WPF 桌面應用程式
- 透過 `apps_repository.json` 管理軟體清單
- 支援新版資料結構：
  - `Apps`
  - `Groups`
  - `Sections`
  - `Presets`
- ViewModel 會自動建立 `All Apps` 群組
- 支援以 Winget 為主的批次安裝流程
- 支援 `Winget`、`DirectUrl`、`LocalFile` 三種來源型態
- 主清單可顯示圖示
- 主視窗已改為 `WPF-UI` 風格

目前仍在調整中：

- 主題切換按鈕行為
- Portable Toolkit 流程
- 部分 JSON / XAML 中文內容仍有編碼問題

## 技術堆疊

- `.NET 8`
- `WPF`
- `CommunityToolkit.Mvvm`
- `WPF-UI 4.2.0`

## 專案結構

```text
InstallToolbox/
|- Models/
|- Services/
|- ViewModels/
|- Assets/
|  \- Icons/
|- apps_repository.json
|- MainWindow.xaml
|- App.xaml
```

主要檔案：

- `MainWindow.xaml`：主畫面 UI
- `ViewModels/MainViewModel.cs`：畫面狀態、分組、預設清單與批次安裝入口
- `Services/InstallEngine.cs`：安裝 / 下載執行邏輯
- `Services/ConfigService.cs`：設定檔讀取
- `apps_repository.json`：軟體清單與分組設定

## 目前資料結構

目前使用的 repository 版本為 `2.0`。

最上層格式如下：

```json
{
  "Version": "2.0",
  "Apps": [],
  "Groups": [],
  "Presets": []
}
```

### Apps

每個軟體只在全域清單定義一次。

目前 `AppItem` 主要欄位包含：

- `Id`
- `Name`
- `Description`
- `IconPath`
- `SourceType`
- `Source`
- `InstallerType`
- `InstallArgs`
- `RequiresAdmin`
- `Dependencies`
- `RetryCount`
- `InstallCheck`
- `CachePolicy`

補充說明：

- `Category` 欄位目前仍存在於 model 中，主要用於舊格式相容，不再是新版 UI 的主要分類依據。
- `Status`、`Progress`、`ErrorMessage`、`IsSelected` 這些屬於執行期間的 UI 狀態，不是靜態設定資料。

### Groups

`Groups` 用來定義左側導覽分組。

每個 Group 底下可以有多個 `Sections`。

範例：

```json
{
  "Id": "dev",
  "Name": "寫程式",
  "Description": "與開發環境相關的工具",
  "Sections": [
    {
      "Id": "ide",
      "Name": "編輯器 / IDE",
      "Description": "程式碼編輯器與 IDE",
      "AppIds": [
        "Microsoft.VisualStudioCode",
        "NotepadPlusPlus"
      ]
    }
  ]
}
```

### Presets

`Presets` 用來定義快速勾選組合。

範例：

```json
{
  "Id": "standard-reinstall",
  "Name": "重灌標準清單",
  "Description": "重灌後常用軟體組合",
  "AppIds": [
    "Google.Chrome",
    "7zip.7zip",
    "Discord.Discord"
  ]
}
```

## 目前 UI 行為

目前畫面主要分成：

- 左側：Groups 分組列表
- 右上：Preset 快捷按鈕區
- 中間：Selected Group 對應的 Section 分頁
- 下方：全域安裝狀態與批次安裝按鈕

目前的額外行為：

- `All Apps` 不是直接寫死在 JSON，而是由 ViewModel 載入時自動產生
- 點選 preset 會將對應 App 設為已勾選
- 批次安裝目前以 Winget 套件最穩定

## 目前支援的來源與安裝型態

### SourceType

目前定義：

- `Winget`
- `DirectUrl`
- `LocalFile`

### InstallerType

目前定義：

- `Winget`
- `Exe`
- `Msi`
- `Zip`

重要限制：

- `Zip` 目前還不是完整的 Portable 部署流程。
- 後續 Portable Toolkit 會補齊這部分，而不是把壓縮檔當成一般安裝程式處理。

## InstallCheck

目前 `InstallCheckType` 定義：

- `Registry`
- `Path`
- `Winget`

就目前程式狀態而言：

- `Winget` 是最主要、最完整的檢查方式
- `Path` 已經在 enum 中，後續 Portable 流程會更依賴它
- `Registry` 目前還沒有完整實作

## 如何執行專案

### 環境需求

- Windows
- .NET 8 SDK
- 系統中可使用 Winget

### 執行

```powershell
dotnet run
```

### 建置

```powershell
dotnet build
```

## 如何新增一般安裝型軟體

1. 在 `apps_repository.json` 的 `Apps` 中新增一筆 App
2. 設定有效的 `Id`
3. 指定正確的 `SourceType`
4. 指定對應的 `InstallerType`
5. 補上 `InstallCheck`
6. 在一個或多個 `Sections` 中用 `AppIds` 引用它
7. 如有需要，再把它加到 `Presets`

範例：

```json
{
  "Id": "Microsoft.VisualStudioCode",
  "Name": "Visual Studio Code",
  "Description": "程式碼編輯器",
  "IconPath": "/Assets/Icons/vscode.png",
  "SourceType": "Winget",
  "Source": "Microsoft.VisualStudioCode",
  "InstallerType": "Winget",
  "InstallArgs": "",
  "RequiresAdmin": true,
  "Dependencies": [],
  "InstallCheck": {
    "Type": "Winget",
    "Value": "Microsoft.VisualStudioCode"
  }
}
```

再把它放進某個 section：

```json
{
  "Id": "ide",
  "Name": "編輯器 / IDE",
  "Description": "程式碼編輯器與 IDE",
  "AppIds": [
    "Microsoft.VisualStudioCode"
  ]
}
```

## 目前已知缺口

- 部分 UI 字串與 JSON 中文內容仍有亂碼，後續應統一成 UTF-8。
- 主題切換按鈕仍在調整中。
- Portable Toolkit 尚未完整落地到 InstallEngine。
- `DirectUrl` / `LocalFile` 相關流程仍比 `Winget` MVP 化，驗證能力還需要補強。

## 相關文件

- `grouping_design_spec.md`：分組架構設計方向
- `issue_fix_step.md`：目前給 AI agent 的修正指示
- `implementation_plan.md`：目前規劃中的實作方向
- `modification_log.md`：AI agent 的修改記錄

## 建議維護原則

如果你有更新以下任一項：

- repository schema
- 安裝流程
- 分組方式
- preset 邏輯

建議同步更新：

- `README.md`
- `modification_log.md`

另外建議：

- `apps_repository.json` 統一使用 UTF-8
- 不要為了分組而重複建立同一個 App
- 優先透過 `AppIds` 來重用 App 定義
