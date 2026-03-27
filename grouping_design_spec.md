# InstallToolbox 分組與預設清單設計規格

## 目標

將目前專案的「單層類別 -> 軟體清單」結構，升級為可支援以下使用情境的結構：

- 依用途分組
  - 例如：寫程式、打電動、新電腦必裝
- 每個用途分組底下再細分子類別
  - 例如：寫程式 -> IDE / 編輯器、語言 / SDK、開發工具
- 同一個軟體可出現在多個分組或多個子類別中
  - 例如：Chrome 可同時出現在「瀏覽器」與「重灌標準清單」
  - 例如：Discord 可同時出現在「通訊 / 日常」與「打電動」
- 支援一鍵勾選的預設清單
  - 例如：重灌標準清單、開發環境清單、遊戲環境清單

本文件的目的，是提供 AI 工程師可直接依規格實作的方案，且盡量降低對既有安裝引擎的破壞。

## 現況問題

目前專案核心模型是：

- `AppsRepository`
  - `Categories: List<string>`
  - `Apps: List<AppItem>`
- `AppItem`
  - `Category: string`

這種設計的限制：

- 一個 App 只能屬於一個 Category
- 無法自然表達「用途分組 -> 子類別 -> App」
- 無法自然表達「同一個 App 被多個用途共用」
- 無法優雅支援「重灌標準清單」這種跨分類的一鍵勾選模板

結論：不要再以 `AppItem.Category` 作為唯一分類依據，應改為「App 主檔 + Group/Section 引用關係 + Preset 引用關係」。

## 設計原則

### 1. App 定義只出現一次

每個軟體只在 `Apps` 主清單定義一次，內容包含：

- 顯示名稱
- 描述
- 安裝來源
- 安裝方式
- 權限需求
- 安裝檢查
- 相依套件

不要因為同一個 App 需要出現在多個地方，就複製多份 App 定義。

### 2. 分組與 App 的關聯改用 `AppIds`

Group 和 Section 不直接存 App 物件，而是只存 `AppIds`。

這樣可以：

- 避免重複資料
- 讓同一個 App 能被多處引用
- 方便做預設清單與搜尋

### 3. 分組與預設清單分開

`Groups` 的目的是 UI 導覽與分類。

`Presets` 的目的是一鍵勾選。

這兩者概念不同，不要混在一起。

## 建議資料模型

### AppsRepository v2

新增或調整以下模型：

```csharp
public class AppsRepository
{
    public string Version { get; set; } = "2.0";
    public List<AppItem> Apps { get; set; } = new();
    public List<AppGroup> Groups { get; set; } = new();
    public List<AppPreset> Presets { get; set; } = new();
}
```

### AppItem

保留目前安裝相關欄位。

建議調整：

- `Category` 不再作為主要分類來源
- 過渡期可先保留 `Category` 以兼容舊格式
- 完整切換後可移除 `Category`

建議結構：

```csharp
public partial class AppItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public SourceType SourceType { get; set; }
    public string Source { get; set; } = string.Empty;
    public InstallerType InstallerType { get; set; }
    public string InstallArgs { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; } = true;
    public List<string> Dependencies { get; set; } = new();
    public int RetryCount { get; set; } = 2;
    public InstallCheck? InstallCheck { get; set; }
    public string CachePolicy { get; set; } = "DeleteOnSuccess";

    [JsonIgnore]
    public AppStatus Status { get; set; }

    [JsonIgnore]
    public int Progress { get; set; }

    [JsonIgnore]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsSelected { get; set; }
}
```

### AppGroup

```csharp
public class AppGroup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<AppSection> Sections { get; set; } = new();
}
```

### AppSection

```csharp
public class AppSection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AppIds { get; set; } = new();
}
```

### AppPreset

```csharp
public class AppPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AppIds { get; set; } = new();
}
```

## 建議 JSON 結構

以下為建議的新格式範例。

