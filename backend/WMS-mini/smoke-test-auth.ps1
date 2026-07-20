# smoke-test-auth.ps1
# Smoke tests: Authentication & Authorization
#
# Usage:
#   .\smoke-test-auth.ps1
#   .\smoke-test-auth.ps1 -BaseUrl "http://localhost:5246" -AdminUsername "admin" -AdminPassword "Admin@123"

param(
    [string]$BaseUrl       = "https://localhost:7116",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "Admin@123"
)

$ErrorActionPreference = "SilentlyContinue"

# Bypass self-signed certificate for local dev
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

$script:pass        = 0
$script:fail        = 0
$script:skip        = 0
$script:ADMIN_TOKEN = $null
$script:STAFF_TOKEN = $null
$TEST_STAFF_USER    = "smoke_staff_$(Get-Random -Maximum 99999)"
$TEST_STAFF_PASS    = "Staff@Test123!"

function Write-Header($title) {
    Write-Host ""
    Write-Host "=== $title ===" -ForegroundColor Cyan
}

function Assert-Status($name, $expected, $actual) {
    if ($actual -eq $expected) {
        Write-Host "  [PASS] $name" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "  [FAIL] $name  (expect $expected, got $actual)" -ForegroundColor Red
        $script:fail++
    }
}

function Skip-Test($name) {
    Write-Host "  [SKIP] $name" -ForegroundColor Yellow
    $script:skip++
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [hashtable]$Body         = $null,
        [string]$Token           = $null,
        [hashtable]$ExtraHeaders = $null
    )

    $headers = @{ "Content-Type" = "application/json" }
    if ($Token)        { $headers["Authorization"] = "Bearer $Token" }
    if ($ExtraHeaders) { $ExtraHeaders.GetEnumerator() | ForEach-Object { $headers[$_.Key] = $_.Value } }

    $params = @{
        Method          = $Method
        Uri             = "$BaseUrl$Path"
        Headers         = $headers
        UseBasicParsing = $true
    }
    if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Compress) }

    try {
        $resp   = Invoke-WebRequest @params
        $parsed = $null
        try { $parsed = $resp.Content | ConvertFrom-Json } catch {}
        return @{ StatusCode = [int]$resp.StatusCode; Body = $parsed }
    } catch {
        $code     = 0
        $bodyText = ""
        if ($_.Exception.Response) {
            $code = [int]$_.Exception.Response.StatusCode
            try {
                $stream   = $_.Exception.Response.GetResponseStream()
                $reader   = New-Object System.IO.StreamReader($stream)
                $bodyText = $reader.ReadToEnd()
            } catch {}
        }
        $parsed = $null
        try { $parsed = $bodyText | ConvertFrom-Json } catch {}
        return @{ StatusCode = $code; Body = $parsed }
    }
}

# ─── 1. AUTHENTICATION - Login ───────────────────────────────────────────────

Write-Header "1. Authentication - Login"

$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ username = $AdminUsername; password = $AdminPassword }
Assert-Status "Login valid admin -> 200" 200 $r.StatusCode
if ($r.StatusCode -eq 200 -and $r.Body.accessToken) {
    $script:ADMIN_TOKEN = $r.Body.accessToken
    Write-Host "       (admin token acquired)" -ForegroundColor DarkGray
} else {
    Write-Host "  [WARN] Cannot get admin token - register/admin-role tests will be skipped" -ForegroundColor Yellow
}

$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ username = $AdminUsername; password = "WrongPassword!" }
Assert-Status "Login wrong password -> 401" 401 $r.StatusCode

$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ username = "ghost_user_x9z"; password = "Whatever@1" }
Assert-Status "Login unknown user -> 401" 401 $r.StatusCode

$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ username = ""; password = "" }
Assert-Status "Login empty credentials -> 401" 401 $r.StatusCode

# ─── 2. AUTHENTICATION - Register ────────────────────────────────────────────

Write-Header "2. Authentication - Register"

