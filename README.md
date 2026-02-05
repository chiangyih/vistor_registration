# 訪客登記系統 - 部署與執行指南

## 系統需求

- .NET 10 SDK
- SQL Server Express 2022 或更高版本
- Windows 10/11 或 Windows Server

## 資料庫設定

### 方法一：使用 SQL Server Express

1. **安裝 SQL Server Express**
   - 下載：https://www.microsoft.com/zh-tw/sql-server/sql-server-downloads
   - 選擇「Express」版本
   - 安裝時選擇「基本」安裝類型

2. **啟動 SQL Server 服務**
   ```powershell
   # 檢查服務狀態
   Get-Service -Name "MSSQL$SQLEXPRESS"
   
   # 啟動服務（如果未執行）
   Start-Service -Name "MSSQL$SQLEXPRESS"
   ```

3. **建立資料庫**
   ```bash
   cd c:\Users\tseng\Documents\vistor_register
   dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web
   ```

### 方法二：使用 LocalDB（開發環境）

如果您已安裝 Visual Studio，可以使用 LocalDB：

1. **修改連線字串**
   
   編輯 `VisitorReg.Web\appsettings.json`：
   ```json
   {
     "ConnectionStrings": {
       "VisitorDb": "Server=(localdb)\\mssqllocaldb;Database=VisitorRegDb;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

2. **建立資料庫**
   ```bash
   dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web
   ```

## 執行應用程式

### 開發模式

```bash
cd c:\Users\tseng\Documents\vistor_register\VisitorReg.Web
dotnet run
```

應用程式將在 `https://localhost:5001` 啟動。

### 發布模式

```bash
# 發布應用程式
dotnet publish VisitorReg.Web -c Release -o ./publish

# 執行發布的應用程式
cd publish
dotnet VisitorReg.Web.dll
```

## 初始設定

### 建立管理員帳號

首次執行時，請先註冊一個帳號：

1. 啟動應用程式
2. 點擊右上角「註冊」
3. 填寫帳號資訊並註冊
4. 登入後即可使用系統

### 資料庫初始化（選用）

如果需要建立測試資料，可以執行以下 SQL：

```sql
USE VisitorRegDb;

-- 插入測試訪客資料
INSERT INTO Visitors (Id, RegisterNo, Name, Company, Purpose, HostName, CheckInAt, Status, CreatedAt, CreatedBy)
VALUES 
(NEWID(), 'V202602040001', '張三', '測試公司', '業務洽談', '李四', GETDATE(), 0, GETDATE(), 'System'),
(NEWID(), 'V202602040002', '王五', 'ABC科技', '技術交流', '趙六', GETDATE(), 0, GETDATE(), 'System');
```

## 功能說明

### 1. 訪客登記
- 路徑：`/Visitors/Create`
- 功能：建立新的訪客登記資料
- 必填欄位：訪客姓名、來訪目的、受訪者、到訪時間

### 2. 查詢訪客
- 路徑：`/Visitors/Index`
- 功能：查詢與篩選訪客資料
- 支援：日期區間、姓名、公司、受訪者、狀態篩選
- 分頁：每頁顯示 20 筆資料

### 3. 訪客詳細資料
- 路徑：`/Visitors/Detail/{id}`
- 功能：檢視訪客完整資訊

### 4. 離場登記
- 路徑：`/Visitors/Checkout/{id}`
- 功能：記錄訪客離場時間
- 限制：僅「在場」狀態的訪客可進行離場登記

## 安全性功能

### 證件號碼遮罩
- 系統自動將證件號碼遮罩儲存
- 格式：保留前 3 碼和後 3 碼，中間以 * 取代
- 範例：A123456789 → A12****789

### 稽核日誌
- 所有重要操作都會記錄在 `AuditLogs` 資料表
- 記錄內容：操作者、動作、目標、時間、IP 位址、結果

### 身分驗證
- 使用 ASP.NET Core Identity
- 密碼政策：
  - 最少 8 個字元
  - 需包含大小寫字母、數字與特殊字元
  - 登入失敗 5 次後鎖定 15 分鐘

## 疑難排解

### 問題：無法連線到資料庫

**解決方案：**
1. 確認 SQL Server 服務已啟動
2. 檢查連線字串是否正確
3. 確認防火牆設定允許 SQL Server 連線

### 問題：Migration 執行失敗

**解決方案：**
```bash
# 移除現有 Migration
dotnet ef migrations remove --project VisitorReg.Infrastructure --startup-project VisitorReg.Web

# 重新建立 Migration
dotnet ef migrations add InitialCreate --project VisitorReg.Infrastructure --startup-project VisitorReg.Web

# 更新資料庫
dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web
```

### 問題：頁面顯示錯誤

**解決方案：**
1. 檢查瀏覽器控制台的錯誤訊息
2. 查看應用程式日誌
3. 確認所有 NuGet 套件已正確安裝

## 技術架構

### 分層架構
- **Domain Layer**：實體、列舉、值物件
- **Infrastructure Layer**：資料存取、DbContext、Repository
- **Application Layer**：Use Cases、DTO、業務邏輯
- **Presentation Layer**：Razor Pages、UI

### 主要技術
- ASP.NET Core 10 Razor Pages
- Entity Framework Core 10
- ASP.NET Core Identity
- SQL Server
- Bootstrap 5
- Bootstrap Icons

## 聯絡資訊

如有問題，請聯絡：
- 學校：國立新化高工資訊科
- 專案：訪客登記系統
