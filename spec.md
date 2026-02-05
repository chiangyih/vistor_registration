# 訪客登記系統 — SDD（Software Design Description）規格書（優化版）

> 版本：v2.0  
> 最後更新：2026-02-04  
> 適用專案：訪客登記系統（Razor Pages）  
> 目標平台：ASP.NET Core（建議 .NET 10；若組織尚未導入，請以現行可用 LTS 版本落地，並保留升級路徑）  

---

## 1. 文件目的

本文件用於描述「訪客登記系統」之**軟體設計**（非需求書），包含：
- 架構與模組切分
- 資料模型與資料存取策略
- 安全設計（輸入驗證、身分鑑別、授權、稽核）
- 錯誤處理、日誌、部署與測試策略

**不在本文件範圍：**UI 視覺稿/品牌規範、硬體門禁整合、跨系統 SSO 方案的組織級決策。

---

## 2. 系統概述

### 2.1 目標
- 提供櫃台/前台人員快速建立訪客登記資料
- 支援查詢與基本報表輸出（可選）
- 以「最小可用、易維護、可稽核、可強化資安」為設計原則

### 2.2 主要角色
- **前台人員**：建立、查詢、更新（離場）訪客資料
- **管理者**：帳號管理、系統設定、稽核查閱（可選）
- **稽核/資安人員**：檢視稽核軌跡、日誌與異常事件（只讀）

### 2.3 設計原則
- **清楚分層**：UI / Application / Domain / Infrastructure
- **低耦合**：依賴反轉（DI）、Repository/Unit of Work（以 EF Core 為核心）
- **可測試**：Application 層可用 InMemory/SQLite 進行測試
- **安全預設**：輸入驗證、最小權限、可追溯（Audit-ready）

---

## 3. 技術與環境需求

| 類別 | 規格 |
|---|---|
| 開發工具 | Visual Studio 2026（或同等 IDE）、Git |
| 後端框架 | ASP.NET Core Razor Pages |
| 語言 | C#（依實際 SDK；建議採用目前組織標準版本） |
| ORM | Entity Framework Core（對應目標 .NET 版本） |
| DB | SQL Server Express 2022（開發/小型部署）；正式環境可升級 SQL Server Standard/Enterprise |
| OS | Windows Server / Windows 11（開發） |
| Web Server | Kestrel + IIS Reverse Proxy（建議）或純 Kestrel（內網） |

> **備註**：文件不綁死「.NET 10 LTS 發布時間」等時程敘述，請以組織實際可用版本與 Microsoft 官方公告為準。

---

## 4. 高階架構

### 4.1 分層架構

```
[Presentation]
  Razor Pages (.cshtml/.cshtml.cs)
        |
        v
[Application]
  Use Cases / Services / DTO / Validation
        |
        v
[Domain]
  Entities / Value Objects / Domain Rules
        |
        v
[Infrastructure]
  EF Core DbContext / Repositories / Logging / Security Helpers
        |
        v
[Database]
  SQL Server
```

### 4.2 模組切分（建議專案結構）

```
VisitorReg.Web            # Razor Pages, Filters, Auth, DI
VisitorReg.Application    # Use cases, DTO, Validators, Interfaces
VisitorReg.Domain         # Entities, Enums, Domain rules
VisitorReg.Infrastructure # EF Core, Repo implementations, Logging, Security
VisitorReg.Tests          # Unit/Integration tests
```

---

## 5. 功能模組設計

> 本節為「設計視角」：以用例（Use Case）描述與 UI Page 的對應，便於後續測試與維護。

### 5.1 訪客登記（Create）
- **輸入**：姓名、身分證/證件號（可選遮罩/雜湊）、公司/單位、來訪目的、受訪者、到訪時間、聯絡方式（可選）、備註（可選）
- **處理**：
  - 欄位格式驗證（長度、正則、字元集）
  - 去除前後空白、標準化（Normalization）
  - 生成登記編號（序號或 GUID）
  - 寫入 DB 並產生稽核事件
- **輸出**：登記成功頁（含登記編號）、可列印/匯出（可選）

### 5.2 訪客查詢（Read/List）
- **查詢條件**：日期區間、姓名、公司、受訪者、狀態（在場/已離場）
- **處理**：分頁、排序、輸入防注入、查詢速率限制（避免暴力枚舉）
- **輸出**：清單 + 詳細檢視

### 5.3 離場登記（Update/Checkout）
- **輸入**：登記編號（或查詢後選取）、離場時間
- **處理**：狀態轉移檢查（未離場 -> 已離場）、併發控制
- **輸出**：更新成功，留存稽核紀錄

### 5.4 管理與設定（可選）
- 帳號/角色（Admin/FrontDesk/Auditor）
- 系統參數（保留期限、密碼政策、匯出開關）

---

## 6. 資料設計

### 6.1 資料表（建議）

#### 6.1.1 Visitors（訪客主表）
| 欄位 | 型別 | 必填 | 說明 |
|---|---|---:|---|
| Id | uniqueidentifier | Y | 主鍵（GUID） |
| RegisterNo | nvarchar(30) | Y | 登記編號（可人讀） |
| Name | nvarchar(80) | Y | 訪客姓名 |
| IdNumberMasked | nvarchar(30) | N | 證件號遮罩（如 A12****789） |
| Company | nvarchar(120) | N | 公司/單位 |
| Purpose | nvarchar(200) | Y | 來訪目的 |
| HostName | nvarchar(80) | Y | 受訪者 |
| CheckInAt | datetime2 | Y | 到訪時間 |
| CheckOutAt | datetime2 | N | 離場時間 |
| Phone | nvarchar(30) | N | 聯絡方式（可選） |
| Note | nvarchar(400) | N | 備註 |
| Status | tinyint | Y | 0=在場,1=已離場,2=作廢 |
| CreatedAt/CreatedBy | datetime2/nvarchar(80) | Y | 建立資訊 |
| UpdatedAt/UpdatedBy | datetime2/nvarchar(80) | N | 更新資訊 |
| RowVersion | rowversion | Y | 併發控制 |

