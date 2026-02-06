<#
.SYNOPSIS
    Visitor Registration System - IIS Deployment Script
.DESCRIPTION
    Automate IIS configuration and database migration setup
    Note: Application files should already be copied to target location
.PARAMETER SourcePath
    Source code path (for Migration)
.PARAMETER WebsitePath
    IIS website physical path (application files should already exist here)
.PARAMETER DatabaseServer
    SQL Server name
.PARAMETER DatabaseName
    Database name
.EXAMPLE
    .\Deploy-VisitorReg.ps1
    Run with default parameters
.EXAMPLE
    .\Deploy-VisitorReg.ps1 -SourcePath "D:\Projects\aa" -DatabaseServer "localhost\SQLEXPRESS"
    Run with custom parameters
#>

param(
    [string]$SourcePath = "C:\vistor_registration-main",
    [string]$WebsitePath = "C:\inetpub\wwwroot\VisitorReg",
    [string]$AppPoolName = "VisitorRegAppPool",
    [string]$WebsiteName = "VisitorReg",
    [int]$Port = 80,
    [string]$DatabaseServer = "localhost\SQLEXPRESS",
    [string]$DatabaseName = "VisitorRegDb_Production",
    [switch]$SkipDatabase,
    [switch]$BackupExisting
)

$ErrorActionPreference = "Stop"

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
    Write-ColorOutput "[OK] $Message" "Green"
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-ColorOutput "[ERROR] $Message" "Red"
}

function Write-WarningMsg {
    param([string]$Message)
    Write-ColorOutput "[WARN] $Message" "Yellow"
}

