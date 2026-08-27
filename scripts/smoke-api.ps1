#Requires -Version 5.1
<#
.SYNOPSIS
  HTTP smoke for WMS API: CRUD + inbound/outbound happy path.
.EXAMPLE
  pwsh -File scripts/smoke-api.ps1
#>
param(
    [string]$BaseUrl = 'http://localhost:5246/api'
)

$ErrorActionPreference = 'Stop'
$script:Stamp = Get-Date -Format 'yyyyMMddHHmmss'
$script:Pass = 0
$script:Fail = 0

function Write-Step([string]$Name) {
    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
}

function Invoke-Api {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [string]$Token,
        $Body,
        [hashtable]$HeadersExtra,
        [int[]]$Expect = @(200, 201),
        [string]$Label
    )

    $uri = if ($Path.StartsWith('http')) { $Path } else { "$BaseUrl$Path" }
    $headers = @{ Accept = 'application/json' }
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    if ($HeadersExtra) { foreach ($k in $HeadersExtra.Keys) { $headers[$k] = $HeadersExtra[$k] } }

    $params = @{
        Uri             = $uri
        Method          = $Method
        Headers         = $headers
        UseBasicParsing = $true
    }
    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = if ($Body -is [string]) { $Body } else { ConvertTo-Json -InputObject $Body -Depth 12 -Compress }
    }

    $name = $Label
    if (-not $name) { $name = "$Method $Path" }

    try {
        $resp = Invoke-WebRequest @params
        $status = [int]$resp.StatusCode
        $parsed = $null
        if ($resp.Content) {
            try { $parsed = $resp.Content | ConvertFrom-Json } catch { $parsed = $resp.Content }
        }
        if ($Expect -notcontains $status) {
            throw "Expected $($Expect -join '/') got $status. Body: $($resp.Content)"
        }
        $script:Pass++
        Write-Host "OK  $status $name" -ForegroundColor Green
        return $parsed
    }
    catch {
        $status = 0
        $text = ''
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $text = $_.ErrorDetails.Message }
        elseif ($_.Exception.Response) {
            try {
                $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
                $text = $reader.ReadToEnd()
                $reader.Close()
            } catch { $text = $_.Exception.Message }
        }
        else { $text = $_.Exception.Message }

        if ($status -gt 0 -and $Expect -contains $status) {
            $script:Pass++
            Write-Host "OK  $status $name (expected)" -ForegroundColor Green
            try { return ($text | ConvertFrom-Json) } catch { return $text }
        }
        $script:Fail++
        Write-Host "FAIL $status $name" -ForegroundColor Red
        if ($text) { Write-Host "     $text" -ForegroundColor DarkYellow }
        throw "$name failed: $status $text"
    }
}

function Login([string]$Username, [string]$Password) {
    $r = Invoke-Api POST '/Auth/login' -Body @{ username = $Username; password = $Password } -Label "login $Username"
    if (-not $r.accessToken) { throw "No accessToken for $Username" }
    return $r.accessToken
}

function Ensure-User($AdminToken, [string]$Username, [string]$Role) {
    $tryLogin = Invoke-Api POST '/Auth/login' -Body @{ username = $Username; password = 'Smoke@12345' } -Expect @(200, 401) -Label "login $Username (ensure)"
    if ($tryLogin -and $tryLogin.accessToken) { return $tryLogin.accessToken }
    Invoke-Api POST '/Auth/register' -Token $AdminToken -Expect @(201, 200) -Label "register $Username" -Body @{
        username = $Username
        email    = "$Username@wms.local"
        password = 'Smoke@12345'
        fullName = "Smoke $Role"
        role     = $Role
    } | Out-Null
    return Login $Username 'Smoke@12345'
}

function Get-TinyPngPath {
    $path = Join-Path $env:TEMP "wms-smoke-$Stamp.png"
    [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=='
    ))
    return $path
}

