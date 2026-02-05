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
dotnet ef database update --connection "Server=localhost;Database=VisitorRegDb_Production;Trusted_Connection=True;"

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
    "DefaultConnection": "Server=localhost;Database=VisitorRegDb_Production;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
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

> 💡 **提示**：如果使用 SQL Server 驗證，連線字串改為：
> ```
> Server=localhost;Database=VisitorRegDb_Production;User Id=VisitorRegUser;Password=YourStrongPassword123!;MultipleActiveResultSets=true;TrustServerCertificate=True
> ```

### 5.2 建立 appsettings.Production.json（建議）

建立 `C:\inetpub\wwwroot\VisitorReg\appsettings.Production.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VisitorRegDb_Production;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
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
**版本：** 1.0  
**專案：** 訪客登記系統
