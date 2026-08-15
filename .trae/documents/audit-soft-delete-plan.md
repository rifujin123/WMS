# WMS Audit And Soft Delete Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans (inline execution is acceptable after approval). Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bảo đảm mọi thao tác tạo, sửa, xóa, duyệt, assign, start, complete, confirm, close và mọi thay đổi trạng thái trong WMS đều lưu được người thực hiện, thời điểm và lịch sử trước/sau; dữ liệu xóa được giữ bằng soft delete.

**Architecture:** Dùng `ICurrentUserService` để lấy actor thống nhất từ JWT, `BaseAuditableEntity` cho audit hiện tại, và `AuditLog`/`StatusHistory` cho lịch sử bất biến. `WmsDbContext.SaveChangesAsync` sẽ tự gắn Created/Updated/Deleted và tạo audit log cho thay đổi entity; các workflow service tạo status/event history trong cùng transaction. Soft delete được thực hiện bằng ChangeTracker và global query filter.

**Tech Stack:** .NET 8, ASP.NET Core, EF Core 8 SQL Server, ASP.NET Identity/JWT, AutoMapper.

---

## 1. Phạm vi đã chốt

- Audit áp dụng cho toàn bộ entity nghiệp vụ kế thừa `BaseAuditableEntity`, gồm aggregate, detail, stock, stock movement và RMA.
- User không kế thừa `BaseAuditableEntity` vì đang kế thừa `IdentityUser<Guid>`; thay đổi User cũng phải ghi `AuditLog`.
- Xóa là soft delete: bản ghi giữ lại, mặc định không xuất hiện trong query; lưu actor và thời điểm xóa.
- Mọi thay đổi `Status` ghi `StatusHistory`, kể cả thay đổi tự động như `SaleOrder Picking -> Packed` hoặc PO `Approved -> Received`.
- Các action nghiệp vụ ghi `AuditLog` với action rõ ràng: `Approved`, `Confirmed`, `Assigned`, `Started`, `Completed`, `Closed`, `UploadedImage`, `PasswordChanged`.
- Audit log và thay đổi dữ liệu phải nằm trong cùng transaction/use case.
- Không sửa migration cũ; tạo migration mới.
- Không yêu cầu thay đổi business flow ngoài việc bổ sung actor, lịch sử và soft delete.

## 2. Hiện trạng

- [BaseAuditableEntity.cs](../backend/WMS-mini/src/WMS.Domain/Common/BaseAuditableEntity.cs) chỉ có `Id`, `CreatedById`, `CreatedDate`; thiếu Updated/Deleted.
- `WmsDbContext` chưa có audit DbSet, query filter hoặc SaveChanges override.
- Các repository `Sql*Repository` tự gọi `SaveChangesAsync` trong Add/Update/Delete, làm commit bị phân tán.
- PurchaseOrder và StockAdjustment đã có ApprovedBy/ApprovedDate; Receiving có ReceivedBy/ReceivedDate; các field này phải giữ và bổ sung history.
- PutAway/Picking chỉ lưu người được assign, chưa lưu người assign và actor start/complete.
- Controller đang tự đọc claim không nhất quán; nhiều command không truyền actor.
- Không có AuditLog/StatusHistory và không có endpoint đọc lịch sử.

## 3. Files dự kiến

### Tạo

- `backend/WMS-mini/src/WMS.Domain/Entities/AuditLog.cs`
- `backend/WMS-mini/src/WMS.Domain/Entities/StatusHistory.cs`
- `backend/WMS-mini/src/WMS.Application/Interfaces/ICurrentUserService.cs`
- `backend/WMS-mini/src/WMS.Application/Interfaces/IAuditLogRepository.cs`
- `backend/WMS-mini/src/WMS.Application/Interfaces/IAuditLogService.cs`
- `backend/WMS-mini/src/WMS.Infrastructure/Services/CurrentUserService.cs`
- `backend/WMS-mini/src/WMS.Infrastructure/Services/AuditLogService.cs`
- `backend/WMS-mini/src/WMS.Infrastructure/Repositories/SqlAuditLogRepository.cs`
- `backend/WMS-mini/src/WMS.API/Controllers/AuditLogsController.cs`
- Audit DTO/query files under `backend/WMS-mini/src/WMS.Application/DTOs/`
- EF migration and designer under `backend/WMS-mini/src/WMS.Infrastructure/Migrations/`

### Sửa

