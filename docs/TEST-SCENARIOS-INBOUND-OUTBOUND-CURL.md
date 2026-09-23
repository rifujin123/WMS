# Kịch bản kiểm thử guard — Inbound, Outbound và CRUD bằng cURL

> **Mục đích:** đây là kiểm thử end-to-end thủ công có thể lặp lại cho các luồng quan trọng nhất của WMS. Kịch bản gọi API thật bằng `curl.exe` từ PowerShell, kiểm tra dữ liệu trả về và các bất biến nghiệp vụ. Không tạo project test, không tạo source test, không dùng test runner.
>
> **Phạm vi guard:** Purchase Order, Receiving, Put-away, Stock/Location, Sale Order, Picking, Shipment; cùng các CRUD có API tương ứng và ảnh hưởng trực tiếp đến inbound/outbound.

## 1. Khi nào phải chạy

Phải chạy **toàn bộ** kịch bản này trước khi báo hoàn thành bất kỳ thay đổi nào có thể ảnh hưởng tới:

- controller/API, DTO, service, repository, entity hoặc migration của các module trong phạm vi;
- tính tồn kho, reserve, location capacity hoặc stock movement;
- phân quyền/hành động theo trạng thái của inbound/outbound;
- frontend gọi các API trên, React Query invalidation hoặc mapping trạng thái.

Không được chạy trên cơ sở dữ liệu production hay dữ liệu người dùng thật. Script có tạo PO/SO/Receiving/Picking/Shipment và có thể để lại chứng từ đã hoàn thành để phục vụ audit. Hãy chạy trên local/test database riêng; mỗi lượt chạy tự sinh mã `RUN_ID` để không va chạm dữ liệu cũ.

## 2. Tiền điều kiện

1. API local đang chạy tại `http://localhost:5246`. Có thể đổi `$ApiBase` bên dưới nếu cần.
2. Database test đã migrate, seed và có ít nhất:
   - một `Admin` để đăng nhập;
   - một `WarehouseStaff` để được phân công;
   - một Warehouse, một Location kiểu `Storage` còn chỗ trống và một Product hoạt động.
3. Cài `curl.exe` (có sẵn trên Windows hiện đại) và PowerShell 5.1 trở lên.
4. Không hard-code password vào file. Đặt biến môi trường trước khi chạy:

```powershell
$env:WMS_TEST_PASSWORD = Read-Host 'Mật khẩu admin dùng cho database test'
```

> Nếu seed local giữ nguyên mật khẩu demo, người chạy tự nhập giá trị đó vào biến môi trường. Không ghi password vào lịch sử shell, source code hay tài liệu này.

## 3. Hành vi phải được chứng minh

### Inbound

| ID | Kiểm tra | Kết quả bắt buộc |
|---|---|---|
| IN-CRUD-01 | PO `Pending` tạo → cập nhật → đọc → xóa | Mọi request thành công; chỉ PO `Pending` được sửa/xóa. |
| IN-CRUD-02 | Receiving `Draft`: tạo → cập nhật → đọc → xóa | Thành công; xóa không được làm đổi số đã nhận của PO. |
| IN-01 | PO chính tạo và approve | Pending → Approved. |
| IN-02 | Receiving draft tạo rồi cập nhật | `Draft`; chi tiết cuối gồm `6 Ok + 4 Damaged` cùng Product. |
| IN-03 | Tạo put-away khi Receiving chưa confirm | Bị từ chối HTTP 400. |
| IN-04 | Confirm Receiving | Receiving `Confirmed`; PO `Received`; `ReceivedQuantity = 10`, tức cả `Damaged` vẫn là đã nhận. |
| IN-05 | Put-away detail `Damaged` | Bị từ chối HTTP 400. |
| IN-06 | Tổng put-away vượt số `Ok` | Bị từ chối HTTP 400. |
| IN-CRUD-03 | Put-away task `Open`: tạo → cập nhật → đọc → xóa | Thành công, không làm tăng tồn. |
| IN-07 | Put-away task hợp lệ hoàn tất | Chỉ `6 Ok` làm stock và `Location.CurrentQuantity` tăng đúng 6; PO tự `Closed` khi không còn task chưa hoàn thành. |

### Outbound

| ID | Kiểm tra | Kết quả bắt buộc |
|---|---|---|
| OUT-CRUD-01 | Sale Order `New`: tạo → cập nhật → đọc → xóa | Mọi request thành công. |
| OUT-01 | Sale Order chính tạo | `New`, số lượng yêu cầu là 6. |
| OUT-CRUD-02 | Picking: tạo → đọc → xóa | Tạo reserve 6; xóa release reserve đúng về giá trị ban đầu. |
| OUT-02 | Picking chính, assign, start và complete | `Open → Assigned → InProgress → Completed`; tất cả detail được pick đủ. |
| OUT-03 | Đồng bộ tồn khi complete picking | Với từng location được picking dùng: `Stock.OnhandQty` và `Location.CurrentQuantity` cùng giảm đúng `QtyToPick`; `ReservedQty` giảm đúng số đó. |
| OUT-04 | Sale Order sau complete | `Packed`. |
| OUT-05 | Shipment create và mark shipped | Shipment được tạo một lần; Sale Order `Packed → Shipped`. |