```json
{
  "Version": "2.0",
  "Apps": [
    {
      "Id": "Google.Chrome",
      "Name": "Google Chrome",
      "Description": "瀏覽器",
      "SourceType": "Winget",
      "Source": "Google.Chrome",
      "InstallerType": "Winget",
      "InstallArgs": "",
      "RequiresAdmin": true,
      "Dependencies": [],
      "RetryCount": 2,
      "InstallCheck": {
        "Type": "Winget",
        "Value": "Google.Chrome"
      },
      "CachePolicy": "DeleteOnSuccess"
    },
    {
      "Id": "Git.Git",
      "Name": "Git",
      "Description": "版本控制工具",
      "SourceType": "Winget",
      "Source": "Git.Git",
      "InstallerType": "Winget",
      "InstallArgs": "",
      "RequiresAdmin": true,
      "Dependencies": [],
      "RetryCount": 2,
      "InstallCheck": {
        "Type": "Winget",
        "Value": "Git.Git"
      },
      "CachePolicy": "DeleteOnSuccess"
    }
  ],
  "Groups": [
    {
      "Id": "dev",
      "Name": "寫程式",
      "Description": "與開發環境相關的工具與語言",
      "Sections": [
        {
          "Id": "ide",
          "Name": "IDE / 編輯器",
          "Description": "程式碼編輯器與 IDE",
          "AppIds": [
            "Microsoft.VisualStudioCode",
            "JetBrains.PyCharm.Community",
            "Spyder.Spyder"
          ]
        },
        {
          "Id": "language",
          "Name": "語言 / SDK",
          "Description": "常用語言與執行環境",
          "AppIds": [
            "Python.Python.3.12",
            "OpenJS.NodeJS.LTS",
            "RProject.R"
          ]
        },
        {
          "Id": "tooling",
          "Name": "開發工具",
          "Description": "版本控制、容器與其他工具",
          "AppIds": [
            "Git.Git",
            "Docker.DockerDesktop"
          ]
        }
      ]
    },
    {
      "Id": "gaming",
      "Name": "打電動",
      "Description": "遊戲平台與溝通工具",
      "Sections": [
        {
          "Id": "platform",
          "Name": "遊戲平台",
          "Description": "常見遊戲平台與啟動器",
          "AppIds": [
            "Valve.Steam",
            "EpicGames.EpicGamesLauncher",
            "Discord.Discord"
          ]
        }
      ]
    },
    {
      "Id": "new-pc",
      "Name": "新電腦必裝",
      "Description": "重灌後常見必裝工具",
      "Sections": [
        {
          "Id": "browser",
          "Name": "瀏覽器",
          "Description": "常見瀏覽器",
          "AppIds": [
            "Google.Chrome",
            "Mozilla.Firefox",
            "Opera.Opera",
            "Arc.Arc"
          ]
        },
        {
          "Id": "archive",
          "Name": "壓縮工具",
          "Description": "壓縮與解壓縮工具",
          "AppIds": [
            "7zip.7zip"
          ]
        },
        {
          "Id": "daily",
          "Name": "通訊 / 日常",
          "Description": "聊天與常用日常工具",
          "AppIds": [
            "Discord.Discord",
            "LINE.LINE",
            "Telegram.TelegramDesktop",
            "Spotify.Spotify",
            "Google.Drive"
          ]
        }
      ]
    }
  ],
  "Presets": [
    {
      "Id": "standard-reinstall",
      "Name": "重灌標準清單",
      "Description": "常見重灌必裝項目",
      "AppIds": [
        "Google.Chrome",
        "7zip.7zip",
        "voidtools.Everything",
        "Microsoft.PowerToys",
        "Microsoft.VisualStudioCode",
        "Git.Git",
        "OpenJS.NodeJS.LTS",
        "Python.Python.3.12",
        "Docker.DockerDesktop",
        "Notion.Notion",
        "Obsidian.Obsidian",
        "Discord.Discord",
        "LINE.LINE",
        "Spotify.Spotify",
        "Google.Drive"
      ]
    }
  ]
}
```

## UI / ViewModel 設計方向

### 目標 UI 結構

建議畫面結構：

- 左側：Group 列表
  - 寫程式
  - 打電動
  - 新電腦必裝
- 右側上方：Preset 快速套用區
  - 套用重灌標準清單
  - 套用開發環境
  - 套用遊戲環境
- 右側主區：SelectedGroup 的 Sections
  - 可用 `TabControl`、`Expander` 或 `ListBox + ItemsControl`
- 每個 Section 底下顯示 App 清單

### 為什麼建議 `TabControl`

以目前專案規模，`TabControl` 是最容易實作且不需要大幅重構資料繫結的方案。

建議互動：