- `WMS.Domain/Common/BaseAuditableEntity.cs`
- `WMS.Domain/Entities/User.cs`
- `WMS.Infrastructure/Data/WmsDbContext.cs`
- `WMS.API/Program.cs`
- `WMS.Application/Mappings/MappingProfile.cs`
- `WMS.Application/Interfaces/*Service.cs` và repository interfaces liên quan
- Các service: `PurchaseOrderService`, `ReceivingService`, `PutAwayService`, `PickingService`, `SaleOrderService`, `ShipmentService`, `StockAdjustmentService`, `ProductService`, `CategoryService`, `LocationService`, `WarehouseService`, `UserService`
- Các controller command tương ứng để dùng current user thống nhất
- Tất cả `Sql*Repository.cs` đang gọi `SaveChangesAsync`
- Entity workflow cần current fields: `PurchaseOrder`, `Receiving`, `PutAwayTask`, `Picking`, `SaleOrder`, `Shipment`, `StockAdjustment`

## 4. Data model

### Task 1: Mở rộng audit fields

**Files:**
- Modify: `WMS.Domain/Common/BaseAuditableEntity.cs`
- Modify: `WMS.Domain/Entities/User.cs`
- Modify: `WMS.Infrastructure/Data/WmsDbContext.cs`

- [ ] Thêm vào BaseAuditableEntity:

```csharp
public Guid? UpdatedById { get; set; }
public User? UpdatedBy { get; set; }
public DateTime? UpdatedDate { get; set; }
public bool IsDeleted { get; set; }
public Guid? DeletedById { get; set; }
public User? DeletedBy { get; set; }
public DateTime? DeletedDate { get; set; }
```

- [ ] Cấu hình các quan hệ CreatedBy/UpdatedBy/DeletedBy với `DeleteBehavior.NoAction` để tránh cascade path SQL Server.
- [ ] Thêm audit fields tương đương trực tiếp vào User: `UpdatedById`, `UpdatedDate`, `DeletedById`, `DeletedDate`, `IsDeleted`; giữ `CreatedAt` hiện có hoặc chuẩn hóa về một field sau khi kiểm tra mapping.
- [ ] Không cho phép soft-deleted entity xuất hiện trong query mặc định.

### Task 2: Tạo AuditLog và StatusHistory

**Files:**
- Create: `AuditLog.cs`, `StatusHistory.cs`
- Modify: `WmsDbContext.cs`

- [ ] `AuditLog` gồm:

```csharp
Guid Id;
string EntityType;
Guid EntityId;
string Action;
Guid? ActorUserId;
DateTime OccurredAtUtc;
string? OldValuesJson;
string? NewValuesJson;
string? ChangedFieldsJson;
string? CorrelationId;
string? RequestPath;
```

- [ ] `StatusHistory` gồm:

```csharp
Guid Id;
string EntityType;
Guid EntityId;
string? FromStatus;
string ToStatus;
string Action;
Guid? ActorUserId;
DateTime OccurredAtUtc;
string? Notes;
string? MetadataJson;
```

- [ ] Không cho AuditLog/StatusHistory bị soft delete; đây là lịch sử bất biến.
- [ ] Thêm DbSet và index `(EntityType, EntityId, OccurredAtUtc)` cùng index `(ActorUserId, OccurredAtUtc)`.
- [ ] Lưu enum status dưới dạng tên string trong history để không phụ thuộc số thứ tự enum.

## 5. Current user và persistence

### Task 3: Chuẩn hóa actor

**Files:**
- Create: `ICurrentUserService.cs`, `CurrentUserService.cs`
- Modify: `Program.cs`
- Modify: controllers/services đang tự parse ClaimTypes.NameIdentifier

- [ ] Interface:

```csharp
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
```

- [ ] Implementation đọc `ClaimTypes.NameIdentifier`, fallback `sub`, trả null cho system/background operation.
- [ ] Đăng ký `AddHttpContextAccessor()` và `ICurrentUserService` scoped.
- [ ] Command service dùng `ICurrentUserService` thay vì nhận actor từ controller; riêng `assignedUserId` vẫn là input nghiệp vụ khác với actor thực hiện.
- [ ] Không cho client truyền `CreatedById`, `UpdatedById`, `DeletedById`, `ApprovedById` hoặc các actor fields qua DTO.

### Task 4: Tự động audit tại DbContext

**Files:**
- Modify: `WmsDbContext.cs`
- Modify: `User.cs` nếu cần xử lý Identity audit riêng

- [ ] Override `SaveChangesAsync` và `SaveChanges` qua một hàm chung.
- [ ] Trước khi save, duyệt ChangeTracker:
  - Added: set Id nếu cần, CreatedDate, CreatedById, `IsDeleted = false`, tạo AuditLog `Created`.
  - Modified: set UpdatedDate/UpdatedById, tạo AuditLog `Updated` với old/new values và changed fields.
  - Deleted: chuyển thành Modified, set IsDeleted/DeletedById/DeletedDate, tạo AuditLog `Deleted`.