`Shipment` hiện chỉ có Create, Read và `mark-shipped`, **không có API update/delete**. Vì vậy OUT-05 là kiểm thử lifecycle đầy đủ của module này, không giả định CRUD chưa tồn tại.

## 4. Script cURL/Powershell đầy đủ

> Dán nguyên khối vào PowerShell **sau khi API test đã chạy**. Script fail-fast: một HTTP code hoặc bất biến sai sẽ `throw` ngay và không được báo pass.

```powershell
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ----- Cấu hình an toàn -----
$ApiBase = if ([string]::IsNullOrWhiteSpace($env:WMS_API_BASE)) { 'http://localhost:5246' } else { $env:WMS_API_BASE }
$AdminUsername = if ([string]::IsNullOrWhiteSpace($env:WMS_TEST_ADMIN)) { 'admin' } else { $env:WMS_TEST_ADMIN }
$StaffUsername = if ([string]::IsNullOrWhiteSpace($env:WMS_TEST_STAFF)) { 'nvhung' } else { $env:WMS_TEST_STAFF }
$ManagerUsername = if ([string]::IsNullOrWhiteSpace($env:WMS_TEST_MANAGER)) { 'manager1' } else { $env:WMS_TEST_MANAGER }
if ([string]::IsNullOrWhiteSpace($env:WMS_TEST_PASSWORD)) {
    throw 'Thiếu WMS_TEST_PASSWORD. Chỉ chạy trên database test/local.'
}
$RunId = "IT-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 6))"

function Assert-That {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT FAILED: $Message" }
}

function Invoke-WmsApi {
    param(
        [Parameter(Mandatory)] [ValidateSet('GET','POST','PUT','PATCH','DELETE')] [string]$Method,
        [Parameter(Mandatory)] [string]$Path,
        [object]$Body,
        [int[]]$ExpectedCodes = @(200)
    )

    $responseFile = New-TemporaryFile
    try {
        $curlArgs = @('-sS', '-o', $responseFile.FullName, '-w', '%{http_code}', '-X', $Method,
            "$ApiBase$Path", '-H', "Authorization: Bearer $script:Token")
        if ($null -ne $Body) {
            $curlArgs += @('-H', 'Content-Type: application/json', '--data',
                ($Body | ConvertTo-Json -Depth 12 -Compress))
        }
        $code = [int](& curl.exe @curlArgs)
        $raw = Get-Content -Raw -LiteralPath $responseFile.FullName
        if ($ExpectedCodes -notcontains $code) {
            throw "HTTP $code cho $Method $Path. Mong đợi: $($ExpectedCodes -join ', '). Response: $raw"
        }
        if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
        return $raw | ConvertFrom-Json
    }
    finally {
        Remove-Item -LiteralPath $responseFile.FullName -Force -ErrorAction SilentlyContinue
    }
}

# ----- Đăng nhập qua curl.exe -----
$loginFile = New-TemporaryFile
try {
    $loginBody = @{ username = $AdminUsername; password = $env:WMS_TEST_PASSWORD } |
        ConvertTo-Json -Compress
    $loginCode = [int](& curl.exe -sS -o $loginFile.FullName -w '%{http_code}' -X POST `
        "$ApiBase/api/Auth/login" -H 'Content-Type: application/json' --data $loginBody)
    $loginRaw = Get-Content -Raw -LiteralPath $loginFile.FullName
    Assert-That ($loginCode -eq 200) "Đăng nhập Admin phải trả HTTP 200. Response: $loginRaw"
    $script:Token = ($loginRaw | ConvertFrom-Json).accessToken
    Assert-That (-not [string]::IsNullOrWhiteSpace($script:Token)) 'Login phải trả accessToken.'
}
finally {
    Remove-Item -LiteralPath $loginFile.FullName -Force -ErrorAction SilentlyContinue
}

# ----- Chọn master data đã seed; không tạo Product vì endpoint Product bắt buộc upload ảnh -----
$products = @(Invoke-WmsApi GET '/api/Products/lookup')
$warehouses = @(Invoke-WmsApi GET '/api/Warehouses/lookup')
$staffs = @(Invoke-WmsApi GET '/api/Users/lookup?role=WarehouseStaff')
Assert-That ($products.Count -gt 0) 'Cần ít nhất một Product trên test DB.'
Assert-That ($warehouses.Count -gt 0) 'Cần ít nhất một Warehouse trên test DB.'
Assert-That ($staffs.Count -gt 0) 'Cần ít nhất một WarehouseStaff trên test DB.'
$product = $products[0]
$warehouse = $warehouses[0]
$locations = @(Invoke-WmsApi GET "/api/Locations/lookup?warehouseId=$($warehouse.id)")
$location = $locations | Where-Object { $_.locationType -eq 'Storage' -and $_.maxQuantity -gt $_.currentQuantity } |
    Select-Object -First 1
Assert-That ($null -ne $location) 'Cần một Storage location còn sức chứa trong warehouse được chọn.'
$staff = $staffs[0]

# Snapshot cho IN-07 và OUT-03
$initialStockAtLocation = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") |
    Where-Object { $_.locationId -eq $location.id } | Select-Object -First 1)[0]
$initialOnhand = if ($null -eq $initialStockAtLocation) { 0 } else { [int]$initialStockAtLocation.onhandQty }
$initialReserved = if ($null -eq $initialStockAtLocation) { 0 } else { [int]$initialStockAtLocation.reservedQty }
$initialLocationQty = [int]$location.currentQuantity

