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
    Write-ColorOutput "  2. 確認 SQL Server Express 正在執行" "White"
    Write-ColorOutput "  3. 檢查防火牆和端口是否被佔用" "White"
    Write-ColorOutput "  4. 查看詳細錯誤日誌" "White"
    
    exit 1
}