try {
    Write-ColorOutput "`n================================================" "Cyan"
    Write-ColorOutput "  Visitor Registration System - IIS Setup" "Cyan"
    Write-ColorOutput "                Version 2.0" "Cyan"
    Write-ColorOutput "================================================`n" "Cyan"

    # Step 1: Check prerequisites
    Write-Step "Step 1/5: Check Prerequisites"
    
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-NOT $isAdmin) {
        Write-ErrorMsg "Administrator rights required!"
        exit 1
    }
    Write-Success "Administrator rights confirmed"

    if (-not (Test-Path $WebsitePath)) {
        Write-ErrorMsg "Application path not found: $WebsitePath"
        Write-ErrorMsg "Please copy application files to this location first"
        exit 1
    }
    Write-Success "Application path exists: $WebsitePath"

    if (-not $SkipDatabase) {
        if (-not (Test-Path $SourcePath)) {
            Write-WarningMsg "Source path not found: $SourcePath"
            Write-WarningMsg "Database migration will be skipped"
            $SkipDatabase = $true
        }
        else {
            Write-Success "Source path exists: $SourcePath"
        }
    }

    $iisFeature = Get-WindowsFeature -Name Web-Server -ErrorAction SilentlyContinue
    if (-not $iisFeature -or -not $iisFeature.Installed) {
        Write-WarningMsg "IIS not installed, installing..."
        Install-WindowsFeature -name Web-Server -IncludeManagementTools
        Write-Success "IIS installed"
    }
    else {
        Write-Success "IIS already installed"
    }

    # Load WebAdministration module for IIS management
    Write-ColorOutput "Loading IIS management module..." "Yellow"
    Import-Module WebAdministration -ErrorAction Stop
    Write-Success "WebAdministration module loaded"

    # Step 2: Stop services and backup
    Write-Step "Step 2/5: Stop Existing Services"
    
    if (Get-Website -Name $WebsiteName -ErrorAction SilentlyContinue) {
        Write-WarningMsg "Stopping existing website..."
        Stop-Website -Name $WebsiteName -ErrorAction SilentlyContinue
        Write-Success "Website stopped"
    }

    if (Get-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Write-WarningMsg "Stopping existing app pool..."
        Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Success "App pool stopped"
    }

    if ($BackupExisting -and (Test-Path $WebsitePath)) {
        $backupPath = "C:\Backup\VisitorReg_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Write-WarningMsg "Backing up to: $backupPath"
        New-Item -ItemType Directory -Path "C:\Backup" -Force -ErrorAction SilentlyContinue | Out-Null
        Copy-Item -Path $WebsitePath -Destination $backupPath -Recurse
        Write-Success "Backup completed"
    }

    $logsPath = Join-Path $WebsitePath "logs"
    if (-not (Test-Path $logsPath)) {
        New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
        Write-Success "Logs directory created"
    }

    # Step 3: Configure IIS
    Write-Step "Step 3/5: Configure IIS"

    if (Get-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Write-WarningMsg "Removing existing app pool..."
        Remove-WebAppPool -Name $AppPoolName
    }
    
    Write-ColorOutput "Creating app pool..." "Yellow"
    New-WebAppPool -Name $AppPoolName | Out-Null
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
    Write-Success "App pool created: $AppPoolName"

    if (Get-Website -Name $WebsiteName -ErrorAction SilentlyContinue) {
        Write-WarningMsg "Removing existing website..."
        Remove-Website -Name $WebsiteName
    }
    
    Write-ColorOutput "Creating IIS website..." "Yellow"
    New-Website -Name $WebsiteName -PhysicalPath $WebsitePath -ApplicationPool $AppPoolName -Port $Port -Force | Out-Null
    Write-Success "Website created: $WebsiteName"

    Write-ColorOutput "Setting directory permissions..." "Yellow"
    $cmd1 = 'icacls "{0}" /grant "IIS_IUSRS:(OI)(CI)RX" /T /Q' -f $WebsitePath
    cmd /c $cmd1 | Out-Null
    
    $appPoolIdentity = "IIS APPPOOL\{0}" -f $AppPoolName
    $cmd2 = 'icacls "{0}" /grant "{1}:(OI)(CI)F" /T /Q' -f $WebsitePath, $appPoolIdentity
    cmd /c $cmd2 | Out-Null
    Write-Success "Permissions configured"

    # Step 4: Configure Database
    if (-not $SkipDatabase) {
        Write-Step "Step 4/5: Configure Database"
        
        $connectionString = "Server=$DatabaseServer;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        
        $appSettingsPath = Join-Path $WebsitePath "appsettings.json"
        if (Test-Path $appSettingsPath) {
            Write-ColorOutput "Updating appsettings.json..." "Yellow"
            $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
            $appSettings.ConnectionStrings.DefaultConnection = $connectionString
            $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
            Write-Success "Connection string updated"
        }

        $prodSettingsPath = Join-Path $WebsitePath "appsettings.Production.json"
        $prodSettings = @{
            ConnectionStrings = @{
                DefaultConnection = $connectionString
            }
            Logging           = @{
                LogLevel = @{
                    Default                = "Error"
                    "Microsoft.AspNetCore" = "Error"
                }
            }
        }
        $prodSettings | ConvertTo-Json -Depth 10 | Set-Content $prodSettingsPath
        Write-Success "appsettings.Production.json created"

        Write-ColorOutput "Running database migration..." "Yellow"
        try {
            Push-Location $SourcePath
            
            $efInstalled = $false
            try {
                $efVersion = dotnet ef --version 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $efInstalled = $true
                }
            }
            catch {
                $efInstalled = $false
            }
            
            if (-not $efInstalled) {
                Write-WarningMsg "Installing Entity Framework Core tools..."
                dotnet tool install --global dotnet-ef
                $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
            }
            
            dotnet ef database update --project VisitorReg.Infrastructure --startup-project VisitorReg.Web --connection $connectionString
            Pop-Location
            Write-Success "Database migration completed"
        }
        catch {
            if ((Get-Location).Path -ne $PSScriptRoot) {
                Pop-Location
            }
            Write-WarningMsg "Database migration failed: $_"
            Write-WarningMsg "Please run migration manually or check database connection"
        }
    }
    else {
        Write-Step "Step 4/5: Skip Database (SkipDatabase parameter enabled)"
    }

    # Step 5: Start services and test
    Write-Step "Step 5/5: Start Services and Test"
    
    Start-WebAppPool -Name $AppPoolName
    Write-Success "App pool started"

    Start-Sleep -Seconds 2

    Start-Website -Name $WebsiteName
    Write-Success "Website started"

    Start-Sleep -Seconds 3

    Write-ColorOutput "`nTesting website..." "Yellow"
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Success "Website test successful! HTTP Status: 200"
        }
        else {
            Write-WarningMsg "Website response abnormal. HTTP Status: $($response.StatusCode)"
        }
    }
    catch {
        Write-WarningMsg "Cannot connect to website. Please check IIS settings and application logs."
    }

    # Deployment complete
    Write-ColorOutput "`n================================================" "Green"
    Write-ColorOutput "       Deployment Completed Successfully!" "Green"
    Write-ColorOutput "================================================`n" "Green"

    Write-ColorOutput "`nDeployment Information:" "Cyan"
    Write-ColorOutput "  Website Name: $WebsiteName" "White"
    Write-ColorOutput "  App Pool: $AppPoolName" "White"
    Write-ColorOutput "  Website Path: $WebsitePath" "White"
    Write-ColorOutput "  Access URL: http://localhost:$Port" "White"
    Write-ColorOutput "  Database: $DatabaseServer\$DatabaseName" "White"

    Write-ColorOutput "`nNext Steps:" "Cyan"
    Write-ColorOutput "  1. Open in browser: http://localhost:$Port" "White"
    Write-ColorOutput "  2. Register new user account" "White"
    Write-ColorOutput "  3. Start using the system" "White"

    Write-ColorOutput "`nLog Locations:" "Cyan"
    Write-ColorOutput "  Application logs: $WebsitePath\logs" "White"
    Write-ColorOutput "  IIS logs: C:\inetpub\logs\LogFiles`n" "White"

    $openBrowser = Read-Host "Open browser to test website? (Y/N)"
    if ($openBrowser -eq 'Y' -or $openBrowser -eq 'y') {
        Start-Process "http://localhost:$Port"
    }

}
catch {
    Write-ColorOutput "`n================================================" "Red"
    Write-ColorOutput "          Deployment Failed!" "Red"
    Write-ColorOutput "================================================`n" "Red"
    
    Write-ColorOutput "`nError Message: $($_.Exception.Message)" "Red"
    Write-ColorOutput "`nError Details:" "Red"
    Write-ColorOutput $_.Exception.ToString() "Red"
    
    Write-ColorOutput "`nTroubleshooting:" "Yellow"
    Write-ColorOutput "  1. Verify application files are in: $WebsitePath" "White"
    Write-ColorOutput "  2. Verify .NET Hosting Bundle is installed" "White"
    Write-ColorOutput "  3. Verify SQL Server Express is running" "White"
    Write-ColorOutput "  4. Check firewall and port availability" "White"
    Write-ColorOutput "  5. Check detailed error logs`n" "White"
    
    exit 1
}