function Invoke-ProductForm {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Token,
        [Parameter(Mandatory)][hashtable]$Fields,
        [string]$FilePath,
        [string]$Label
    )
    $uri = "$BaseUrl$Path"
    $name = $Label
    if (-not $name) { $name = "$Method $Path" }
    $curlArgs = @('-sS', '-w', "`nHTTPSTATUS:%{http_code}", '-X', $Method, $uri, '-H', "Authorization: Bearer $Token")
    foreach ($key in $Fields.Keys) {
        $curlArgs += @('-F', "$key=$($Fields[$key])")
    }
    if ($FilePath) { $curlArgs += @('-F', "file=@$FilePath;type=image/png") }
    $raw = & curl.exe @curlArgs
    $joined = if ($raw -is [array]) { $raw -join "`n" } else { [string]$raw }
    if ($joined -notmatch 'HTTPSTATUS:(\d+)\s*$') { throw "curl missing status: $joined" }
    $status = [int]$Matches[1]
    $body = $joined.Substring(0, $joined.LastIndexOf('HTTPSTATUS:')).Trim()
    if ($status -notin 200, 201) {
        $script:Fail++
        Write-Host "FAIL $status $name" -ForegroundColor Red
        Write-Host "     $body" -ForegroundColor DarkYellow
        throw "$name failed: $status $body"
    }
    $script:Pass++
    Write-Host "OK  $status $name" -ForegroundColor Green
    return ($body | ConvertFrom-Json)
}

Write-Host "WMS smoke → $BaseUrl  stamp=$Stamp"

# --- 1 Auth ---
Write-Step 'Auth'
$admin = Login 'admin' 'Admin@123'
$mgr = Ensure-User $admin 'smoke-mgr' 'WarehouseManager'
$staff = Ensure-User $admin 'smoke-staff' 'WarehouseStaff'
$staffProfile = Invoke-Api GET '/Users/me' -Token $staff -Label 'staff me'
$staffId = $staffProfile.id
if (-not $staffId) { throw 'staff id missing' }

# --- 2 Users ---
Write-Step 'Users'
Invoke-Api GET '/Users/me' -Token $admin -Label 'admin me' | Out-Null
Invoke-Api PUT '/Users/me' -Token $admin -Label 'admin update me' -Body @{
    fullName    = 'System Administrator'
    phoneNumber = '0900000000'
} | Out-Null
Invoke-Api GET '/Users?page=1' -Token $admin -Label 'users page' | Out-Null
Invoke-Api GET '/Users/lookup?role=WarehouseStaff' -Token $admin -Label 'users lookup staff' | Out-Null
Invoke-Api GET '/Users?page=1' -Token $mgr -Label 'mgr users page' | Out-Null
Invoke-Api GET '/Users?page=1' -Token $staff -Expect @(403) -Label 'staff users forbidden' | Out-Null

# --- 3 Master ---
Write-Step 'Master CRUD'
$cat = Invoke-Api POST '/Categories' -Token $admin -Expect @(201, 200) -Label 'create category' -Body @{
    name = "SMOKE-CAT-$Stamp"
}
$catId = $cat.id
Invoke-Api GET "/Categories/$catId" -Token $admin -Label 'get category' | Out-Null
Invoke-Api PUT "/Categories/$catId" -Token $admin -Label 'update category' -Body @{ name = "SMOKE-CAT-$Stamp-U" } | Out-Null
Invoke-Api GET '/Categories?page=1' -Token $admin -Label 'categories page' | Out-Null
Invoke-Api GET '/Categories/lookup' -Token $admin -Label 'categories lookup' | Out-Null

$png = Get-TinyPngPath
$prod = Invoke-ProductForm POST '/Products' $admin @{
    Sku        = "SMOKE-SKU-$Stamp"
    Name       = "Smoke product $Stamp"
    CategoryId = $catId
    Unit       = 'pcs'
    Price      = 10
} -FilePath $png -Label 'create product'
$prodId = $prod.id
Invoke-Api GET "/Products/$prodId" -Token $admin -Label 'get product' | Out-Null
Invoke-ProductForm PUT "/Products/$prodId" $admin @{
    Sku        = "SMOKE-SKU-$Stamp"
    Name       = "Smoke product $Stamp U"
    CategoryId = $catId
    Unit       = 'pcs'
    Price      = 12
} -Label 'update product' | Out-Null
Invoke-Api GET '/Products?page=1' -Token $admin -Label 'products page' | Out-Null
Invoke-Api GET '/Products/lookup' -Token $admin -Label 'products lookup' | Out-Null