#### 6.1.2 AuditLogs（稽核事件）
| 欄位 | 型別 | 必填 | 說明 |
|---|---|---:|---|
| Id | bigint | Y | Identity |
| OccurredAt | datetime2 | Y | 事件時間 |
| Actor | nvarchar(80) | Y | 操作者（帳號/AD） |
| Action | nvarchar(40) | Y | CREATE/UPDATE/DELETE/EXPORT/LOGIN 等 |
| TargetType | nvarchar(40) | Y | Visitors / Users / Settings |
| TargetId | nvarchar(64) | N | 目標主鍵 |
| Result | nvarchar(20) | Y | SUCCESS/FAIL |
| Detail | nvarchar(max) | N | 事件細節（避免存放敏感原文） |
| Ip | nvarchar(45) | N | IPv4/IPv6 |

### 6.2 索引建議
- Visitors(CheckInAt), Visitors(Status), Visitors(RegisterNo) Unique
- 查詢常用欄位可加複合索引： (CheckInAt, Status), (HostName, CheckInAt)

### 6.3 個資與保存
- **最小化**：若非必要，避免存放完整證件號；以遮罩/雜湊取代。
- **保存期限**：以組織政策（例如 90/180/365 天）設定；到期批次清理並留存清理稽核。
- **匯出**：匯出檔案視同個資；需角色限制與匯出稽核。

---

## 7. 安全設計

### 7.1 身分鑑別與授權
- **建議**：Windows/AD 整合（內網）或 ASP.NET Core Identity（獨立帳密）
- **角色**：FrontDesk（CRUD 基本）、Admin（管理）、Auditor（只讀稽核/報表）
- **最小權限**：預設使用者不可匯出，需 Admin 明確授權

### 7.2 輸入驗證與防護
- **Server-side validation** 為主（DataAnnotations/FluentValidation 皆可）
- **XSS**：Razor 預設 HTML encode；仍需限制可輸入字元集，避免「儲存型 XSS」的可疑內容落 DB
- **SQL Injection**：EF Core 參數化查詢；禁止拼接 raw SQL（除非使用 FromSqlInterpolated 且經審查）
- **CSRF**：Razor Pages Anti-forgery token（表單 POST 必開）
- **Rate limit**：對登入、查詢、匯出等敏感端點加上限制（防暴力枚舉/DoS）

### 7.3 日誌與稽核
- 重要事件：登入/登出、建立/更新/作廢、匯出、系統設定變更、權限變更
- 日誌分級：Information（一般）、Warning（可疑）、Error（例外）
- **不可**在 log 中寫入完整證件號或電話原文（必要時遮罩）

### 7.4 錯誤處理
- 使用全域 Exception Handler（中介軟體）：
  - 對使用者：顯示友善訊息 + 追蹤碼
  - 對系統：記錄完整 stack trace（僅內部 log）
- ModelState 驗證失敗：回填表單並顯示欄位錯誤

---

## 8. 交易一致性與併發控制

- EF Core 交易：Create / Checkout 以單一交易提交
- **RowVersion**：避免多人同時更新同筆資料導致覆寫
- Checkout 狀態轉移：
  - 允許：在場 -> 已離場
  - 禁止：已離場再次離場（需回報錯誤或要求管理者作廢/更正）

---

## 9. 部署設計

### 9.1 設定管理
- appsettings.json 僅放非敏感預設值
- **敏感設定**（DB 密碼、金鑰）使用：
  - Windows 環境變數 / Secret Manager（開發）/ 企業級密碼保管庫（正式）

### 9.2 連線字串（範例）
> 依實際環境調整，正式環境建議使用 SQL Account + 最小權限。

```json
{
  "ConnectionStrings": {
    "VisitorDb": "Server=.\\SQLEXPRESS;Database=VisitorDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 9.3 Migration 策略
- 開發/測試：CI/CD 可自動套用 Migration
- 正式：以 DBA 審核後腳本部署（產出 SQL script）

---

## 10. 測試策略

| 測試類型 | 重點 |
|---|---|
| 單元測試 | Validators、Use Case、狀態轉移規則 |
| 整合測試 | DbContext + Repository（SQLite/Container SQL Server） |
| 安全測試 | XSS/CSRF、輸入邊界值、暴力查詢限制 |
| UAT 驗收 | 前台流程、查詢/離場、權限、匯出稽核 |

---

## 11. 驗收標準（AC，精簡版）

- AC-01：可建立訪客登記，必填欄位缺漏會被阻擋並提示
- AC-02：可用日期區間查詢，支援分頁與排序
- AC-03：離場登記不可重複離場，且會留下稽核紀錄
- AC-04：一般使用者不可匯出；具權限者匯出會留下稽核紀錄
- AC-05：系統錯誤不洩漏敏感資訊，並產生可追蹤 log

---

## 12. 附錄：建議 NuGet（依目標框架版本選擇）

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools
- Microsoft.AspNetCore.RateLimiting（或等效方案）
- Serilog.AspNetCore（或內建 logging + Sink）

---

## 13. 變更紀錄

| 版本 | 日期 | 說明 |
|---|---|---|
| v2.0 | 2026-02-04 | 重新整理 SDD 結構、補齊安全/稽核/部署/測試章節、移除不可靠引用、明確資料表設計與 AC |
