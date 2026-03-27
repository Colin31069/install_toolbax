# InstallToolbox

InstallToolbox 是一個以 WPF 製作的 Windows 軟體安裝管理工具，目標是在重灌、換新電腦、建立工作站或整理常用工具時，快速完成軟體勾選、分組瀏覽與批次安裝。

目前介面採用 `WPF-UI`，主視覺以 Fluent + Mica 為基底，並以深色主題作為預設體驗。

## 目前功能

- 左側以「分類」為主的瀏覽模式，並保留 `所有程式` 與 `Portable Toolkit` 特殊入口。
- 上方快速預設區可一鍵勾選常用組合，也支援展開後拖曳排序。
- 中央清單可逐項勾選 App，並顯示狀態、進度與錯誤訊息。
- 底部提供全域「取消所有選取」與「開始安裝勾選項目」。
- 內建設定視窗，可調整主題、UI 縮放、清單密度、安裝後清除選取行為，以及新增/編輯 App。
- 使用 `apps_repository.json` 管理共用軟體清單，使用 `user_settings.json` 儲存本機個人化設定。

## 目前 UI 結構

主畫面分成三個區域：

1. 左側分類導覽
   - `所有程式`：顯示全部 App。
   - 中間分類：由 repository 中各 `Group -> Section` 攤平成可直接點選的分類。
   - `Portable Toolkit`：保留原本的特殊入口與內部分頁結構。
2. 上方快速預設區
   - 點一下 preset 會把對應的 App 標記為已勾選。
   - 預設只顯示單列，按下「顯示全部」後可展開更多按鈕。
   - 展開狀態下可直接拖曳 preset 按鈕重新排序，排序會寫回本機設定。
3. 下方批次操作列
   - 顯示全域狀態訊息與總進度。
   - 可一鍵取消所有已勾選項目。
   - 可對目前勾選的 App 啟動批次安裝。

## 快速預設與選取邏輯

### 快速預設

- 快速預設來源包含：
  - repository 中原生定義的 `Presets`
  - 非 `portable-toolkit` 群組在執行時轉出的 bundle preset
- 拖曳排序後會把 preset ID 順序存進 `user_settings.json` 的 `PresetOrder`
- 重新啟動後會沿用上次自訂的順序

### 勾選與清除

- 可直接在清單中用勾選框選取 App
- 點擊 preset 會補選該 preset 對應的 App
- 底部「取消所有選取」會清除目前所有分類中的已勾選項目，不受左側分類限制
- 若設定了安裝後清除策略，也會在安裝完成後依規則清掉選取狀態

## 主題與本機設定

### 深色主題

- 新啟動時預設使用深色主題
- 啟動階段會先套用主題，再建立主視窗，避免先閃出淺色畫面
- 針對舊版 `user_settings.json`：
  - 若尚未做過深色預設遷移
  - 且主題設定為 `Light`
  - 啟動時會自動改為 `Dark` 一次
- 使用者之後仍可透過主畫面右上角按鈕或設定視窗手動切回淺色

### `user_settings.json`

目前本機設定包含：

- `ThemeMode`
- `HasAppliedDefaultDarkMigration`
- `PortableInstallRoot`
- `PostInstallSelectionBehavior`
- `RememberLastNavigation`
- `LastSelectedGroupId`
- `LastSelectedSectionId`
- `UiScalePreset`
- `ListDensity`
- `PresetOrder`

## Repository 資料結構

專案目前使用 `apps_repository.json` 作為主要資料來源，版本為 `2.0`。

```json
{
  "Version": "2.0",
  "Apps": [],
  "Groups": [],
  "Presets": []
}
```

### Apps

`Apps` 定義每一個可安裝項目。常用欄位包含：

- `Id`
- `Name`
- `Description`
- `IconPath`
- `SourceType`
- `Source`
- `InstallerType`
- `DeploymentType`
- `InstallArgs`
- `RequiresAdmin`
- `Dependencies`
- `RetryCount`
- `InstallCheck`

範例：