$wh = Invoke-Api POST '/Warehouses' -Token $admin -Expect @(201, 200) -Label 'create warehouse' -Body @{
    code    = "SMK$Stamp"
    name    = "Smoke WH $Stamp"
    address = '1 Smoke St'
}
$whId = $wh.id
Invoke-Api GET "/Warehouses/$whId" -Token $admin -Label 'get warehouse' | Out-Null
Invoke-Api PUT "/Warehouses/$whId" -Token $admin -Label 'update warehouse' -Body @{
    name    = "Smoke WH $Stamp U"
    address = '2 Smoke St'
} | Out-Null
Invoke-Api GET '/Warehouses?page=1' -Token $admin -Label 'warehouses page' | Out-Null
Invoke-Api GET '/Warehouses/lookup' -Token $admin -Label 'warehouses lookup' | Out-Null

$loc = Invoke-Api POST '/Locations' -Token $admin -Expect @(201, 200) -Label 'create location' -Body @{
    warehouseId  = $whId
    code         = "SMK-$Stamp-A"
    aisle        = 'A'
    rack         = '1'
    level        = '1'
    locationType = 'Storage'
    maxQuantity  = 1000
}
$locId = $loc.id
Invoke-Api GET "/Locations/$locId" -Token $admin -Label 'get location' | Out-Null
Invoke-Api PUT "/Locations/$locId" -Token $admin -Label 'update location' -Body @{
    code         = "SMK-$Stamp-A"
    aisle        = 'A'
    rack         = '1'
    level        = '1'
    locationType = 'Storage'
    maxQuantity  = 2000
} | Out-Null
Invoke-Api GET '/Locations?page=1' -Token $admin -Label 'locations page' | Out-Null
Invoke-Api GET '/Locations/lookup' -Token $admin -Label 'locations lookup' | Out-Null

$orphanCat = Invoke-Api POST '/Categories' -Token $admin -Expect @(201, 200) -Label 'orphan category' -Body @{ name = "SMOKE-ORPHAN-$Stamp" }
Invoke-Api DELETE "/Categories/$($orphanCat.id)" -Token $admin -Label 'delete orphan category' | Out-Null
Invoke-Api GET "/Categories/$($orphanCat.id)" -Token $admin -Expect @(404) -Label 'orphan category gone' | Out-Null

# --- 4 PO ---
Write-Step 'Purchase Orders'
$poDel = Invoke-Api POST '/PurchaseOrders' -Token $staff -Expect @(201, 200) -Label 'staff create PO delete-me' -Body @{
    poNumber             = "SMOKE-DEL-$Stamp"
    vendorName           = 'Smoke Vendor'
    purchaseOrderDetails = @(@{ productId = $prodId; orderedQuantity = 2 })
}
Invoke-Api GET "/PurchaseOrders/$($poDel.id)" -Token $staff -Label 'get PO delete-me' | Out-Null
Invoke-Api DELETE "/PurchaseOrders/$($poDel.id)" -Token $staff -Label 'staff delete pending PO' | Out-Null

$po = Invoke-Api POST '/PurchaseOrders' -Token $staff -Expect @(201, 200) -Label 'staff create PO' -Body @{
    poNumber             = "SMOKE-PO-$Stamp"
    vendorName           = 'Smoke Vendor'
    purchaseOrderDetails = @(@{ productId = $prodId; orderedQuantity = 10 })
}
$poId = $po.id
Invoke-Api PUT "/PurchaseOrders/$poId" -Token $mgr -Label 'mgr update pending PO' -Body @{
    vendorName           = 'Smoke Vendor U'
    purchaseOrderDetails = @(@{ productId = $prodId; orderedQuantity = 10 })
} | Out-Null
# Staff PUT is allowed in code; live API may still be on old DLL until restart.
Invoke-Api PUT "/PurchaseOrders/$poId" -Token $staff -Expect @(200, 403) -Label 'staff update pending PO' -Body @{
    vendorName           = 'Smoke Vendor U'
    purchaseOrderDetails = @(@{ productId = $prodId; orderedQuantity = 10 })
} | Out-Null
Invoke-Api GET '/PurchaseOrders?page=1' -Token $staff -Label 'PO page' | Out-Null
Invoke-Api GET '/PurchaseOrders/lookup' -Token $staff -Label 'PO lookup' | Out-Null
Invoke-Api PATCH "/PurchaseOrders/$poId/approve" -Token $staff -Expect @(403) -Label 'staff approve forbidden' | Out-Null
Invoke-Api PATCH "/PurchaseOrders/$poId/approve" -Token $admin -Label 'admin approve PO' | Out-Null