- 左邊選 Group
- 右邊 `TabControl` 顯示 Group 底下所有 Sections
- 每個 Tab 顯示該 Section 的 Apps

這樣能保留你現在「左邊選類別，右邊看清單」的操作習慣，只是把右側再分頁。

## ViewModel 建議結構

### UI 專用模型

建議不要直接把 JSON 的 `AppSection` 當成 UI 顯示模型。

建議再建立 UI 專用結構：

```csharp
public class AppSectionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<AppItem> Apps { get; set; } = new();
}

public class AppGroupViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<AppSectionViewModel> Sections { get; set; } = new();
}
```

### MainViewModel 建議狀態

```csharp
public ObservableCollection<AppGroupViewModel> Groups { get; set; } = new();
public AppGroupViewModel? SelectedGroup { get; set; }

public ObservableCollection<AppPreset> Presets { get; set; } = new();
public AppPreset? SelectedPreset { get; set; }
```

## 資料載入邏輯

### 建議流程

1. 載入 repository JSON
2. 建立 `Dictionary<string, AppItem>`，key = `App.Id`
3. 迭代 `Groups`
4. 對每個 Group 建立 `AppGroupViewModel`
5. 對每個 Section 根據 `AppIds` 去字典中查找對應 App
6. 將查找到的 App 加入該 Section 的 `Apps`
7. 組成最終 `Groups` 集合供 UI 綁定
8. 載入 `Presets`

### 重要注意事項

- `AppIds` 若找不到對應 App，應記錄錯誤或忽略並留下診斷訊息
- 同一個 `AppItem` 可能在多個 Section 中共用，這是預期行為
- `IsSelected` 應該是 App 本身狀態，而不是 Section 層狀態

這代表：

- 若同一個 App 同時出現在多個 Section，被任一處勾選時，其他出現位置也應同步顯示已勾選

若未來 UI 無法正確反映同一物件的共享狀態，再考慮引入更明確的 item wrapper，但第一版可先直接共用 `AppItem` 實例。

## Preset 功能規格

### 需求

使用者可快速套用預設清單，例如：

- 重灌標準清單
- 寫程式基本環境
- 遊戲環境

### 行為規則

- 點擊某個 Preset 後，將其 `AppIds` 對應到的 App 設為 `IsSelected = true`
- 不在該 Preset 內的 App 是否取消勾選，需明確定義

### 建議第一版策略

採用「附加勾選」模式：

- Preset 只會把包含的 App 勾選起來
- 不會自動取消其他已選項目

原因：

- 比較安全
- 符合使用者先手動選一些、再套用 Preset 的直覺
- 避免誤清除使用者已勾選的其他工具

若未來需要，可再增加：

- `套用並覆蓋`
- `先清空再套用`

## 推薦初始分組內容

以下是可優先建立的分組，直接對應使用者需求。

### Group: 寫程式

Sections:

- IDE / 編輯器
  - VSCode
  - Spyder
  - PyCharm
- 語言 / SDK
  - Python
  - C
  - C++
  - R
  - Node.js
- 開發工具
  - Git
  - Docker

### Group: 打電動

Sections:

- 遊戲平台
  - Steam
  - Epic Games Launcher
  - Riot Client / LOL 啟動器
- 社群 / 語音
  - Discord

### Group: 新電腦必裝

Sections:

- 瀏覽器
  - Chrome
  - Arc
  - Opera
  - Firefox
- 壓縮工具
  - 7-Zip
- 通訊 / 日常
  - LINE
  - Telegram
  - Discord
  - Spotify
  - Google Drive
- 系統工具
  - Everything
  - PowerToys

### Preset: 重灌標準清單

建議包含：

- Chrome
- 7-Zip
- Everything
- PowerToys
- VSCode
- Git
- Node.js
- Python
- Docker
- Notion
- Obsidian
- Discord
- LINE
- Spotify
- Google Drive

## 實作順序建議

為降低風險，建議依照以下順序做。

### Phase 1: 模型與 JSON 升級

- 新增 `AppGroup`
- 新增 `AppSection`
- 新增 `AppPreset`
- 調整 `AppsRepository`
- 保留 `AppItem` 的安裝欄位
- 過渡期暫時保留 `Category`

完成標準：

- 可成功讀取新版 JSON

### Phase 2: ViewModel 載入邏輯改造