# ===== IN-CRUD-01: Purchase Order CRUD ở trạng thái Pending =====
$poCrud = Invoke-WmsApi POST '/api/PurchaseOrders' @{
    poNumber = "$RunId-PO-CRUD"; vendorName = 'Vendor test CRUD'
    purchaseOrderDetails = @(@{ productId = $product.id; orderedQuantity = 1 })
} @(201)
Assert-That ($poCrud.status -eq 'Pending') 'PO mới phải ở trạng thái Pending.'
$poCrud = Invoke-WmsApi PUT "/api/PurchaseOrders/$($poCrud.id)" @{
    vendorName = 'Vendor test CRUD updated'
    purchaseOrderDetails = @(@{ productId = $product.id; orderedQuantity = 2 })
}
Assert-That ($poCrud.purchaseOrderDetails[0].orderedQuantity -eq 2) 'PO update phải đổi quantity thành 2.'
$null = Invoke-WmsApi GET "/api/PurchaseOrders/$($poCrud.id)"
$null = Invoke-WmsApi DELETE "/api/PurchaseOrders/$($poCrud.id)" $null @(200)

# ===== IN-CRUD-02: Receiving Draft create → update → read → delete =====
$poReceivingCrud = Invoke-WmsApi POST '/api/PurchaseOrders' @{
    poNumber = "$RunId-PO-RC-CRUD"; vendorName = 'Vendor receiving CRUD'
    purchaseOrderDetails = @(@{ productId = $product.id; orderedQuantity = 1 })
} @(201)
$poReceivingCrud = Invoke-WmsApi PATCH "/api/PurchaseOrders/$($poReceivingCrud.id)/approve" $null
$receivingCrud = Invoke-WmsApi POST '/api/Receivings' @{
    purchaseOrderId = $poReceivingCrud.id; notes = "$RunId receiving CRUD"
    details = @(@{ productId = $product.id; expectedQuantity = 1; actualQuantity = 1; condition = 'Ok' })
} @(201)
$receivingCrud = Invoke-WmsApi PUT "/api/Receivings/$($receivingCrud.id)" @{
    purchaseOrderId = $poReceivingCrud.id; notes = "$RunId receiving CRUD updated"
    details = @(@{ productId = $product.id; expectedQuantity = 1; actualQuantity = 1; condition = 'Ok' })
}
Assert-That ($receivingCrud.notes -eq "$RunId receiving CRUD updated") 'Receiving update must change notes.'
$null = Invoke-WmsApi GET "/api/Receivings/$($receivingCrud.id)"
$null = Invoke-WmsApi DELETE "/api/Receivings/$($receivingCrud.id)" $null @(200)
$poReceivingCrud = Invoke-WmsApi GET "/api/PurchaseOrders/$($poReceivingCrud.id)"
Assert-That ($poReceivingCrud.status -eq 'Approved') 'Deleting a Draft Receiving must not change PO status.'

# ===== IN-01: PO chính =====
$po = Invoke-WmsApi POST '/api/PurchaseOrders' @{
    poNumber = "$RunId-PO"; vendorName = 'Vendor inbound guard'
    purchaseOrderDetails = @(@{ productId = $product.id; orderedQuantity = 10 })
} @(201)
$po = Invoke-WmsApi PATCH "/api/PurchaseOrders/$($po.id)/approve" $null
Assert-That ($po.status -eq 'Approved') 'PO chính phải chuyển Approved.'

# ===== IN-02: Receiving Draft create + update =====
$receiving = Invoke-WmsApi POST '/api/Receivings' @{
    purchaseOrderId = $po.id; notes = "$RunId receiving draft"
    details = @(
        @{ productId = $product.id; expectedQuantity = 5; actualQuantity = 5; condition = 'Ok' },
        @{ productId = $product.id; expectedQuantity = 5; actualQuantity = 5; condition = 'Damaged' }
    )
} @(201)
Assert-That ($receiving.status -eq 'Draft') 'Receiving mới phải là Draft.'
$receiving = Invoke-WmsApi PUT "/api/Receivings/$($receiving.id)" @{
    purchaseOrderId = $po.id; notes = "$RunId receiving final"
    details = @(
        @{ productId = $product.id; expectedQuantity = 6; actualQuantity = 6; condition = 'Ok' },
        @{ productId = $product.id; expectedQuantity = 4; actualQuantity = 4; condition = 'Damaged' }
    )
}
$okDetail = @($receiving.details | Where-Object { $_.condition -eq 'Ok' })[0]
$damagedDetail = @($receiving.details | Where-Object { $_.condition -eq 'Damaged' })[0]
Assert-That (($null -ne $okDetail) -and ($null -ne $damagedDetail)) 'Receiving cập nhật phải có cả dòng Ok và Damaged.'

# ===== IN-03: Không được put-away trước Confirm =====
$null = Invoke-WmsApi POST '/api/PutAwayTasks' @{
    receivingDetailId = $okDetail.id; productId = $product.id; quantity = 1; toLocationId = $location.id
} @(400)