- [ ] Bỏ qua AuditLog/StatusHistory khi tự động tạo audit để tránh vòng lặp.
- [ ] Dùng `Entry.OriginalValues` và `CurrentValues`; chỉ serialize properties nghiệp vụ, không serialize navigation hoặc password hash.
- [ ] Với User, xử lý Identity entity riêng hoặc tạo service audit explicit; tuyệt đối không lưu password/password hash vào OldValuesJson/NewValuesJson.
- [ ] Nếu đang có transaction, mọi AuditLog cùng dùng transaction hiện tại.
- [ ] Không ghi log cho các thay đổi không thực sự đổi giá trị.

### Task 5: Refactor repository commit boundary

**Files:**
- Modify: toàn bộ `WMS.Application/Interfaces/*Repository.cs` cần dùng unit of work
- Modify: toàn bộ `WMS.Infrastructure/Repositories/Sql*.cs`
- Create nếu cần: `IUnitOfWork.cs`, `EfUnitOfWork.cs`

- [ ] Repository Add/Update/Delete chỉ Add/Update/Remove entity và không gọi `SaveChangesAsync`.
- [ ] Expose `SaveChangesAsync` qua `IUnitOfWork`/DbContext tại application service.
- [ ] Cập nhật các service command để commit một lần ở cuối use case.
- [ ] Giữ transaction cho PutAway complete, Picking create/complete/delete, StockAdjustment approve; mở rộng transaction cho Receiving confirm.
- [ ] Refactor `RemoveDetailsAsync` và các thao tác thay thế detail: dùng soft delete cho detail cũ, không hard delete làm mất lịch sử.
- [ ] Kiểm tra mọi repository hiện có để không còn SaveChanges tự phát; ngoại lệ duy nhất là infrastructure operation được ghi rõ và vẫn nằm trong transaction.

## 6. Workflow actor và status history

### Task 6: Bổ sung current workflow fields

**Files:**
- Modify: `PurchaseOrder.cs`, `Receiving.cs`, `PutAwayTask.cs`, `Picking.cs`, `SaleOrder.cs`, `Shipment.cs`, `StockAdjustment.cs`

- [ ] Bổ sung tối thiểu:

```csharp
// PurchaseOrder
Guid? ClosedById;
DateTime? ClosedDate;

// Receiving
Guid? ConfirmedById;
DateTime? ConfirmedDate;

// PutAwayTask/Picking
Guid? AssignedById;
DateTime? AssignedDate;
Guid? StartedById;
DateTime? StartedDate;
Guid? CompletedById;
DateTime? CompletedDate;

// SaleOrder
Guid? PackedById;
DateTime? PackedDate;
```

- [ ] `AssignedToId`/`AssignToId` tiếp tục là người được giao; `AssignedById` là actor giao việc.
- [ ] Shipment MVP hiện chỉ tạo shipment; giữ `ShippedDate` null. Khi chưa có ship action, không giả lập `ShippedById`.
- [ ] Không dùng current fields thay cho StatusHistory; current fields chỉ tối ưu truy vấn trạng thái hiện tại.

### Task 7: Cập nhật service workflows

**Files:**
- Modify: PurchaseOrder, Receiving, PutAway, Picking, SaleOrder, Shipment, StockAdjustment services
- Modify: Product, Category, Location, Warehouse, User services

- [ ] Mỗi command lấy actor từ `ICurrentUserService` và ghi current fields/history:
  - PO: Create, Update, Delete, Approve, Close.
  - Receiving: Create, Update, Delete, Confirm; task tự tạo phải có actor hệ thống/use-case và CreatedDate.
  - PutAway: Create, Update, Delete, Assign, Start, Complete.
  - Picking: Create, Delete, Assign, Start, Complete; tự động `SaleOrder Picking -> Packed` ghi actor của Complete.
  - SaleOrder: Create, Update, Delete; không cho đổi status trực tiếp ngoài workflow.
  - Shipment: Create và duplicate check; chưa có ship transition.
  - StockAdjustment: Create, Approve, Delete.
  - Product/Category/Location/Warehouse: Create, Update, Delete, UploadImage nếu có.
  - User: Register, UpdateProfile, ChangePassword, UploadAvatar, role changes nếu có endpoint.
- [ ] Mỗi status transition gọi một helper thống nhất để tạo `StatusHistory` với from/to/action/actor/time.
- [ ] Nếu một use case thay đổi nhiều entity, ghi log cho từng entity và commit cùng transaction.
- [ ] Khi hard-coded status assignment hiện có được thay thế, lấy status cũ trước khi set mới để history không bị mất `FromStatus`.
- [ ] Delete command phải ghi `Deleted` trước khi filter ẩn bản ghi; không cho delete entity đã soft-deleted lần nữa.

## 7. Audit read API

### Task 8: Đọc lịch sử