```json
{
  "Id": "Microsoft.VisualStudioCode",
  "Name": "Visual Studio Code",
  "Description": "輕量且常用的程式碼編輯器",
  "IconPath": "/Assets/Icons/vscode.png",
  "SourceType": "Winget",
  "Source": "Microsoft.VisualStudioCode",
  "InstallerType": "Winget",
  "DeploymentType": "Installed",
  "InstallArgs": "",
  "RequiresAdmin": true,
  "Dependencies": [],
  "RetryCount": 2,
  "InstallCheck": {
    "Type": "Winget",
    "Value": "Microsoft.VisualStudioCode"
  }
}
```

### Groups 與 Sections

`Groups` 是 repository 內的原始分組資料，但 UI 會做以下轉換：

- `portable-toolkit` 群組保留為左側特殊入口
- 其他群組底下的 `Sections` 會攤平成左側可直接點選的分類
- 非 `portable-toolkit` 群組本身會在執行時轉成上方的快速預設 bundle

範例：

```json
{
  "Id": "dev",
  "Name": "寫程式",
  "Description": "開發工具與日常工程環境",
  "Sections": [
    {
      "Id": "editors",
      "Name": "編輯器 / IDE",
      "Description": "程式碼編輯與 IDE",
      "AppIds": [
        "Microsoft.VisualStudioCode",
        "NotepadPlusPlus"
      ]
    }
  ]
}
```

### Presets

`Presets` 用來定義一鍵勾選的 App 組合。

範例：

```json
{
  "Id": "standard-reinstall",
  "Name": "重灌標準清單",
  "Description": "重灌後第一輪常用軟體",
  "AppIds": [
    "Google.Chrome",
    "7zip.7zip",
    "Microsoft.PowerToys"
  ]
}
```

## 支援的來源與安裝型態

### SourceType

目前模型定義：

- `Winget`
- `DirectUrl`
- `LocalFile`

### InstallerType

目前模型定義：

- `Winget`
- `Exe`
- `Msi`
- `Zip`

### DeploymentType

目前模型定義：

- `Installed`
- `Portable`

## 設定視窗

設定視窗目前分成三個方向：

1. 新增 App
   - 可填寫基本資訊、來源、安裝型態、是否需管理員、圖示
   - 可指定要加入哪些群組/分類
2. 編輯既有 App
   - 可修改既有 App 基本欄位
   - 可重新指定圖示與分組
3. 一般設定
   - 主題模式
   - UI 縮放
   - 清單密度
   - Portable 安裝根目錄
   - 安裝後是否清除已選項目
   - 是否記住上次導覽位置

## 專案結構

```text
InstallToolbox/
|- Assets/
|- Converters/
|- Models/
|- Services/
|- ViewModels/
|- App.xaml
|- MainWindow.xaml
|- SettingsWindow.xaml
|- apps_repository.json
|- README.md
```

重點檔案：

- `MainWindow.xaml`
  - 主畫面 UI，包含左側分類、快速預設區、App 清單與底部操作列
- `MainWindow.xaml.cs`
  - 快速預設拖曳排序事件
- `ViewModels/MainViewModel.cs`
  - 主畫面資料載入、分類轉換、preset 套用、全域清除選取、批次安裝與主題切換
- `SettingsWindow.xaml`
  - 設定視窗 UI
- `ViewModels/SettingsViewModel.cs`
  - 新增/編輯 App 與一般設定的邏輯
- `Services/ConfigService.cs`
  - 載入與儲存 `apps_repository.json`
- `Services/SettingsService.cs`
  - 載入與儲存 `user_settings.json`

## 執行與建置

### 環境需求

- Windows
- .NET 8 SDK
- 建議已安裝並可使用 `winget`

### 啟動

```powershell
dotnet run
```

### 建置

```powershell
dotnet build
```

## 目前限制

- 目前仍以 `Winget` 安裝流程最穩定
- `DirectUrl`、`LocalFile` 與部分 portable 流程仍持續補強中
- repository 與設定檔都屬於本機檔案操作，尚未提供雲端同步或多人共用設定能力
- 專案目前沒有獨立自動化測試專案，驗證以 `dotnet build` 與手動操作測試為主

## 維護建議

當你更新以下任一項時，建議同步更新 README：

- UI 操作流程
- repository schema
- preset 行為
- 設定欄位
- 安裝流程或支援的來源型態

若實作與 README 不一致，請以程式碼目前行為為準，並盡快補上文件同步。