# ===== IN-04: Confirm; Damaged vẫn là đã nhận =====
$receiving = Invoke-WmsApi POST "/api/Receivings/$($receiving.id)/confirm" $null
Assert-That ($receiving.status -eq 'Confirmed') 'Receiving phải chuyển Confirmed.'
$poAfterReceiving = Invoke-WmsApi GET "/api/PurchaseOrders/$($po.id)"
Assert-That ($poAfterReceiving.status -eq 'Received') 'PO phải Received dù có 4 Damaged.'
Assert-That (([int]$poAfterReceiving.purchaseOrderDetails[0].receivedQuantity) -eq 10) 'Damaged vẫn phải được tính vào ReceivedQuantity.'

# ===== IN-05, IN-06: guard condition và quantity =====
$null = Invoke-WmsApi POST '/api/PutAwayTasks' @{
    receivingDetailId = $damagedDetail.id; productId = $product.id; quantity = 4; toLocationId = $location.id
} @(400)
$null = Invoke-WmsApi POST '/api/PutAwayTasks' @{
    receivingDetailId = $okDetail.id; productId = $product.id; quantity = 7; toLocationId = $location.id
} @(400)

# ===== IN-CRUD-03: Put-away Open create → update → read → delete =====
$tempTask = Invoke-WmsApi POST '/api/PutAwayTasks' @{
    receivingDetailId = $okDetail.id; productId = $product.id; quantity = 2; toLocationId = $location.id
} @(201)
$tempTask = Invoke-WmsApi PUT "/api/PutAwayTasks/$($tempTask.id)" @{
    receivingDetailId = $okDetail.id; productId = $product.id; quantity = 3; toLocationId = $location.id
}
Assert-That ($tempTask.quantity -eq 3) 'Put-away update phải đổi quantity thành 3.'
$null = Invoke-WmsApi GET "/api/PutAwayTasks/$($tempTask.id)"
$null = Invoke-WmsApi DELETE "/api/PutAwayTasks/$($tempTask.id)" $null @(200)

# ===== IN-07: Put-away Ok hoàn tất, tăng Stock + Location đúng 6 =====
$putAway = Invoke-WmsApi POST '/api/PutAwayTasks' @{
    receivingDetailId = $okDetail.id; productId = $product.id; quantity = 6; toLocationId = $location.id
} @(201)
$putAway = Invoke-WmsApi POST "/api/PutAwayTasks/$($putAway.id)/assign" @{ userId = $staff.id }
$putAway = Invoke-WmsApi POST "/api/PutAwayTasks/$($putAway.id)/start" $null
Assert-That ($putAway.status -eq 'InProgress') 'Put-away phải InProgress sau start.'
$putAway = Invoke-WmsApi POST "/api/PutAwayTasks/$($putAway.id)/complete" $null
Assert-That ($putAway.status -eq 'Completed') 'Put-away phải Completed.'
$stockAfterInbound = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") |
    Where-Object { $_.locationId -eq $location.id } | Select-Object -First 1)[0]
$locationAfterInbound = Invoke-WmsApi GET "/api/Locations/$($location.id)"
Assert-That ([int]$stockAfterInbound.onhandQty -eq ($initialOnhand + 6)) 'Inbound chỉ tăng OnhandQty bằng 6 Ok.'
Assert-That ([int]$locationAfterInbound.currentQuantity -eq ($initialLocationQty + 6)) 'Inbound chỉ tăng Location.CurrentQuantity bằng 6 Ok.'
$poAfterPutAway = Invoke-WmsApi GET "/api/PurchaseOrders/$($po.id)"
Assert-That ($poAfterPutAway.status -eq 'Closed') 'PO phải tự Closed khi mọi put-away task hợp lệ đã hoàn tất.'

# ===== OUT-CRUD-01: Sale Order New create → update → read → delete =====
$soCrud = Invoke-WmsApi POST '/api/SaleOrders' @{
    orderNo = "$RunId-SO-CRUD"; customerName = 'Customer test CRUD'; orderDate = (Get-Date).ToUniversalTime().ToString('o')
    saleOrderDetails = @(@{ productId = $product.id; quantity = 1 })
} @(201)
$soCrud = Invoke-WmsApi PUT "/api/SaleOrders/$($soCrud.id)" @{
    orderNo = "$RunId-SO-CRUD"; customerName = 'Customer test CRUD updated'; orderDate = (Get-Date).ToUniversalTime().ToString('o')
    saleOrderDetails = @(@{ productId = $product.id; quantity = 2 })
}
Assert-That ($soCrud.saleOrderDetails[0].quantity -eq 2) 'SO update phải đổi quantity thành 2.'
$null = Invoke-WmsApi GET "/api/SaleOrders/$($soCrud.id)"
$null = Invoke-WmsApi DELETE "/api/SaleOrders/$($soCrud.id)" $null @(200)

# ===== OUT-01 và OUT-CRUD-02: Sale Order + Picking create/read/delete/release reserve =====
$saleOrder = Invoke-WmsApi POST '/api/SaleOrders' @{
    orderNo = "$RunId-SO"; customerName = 'Customer outbound guard'; orderDate = (Get-Date).ToUniversalTime().ToString('o')
    saleOrderDetails = @(@{ productId = $product.id; quantity = 6 })
} @(201)
Assert-That ($saleOrder.status -eq 'New') 'Sale Order mới phải là New.'
$pickingCrud = Invoke-WmsApi POST '/api/Pickings' @{ saleOrderId = $saleOrder.id; warehouseId = $warehouse.id } @(201)
Assert-That ((@($pickingCrud.details | Measure-Object -Property qtyToPick -Sum).Sum) -eq 6) 'Picking CRUD phải reserve đủ 6.'
$null = Invoke-WmsApi GET "/api/Pickings/$($pickingCrud.id)"
$null = Invoke-WmsApi DELETE "/api/Pickings/$($pickingCrud.id)" $null @(200)
$stocksAfterPickingDelete = @(Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)")
$stockAtTestLocationAfterDelete = @($stocksAfterPickingDelete | Where-Object { $_.locationId -eq $location.id })[0]
Assert-That ([int]$stockAtTestLocationAfterDelete.reservedQty -eq $initialReserved) 'Xóa Picking Open phải release reserve về baseline.'