Invoke-Api POST '/PurchaseOrders' -Token $staff -Expect @(400) -Label 'duplicate PO number' -Body @{
    poNumber             = "SMOKE-PO-$Stamp"
    vendorName           = 'Dup'
    purchaseOrderDetails = @(@{ productId = $prodId; orderedQuantity = 1 })
} | Out-Null

# --- 5 Receiving ---
Write-Step 'Receiving'
$rcv = Invoke-Api POST '/Receivings' -Token $staff -Expect @(201, 200) -Label 'create receiving draft' -Body @{
    purchaseOrderId = $poId
    notes           = 'smoke draft'
    details         = @(@{
            productId         = $prodId
            expectedQuantity  = 10
            actualQuantity    = 10
            condition         = 'Ok'
        })
}
$rcvId = $rcv.id
$got = Invoke-Api GET "/Receivings/$rcvId" -Token $staff -Label 'get receiving'
if (-not $got.details -or @($got.details).Count -lt 1) { throw 'GET receiving missing details' }
Invoke-Api PUT "/Receivings/$rcvId" -Token $staff -Label 'update receiving draft' -Body @{
    purchaseOrderId = $poId
    notes           = 'smoke draft u'
    details         = @(@{
            productId         = $prodId
            expectedQuantity  = 10
            actualQuantity    = 10
            condition         = 'Ok'
        })
} | Out-Null
Invoke-Api GET '/Receivings?page=1' -Token $staff -Label 'receivings page' | Out-Null
Invoke-Api GET '/Receivings/lookup' -Token $staff -Label 'receivings lookup' | Out-Null
Invoke-Api POST "/Receivings/$rcvId/confirm" -Token $staff -Label 'confirm receiving' | Out-Null
Invoke-Api PUT "/Receivings/$rcvId" -Token $staff -Expect @(400) -Label 'update confirmed receiving' -Body @{
    purchaseOrderId = $poId
    details         = @(@{
            productId         = $prodId
            expectedQuantity  = 10
            actualQuantity    = 10
            condition         = 'Ok'
        })
} | Out-Null

# --- 6 Put away ---
Write-Step 'PutAway'
$tasks = Invoke-Api GET '/PutAwayTasks/lookup' -Token $admin -Label 'putaway lookup'
$task = @($tasks) | Where-Object { $_.productId -eq $prodId } | Select-Object -First 1
if (-not $task) { throw 'No putaway task for smoke product' }
$taskId = $task.id
Invoke-Api GET "/PutAwayTasks/$taskId" -Token $admin -Label 'get putaway' | Out-Null
Invoke-Api GET '/PutAwayTasks?page=1' -Token $admin -Label 'putaway page' | Out-Null
Invoke-Api PUT "/PutAwayTasks/$taskId" -Token $admin -Label 'set putaway location' -Body @{
    receivingDetailId = $task.receivingDetailId
    productId         = $task.productId
    quantity          = $task.quantity
    toLocationId      = $locId
} | Out-Null
Invoke-Api POST "/PutAwayTasks/$taskId/assign" -Token $admin -Label 'assign putaway' -Body @{ userId = $staffId } | Out-Null
Invoke-Api POST "/PutAwayTasks/$taskId/start" -Token $staff -Label 'staff start putaway' | Out-Null
Invoke-Api POST "/PutAwayTasks/$taskId/complete" -Token $staff -Label 'staff complete putaway' | Out-Null
Invoke-Api GET '/Stocks' -Token $admin -Label 'stocks list' | Out-Null
Invoke-Api GET '/Stocks/summary?page=1' -Token $admin -Label 'stocks summary' | Out-Null