**Files:**
- Create audit DTOs, `IAuditLogRepository`, `SqlAuditLogRepository`, `IAuditLogService`, `AuditLogService`, `AuditLogsController`
- Modify: `Program.cs`, `MappingProfile.cs` nếu dùng AutoMapper

- [ ] Endpoint:

```http
GET /api/audit-logs?entityType=Picking&entityId={id}&actorId={id}&fromUtc={date}&toUtc={date}&page=1&pageSize=50
GET /api/audit-logs/{entityType}/{entityId}/status-history
```

- [ ] Chỉ `Admin` và `WarehouseManager` được xem audit; không cho WarehouseStaff xem toàn hệ thống.
- [ ] Validate page/pageSize: page >= 1, pageSize trong khoảng 1..100.
- [ ] Sắp xếp mới nhất trước.
- [ ] Trả actor display name nếu query được, nhưng vẫn giữ ActorUserId.
- [ ] Không cho update/delete AuditLog hoặc StatusHistory qua API.

## 8. Migration và tương thích dữ liệu

### Task 9: EF migration

**Files:**
- Create migration mới dưới `WMS.Infrastructure/Migrations/`
- Modify: `WmsDbContextModelSnapshot.cs` do EF tạo

- [ ] Tạo migration tên `AddSystemAuditAndSoftDelete` sau khi model hoàn chỉnh.
- [ ] Thêm columns audit cho tất cả entity base và User.
- [ ] Tạo bảng AuditLogs/StatusHistories, indexes, foreign keys actor với `NoAction`.
- [ ] Với dữ liệu cũ: `IsDeleted = false`; `CreatedDate` đang MinValue giữ nguyên hoặc chuẩn hóa thành migration-safe UTC theo quyết định triển khai.
- [ ] Không sửa các migration đã apply.
- [ ] Chạy `dotnet ef database update` trên database mục tiêu sau khi review migration SQL.

## 9. Verification

### Build và migration

```powershell
cd d:\GITHUB\WMS\backend\WMS-mini
dotnet build WMS.sln --no-restore
dotnet ef migrations script --startup-project .\src\WMS.API --project .\src\WMS.Infrastructure
```

Expected: build không lỗi; script chỉ thêm schema/columns mới.

### Audit behavior

- Tạo Product: có `CreatedById`, `CreatedDate`, AuditLog `Created`.
- Sửa Product: có `UpdatedById`, `UpdatedDate`, AuditLog `Updated` với old/new values.
- Xóa Product: bản ghi vẫn tồn tại DB, `IsDeleted = true`, có DeletedBy/DeletedDate và AuditLog `Deleted`; GET mặc định không trả.
- Approve PO: có ApprovedBy/ApprovedDate và StatusHistory `Pending -> Approved`.
- Close PO: có ClosedBy/ClosedDate và StatusHistory `Received -> Closed`.
- Confirm Receiving: có ConfirmedBy/ConfirmedDate và history `Draft -> Confirmed`.
- Assign/Start/Complete PutAway và Picking: có actor thực hiện, current fields và StatusHistory tương ứng.
- Picking complete tự động đổi SaleOrder `Picking -> Packed` và history có actor picker.
- Approve StockAdjustment: audit adjustment, stock, location và movement trong cùng transaction.
- Update Profile/Password/Avatar: có AuditLog; tuyệt đối không chứa password hoặc password hash.

### Consistency and security

- AuditLog/StatusHistory không bị ẩn hoặc soft delete.
- Không có command nào dùng actor do client tự gửi.
- Rollback workflow phải rollback cả dữ liệu nghiệp vụ và audit log.
- GET audit chỉ Admin/WarehouseManager.
- Không còn repository command tự commit trước khi use case kết thúc.
- `dotnet test WMS.sln --no-restore` chạy thành công; nếu chưa có test project, phải bổ sung integration tests cho audit/soft delete trước khi kết luận.
- Chạy smoke test inbound và outbound hiện có để bảo đảm audit không làm thay đổi trạng thái nghiệp vụ.

## 10. Assumptions và quyết định

- Audit toàn bộ entity, nhưng Password/PasswordHash/refresh token không bao giờ được serialize vào audit.
- Soft delete áp dụng cho entity nghiệp vụ; AuditLog/StatusHistory luôn immutable.
- `User` được audit riêng do không thể kế thừa BaseAuditableEntity.
- Background/system operation có actor null hoặc System User riêng; không gán giả actor admin.
- Không thêm bước Shipment `Shipped` trong plan này vì MVP hiện chỉ tạo Shipment.
- Không tự động bổ sung frontend audit UI trong phạm vi backend plan; endpoint API đủ cho frontend tích hợp sau.
- Tất cả migration và thay đổi commit boundary phải được review trước khi chạy trên database thật.