# ===== OUT-02, OUT-03, OUT-04: Picking chính =====
$picking = Invoke-WmsApi POST '/api/Pickings' @{ saleOrderId = $saleOrder.id; warehouseId = $warehouse.id } @(201)
$picking = Invoke-WmsApi POST "/api/Pickings/$($picking.id)/assign" @{ userId = $staff.id }
$picking = Invoke-WmsApi POST "/api/Pickings/$($picking.id)/start" $null
Assert-That ($picking.status -eq 'InProgress') 'Picking phải InProgress sau start.'

# Snapshot từng location mà allocator thực tế đã chọn.
$beforePick = foreach ($detail in $picking.details) {
    $stock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($detail.productId)") |
        Where-Object { $_.locationId -eq $detail.locationId })[0]
    $loc = Invoke-WmsApi GET "/api/Locations/$($detail.locationId)"
    [pscustomobject]@{ DetailId = $detail.id; ProductId = $detail.productId; LocationId = $detail.locationId;
        Qty = [int]$detail.qtyToPick; Onhand = [int]$stock.onhandQty; Reserved = [int]$stock.reservedQty;
        LocationQty = [int]$loc.currentQuantity }
}
$completeBody = @{ details = @($picking.details | ForEach-Object { @{ detailId = $_.id; qtyPicked = $_.qtyToPick } }) }
$picking = Invoke-WmsApi POST "/api/Pickings/$($picking.id)/complete" $completeBody
Assert-That ($picking.status -eq 'Completed') 'Picking phải Completed.'
foreach ($snapshot in $beforePick) {
    $stock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($snapshot.ProductId)") |
        Where-Object { $_.locationId -eq $snapshot.LocationId })[0]
    $loc = Invoke-WmsApi GET "/api/Locations/$($snapshot.LocationId)"
    Assert-That ([int]$stock.onhandQty -eq ($snapshot.Onhand - $snapshot.Qty)) "Onhand phải giảm đúng tại location $($snapshot.LocationId)."
    Assert-That ([int]$stock.reservedQty -eq ($snapshot.Reserved - $snapshot.Qty)) "Reserved phải giảm đúng tại location $($snapshot.LocationId)."
    Assert-That ([int]$loc.currentQuantity -eq ($snapshot.LocationQty - $snapshot.Qty)) "Location.CurrentQuantity phải giảm đúng tại location $($snapshot.LocationId)."
}
$saleOrder = Invoke-WmsApi GET "/api/SaleOrders/$($saleOrder.id)"
Assert-That ($saleOrder.status -eq 'Packed') 'Sale Order phải Packed sau khi pick đủ.'

# ===== OUT-05: Shipment lifecycle =====
$shipment = Invoke-WmsApi POST '/api/Shipments' @{ saleOrderId = $saleOrder.id; carrier = 'CURL Guard'; trackingNo = "$RunId-TRACK" } @(201)
$shipment = Invoke-WmsApi POST "/api/Shipments/$($shipment.id)/mark-shipped" $null
Assert-That ($null -ne $shipment.shippedDate) 'Shipment phải có ShippedDate sau mark-shipped.'
$saleOrder = Invoke-WmsApi GET "/api/SaleOrders/$($saleOrder.id)"
Assert-That ($saleOrder.status -eq 'Shipped') 'Sale Order phải Shipped sau shipment mark-shipped.'