$r = Invoke-Api -Method POST -Path "/api/auth/register" `
    -Body @{ username = "any_user"; email = "any@test.com"; password = "Pass@123!"; fullName = "Any User" }
Assert-Status "Register without token -> 401" 401 $r.StatusCode

if ($script:ADMIN_TOKEN) {
    $r = Invoke-Api -Method POST -Path "/api/auth/register" `
        -Body @{ username = $TEST_STAFF_USER; email = "$TEST_STAFF_USER@test.com"; password = $TEST_STAFF_PASS; fullName = "Smoke Test Staff" } `
        -Token $script:ADMIN_TOKEN
    Assert-Status "Register with Admin token -> 201" 201 $r.StatusCode

    $r = Invoke-Api -Method POST -Path "/api/auth/register" `
        -Body @{ username = $TEST_STAFF_USER; email = "dup@test.com"; password = $TEST_STAFF_PASS; fullName = "Dup" } `
        -Token $script:ADMIN_TOKEN
    Assert-Status "Register duplicate username -> 400" 400 $r.StatusCode

    $r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ username = $TEST_STAFF_USER; password = $TEST_STAFF_PASS }
    Assert-Status "Login newly registered staff -> 200" 200 $r.StatusCode
    if ($r.StatusCode -eq 200 -and $r.Body.accessToken) {
        $script:STAFF_TOKEN = $r.Body.accessToken
        Write-Host "       (staff token acquired)" -ForegroundColor DarkGray
    }

    if ($script:STAFF_TOKEN) {
        $r = Invoke-Api -Method POST -Path "/api/auth/register" `
            -Body @{ username = "forbidden_user"; email = "fb@test.com"; password = "Pass@123!"; fullName = "Forbidden" } `
            -Token $script:STAFF_TOKEN
        Assert-Status "Register with Staff token -> 403" 403 $r.StatusCode
    } else {
        Skip-Test "Register with Staff token -> 403 (no staff token)"
    }
} else {
    Skip-Test "Register with Admin token -> 201 (no admin token)"
    Skip-Test "Register duplicate username -> 400 (no admin token)"
    Skip-Test "Login newly registered staff -> 200 (no admin token)"
    Skip-Test "Register with Staff token -> 403 (no staff token)"
}

# ─── 3. AUTHORIZATION - Products ─────────────────────────────────────────────

Write-Header "3. Authorization - Products"

$r = Invoke-Api -Method GET -Path "/api/products"
Assert-Status "GET /api/products - no token -> 401" 401 $r.StatusCode

if ($script:STAFF_TOKEN) {
    $r = Invoke-Api -Method GET -Path "/api/products" -Token $script:STAFF_TOKEN
    Assert-Status "GET /api/products - Staff -> 200" 200 $r.StatusCode
} else { Skip-Test "GET /api/products - Staff -> 200" }

if ($script:ADMIN_TOKEN) {
    $r = Invoke-Api -Method GET -Path "/api/products" -Token $script:ADMIN_TOKEN
    Assert-Status "GET /api/products - Admin -> 200" 200 $r.StatusCode
} else { Skip-Test "GET /api/products - Admin -> 200" }

if ($script:STAFF_TOKEN) {
    $r = Invoke-Api -Method POST -Path "/api/products" `
        -Body @{ sku = "SMOKE-001"; name = "Smoke Product"; categoryId = "00000000-0000-0000-0000-000000000000"; unit = "pcs"; price = 9.99 } `
        -Token $script:STAFF_TOKEN
    Assert-Status "POST /api/products - Staff -> 403" 403 $r.StatusCode
} else { Skip-Test "POST /api/products - Staff -> 403" }

if ($script:STAFF_TOKEN) {
    $r = Invoke-Api -Method PUT -Path "/api/products/00000000-0000-0000-0000-000000000001" `
        -Body @{ name = "Updated Name" } -Token $script:STAFF_TOKEN
    Assert-Status "PUT /api/products/{id} - Staff -> 403" 403 $r.StatusCode
} else { Skip-Test "PUT /api/products/{id} - Staff -> 403" }

if ($script:STAFF_TOKEN) {
    $r = Invoke-Api -Method DELETE -Path "/api/products/00000000-0000-0000-0000-000000000001" -Token $script:STAFF_TOKEN
    Assert-Status "DELETE /api/products/{id} - Staff -> 403" 403 $r.StatusCode
} else { Skip-Test "DELETE /api/products/{id} - Staff -> 403" }

# ─── 4. AUTHORIZATION - Warehouses, Locations, Categories ────────────────────

Write-Header "4. Authorization - Other Controllers"

foreach ($ep in @("/api/warehouses", "/api/locations", "/api/categories")) {
    $r = Invoke-Api -Method GET -Path $ep
    Assert-Status "GET $ep - no token -> 401" 401 $r.StatusCode

    if ($script:STAFF_TOKEN) {
        $r = Invoke-Api -Method GET -Path $ep -Token $script:STAFF_TOKEN
        Assert-Status "GET $ep - Staff -> 200" 200 $r.StatusCode
    } else { Skip-Test "GET $ep - Staff -> 200" }

    if ($script:STAFF_TOKEN) {
        $r = Invoke-Api -Method POST -Path $ep -Body @{ name = "Smoke Test" } -Token $script:STAFF_TOKEN
        Assert-Status "POST $ep - Staff -> 403" 403 $r.StatusCode
    } else { Skip-Test "POST $ep - Staff -> 403" }
}

# ─── 5. TOKEN EDGE CASES ─────────────────────────────────────────────────────

Write-Header "5. Token Edge Cases"

$r = Invoke-Api -Method GET -Path "/api/products" -Token "this.is.not.a.valid.jwt.token"
Assert-Status "GET with malformed token -> 401" 401 $r.StatusCode

$r = Invoke-Api -Method GET -Path "/api/products" -ExtraHeaders @{ Authorization = "Basic abc123xyz" }
Assert-Status "GET with Basic auth scheme -> 401" 401 $r.StatusCode

# ─── Summary ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "====================================================" -ForegroundColor DarkGray
$total = $script:pass + $script:fail
Write-Host "  Passed : $($script:pass)" -ForegroundColor Green
if ($script:fail -gt 0) {
    Write-Host "  Failed : $($script:fail)" -ForegroundColor Red
} else {
    Write-Host "  Failed : 0" -ForegroundColor DarkGray
}
if ($script:skip -gt 0) {
    Write-Host "  Skipped: $($script:skip)" -ForegroundColor Yellow
}
Write-Host "  Total  : $total tests run" -ForegroundColor DarkGray
Write-Host "====================================================" -ForegroundColor DarkGray
Write-Host ""

if ($script:fail -gt 0) { exit 1 } else { exit 0 }
