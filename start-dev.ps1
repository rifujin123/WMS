# ============================================================
#  WMS — Khởi động backend (.NET) + frontend (Vite)
#
#  Cách dùng:
#    .\start-dev.ps1
#
#  Mỗi phần chạy trong 1 cửa sổ terminal riêng.
#  Muốn dừng: đóng 2 cửa sổ vừa mở (hoặc Ctrl+C trong từng cửa sổ).
# ============================================================

$ErrorActionPreference = 'Stop'

$root        = $PSScriptRoot
$backendDir  = Join-Path $root 'backend\WMS-mini'
$frontendDir = Join-Path $root 'frontend'

# ---- Kiểm tra công cụ -------------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host '[LOI] Khong tim thay "dotnet". Hay cai .NET 8 SDK roi thu lai.' -ForegroundColor Red
    exit 1
}
if (-not (Get-Command node -ErrorAction SilentlyContinue) -or -not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host '[LOI] Khong tim thay "node"/"npm". Hay cai Node.js roi thu lai.' -ForegroundColor Red
    exit 1
}

# ---- Kiểm tra thư mục -------------------------------------------------------
if (-not (Test-Path $backendDir))  { Write-Host "[LOI] Khong thay thu muc backend: $backendDir"  -ForegroundColor Red; exit 1 }
if (-not (Test-Path $frontendDir)) { Write-Host "[LOI] Khong thay thu muc frontend: $frontendDir" -ForegroundColor Red; exit 1 }

if (-not (Test-Path (Join-Path $frontendDir 'node_modules'))) {
    Write-Host '[CANH BAO] Chua co node_modules o frontend. Chay "npm install" trong thu muc frontend truoc neu npm run dev loi.' -ForegroundColor Yellow
}

# Mở 1 cửa sổ PowerShell mới chạy lệnh.
# Dùng -EncodedCommand (base64) để tránh lỗi escape/quote khi truyền lệnh phức tạp.
function Start-DevWindow([string]$Command, [string]$WorkingDir) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Command))
    Start-Process powershell -WorkingDirectory $WorkingDir -ArgumentList @('-NoExit', '-EncodedCommand', $encoded)
}

# ---- Khởi động backend (http://localhost:5246) ------------------------------
Write-Host '[1/2] Dang khoi dong backend (dotnet run)...' -ForegroundColor Cyan
$backendCmd = "`$Host.UI.RawUI.WindowTitle = 'WMS - Backend'; Set-Location -LiteralPath '$backendDir'; `$env:ASPNETCORE_ENVIRONMENT = 'Development'; `$env:ASPNETCORE_URLS = 'http://localhost:5246'; dotnet run --project .\src\WMS.API"
Start-DevWindow $backendCmd $backendDir

# ---- Khởi động frontend (http://localhost:5173) -----------------------------
Write-Host '[2/2] Dang khoi dong frontend (npm run dev)...' -ForegroundColor Cyan
$frontendCmd = "`$Host.UI.RawUI.WindowTitle = 'WMS - Frontend'; Set-Location -LiteralPath '$frontendDir'; npm run dev"
Start-DevWindow $frontendCmd $frontendDir

# ---- Thông tin truy cập -----------------------------------------------------
Write-Host ''
Write-Host '======================================================' -ForegroundColor Green
Write-Host '  WMS da duoc khoi dong:'                                -ForegroundColor Green
Write-Host '    Frontend : http://localhost:5173'                    -ForegroundColor Green
Write-Host '    Backend  : http://localhost:5246'                    -ForegroundColor Green
Write-Host '    Swagger  : http://localhost:5246/swagger'            -ForegroundColor Green
Write-Host '  (Frontend proxy /api -> http://localhost:5246)'        -ForegroundColor DarkGray
Write-Host '======================================================' -ForegroundColor Green