Write-Host "Inbound/Outbound đã PASS. Chạy thêm mục 7 trước khi báo overall PASS. RunId: $RunId" -ForegroundColor Yellow
```

## 5. Điều kiện pass/fail và cách báo cáo

- **PASS:** script in dòng `PASS — Inbound/Outbound CURL guard hoàn tất` và không có exception.
- **FAIL:** bất kỳ assertion, HTTP code hoặc API error nào khác kết quả trong bảng. Không được nói tính năng hoàn tất; phải nêu chính xác step ID, request, response và nguyên nhân.
- Khi thay đổi chỉ ở frontend nhưng có gọi các API phạm vi guard, vẫn chạy script để xác nhận integration API. Sau đó chạy thêm `npm run lint` và `npm run build`.
- Khi thay đổi backend, chạy script và `dotnet build WMS.sln`; nếu repository có test project ở tương lai thì chạy `dotnet test WMS.sln` thêm. Hiện tại không được tạo test project chỉ để đáp ứng guard này.

## 6. Giới hạn đã biết của guard

1. Kịch bản không tự dọn các chứng từ hoàn thành vì hệ thống cố ý không cho xóa chứng từ đã qua lifecycle; đây là lý do phải dùng database test riêng.
2. Kịch bản không kiểm thử upload ảnh Product/OCR invoice vì hai tính năng phụ thuộc file và dịch vụ ngoài; chúng phải có kịch bản riêng nếu bị sửa.
3. Kịch bản không chứng minh race condition của hai request put-away đồng thời. Đây là rủi ro concurrency đã biết và cần một bài kiểm thử tải/transaction riêng khi xử lý P1.



## 7. Inventory Audit MVP cURL guard

Run this extension in the same PowerShell session as section 4, only against the local or test database. It covers Staff Draft creation, Admin-only approval, positive/negative/zero deltas, repeat approval, and rollback for capacity or reservation violations. Use `WMS_TEST_STAFF` and `WMS_TEST_MANAGER` environment variables for the seeded WarehouseStaff and WarehouseManager accounts; keep the password only in `WMS_TEST_PASSWORD`.

| Step | Required assertion |
|---|---|
| AUD-01 | WarehouseStaff creates a Draft with CountedQty >= 0; stock, location, and stock movements remain unchanged; list/detail access excludes other creators. |
| AUD-02 | WarehouseStaff and WarehouseManager receive 403 for approve/delete; Admin approves a positive delta and creates one Adjustment movement with that delta. |
| AUD-03 | Admin approves a negative delta and CountedQty = 0 when ReservedQty = 0. |
| AUD-04 | Re-approving an Approved document returns 400 and creates no new movement. |
| AUD-05 | A Draft below a positive ReservedQty returns 400; every stock, location, movement, and status value remains unchanged. |
| AUD-06 | A Draft exceeding MaxQuantity returns 400; every stock, location, movement, and status value remains unchanged. |

For each AUD step, use the existing `Invoke-WmsApi` helper with tokens obtained from `POST /api/Auth/login`, then assert the listed response code and the snapshots from `/api/Stocks`, `/api/Locations/{id}`, `/api/stock-movements`, and `/api/StockAdjustments/{id}`. The final `PASS` line in section 4 is valid only after AUD-01 through AUD-06 pass.


Chạy đoạn mở rộng này **sau** script ở mục 4 trong cùng PowerShell session. Script dùng `WMS_TEST_STAFF`, `WMS_TEST_MANAGER` và `WMS_TEST_PASSWORD` để lấy JWT ngắn hạn qua API, không ghi password hoặc JWT vào source/log. Chỉ được báo overall PASS khi đoạn này in dòng PASS màu xanh.

```powershell
$AdminToken = $script:Token

