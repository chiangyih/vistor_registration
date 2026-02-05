# 訪客登記系統 - 專案啟動指南

## 📋 目錄
- [環境需求](#環境需求)
- [快速啟動](#快速啟動)
- [詳細安裝步驟](#詳細安裝步驟)
- [啟動應用程式](#啟動應用程式)
- [存取應用程式](#存取應用程式)
- [常見問題](#常見問題)

---

## 環境需求

### 必要軟體
- **.NET SDK 10.0** 或更新版本（或 .NET 8 LTS）
- **SQL Server Express 2022** 或更新版本
- **Windows 10/11** 作業系統

---

## 快速啟動

```powershell
# 1. 進入專案目錄
cd C:\Users\a\Documents\aa

# 2. 還原套件
dotnet restore

# 3. 執行資料庫 Migration
dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web

# 4. 啟動應用程式
cd VisitorReg.Web
dotnet run
```

然後開啟瀏覽器訪問：**http://localhost:5033**

---

## 詳細安裝步驟

### 步驟 1：安裝 .NET SDK

1. 前往 [.NET 下載頁面](https://dotnet.microsoft.com/download)
2. 下載「SDK」版本（Windows x64）
3. 執行安裝程式並完成安裝
4. 驗證安裝：
   ```powershell
   dotnet --version
   ```

### 步驟 2：安裝 SQL Server

1. 前往 [SQL Server Express 下載頁面](https://www.microsoft.com/sql-server/sql-server-downloads)
2. 下載「Express」版本
3. 執行安裝程式，選擇「基本」安裝

### 步驟 3：安裝 EF Core 工具

```powershell
dotnet tool install --global dotnet-ef
```

---

## 啟動應用程式

### 方法 1：使用命令列

```powershell
# 1. 還原套件
cd C:\Users\a\Documents\aa
dotnet restore

# 2. 建置專案
dotnet build

# 3. 執行資料庫 Migration
dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web

# 4. 啟動應用程式
cd VisitorReg.Web
dotnet run
```

### 方法 2：背景執行

```powershell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\a\Documents\aa\VisitorReg.Web; dotnet run"
```

---

## 存取應用程式

### 應用程式 URL

- **HTTP**: http://localhost:5033
- **HTTPS**: https://localhost:7006

### 首次使用

1. 開啟瀏覽器訪問 http://localhost:5033
2. 點擊「註冊」建立新帳號
3. 密碼需符合政策：至少 8 字元，包含大小寫、數字和特殊字元
4. 登入後即可使用系統

---

## 常見問題

### Q1: 找不到 dotnet 命令

```powershell
# 重新載入環境變數
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
dotnet --version
```

### Q2: 資料庫連線失敗

檢查 `appsettings.json` 的連線字串：
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VisitorRegDb;Trusted_Connection=True"
}
```

如果使用 SQL Server Express：
```json
"Server=localhost\\SQLEXPRESS;Database=VisitorRegDb;Trusted_Connection=True"
```

### Q3: 端口已被佔用

```powershell
# 找出佔用端口的程序
netstat -ano | findstr "5033"

# 停止該程序
Stop-Process -Id <PID> -Force
```

---

## 停止應用程式

在終端機中按 `Ctrl + C`

或使用 PowerShell：
```powershell
Stop-Process -Name "VisitorReg.Web" -Force
```

---

## 執行測試

```powershell
# 執行所有測試
dotnet test

# 顯示詳細輸出
dotnet test --verbosity normal
```

---

**最後更新：** 2026-02-05  
**專案：** 訪客登記系統
