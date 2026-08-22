# CONTEXT — Demo data thật: điện thoại / laptop / phụ kiện

> Paper trail của `/grill-with-docs` cho feature "dữ liệu thật trong WMS".
> Mọi quyết định ở đây đã được người dùng xác nhận. Đừng thay đổi nếu không được yêu cầu.

## Goal

Seed dữ liệu thật cho WMS để demo nghiệp vụ liền mạch: **master data + tồn kho + lịch sử** theo ngành
phân phối điện thoại / laptop / phụ kiện. Không còn màn hình "trống trơn".

## Quyết định người dùng đã chốt

| # | Quyết định | Giá trị |
|---|---|---|
| Q1 | Cấp độ | **B** — Master + tồn kho + lịch sử (không phải full transaction chain lớn) |
| Q2 | Thương hiệu | **Thật** (Apple, Samsung, Dell, Logitech…) — data giả lập, chỉ dùng demo |
| Q3 | Quy mô | **2 kho** (HCM, HN) • HCM **24 vị trí** • HN **40 vị trí** • **60 sản phẩm** • 8–12 vendor • 15–20 khách |
| Q4 | Ngôn ngữ & giá | **Tiếng Việt** (tên sản phẩm thị trường VN) + **giá VND** |
| Q5 | User | **2 manager + 4 staff** (ngoài admin) để `CreatedBy`/`ConfirmedBy`/`PickedBy` hiển thị người thật |
| Q6 | Lịch sử | **A** — backdate `StockMovement`/`AuditLog`/`StatusHistory` **rải 4–6 tuần** (~300–400 movement, ~600 audit) |
| Q7 | Ảnh sản phẩm | **A** — placeholder URL ổn định theo SKU (cùng SKU → cùng ảnh; cần internet khi demo) |
| Q8 | Document mẫu | **A** — seed ~4–6 chuỗi hoàn chỉnh: 3–4 PO→Receiving→PutAway xong, 2–3 SaleOrder→Picking→Packed, 1–2 StockAdjustment được duyệt |
| Q9 | Cơ chế | **A** — tự chạy lúc startup trong `Development` (sau `Migrate()`), **idempotent**, gate bằng `Seed:Enabled`, password demo chung `Admin@123` |

## Fact đã xác minh từ codebase

- **Không có sẵn seeder nào** ngoài roles + admin (`Program.cs`). Grep `Seeder|DataSeeder|HasData` chỉ trúng
  migrations (`HasDatabaseName` — false positive). → Phải xây mới cơ chế seed.
- **`WmsDbContext.PrepareAuditEntries()`** chạy ở mọi `SaveChanges(Async)`:
  - Tự sinh `AuditLog` (`Created`/`Updated`/`Deleted`) + `StatusHistory` (khi property `Status` đổi).
  - **Ép `OccurredAtUtc = now`** cho audit → muốn backdate lịch sử phải ghi trực tiếp / raw SQL UPDATE.
  - **Ép `CreatedDate = now`** cho `BaseAuditableEntity` khi `Added` → `StockMovement`/`Stock` cũng bị ép.
  - **Exclude `AuditLog` + `StatusHistory` khỏi việc tự audit** (không bị audit kép khi seed trực tiếp).
- Entities: `Warehouse`(Code/Name/Address), `Location`(WarehouseId/Code/Aisle/Rack/Level/LocationType/MaxQuantity/CurrentQuantity),
  `Category`(phẳng, không phân cấp), `Product`(Sku/Name/CategoryId/Unit/Price/Dimension/ImageUrl),
  `Vendor`/`Customer`(Name/ContactName/Phone/Email/Address), `Stock`(ProductId+LocationId/OnhandQty/ReservedQty),
  `StockMovement`(ProductId/LocationId/MovementType/Qty/Notes — BaseAuditableEntity), `AuditLog`, `StatusHistory`,
  `PurchaseOrder`+Detail, `Receiving`+Detail, `PutAwayTask`, `SaleOrder`+Detail, `Picking`+Detail,
  `StockAdjustment`+Detail, `Shipment`, `Rma`, `AssociationRule`.
- **`ICurrentUserService`** là scoped từ `HttpContext`; khi seed lúc startup không có request → cần cơ chế
  đặt "actor" tạm (giả lập user đang ghi) cho các thao tác seed để audit/history có `ActorUserId` đúng.
- Có sẵn `Create*Dto` + service cho: Warehouse, Location, Category, Product, Vendor, Customer. Nhưng seed nên
  đi **thẳng `WmsDbContext`** sau `Migrate()` (idempotent), đặt `CreatedBy = admin`.
- Frontend `Products/index.tsx`: nếu `ImageUrl` trống → hiện icon xám `ShoppingOutlined` (ok nhưng kém "thật").
- Convention ticket: `.scratch/<feature>/issues/NN-slug.md` — format:
  `# NN — title` / `**What to build:**` / `**Blocked by:**` / `**Status:** ready-for-agent` / checklist `- [ ]`.

## Hướng kỹ thuật đề xuất (việc của agent — để spec/tickets chi tiết hoá)

1. **SeederService** trong `WMS.Infrastructure` (+ interface tách `ISeeder`), gọi sau `Migrate()` trong
   Development, gate `Seed:Enabled` (config/env).
2. **Idempotent**: check theo unique key (Sku, Warehouse.Code, Vendor.Name, Customer.Name, PoNumber…) —
   chạy lại `dotnet run` không duplicate.
3. **Order đúng ràng buộc FK**: users → warehouses → locations → categories → products → vendors/customers
   → stock → movements → documents (PO→Receiving→PutAway / SaleOrder→Picking / StockAdjustment).
4. **Backdate**: seed trực tiếp + xử lý timestamp. `StockMovement`/`Stock` là `BaseAuditableEntity` bị ép
   `CreatedDate=now` → sau khi add phải raw SQL UPDATE `CreatedDate` về lịch sử 4–6 tuần; `AuditLog`/
   `StatusHistory` dựng trực tiếp với `OccurredAtUtc` tùy ý.
5. **Consistency bất biến**: tồn kho mỗi sản phẩm phải **khớp tổng movements** (In−Out) — tính `OnhandQty`
   từ ledger movements, không hardcode số máy. Phải thỏa ràng buộc nghiệp vụ hiện có (capacity location,
   không tồn âm, 1 PO chỉ 1 receiving Confirmed…).
6. **Consistency nghiệp vụ chuỗi mẫu**: dựng document mẫu đi qua đúng các service/trạng thái hoặc chèn
   entity + StatusHistory/AuditLog khớp chain, đảm bảo `StatusHistory` cónội dung cho `StatusHistoryTable`.
7. **Ảnh placeholder**: `${SKU}` trong URL ổn định (ví dụ `https://picsum.photos/seed/<sku>/240/240`),
   lưu vào `Product.ImageUrl` — idempotent và cùng SKU cùng ảnh.
8. **Test**: unit test cho SeederService (idempotency, khớp tồn kho, khớp FK, backdate). Với môi trường
   read-only/CI chạy `dotnet test`.