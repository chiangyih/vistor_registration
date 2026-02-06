# 訪客登記系統 - IIS 部署指南

## 📋 目錄
- [部署前準備](#部署前準備)
- [步驟 1：安裝必要元件](#步驟-1安裝必要元件)
- [步驟 2：發佈應用程式](#步驟-2發佈應用程式)
- [步驟 3：設定 IIS](#步驟-3設定-iis)
- [步驟 4：設定資料庫](#步驟-4設定資料庫)
- [步驟 5：設定應用程式](#步驟-5設定應用程式)
- [步驟 6：測試部署](#步驟-6測試部署)
- [進階設定](#進階設定)
- [常見問題排解](#常見問題排解)

---

## 部署前準備

### 伺服器需求
- **作業系統**：Windows Server 2016 或更新版本（或 Windows 10/11）
- **IIS 版本**：IIS 10.0 或更新版本
- **記憶體**：至少 2GB RAM（建議 4GB 以上）
- **硬碟空間**：至少 1GB 可用空間

### 檢查清單
- [ ] 伺服器已安裝 IIS
- [ ] 伺服器已安裝 SQL Server
- [ ] 擁有伺服器管理員權限
- [ ] 已準備好資料庫連線字串
- [ ] 已準備好網域名稱（如需要）

---

## 步驟 1：安裝必要元件

### 1.1 安裝 IIS（如果尚未安裝）

#### 使用 PowerShell（管理員權限）
```powershell
# 安裝 IIS 及必要功能
Install-WindowsFeature -name Web-Server -IncludeManagementTools

# 安裝 ASP.NET 支援
Install-WindowsFeature Web-Asp-Net45
```

#### 使用控制台
1. 開啟「控制台」→「程式和功能」→「開啟或關閉 Windows 功能」
2. 勾選「Internet Information Services」
3. 展開並勾選以下項目：
   - Web 管理工具 → IIS 管理主控台
   - World Wide Web 服務 → 應用程式開發功能 → ASP.NET 4.8
   - World Wide Web 服務 → 一般 HTTP 功能（全部）
   - World Wide Web 服務 → 安全性 → 基本驗證、Windows 驗證
4. 點擊「確定」並等待安裝完成

### 1.2 安裝 .NET Hosting Bundle

> ⚠️ **重要**：這是部署 ASP.NET Core 應用程式到 IIS 的必要元件

1. 前往 [.NET 下載頁面](https://dotnet.microsoft.com/download/dotnet/10.0)
2. 找到「Hosting Bundle」區塊
3. 下載「ASP.NET Core Runtime 10.0.x - Windows Hosting Bundle」
4. 執行安裝程式
5. **重新啟動 IIS**：
   ```powershell
   net stop was /y
   net start w3svc
   ```

#### 驗證安裝
```powershell
# 檢查 ASP.NET Core Module 是否已安裝
Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\InetStp\Components' | Where-Object { $_.GetValue("AspNetCoreModule") -eq 1 }
```

### 1.3 安裝 SQL Server（如果尚未安裝）

參考 [START_GUIDE.md](file:///C:/Users/a/Documents/aa/START_GUIDE.md) 中的 SQL Server 安裝步驟。

---

## 步驟 2：發佈應用程式

### 2.1 使用命令列發佈

在開發機器上執行：

```powershell
# 進入專案目錄
cd C:\Users\a\Documents\aa

# 發佈應用程式（Release 模式）
dotnet publish VisitorReg.Web\VisitorReg.Web.csproj -c Release -o C:\Publish\VisitorReg
```

**參數說明：**
- `-c Release`：使用 Release 組態（優化效能）
- `-o C:\Publish\VisitorReg`：輸出目錄

### 2.2 使用 Visual Studio 發佈

1. 在 Visual Studio 中開啟專案
2. 右鍵點擊「VisitorReg.Web」專案
3. 選擇「發佈」
4. 選擇「資料夾」作為目標
5. 設定發佈路徑（例如：`C:\Publish\VisitorReg`）
6. 點擊「發佈」

### 2.3 複製檔案到伺服器

將發佈的檔案從開發機器複製到伺服器：

**方法 1：使用網路共用**
```powershell
# 在伺服器上建立目錄
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\VisitorReg" -Force

# 從開發機器複製檔案
Copy-Item -Path "C:\Publish\VisitorReg\*" -Destination "\\ServerName\C$\inetpub\wwwroot\VisitorReg" -Recurse
```

**方法 2：使用遠端桌面**
1. 連線到伺服器
2. 手動複製發佈資料夾到 `C:\inetpub\wwwroot\VisitorReg`

---

## 步驟 3：設定 IIS

### 3.1 建立應用程式集區

1. 開啟「IIS 管理員」（執行 `inetmgr`）
2. 在左側樹狀結構中，展開伺服器節點
3. 右鍵點擊「應用程式集區」→「新增應用程式集區」
4. 設定如下：
   - **名稱**：`VisitorRegAppPool`
   - **.NET CLR 版本**：選擇「沒有 Managed 程式碼」
   - **Managed 管線模式**：整合式
5. 點擊「確定」

#### 進階設定（建議）

右鍵點擊剛建立的應用程式集區 → 「進階設定」：

| 設定項目 | 建議值 | 說明 |
|---------|--------|------|
| 啟用 32 位元應用程式 | False | 使用 64 位元 |
| 閒置逾時 (分鐘) | 20 | 閒置 20 分鐘後回收 |
| 定期回收時間 (分鐘) | 1740 | 每天回收一次 |
| 身分識別 | ApplicationPoolIdentity | 預設值（最安全） |

### 3.2 建立網站

#### 方法 1：使用 IIS 管理員

1. 在 IIS 管理員中，右鍵點擊「網站」→「新增網站」
2. 設定如下：
   - **網站名稱**：`VisitorReg`
   - **應用程式集區**：選擇 `VisitorRegAppPool`
   - **實體路徑**：`C:\inetpub\wwwroot\VisitorReg`
   - **繫結**：
     - 類型：`http`
     - IP 位址：全部未指定
     - 連接埠：`80`（或其他可用端口，如 `8080`）
     - 主機名稱：（選填，如 `visitor.example.com`）
3. 點擊「確定」

#### 方法 2：使用 PowerShell

```powershell
# 建立應用程式集區
New-WebAppPool -Name "VisitorRegAppPool"
Set-ItemProperty IIS:\AppPools\VisitorRegAppPool -Name managedRuntimeVersion -Value ""

# 建立網站
New-Website -Name "VisitorReg" `
    -PhysicalPath "C:\inetpub\wwwroot\VisitorReg" `
    -ApplicationPool "VisitorRegAppPool" `
    -Port 80
```

### 3.3 設定目錄權限

應用程式集區需要讀取權限：

```powershell
# 授予 IIS_IUSRS 群組讀取權限
icacls "C:\inetpub\wwwroot\VisitorReg" /grant "IIS_IUSRS:(OI)(CI)RX" /T

# 授予應用程式集區身分識別完整權限（如需寫入）
icacls "C:\inetpub\wwwroot\VisitorReg" /grant "IIS APPPOOL\VisitorRegAppPool:(OI)(CI)F" /T
```

---

## 步驟 4：設定資料庫

### 4.1 建立生產環境資料庫

在 SQL Server 上執行：

```sql
-- 建立資料庫
CREATE DATABASE VisitorRegDb_Production;
GO

-- 建立登入帳號（選用，建議使用 Windows 驗證）
CREATE LOGIN VisitorRegUser WITH PASSWORD = 'YourStrongPassword123!';
GO

USE VisitorRegDb_Production;
GO

-- 建立使用者並授予權限
CREATE USER VisitorRegUser FOR LOGIN VisitorRegUser;
GO

ALTER ROLE db_owner ADD MEMBER VisitorRegUser;
GO
```

### 4.2 執行 Migration

在伺服器上執行（或從開發機器遠端執行）：

```powershell
# 方法 1：使用 dotnet ef（需要安裝 .NET SDK）
cd C:\inetpub\wwwroot\VisitorReg
dotnet ef database update --connection "Server=localhost\SQLEXPRESS;Database=VisitorRegDb_Production;Trusted_Connection=True;"

# 方法 2：使用 SQL 腳本（從開發機器生成）
# 在開發機器上執行：
dotnet ef migrations script -o migration.sql --project VisitorReg.Infrastructure --startup-project VisitorReg.Web

# 然後在 SQL Server 上執行 migration.sql
```

---

## 步驟 5：設定應用程式

### 5.1 修改 appsettings.json

編輯 `C:\inetpub\wwwroot\VisitorReg\appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=VisitorRegDb_Production;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> 💡 **提示**：本系統使用 SQL Server Express 和 Windows 驗證。連線字串中的 `Trusted_Connection=True` 表示使用 Windows 驗證。

### 5.2 建立 appsettings.Production.json（建議）

建立 `C:\inetpub\wwwroot\VisitorReg\appsettings.Production.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=VisitorRegDb_Production;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft.AspNetCore": "Error"
    }
  }
}
```

### 5.3 設定環境變數

在 IIS 管理員中：

1. 選擇「VisitorReg」網站
2. 雙擊「設定編輯器」
3. 在「區段」下拉選單中選擇：`system.webServer/aspNetCore`
4. 找到 `environmentVariables` 並點擊「...」
5. 新增環境變數：
   - **名稱**：`ASPNETCORE_ENVIRONMENT`
   - **值**：`Production`
6. 點擊「確定」並套用變更

---

## 步驟 6：測試部署

### 6.1 啟動網站

1. 在 IIS 管理員中，選擇「VisitorReg」網站
2. 點擊右側「管理網站」→「啟動」
3. 確認狀態顯示為「已啟動」

### 6.2 測試存取

開啟瀏覽器，訪問：
```
http://localhost
# 或
http://ServerIP
# 或
http://visitor.example.com
```

### 6.3 檢查應用程式日誌

如果網站無法啟動，檢查日誌：

**ASP.NET Core 日誌位置：**
```
C:\inetpub\wwwroot\VisitorReg\logs\
```

**IIS 日誌位置：**
```
C:\inetpub\logs\LogFiles\
```

**Windows 事件檢視器：**
1. 執行 `eventvwr`
2. 檢查「Windows 記錄檔」→「應用程式」

---

## 進階設定

### 7.1 設定 HTTPS（SSL）

#### 使用自我簽署憑證（測試用）

```powershell
# 建立自我簽署憑證
$cert = New-SelfSignedCertificate -DnsName "visitor.example.com" -CertStoreLocation "cert:\LocalMachine\My"

# 綁定到網站
New-WebBinding -Name "VisitorReg" -Protocol "https" -Port 443
$binding = Get-WebBinding -Name "VisitorReg" -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "my")
```

#### 使用正式憑證

1. 購買 SSL 憑證或使用 Let's Encrypt
2. 在 IIS 管理員中：
   - 選擇伺服器節點
   - 雙擊「伺服器憑證」
   - 匯入憑證
3. 在網站繫結中新增 HTTPS：
   - 類型：`https`
   - 連接埠：`443`
   - SSL 憑證：選擇已匯入的憑證

### 7.2 設定 URL 重寫（HTTP 轉 HTTPS）

1. 安裝 [URL Rewrite 模組](https://www.iis.net/downloads/microsoft/url-rewrite)
2. 在網站根目錄建立或編輯 `web.config`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="HTTP to HTTPS redirect" stopProcessing="true">
          <match url="(.*)" />
          <conditions>
            <add input="{HTTPS}" pattern="off" ignoreCase="true" />
          </conditions>
          <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

### 7.3 設定應用程式集區回收

為避免記憶體洩漏，設定定期回收：

```powershell
# 設定每天凌晨 3:00 回收
Set-ItemProperty IIS:\AppPools\VisitorRegAppPool -Name Recycling.periodicRestart.schedule -Value @{value="03:00:00"}

# 設定記憶體限制（例如：1.5GB）
Set-ItemProperty IIS:\AppPools\VisitorRegAppPool -Name Recycling.periodicRestart.privateMemory -Value 1572864
```

### 7.4 設定壓縮

啟用 Gzip 壓縮以提升效能：

```powershell
# 啟用動態內容壓縮
Set-WebConfigurationProperty -Filter "/system.webServer/httpCompression" -Name "dynamicCompressionEnableCpuUsage" -Value 90 -PSPath "IIS:\Sites\VisitorReg"

# 啟用靜態內容壓縮
Set-WebConfigurationProperty -Filter "/system.webServer/httpCompression" -Name "staticCompressionEnableCpuUsage" -Value 90 -PSPath "IIS:\Sites\VisitorReg"
```

---

## 常見問題排解

### Q1: 網站顯示 500.19 錯誤

**原因**：web.config 設定錯誤或 ASP.NET Core Module 未安裝

**解決方法**：
1. 確認已安裝 .NET Hosting Bundle
2. 重新啟動 IIS：
   ```powershell
   iisreset
   ```

### Q2: 網站顯示 502.5 錯誤

**原因**：應用程式無法啟動

**解決方法**：
1. 檢查應用程式日誌（`logs` 資料夾）
2. 確認 .NET Runtime 版本正確
3. 檢查 `appsettings.json` 設定
4. 確認資料庫連線字串正確

### Q3: 資料庫連線失敗

**錯誤訊息**：`Login failed for user 'IIS APPPOOL\VisitorRegAppPool'`

**解決方法**：

```sql
-- 在 SQL Server 上執行
USE VisitorRegDb_Production;
GO

CREATE USER [IIS APPPOOL\VisitorRegAppPool] FOR LOGIN [IIS APPPOOL\VisitorRegAppPool];
GO

ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\VisitorRegAppPool];
GO
```

### Q4: 靜態檔案無法載入

**解決方法**：

確認 `web.config` 包含靜態檔案處理：

```xml
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\VisitorReg.Web.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

### Q5: 應用程式效能緩慢

**檢查項目**：
1. 應用程式集區是否使用正確的 .NET 版本
2. 是否啟用了壓縮
3. 資料庫索引是否正確
4. 檢查記憶體和 CPU 使用率

---

## 更新部署

當需要更新應用程式時：

```powershell
# 1. 停止網站
Stop-Website -Name "VisitorReg"

# 2. 停止應用程式集區
Stop-WebAppPool -Name "VisitorRegAppPool"

# 3. 備份現有檔案
Copy-Item -Path "C:\inetpub\wwwroot\VisitorReg" -Destination "C:\Backup\VisitorReg_$(Get-Date -Format 'yyyyMMdd_HHmmss')" -Recurse

# 4. 複製新檔案
Copy-Item -Path "C:\Publish\VisitorReg\*" -Destination "C:\inetpub\wwwroot\VisitorReg" -Recurse -Force

# 5. 執行資料庫 Migration（如有需要）
# dotnet ef database update

# 6. 啟動應用程式集區
Start-WebAppPool -Name "VisitorRegAppPool"

# 7. 啟動網站
Start-Website -Name "VisitorReg"
```

---

## 監控與維護

### 啟用應用程式日誌

在 `appsettings.Production.json` 中：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "File": {
      "Path": "logs/app-.log",
      "RollingInterval": "Day"
    }
  }
}
```

### 設定效能監控

使用 Windows Performance Monitor：
1. 執行 `perfmon`
2. 新增計數器：
   - `.NET CLR Memory` → `# Bytes in all Heaps`
   - `Process` → `% Processor Time`
   - `Web Service` → `Current Connections`

---

## 安全性建議

1. **使用 HTTPS**：強制所有連線使用 HTTPS
2. **定期更新**：保持 .NET Runtime 和 Windows 更新
3. **最小權限原則**：應用程式集區使用最小必要權限
4. **防火牆設定**：只開放必要的端口（80, 443）
5. **資料庫安全**：使用強密碼，限制資料庫存取權限
6. **備份策略**：定期備份資料庫和應用程式檔案

---

**最後更新：** 2026-02-05  
**版本：** 1.1  
**專案：** 訪客登記系統

---

## 🚀 一鍵部署腳本

> ⚠️ **重要提示**  
> 使用此腳本前，請確認已完成以下準備：
> 1. 已安裝 IIS 和 .NET Hosting Bundle
> 2. 已安裝 SQL Server
> 3. 以**系統管理員權限**執行 PowerShell

### 完整自動化部署腳本

將以下腳本儲存為 `Deploy-VisitorReg.ps1`，然後以管理員權限執行：

```powershell
<#
.SYNOPSIS
    訪客登記系統 - IIS 一鍵部署腳本
.DESCRIPTION
    自動化部署訪客登記系統到 IIS，包含發佈、IIS 設定、資料庫 Migration 等所有步驟
.PARAMETER SourcePath
    專案原始碼路徑
.PARAMETER PublishPath
    發佈輸出路徑
.PARAMETER WebsitePath
    IIS 網站實體路徑
.PARAMETER DatabaseServer
    SQL Server 伺服器名稱
.PARAMETER DatabaseName
    資料庫名稱
.EXAMPLE
    .\Deploy-VisitorReg.ps1
    使用預設參數執行部署
.EXAMPLE
    .\Deploy-VisitorReg.ps1 -SourcePath "D:\Projects\aa" -DatabaseServer "localhost\SQLEXPRESS"
    使用自訂參數執行部署
#>

param(
    [string]$SourcePath = "C:\Users\a\Documents\aa",
    [string]$PublishPath = "C:\Publish\VisitorReg",
    [string]$WebsitePath = "C:\inetpub\wwwroot\VisitorReg",
    [string]$AppPoolName = "VisitorRegAppPool",
    [string]$WebsiteName = "VisitorReg",
    [int]$Port = 80,
    [string]$DatabaseServer = "localhost\SQLEXPRESS",
    [string]$DatabaseName = "VisitorRegDb_Production",
    [switch]$SkipDatabase,
    [switch]$BackupExisting
)

# 設定錯誤處理
$ErrorActionPreference = "Stop"

# 顏色輸出函數
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host ">>> $Message" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" "Green"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" "Red"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" "Yellow"
}

try {
    Write-ColorOutput "`n
╔═══════════════════════════════════════════════════════════╗
║         訪客登記系統 - IIS 一鍵部署腳本                 ║
║                     Version 1.0                           ║
╚═══════════════════════════════════════════════════════════╝
" "Cyan"

    # ====================================
    # 步驟 1：檢查必要條件
    # ====================================
    Write-Step "步驟 1/7：檢查部署必要條件"
    
    # 檢查是否為管理員
    if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        Write-Error "此腳本需要系統管理員權限！請以管理員身分執行 PowerShell。"
        exit 1
    }
    Write-Success "管理員權限確認"

    # 檢查專案路徑
    if (-not (Test-Path $SourcePath)) {
        Write-Error "找不到專案路徑: $SourcePath"
        exit 1
    }
    Write-Success "專案路徑存在: $SourcePath"

    # 檢查 .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK 已安裝: $dotnetVersion"
    } catch {
        Write-Error ".NET SDK 未安裝！請先安裝 .NET SDK。"
        exit 1
    }

    # 檢查 IIS
    if (-not (Get-WindowsFeature -Name Web-Server -ErrorAction SilentlyContinue)) {
        Write-Warning "IIS 未安裝，正在安裝..."
        Install-WindowsFeature -name Web-Server -IncludeManagementTools
        Write-Success "IIS 安裝完成"
    } else {
        Write-Success "IIS 已安裝"
    }

    # ====================================
    # 步驟 2：發佈應用程式
    # ====================================
    Write-Step "步驟 2/7：發佈應用程式"
    
    # 清理並建立發佈目錄
    if (Test-Path $PublishPath) {
        Write-Warning "清理舊的發佈目錄..."
        Remove-Item $PublishPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
    Write-Success "發佈目錄已建立: $PublishPath"

    # 發佈專案
    Write-ColorOutput "正在發佈專案（這可能需要幾分鐘）..." "Yellow"
    Push-Location $SourcePath
    dotnet publish VisitorReg.Web\VisitorReg.Web.csproj -c Release -o $PublishPath --verbosity quiet
    Pop-Location
    Write-Success "應用程式發佈完成"

    # ====================================
    # 步驟 3：停止現有服務（如果存在）
    # ====================================
    Write-Step "步驟 3/7：停止現有服務"
    
    if (Get-Website -Name $WebsiteName -ErrorAction SilentlyContinue) {
        Write-Warning "停止現有網站..."
        Stop-Website -Name $WebsiteName -ErrorAction SilentlyContinue
        Write-Success "網站已停止"
    }

    if (Get-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Write-Warning "停止現有應用程式集區..."
        Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Success "應用程式集區已停止"
    }

    # ====================================
    # 步驟 4：備份與複製檔案
    # ====================================
    Write-Step "步驟 4/7：部署應用程式檔案"
    
    # 備份現有檔案
    if ($BackupExisting -and (Test-Path $WebsitePath)) {
        $backupPath = "C:\Backup\VisitorReg_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Write-Warning "備份現有檔案到: $backupPath"
        Copy-Item -Path $WebsitePath -Destination $backupPath -Recurse
        Write-Success "備份完成"
    }

    # 建立網站目錄
    if (Test-Path $WebsitePath) {
        Remove-Item $WebsitePath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $WebsitePath -Force | Out-Null
    Write-Success "網站目錄已建立: $WebsitePath"

    # 複製發佈檔案
    Write-ColorOutput "正在複製檔案..." "Yellow"
    Copy-Item -Path "$PublishPath\*" -Destination $WebsitePath -Recurse -Force
    Write-Success "檔案複製完成"

    # 建立日誌目錄
    $logsPath = Join-Path $WebsitePath "logs"
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Success "日誌目錄已建立"

    # ====================================
    # 步驟 5：設定 IIS
    # ====================================
    Write-Step "步驟 5/7：設定 IIS"
    
    # 匯入 WebAdministration 模組
    Import-Module WebAdministration

    # 建立應用程式集區
    if (Get-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Write-Warning "移除現有應用程式集區..."
        Remove-WebAppPool -Name $AppPoolName
    }
    
    Write-ColorOutput "建立應用程式集區..." "Yellow"
    New-WebAppPool -Name $AppPoolName | Out-Null
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
    Write-Success "應用程式集區已建立: $AppPoolName"

    # 建立網站
    if (Get-Website -Name $WebsiteName -ErrorAction SilentlyContinue) {
        Write-Warning "移除現有網站..."
        Remove-Website -Name $WebsiteName
    }
    
    Write-ColorOutput "建立 IIS 網站..." "Yellow"
    New-Website -Name $WebsiteName `
        -PhysicalPath $WebsitePath `
        -ApplicationPool $AppPoolName `
        -Port $Port `
        -Force | Out-Null
    Write-Success "網站已建立: $WebsiteName"

    # 設定權限
    Write-ColorOutput "設定目錄權限..." "Yellow"
    icacls $WebsitePath /grant "IIS_IUSRS:(OI)(CI)RX" /T /Q | Out-Null
    icacls $WebsitePath /grant "IIS APPPOOL\$AppPoolName:(OI)(CI)F" /T /Q | Out-Null
    Write-Success "權限設定完成"

    # ====================================
    # 步驟 6：設定資料庫
    # ====================================
    if (-not $SkipDatabase) {
        Write-Step "步驟 6/7：設定資料庫"
        
        # 修改連線字串
        $connectionString = "Server=$DatabaseServer;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        
        $appSettingsPath = Join-Path $WebsitePath "appsettings.json"
        if (Test-Path $appSettingsPath) {
            Write-ColorOutput "更新 appsettings.json..." "Yellow"
            $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
            $appSettings.ConnectionStrings.DefaultConnection = $connectionString
            $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
            Write-Success "連線字串已更新"
        }

        # 建立 appsettings.Production.json
        $prodSettingsPath = Join-Path $WebsitePath "appsettings.Production.json"
        $prodSettings = @{
            ConnectionStrings = @{
                DefaultConnection = $connectionString
            }
            Logging = @{
                LogLevel = @{
                    Default = "Error"
                    "Microsoft.AspNetCore" = "Error"
                }
            }
        }
        $prodSettings | ConvertTo-Json -Depth 10 | Set-Content $prodSettingsPath
        Write-Success "appsettings.Production.json 已建立"

        # 執行 Migration
        Write-ColorOutput "執行資料庫 Migration..." "Yellow"
        try {
            Push-Location $SourcePath
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
            
            # 檢查 dotnet-ef 工具
            $efVersion = dotnet ef --version 2>$null
            if (-not $efVersion) {
                Write-Warning "安裝 Entity Framework Core 工具..."
                dotnet tool install --global dotnet-ef
                $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
            }
            
            dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web --connection $connectionString
            Pop-Location
            Write-Success "資料庫 Migration 完成"
        } catch {
            Pop-Location
            Write-Warning "資料庫 Migration 失敗: $_"
            Write-Warning "請手動執行 Migration 或檢查資料庫連線"
        }
    } else {
        Write-Step "步驟 6/7：跳過資料庫設定（SkipDatabase 參數已啟用）"
    }

    # ====================================
    # 步驟 7：啟動服務並測試
    # ====================================
    Write-Step "步驟 7/7：啟動服務並測試"
    
    # 啟動應用程式集區
    Start-WebAppPool -Name $AppPoolName
    Write-Success "應用程式集區已啟動"

    # 等待應用程式集區啟動
    Start-Sleep -Seconds 2

    # 啟動網站
    Start-Website -Name $WebsiteName
    Write-Success "網站已啟動"

    # 等待網站啟動
    Start-Sleep -Seconds 3

    # 測試網站
    Write-ColorOutput "`n正在測試網站..." "Yellow"
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Success "網站測試成功！HTTP 狀態碼: 200"
        } else {
            Write-Warning "網站回應異常。HTTP 狀態碼: $($response.StatusCode)"
        }
    } catch {
        Write-Warning "無法連線到網站。請檢查 IIS 設定和應用程式日誌。"
    }

    # ====================================
    # 部署完成
    # ====================================
    Write-ColorOutput "`n
╔═══════════════════════════════════════════════════════════╗
║                    部署成功完成！                        ║
╚═══════════════════════════════════════════════════════════╝
" "Green"

    Write-ColorOutput "`n部署資訊：" "Cyan"
    Write-ColorOutput "  網站名稱：$WebsiteName" "White"
    Write-ColorOutput "  應用程式集區：$AppPoolName" "White"
    Write-ColorOutput "  網站路徑：$WebsitePath" "White"
    Write-ColorOutput "  存取 URL：http://localhost:$Port" "White"
    Write-ColorOutput "  資料庫：$DatabaseServer\$DatabaseName" "White"

    Write-ColorOutput "`n下一步：" "Cyan"
    Write-ColorOutput "  1. 在瀏覽器中開啟：http://localhost:$Port" "White"
    Write-ColorOutput "  2. 註冊新使用者帳號" "White"
    Write-ColorOutput "  3. 開始使用訪客登記系統" "White"

    Write-ColorOutput "`n日誌位置：" "Cyan"
    Write-ColorOutput "  應用程式日誌：$WebsitePath\logs" "White"
    Write-ColorOutput "  IIS 日誌：C:\inetpub\logs\LogFiles" "White"

    # 詢問是否開啟瀏覽器
    $openBrowser = Read-Host "`n是否要開啟瀏覽器測試網站？(Y/N)"
    if ($openBrowser -eq 'Y' -or $openBrowser -eq 'y') {
        Start-Process "http://localhost:$Port"
    }

} catch {
    Write-ColorOutput "`n
╔═══════════════════════════════════════════════════════════╗
║                    部署失敗！                            ║
╚═══════════════════════════════════════════════════════════╝
" "Red"
    
    Write-Error "錯誤訊息：$($_.Exception.Message)"
    Write-ColorOutput "`n錯誤詳情：" "Red"
    Write-ColorOutput $_.Exception.ToString() "Red"
    
    Write-ColorOutput "`n建議檢查：" "Yellow"
    Write-ColorOutput "  1. 確認已安裝 .NET SDK 和 .NET Hosting Bundle" "White"
    Write-ColorOutput "  2. 確認 SQL Server 正在執行" "White"
    Write-ColorOutput "  3. 檢查防火牆和端口是否被佔用" "White"
    Write-ColorOutput "  4. 查看詳細錯誤日誌" "White"
    
    exit 1
}
```

---

### 使用方式

#### 基本使用（使用預設參數）

```powershell
# 以系統管理員身分執行 PowerShell，然後執行：
.\Deploy-VisitorReg.ps1
```

#### 自訂參數使用

```powershell
# 指定專案路徑和資料庫伺服器
.\Deploy-VisitorReg.ps1 `
    -SourcePath "D:\Projects\VisitorReg" `
    -DatabaseServer "localhost\SQLEXPRESS" `
    -Port 8080

# 跳過資料庫設定（手動執行 Migration）
.\Deploy-VisitorReg.ps1 -SkipDatabase

# 備份現有部署
.\Deploy-VisitorReg.ps1 -BackupExisting

# 完整自訂參數
.\Deploy-VisitorReg.ps1 `
    -SourcePath "D:\Projects\VisitorReg" `
    -PublishPath "D:\Publish\VisitorReg" `
    -WebsitePath "C:\WebApps\VisitorReg" `
    -AppPoolName "MyVisitorRegPool" `
    -WebsiteName "MyVisitorReg" `
    -Port 8080 `
    -DatabaseServer "localhost\SQLEXPRESS" `
    -DatabaseName "VisitorRegDb" `
    -BackupExisting
```

---

### 腳本參數說明

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `-SourcePath` | `C:\Users\a\Documents\aa` | 專案原始碼路徑 |
| `-PublishPath` | `C:\Publish\VisitorReg` | 發佈輸出路徑 |
| `-WebsitePath` | `C:\inetpub\wwwroot\VisitorReg` | IIS 網站實體路徑 |
| `-AppPoolName` | `VisitorRegAppPool` | 應用程式集區名稱 |
| `-WebsiteName` | `VisitorReg` | 網站名稱 |
| `-Port` | `80` | 網站監聽端口 |
| `-DatabaseServer` | `localhost\SQLEXPRESS` | SQL Server 伺服器 |
| `-DatabaseName` | `VisitorRegDb_Production` | 資料庫名稱 |
| `-SkipDatabase` | (開關) | 跳過資料庫設定 |
| `-BackupExisting` | (開關) | 備份現有部署 |

---

### 腳本執行流程

腳本會自動執行以下步驟：

1. ✅ **檢查必要條件**
   - 確認管理員權限
   - 驗證 .NET SDK 安裝
   - 檢查 IIS 安裝狀態

2. ✅ **發佈應用程式**
   - 清理舊的發佈目錄
   - 執行 `dotnet publish` 
   - 產生 Release 版本

3. ✅ **停止現有服務**
   - 停止現有網站
   - 停止現有應用程式集區

4. ✅ **部署檔案**
   - 備份現有檔案（如啟用）
   - 複製發佈檔案到網站目錄
   - 建立日誌目錄

5. ✅ **設定 IIS**
   - 建立應用程式集區（無 Managed 程式碼）
   - 建立網站並繫結端口
   - 設定目錄權限

6. ✅ **設定資料庫**
   - 更新連線字串
   - 建立 Production 設定檔
   - 執行 EF Core Migration

7. ✅ **啟動與測試**
   - 啟動應用程式集區
   - 啟動網站
   - 測試 HTTP 連線

---

### 疑難排解

#### Q: 腳本執行失敗，顯示「無法執行，因為已停用指令碼執行」

**解決方法：**
```powershell
# 暫時允許執行腳本（目前 PowerShell 視窗）
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

# 然後再次執行部署腳本
.\Deploy-VisitorReg.ps1
```

#### Q: 資料庫 Migration 失敗

**解決方法：**
```powershell
# 跳過資料庫設定，手動執行 Migration
.\Deploy-VisitorReg.ps1 -SkipDatabase

# 然後手動執行 Migration
cd C:\Users\a\Documents\aa
dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web
```

#### Q: 端口 80 已被佔用

**解決方法：**
```powershell
# 使用不同端口
.\Deploy-VisitorReg.ps1 -Port 8080
```

#### Q: 網站顯示 502.5 錯誤

**解決方法：**
1. 檢查應用程式日誌：`C:\inetpub\wwwroot\VisitorReg\logs`
2. 確認 .NET Hosting Bundle 已安裝
3. 重新啟動 IIS：`iisreset`

---

### 快速指令參考

```powershell
# ==========================================
# 一鍵完整部署（預設設定）
# ==========================================
.\Deploy-VisitorReg.ps1

# ==========================================
# 使用 SQL Server Express
# ==========================================
.\Deploy-VisitorReg.ps1 -DatabaseServer "localhost\SQLEXPRESS"

# ==========================================
# 更新部署（保留資料庫）
# ==========================================
.\Deploy-VisitorReg.ps1 -SkipDatabase -BackupExisting

# ==========================================
# 重新部署（包含資料庫）
# ==========================================
.\Deploy-VisitorReg.ps1 -BackupExisting

# ==========================================
# 檢查網站狀態
# ==========================================
Get-Website -Name "VisitorReg"
Get-WebAppPool -Name "VisitorRegAppPool"

# ==========================================
# 手動啟動/停止
# ==========================================
Start-Website -Name "VisitorReg"
Stop-Website -Name "VisitorReg"
Start-WebAppPool -Name "VisitorRegAppPool"
Stop-WebAppPool -Name "VisitorRegAppPool"

# ==========================================
# 查看日誌
# ==========================================
Get-Content "C:\inetpub\wwwroot\VisitorReg\logs\*.log" -Tail 50

# ==========================================
# 重新啟動 IIS
# ==========================================
iisreset
```

---

### 建議的部署工作流程

1. **首次部署**
   ```powershell
   .\Deploy-VisitorReg.ps1 -BackupExisting
   ```

2. **更新應用程式（保留資料庫）**
   ```powershell
   .\Deploy-VisitorReg.ps1 -SkipDatabase -BackupExisting
   ```

3. **完整重新部署（包含資料庫重建）**
   ```powershell
   .\Deploy-VisitorReg.ps1 -BackupExisting
   ```

4. **驗證部署**
   - 開啟瀏覽器訪問 http://localhost
   - 註冊新帳號測試
   - 檢查應用程式日誌

---

**提示：** 建議將此腳本加入版本控制，並根據實際環境調整預設參數！