function Get-WmsAccessToken {
    param([Parameter(Mandatory)] [string]$Username, [Parameter(Mandatory)] [string]$RoleLabel)

    $loginFile = New-TemporaryFile
    try {
        $loginBody = @{ username = $Username; password = $env:WMS_TEST_PASSWORD } | ConvertTo-Json -Compress
        $loginCode = [int](& curl.exe -sS -o $loginFile.FullName -w '%{http_code}' -X POST `
            "$ApiBase/api/Auth/login" -H 'Content-Type: application/json' --data $loginBody)
        $loginRaw = Get-Content -Raw -LiteralPath $loginFile.FullName
        Assert-That ($loginCode -eq 200) "Đăng nhập $RoleLabel phải trả HTTP 200. Response: $loginRaw"
        $token = ($loginRaw | ConvertFrom-Json).accessToken
        Assert-That (-not [string]::IsNullOrWhiteSpace($token)) "Login $RoleLabel phải trả accessToken."
        return $token
    }
    finally {
        Remove-Item -LiteralPath $loginFile.FullName -Force -ErrorAction SilentlyContinue
    }
}

$StaffToken = Get-WmsAccessToken -Username $StaffUsername -RoleLabel 'WarehouseStaff'
$ManagerToken = Get-WmsAccessToken -Username $ManagerUsername -RoleLabel 'WarehouseManager'
$beforeStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") | Where-Object { $_.locationId -eq $location.id })[0]
$beforeLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
Assert-That ([int]$beforeStock.reservedQty -eq 0) 'AUD requires ReservedQty = 0.'
Assert-That ([int]$beforeLocation.currentQuantity -lt [int]$beforeLocation.maxQuantity) 'AUD requires free capacity.'

$adminOnlyDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD admin-only draft"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = [int]$beforeStock.onhandQty })
} @(201)

$script:Token = $StaffToken
$staffListBeforeCreate = @(Invoke-WmsApi GET '/api/StockAdjustments')
Assert-That (-not ($staffListBeforeCreate.id -contains $adminOnlyDraft.id)) 'AUD-01 Staff list must not include another creator''s Draft.'
$null = Invoke-WmsApi GET "/api/StockAdjustments/$($adminOnlyDraft.id)" $null @(404)
$staffDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD staff draft"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = ([int]$beforeStock.onhandQty + 1) })
} @(201)
Assert-That ($staffDraft.status -eq 'Draft') 'AUD-01 Staff must create Draft.'
$staffList = @(Invoke-WmsApi GET '/api/StockAdjustments')
Assert-That ($staffList.id -contains $staffDraft.id) 'AUD-01 Staff list must include its own Draft.'
$staffDetail = Invoke-WmsApi GET "/api/StockAdjustments/$($staffDraft.id)"
Assert-That ($staffDetail.id -eq $staffDraft.id) 'AUD-01 Staff must read its own Draft.'
$afterDraftStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") | Where-Object { $_.locationId -eq $location.id })[0]
$afterDraftLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
Assert-That ([int]$afterDraftStock.onhandQty -eq [int]$beforeStock.onhandQty) 'AUD-01 Draft changed Stock.'
Assert-That ([int]$afterDraftLocation.currentQuantity -eq [int]$beforeLocation.currentQuantity) 'AUD-01 Draft changed Location.'
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($staffDraft.id)/approve" $null @(403)
$null = Invoke-WmsApi DELETE "/api/StockAdjustments/$($staffDraft.id)" $null @(403)

$script:Token = $ManagerToken
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($staffDraft.id)/approve" $null @(403)
$null = Invoke-WmsApi DELETE "/api/StockAdjustments/$($staffDraft.id)" $null @(403)

$script:Token = $AdminToken
$beforeInvalidDraftCount = @(Invoke-WmsApi GET '/api/StockAdjustments').Count
$missingProductId = [Guid]::NewGuid()
$missingLocationId = [Guid]::NewGuid()

$null = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD invalid duplicate"
    details = @(
        @{ productId = $product.id; locationId = $location.id; countedQty = 0 },
        @{ productId = $product.id; locationId = $location.id; countedQty = 0 }
    )
} @(400)
Assert-That (@(Invoke-WmsApi GET '/api/StockAdjustments').Count -eq $beforeInvalidDraftCount) 'AUD validation duplicate Product+Location must not create Draft.'

$null = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD invalid product"
    details = @(@{ productId = $missingProductId; locationId = $location.id; countedQty = 0 })
} @(400)
Assert-That (@(Invoke-WmsApi GET '/api/StockAdjustments').Count -eq $beforeInvalidDraftCount) 'AUD validation missing Product must not create Draft.'

$null = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD invalid location"
    details = @(@{ productId = $product.id; locationId = $missingLocationId; countedQty = 0 })
} @(400)
Assert-That (@(Invoke-WmsApi GET '/api/StockAdjustments').Count -eq $beforeInvalidDraftCount) 'AUD validation missing Location must not create Draft.'

$null = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD invalid counted quantity"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = -1 })
} @(400)
Assert-That (@(Invoke-WmsApi GET '/api/StockAdjustments').Count -eq $beforeInvalidDraftCount) 'AUD validation negative CountedQty must not create Draft.'

$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($staffDraft.id)/approve" $null @(200)
$positiveStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") | Where-Object { $_.locationId -eq $location.id })[0]
Assert-That ([int]$positiveStock.onhandQty -eq ([int]$beforeStock.onhandQty + 1)) 'AUD-02 positive delta did not apply.'
$positiveLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
Assert-That ([int]$positiveLocation.currentQuantity -eq ([int]$beforeLocation.currentQuantity + 1)) 'AUD-02 positive delta did not update Location.'
$positiveMovements = @((Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").items | Where-Object { $_.notes -like "*$($staffDraft.adjustmentNo)*" })
Assert-That (($positiveMovements.Count -eq 1) -and ([int]$positiveMovements[0].qty -eq 1) -and ($positiveMovements[0].movementType -eq 'Adjustment')) 'AUD-02 must create one Adjustment movement with Qty +1.'

$negativeDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD negative"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = [int]$beforeStock.onhandQty })
} @(201)
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($negativeDraft.id)/approve" $null @(200)
$negativeLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
Assert-That ([int]$negativeLocation.currentQuantity -eq [int]$beforeLocation.currentQuantity) 'AUD-03 negative delta did not update Location.'
$negativeMovements = @((Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").items | Where-Object { $_.notes -like "*$($negativeDraft.adjustmentNo)*" })
Assert-That (($negativeMovements.Count -eq 1) -and ([int]$negativeMovements[0].qty -eq -1)) 'AUD-03 must create one Adjustment movement with Qty -1.'
$zeroDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD counted zero"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = 0 })
} @(201)
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($zeroDraft.id)/approve" $null @(200)
$afterZero = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") | Where-Object { $_.locationId -eq $location.id })[0]
Assert-That ([int]$afterZero.onhandQty -eq 0) 'AUD-03 CountedQty = 0 did not apply.'
$zeroLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
Assert-That ([int]$zeroLocation.currentQuantity -eq ([int]$beforeLocation.currentQuantity - [int]$beforeStock.onhandQty)) 'AUD-03 CountedQty = 0 did not update Location.'
$zeroMovements = @((Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").items | Where-Object { $_.notes -like "*$($zeroDraft.adjustmentNo)*" })
Assert-That (($zeroMovements.Count -eq 1) -and ([int]$zeroMovements[0].qty -eq (-[int]$beforeStock.onhandQty))) 'AUD-03 must create one Adjustment movement with the zero-count delta.'
$movementCount = (Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").totalCount
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($zeroDraft.id)/approve" $null @(400)
Assert-That ((Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").totalCount -eq $movementCount) 'AUD-04 repeated approval created a movement.'

# ===== AUD-05: Approval below ReservedQty must roll back completely =====
# Seed exactly one unit at the known test location, then reserve it through the standard SaleOrder/Picking flow.
$reserveSeedDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD reserve seed"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = 1 })
} @(201)
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($reserveSeedDraft.id)/approve" $null @(200)

$reserveSaleOrder = Invoke-WmsApi POST '/api/SaleOrders' @{
    orderNo = "$RunId-AUD-RESERVE"; customerName = 'Customer audit reserve guard'; orderDate = (Get-Date).ToUniversalTime().ToString('o')
    saleOrderDetails = @(@{ productId = $product.id; quantity = 1 })
} @(201)
$reservePicking = Invoke-WmsApi POST '/api/Pickings' @{ saleOrderId = $reserveSaleOrder.id; warehouseId = $warehouse.id } @(201)
$reserveDetail = @($reservePicking.details)[0]
Assert-That ($null -ne $reserveDetail) 'AUD-05 reserve Picking must contain a detail.'
$reservedStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($reserveDetail.productId)") | Where-Object { $_.locationId -eq $reserveDetail.locationId })[0]
$reservedLocation = Invoke-WmsApi GET "/api/Locations/$($reserveDetail.locationId)"
$reservedMovementCount = (Invoke-WmsApi GET "/api/stock-movements?productId=$($reserveDetail.productId)&locationId=$($reserveDetail.locationId)&movementType=Adjustment").totalCount
Assert-That ([int]$reservedStock.reservedQty -gt 0) 'AUD-05 requires a positive ReservedQty.'

$reservedRollbackDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD reserved rollback"
    details = @(@{ productId = $reserveDetail.productId; locationId = $reserveDetail.locationId; countedQty = ([int]$reservedStock.reservedQty - 1) })
} @(201)
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($reservedRollbackDraft.id)/approve" $null @(400)
$afterReservedRollbackStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($reserveDetail.productId)") | Where-Object { $_.locationId -eq $reserveDetail.locationId })[0]
$afterReservedRollbackLocation = Invoke-WmsApi GET "/api/Locations/$($reserveDetail.locationId)"
$afterReservedRollbackMovementCount = (Invoke-WmsApi GET "/api/stock-movements?productId=$($reserveDetail.productId)&locationId=$($reserveDetail.locationId)&movementType=Adjustment").totalCount
$reservedRollbackDraft = Invoke-WmsApi GET "/api/StockAdjustments/$($reservedRollbackDraft.id)"
Assert-That ($reservedRollbackDraft.status -eq 'Draft') 'AUD-05 failed approval below ReservedQty must remain Draft.'
Assert-That ([int]$afterReservedRollbackStock.onhandQty -eq [int]$reservedStock.onhandQty) 'AUD-05 failed approval below ReservedQty changed Stock.'
Assert-That ([int]$afterReservedRollbackStock.reservedQty -eq [int]$reservedStock.reservedQty) 'AUD-05 failed approval below ReservedQty changed ReservedQty.'
Assert-That ([int]$afterReservedRollbackLocation.currentQuantity -eq [int]$reservedLocation.currentQuantity) 'AUD-05 failed approval below ReservedQty changed Location.'
Assert-That ($afterReservedRollbackMovementCount -eq $reservedMovementCount) 'AUD-05 failed approval below ReservedQty created a movement.'

# Release the temporary reserve and restore the seeded location for the test database.
$null = Invoke-WmsApi DELETE "/api/Pickings/$($reservePicking.id)" $null @(200)
$reserveCleanupDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD reserve cleanup"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = 0 })
} @(201)
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($reserveCleanupDraft.id)/approve" $null @(200)

# ===== AUD-06: Approval exceeding location capacity must roll back completely =====
$beforeRollbackStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") | Where-Object { $_.locationId -eq $location.id })[0]
$beforeRollbackLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
$beforeRollbackMovementCount = (Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").totalCount
$rollbackDraft = Invoke-WmsApi POST '/api/StockAdjustments' @{
    notes = "$RunId AUD capacity rollback"
    details = @(@{ productId = $product.id; locationId = $location.id; countedQty = ([int]$beforeRollbackStock.onhandQty + ([int]$beforeRollbackLocation.maxQuantity - [int]$beforeRollbackLocation.currentQuantity) + 1) })
} @(201)
$null = Invoke-WmsApi PATCH "/api/StockAdjustments/$($rollbackDraft.id)/approve" $null @(400)
$afterRollbackStock = @((Invoke-WmsApi GET "/api/Stocks?productId=$($product.id)") | Where-Object { $_.locationId -eq $location.id })[0]
$afterRollbackLocation = Invoke-WmsApi GET "/api/Locations/$($location.id)"
$afterRollbackMovementCount = (Invoke-WmsApi GET "/api/stock-movements?productId=$($product.id)&locationId=$($location.id)&movementType=Adjustment").totalCount
$rollbackDraft = Invoke-WmsApi GET "/api/StockAdjustments/$($rollbackDraft.id)"
Assert-That ($rollbackDraft.status -eq 'Draft') 'AUD-06 failed approval must remain Draft.'
Assert-That ([int]$afterRollbackStock.onhandQty -eq [int]$beforeRollbackStock.onhandQty) 'AUD-06 failed approval changed Stock.'
Assert-That ([int]$afterRollbackLocation.currentQuantity -eq [int]$beforeRollbackLocation.currentQuantity) 'AUD-06 failed approval changed Location.'
Assert-That ($afterRollbackMovementCount -eq $beforeRollbackMovementCount) 'AUD-06 failed approval created a movement.'
Write-Host "PASS — Inbound/Outbound và Inventory Audit CURL guard hoàn tất. RunId: $RunId" -ForegroundColor Green
```
