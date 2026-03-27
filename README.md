# How to Use

## 專案用途

這個專案是一個開發中的 WPF 安裝工具，會從 `apps_repository.json` 讀取可安裝的 app 清單，並在 UI 中依分類顯示，讓使用者勾選後批次安裝。

目前的主要流程是：

1. 啟動程式
2. 讀取 `apps_repository.json`
3. 依 `Category` 分組顯示 app
4. 使用者勾選要安裝的項目
5. 執行安裝流程

---

## 目前要新增其他想下載的 App，該怎麼做

這個專案現在是資料驅動設計，也就是說，大部分情況下你只要修改 `apps_repository.json`，不需要改 UI。

### 1. 找到設定檔

專案根目錄有一個檔案：

- `apps_repository.json`

這個檔案定義了：

- 所有分類
- 所有可安裝 app
- 每個 app 的來源與檢查方式

---

## `apps_repository.json` 結構

基本結構如下：

```json
{
  "Version": "1.0",
  "Categories": [
    "系統元件",
    "開發工具",
    "常用工具"
  ],
  "Apps": [
    {
      "Id": "VideoLAN.VLC",
      "Name": "VLC media player",
      "Category": "常用工具",
      "Description": "多媒體播放器",
      "SourceType": "Winget",
      "Source": "VideoLAN.VLC",
      "InstallerType": "Winget",
      "InstallArgs": "",
      "RequiresAdmin": true,
      "Dependencies": [],
      "InstallCheck": {
        "Type": "Winget",
        "Value": "VideoLAN.VLC"
      }
    }
  ]
}
```

---

## 新增一個 App 的步驟

### 1. 先決定分類

如果你要加的 app 屬於既有分類，就直接填那個分類名稱。

如果是新分類，就要先把分類名稱加到：

```json
"Categories": [
  "系統元件",
  "開發工具",
  "常用工具",
  "你的新分類"
]
```

注意：

- `App.Category` 必須和 `Categories` 裡的字串完全一致
- 如果分類名稱對不上，UI 不會顯示那個 app

---

### 2. 在 `Apps` 陣列新增一筆資料

最常見也最推薦的是使用 `Winget`。範例如下：

```json
{
  "Id": "VideoLAN.VLC",
  "Name": "VLC media player",
  "Category": "常用工具",
  "Description": "多媒體播放器",
  "SourceType": "Winget",
  "Source": "VideoLAN.VLC",
  "InstallerType": "Winget",
  "InstallArgs": "",
  "RequiresAdmin": true,
  "Dependencies": [],
  "InstallCheck": {
    "Type": "Winget",
    "Value": "VideoLAN.VLC"
  }
}
```

---

## 每個欄位是什麼意思

### 基本欄位

- `Id`
  - 這筆 app 的唯一識別值
  - 建議直接用 winget id，最省事

- `Name`
  - UI 顯示名稱

- `Category`
  - 分類名稱
  - 必須存在於 `Categories`

- `Description`
  - 顯示在 UI 裡的說明文字

---

### 安裝來源相關

- `SourceType`
  - 目前列舉支援：
    - `Winget`
    - `DirectUrl`
    - `LocalFile`

- `Source`
  - 如果 `SourceType` 是 `Winget`，這裡通常填 winget package id
  - 例如：`Git.Git`、`VideoLAN.VLC`

- `InstallerType`
  - 目前列舉支援：
    - `Winget`
    - `Exe`
    - `Msi`
    - `Zip`

- `InstallArgs`
  - 安裝參數
  - 如果使用 `Winget`，通常可留空字串

---

### 其他欄位

- `RequiresAdmin`
  - 是否需要管理員權限
  - 目前程式裡有這個欄位，但安裝流程尚未真正依它做特殊處理

- `Dependencies`
  - 相依項目
  - 目前模型有這個欄位，但實際安裝流程還沒有做相依排序

- `RetryCount`
  - 重試次數
  - 目前還沒有完整用在實際流程裡