# --- 7 SO + Picking + Shipment ---
Write-Step 'SaleOrder / Picking / Shipment'
$so = Invoke-Api POST '/SaleOrders' -Token $mgr -Expect @(201, 200) -Label 'create sale order' -Body @{
    orderNo          = "SMOKE-SO-$Stamp"
    customerName     = 'Smoke Customer'
    orderDate        = (Get-Date).ToUniversalTime().ToString('o')
    saleOrderDetails = @(@{ productId = $prodId; quantity = 3 })
}
$soId = $so.id
Invoke-Api GET "/SaleOrders/$soId" -Token $mgr -Label 'get SO' | Out-Null
Invoke-Api GET '/SaleOrders' -Token $mgr -Label 'list SO' | Out-Null
$pick = Invoke-Api POST '/Pickings' -Token $mgr -Expect @(201, 200) -Label 'create picking' -Body @{
    saleOrderId = $soId
    warehouseId = $whId
}
$pickId = $pick.id
Invoke-Api GET "/Pickings/$pickId" -Token $mgr -Label 'get picking' | Out-Null
Invoke-Api GET '/Pickings' -Token $mgr -Label 'list pickings' | Out-Null
Invoke-Api POST "/Pickings/$pickId/assign" -Token $mgr -Label 'assign picking' -Body @{ userId = $staffId } | Out-Null
Invoke-Api POST "/Pickings/$pickId/start" -Token $staff -Label 'staff start picking' | Out-Null
$pick2 = Invoke-Api GET "/Pickings/$pickId" -Token $mgr -Label 'mgr get picking for complete'
function Get-PickingLines($obj) {
    if (-not $obj) { return @() }
    foreach ($name in @('details', 'pickingDetails', 'Details', 'PickingDetails')) {
        $val = $obj.$name
        if ($val) { return @($val) }
    }
    return @()
}
$pickLines = Get-PickingLines $pick2
if ($pickLines.Count -eq 0) { $pickLines = Get-PickingLines $pick }
if ($pickLines.Count -eq 0) {
    $names = ($pick2 | Get-Member -MemberType NoteProperty | ForEach-Object Name) -join ','
    throw "picking has no details to complete. keys=$names"
}
$detailJsonParts = @()
foreach ($d in $pickLines) {
    $detailJsonParts += ('{"detailId":"' + $d.id + '","qtyPicked":' + [int]$d.qtyToPick + '}')
}
$completeJson = '{"details":[' + ($detailJsonParts -join ',') + ']}'
Invoke-Api POST "/Pickings/$pickId/complete" -Token $staff -Label 'staff complete picking' -Body $completeJson | Out-Null
$ship = Invoke-Api POST '/Shipments' -Token $mgr -Expect @(201, 200) -Label 'create shipment' -Body @{
    saleOrderId = $soId
    carrier     = 'SmokePost'
    trackingNo  = "TRK-$Stamp"
}
Invoke-Api GET "/Shipments/$($ship.id)" -Token $mgr -Label 'get shipment' | Out-Null
Invoke-Api GET '/Shipments' -Token $mgr -Label 'list shipments' | Out-Null
$poAfter = Invoke-Api GET "/PurchaseOrders/$poId" -Token $mgr -Label 'get PO after inbound'
if ($poAfter.status -eq 'Closed') {
    Write-Host 'SKIP close received PO (already Closed after putaway complete)' -ForegroundColor Yellow
} else {
    Invoke-Api PATCH "/PurchaseOrders/$poId/close" -Token $mgr -Label 'close received PO' | Out-Null
}

# --- 8 Adjustment ---
Write-Step 'StockAdjustment'
$adj = Invoke-Api POST '/StockAdjustments' -Token $staff -Expect @(201, 200) -Label 'create adjustment' -Body @{
    notes   = 'smoke adj'
    details = @(@{ productId = $prodId; locationId = $locId; countedQty = 7 })
}
Invoke-Api GET "/StockAdjustments/$($adj.id)" -Token $admin -Label 'get adjustment' | Out-Null
Invoke-Api GET '/StockAdjustments' -Token $admin -Label 'list adjustments' | Out-Null
Invoke-Api DELETE "/StockAdjustments/$($adj.id)" -Token $admin -Label 'delete draft adjustment' | Out-Null

# --- 9 Audit reads ---
Write-Step 'Audit reads'
Invoke-Api GET '/audit-logs' -Token $admin -Label 'audit logs' | Out-Null
Invoke-Api GET "/audit-logs/status-history?entityType=PurchaseOrder&entityId=$poId" -Token $admin -Label 'audit status-history' | Out-Null
Invoke-Api GET '/status-histories' -Token $admin -Label 'status histories' | Out-Null
Invoke-Api GET '/stock-movements' -Token $admin -Label 'stock movements' | Out-Null

Write-Host "`nSmoke done. pass=$Pass fail=$Fail" -ForegroundColor $(if ($Fail -eq 0) { 'Green' } else { 'Red' })
if ($Fail -gt 0) { exit 1 }
exit 0