- 建立 `App.Id -> AppItem` 字典
- 將 `Groups -> Sections -> AppIds` 映射成 UI 可綁定資料
- `SelectedCategory` 改為 `SelectedGroup`

完成標準：

- 左側可顯示 Group
- 右側可顯示 Group 底下對應的 Section 與 App

### Phase 3: UI 改版

- 左側保留 Group 導覽
- 右側改為 Section 分頁
- App 清單保留既有 DataGrid 呈現方式

完成標準：

- 使用者可在不同 Group / Section 間切換
- 勾選狀態正常顯示

### Phase 4: Preset 功能

- 顯示 Preset 清單或快速按鈕
- 點擊後自動勾選對應 App

完成標準：

- 使用者可以一鍵套用「重灌標準清單」

### Phase 5: 舊格式相容或遷移

二選一：

- 相容模式：若 JSON 仍為舊版 `Categories`，就自動轉成單層 Group/Section
- 遷移模式：專案直接改用 v2，不再支援舊版

建議：

- 若目前專案還在早期開發，可直接升級到 v2，減少雙格式維護成本

## 與現有安裝流程的相容性

此重構不應優先動到安裝引擎。

安裝流程應保持：

- UI 勾選多個 `AppItem`
- 收集 `IsSelected == true` 的 App
- 交由既有 `InstallEngine` 安裝

也就是說，第一階段只重構「如何呈現與組織 App」，不改「如何安裝 App」。

## 錯誤處理與驗證規格

### 載入時驗證

載入 repository 時應檢查：

- `Apps.Id` 不可重複
- `Groups.Id` 不可重複
- 同一個 Group 內 `Sections.Id` 不可重複
- `Sections.AppIds` 指向的 App 必須存在
- `Presets.AppIds` 指向的 App 必須存在

### 驗證失敗處理建議

- 致命錯誤
  - 重複 App Id
  - JSON 結構錯誤
  - 必要欄位遺失
- 非致命錯誤
  - 某個 `AppId` 找不到對應 App
  - 某個 Group 或 Section 為空

建議：

- 致命錯誤直接停止載入並顯示錯誤訊息
- 非致命錯誤略過並保留診斷資訊

## 編碼與檔案格式要求

目前專案中的中文在部分終端輸出有亂碼跡象。

建議：

- 所有 JSON、XAML、Markdown、C# 檔案統一使用 UTF-8
- 若要避免 BOM 相關問題，團隊可統一 UTF-8 with BOM 或 UTF-8 without BOM，但要固定
- 中文欄位值與 UI 文案請避免混用不同編碼來源

## 參考產品的落點

### Winget

建議作為底層安裝來源與套件識別來源。

### Ninite

建議參考其使用者流程：

- 使用者勾選想裝的軟體
- 一次批次安裝

### Chocolatey

建議參考其套件 metadata 與可維護性思路，但不必直接複製使用模式。

## 最終建議

最適合這個專案的方向是：

- 安裝來源：以 `Winget` 為主
- UI 操作：走 `Ninite` 風格
- 分類能力：加入你自己的 `Groups + Sections + Presets`

也就是：

「用 Winget 當安裝引擎，UI 做成可依用途分組的 Ninite，並支援重灌標準清單與個人情境模板。」

## AI 工程師實作摘要

若要快速開始，請直接照以下順序做：

1. 新增 `AppGroup`、`AppSection`、`AppPreset`
2. 將 `AppsRepository` 改為 `Apps + Groups + Presets`
3. 保留 `AppItem` 安裝欄位，逐步淘汰 `Category`
4. 將載入邏輯改為 `AppId` 映射
5. 左側顯示 Group，右側用 `TabControl` 顯示 Sections
6. 每個 Section 內仍沿用現有 App DataGrid
7. 新增 Preset 套用功能，第一版採附加勾選
8. 最後再決定是否保留舊版 JSON 相容

## 驗收標準

實作完成後，至少需滿足以下條件：

- 使用者可選擇「寫程式」、「打電動」、「新電腦必裝」
- 「寫程式」底下可再切換 `IDE / 編輯器`、`語言 / SDK`、`開發工具`
- 同一個 App 可同時存在於多個 Group / Section
- 點選「重灌標準清單」可自動勾選對應項目
- 批次安裝流程仍可沿用既有安裝引擎
- JSON 配置可維護且不需為同一個 App 重複建資料