- `CachePolicy`
  - 快取策略
  - 目前也沒有完整落實

---

## `InstallCheck` 怎麼填

這個欄位是用來判斷 app 是否已經安裝。

目前最穩定的是：

```json
"InstallCheck": {
  "Type": "Winget",
  "Value": "VideoLAN.VLC"
}
```

### 目前實作狀態

雖然程式列舉中有：

- `Registry`
- `Path`
- `Winget`

但目前真正有實作檢查邏輯的只有：

- `Winget`

也就是說，如果你新增的是 `DirectUrl` 或 `LocalFile` 類型的安裝包，安裝後驗證這一塊目前不完整，之後可能還要補 C# 程式碼。

---

## 建議你目前優先新增的類型

目前專案最適合新增的是：

- 可以直接用 `winget install --id xxx` 安裝的 app

例如：

- `Google.Chrome`
- `Mozilla.Firefox`
- `VideoLAN.VLC`
- `Microsoft.VisualStudioCode`
- `Discord.Discord`
- `OBSProject.OBSStudio`

這類 app 通常只要補一筆 JSON 即可。

---

## 新增 App 的推薦模板

你之後可以直接複製這段改：

```json
{
  "Id": "Package.Id.Here",
  "Name": "App Name Here",
  "Category": "常用工具",
  "Description": "這裡填 app 說明",
  "SourceType": "Winget",
  "Source": "Package.Id.Here",
  "InstallerType": "Winget",
  "InstallArgs": "",
  "RequiresAdmin": true,
  "Dependencies": [],
  "InstallCheck": {
    "Type": "Winget",
    "Value": "Package.Id.Here"
  }
}
```

---

## 目前專案要注意的地方

### 1. `apps_repository.json` 目前有格式問題

我檢查過目前專案裡這份 JSON，裡面的多個 `Description` 字串少了結尾引號，所以現在這份檔案其實不是合法 JSON。

這代表：

- 在新增 app 之前
- 你要先把原本 JSON 修正成合法格式
- 否則程式讀設定時就會失敗

---

### 2. 中文內容看起來有編碼問題

目前專案中部分中文有亂碼現象，可能是檔案編碼不一致造成的。

建議：

- 統一使用 UTF-8 編碼儲存
- 尤其是：
  - `apps_repository.json`
  - `.xaml`
  - `.cs`

---

### 3. `apps_repository.json` 可能沒有自動複製到輸出目錄

目前 `.csproj` 沒有明確設定把 `apps_repository.json` 複製到輸出資料夾，因此執行位置不同時，程式可能讀不到這份設定檔。

這部分之後如果要正式使用，建議補上專案設定。

---

## 如果你要新增 `DirectUrl` 或 `LocalFile`

這兩種目前不是不能用，但要注意：

- 安裝執行雖然有基礎支援
- 已安裝檢查與驗證仍不完整
- `Registry` / `Path` 檢查邏輯目前尚未完成

所以如果你要的是穩定可用版本，建議先以 `Winget` 類型為主。

---

## 實務建議

如果你只是想快速擴充可安裝 app：

1. 先修正 `apps_repository.json`
2. 只新增 `Winget` 類型 app
3. `InstallCheck.Type` 一律先用 `Winget`
4. 確保 `Category` 存在且名稱完全一致
5. 每次新增完都先驗證 JSON 格式是否正確

---

## 範例：新增 VS Code

```json
{
  "Id": "Microsoft.VisualStudioCode",
  "Name": "Visual Studio Code",
  "Category": "開發工具",
  "Description": "輕量且常用的程式碼編輯器",
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

---

## 總結

目前這個專案新增 app 的主要方式是修改 `apps_repository.json`。

如果是 `Winget` 套件：

- 通常只要補一筆 JSON
- 不需要改 UI
- 也不一定需要改 C# 程式碼

如果是 `DirectUrl` 或 `LocalFile`：

- 雖然可加
- 但安裝後驗證機制目前還不完整
- 之後可能要補 `InstallEngine` 的邏輯
