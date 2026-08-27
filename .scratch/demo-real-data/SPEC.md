# SPEC — Dữ liệu demo thật: điện thoại / laptop / phụ kiện

> Bản chính thức được publish lên issue tracker local tại `docs/issues/23-real-demo-data.md` (status
> `ready-for-agent`). Bản này là bản sao dùng chung trong feature folder để `/implement` từng ticket tham chiếu.
> Mọi cập nhật phải giữ đồng bộ với bản chính.

## Problem Statement

WMS hiện gần như "trống trơn": seed startup chỉ tạo roles + admin (`Admin@123`), không có master data,
không tồn kho, không lịch sử. Các màn Products/Categories/Vendors/Customers/Warehouses, dashboard (AuditLog,
StatusHistory, StockMovement) và form nghiệp vụ đều hiển thị rỗng. Người đánh giá đồ án không thể demo được
luồng nghiệp vụ (tạo đơn mua → nhận → cất → tồn → đơn bán → lấy hàng → xuất) vì mất rất nhiều bước nhập liệu
thủ công, và dashboard/pagination không có nội dung để khoe.

## Solution

Xây một cơ chế **seed dữ liệu demo thật** chạy tự động khi khởi động ứng dụng trong môi trường `Development`
(sau `Migrate()`), **idempotent** (chạy lại không duplicate), được gate bằng cờ `Seed:Enabled`. Dữ liệu thuộc
ngành **phân phối điện thoại / laptop / phụ kiện** với thương hiệu thật, tên tiếng Việt, giá VND: 2 kho
(HCM 24 vị trí, HN 40 vị trí), 60 sản phẩm, 8–12 nhà cung cấp, 15–20 khách hàng, 2 manager + 4 staff cùng
admin, tồn kho + khoảng 300–400 `StockMovement` + ~600 `AuditLog` rải 4–6 tuần, và vài chuỗi document mẫu
hoàn chỉnh (3–4 PO→Receiving→PutAway xong, 2–3 SaleOrder→Picking→Packed, 1–2 StockAdjustment được duyệt) để
`StatusHistory` và các trang đơn có nội dung. Ảnh sản phẩm dùng placeholder URL ổn định theo SKU.

## User Stories

1. As an **admin**, I want the system to auto-populate realistic master data when I start the app in development, so that I can demo the whole business flow without typing in 100+ records manually.
2. As a **reviewer**, I want to see 60 products across phones / laptops / accessories with real brand names (iPhone, Galaxy, MacBook, Dell, Logitech…) and price in VND, so that the catalog looks authentic for a Vietnamese SMB.
3. As a **warehouse manager**, I want two warehouses (HCM with 24 locations, HN with 40 locations), so that location-based receiving / putaway / picking pages have real data to work with.
4. As a **warehouse manager**, I want locations with realistic codes (Aisle-Rack-Level), capacity and type (Storage / Picking / Receiving / Shipping), so that putaway capacity checks and the Stock screen behave realistically.
5. As a **staff member**, I want to log in with one of the seeded warehouse staff accounts (demo password), so that I can experience role-based permissions.
6. As a **warehouse manager**, I want seeded manager accounts with the WarehouseManager role, so that screens restricted to managers are demonstrable.
7. As a **staff member**, I want the seeded data to record actions against real seeded users (CreatedBy / ConfirmedBy / PickedBy / ApprovedBy show names, not null), so that audit screens look human.
8. As a **reviewer**, I want the Stock screen to show non-zero Onhand/Reserved quantities across products and locations, so that inventory is demonstrable.
9. As a **reviewer**, I want the StockMovement table on the dashboard to be populated with ~300–400 rows spread over the past 4–6 weeks, so that pagination and the date-range filter have meaningful content.
10. As a **reviewer**, I want the AuditLog table on the dashboard to be populated (~600 rows) with realistic timestamps, actor names and entity types, so that the audit trail is demonstrable.
11. As a **reviewer**, I want the StatusHistory table to contain status transitions from the seeded document chains (e.g. PO Pending→Approved→Received→Closed), so that the status-timeline feature is demonstrable.
12. As a **receiving staff member**, I want a few completed PurchaseOrders that flowed Receiving → PutAway → Closed, so that PO/Receiving/PutAway pages show full lifecycle examples.
13. As a **picking staff member**, I want a few SaleOrders that flowed New → Allocated → Picking → Packed, so that outbound pages show complete examples.
14. As a **stockkeeper**, I want 1–2 approved StockAdjustments, so that the adjustment screen shows approved records with their stock-movement effects.
15. As a **reviewer**, I want stock balances to be consistent with the movement ledger (Onhand == sum of In − Out ± Adjustment at each product+location), so that INN inventory reconciliation is trustworthy.
16. As an **admin**, I want seeding to be idempotent — re-running `dotnet run` does not duplicate any record, so that the demo remains stable across restarts.
17. As an **operator**, I want seeding gated behind a `Seed:Enabled` flag so I can turn it off (e.g. for a clean demo or a shared DB), so that I keep control over when demo data is created.
18. As an **admin**, I want product images as stable placeholder URLs derived from the SKU, so that the product list shows images (not just grey icons) without needing real uploads.
19. As a **reviewer**, I want the main pages (Products, Categories, Vendors, Customers, Warehouses, Stock, PO, Receiving, PutAway, SaleOrder, Picking, StockAdjustment, Dashboard logs) all non-empty after first startup, so that a single run produces a complete demo environment.
20. As a **developer**, I want to be able to reset the demo (drop/recreate DB) and re-seed deterministically, so that I can reproduce the same demo state repeatedly.
21. As a **developer**, I want predictable business-rule safety in the seeded data — it must not violate existing invariants (location capacity, no negative stock, one Confirmed receiving per PO, putaway ≤ receiving detail), so that the demo data is not "lying" to the existing services.

## Implementation Decisions

- **Modules built / modified**
  - New seeding component in `WMS.Infrastructure`: responsible for (a) users/roles via `UserManager`, (b) master
    data (warehouses, locations, categories, products, vendors, customers), (c) stock + stock movements +
    complementary audit/status rows, (d) a few full document chains. Registered in DI and invoked from the API
    host after `Migrate()` when `Development && Seed:Enabled`. Logs a summary (counts) on completion.
  - `WMS.API` host: invoke the seeder in a startup scope after migration, gated by configuration; no API/contract
    changes.
  - No EF schema migration required for this feature — everything rides on existing tables. Placeholder image
    URLs reuse the existing `Product.ImageUrl`.
- **Interfaces**
  - New single entry seam: the seeder component root (registered in DI, called once from host). Master-data and
    document-chain building are internal to it; the external contract is "seed the demo data set, idempotently".
  - Existing services (`ICurrentUserService`, `IUnitOfWork`, repositories, `UserManager<User>`) are reused where
    practical. Because seeding runs at startup outside an HTTP request, `ICurrentUserService` will have no HTTP
    context: the seeder explicitly assigns `CreatedBy`/`*ById` actor values (seeded admin/staff/managers) instead
    of relying on the ambient user.
- **Architectural decisions**
  - **Seam for testing — one seam:** the seeder root contract. Tests drive the seeder against a real
    `WmsDbContext` with an in-memory provider (EF InMemory / SQLite) and assert on resulting entities. This is
    the fewest possible seams and the highest point at which the whole feature can be verified (idempotency,
    stock↔movement consistency, FK integrity, backdated timestamps, role creation).
  - **Audit backdating constraint (verified):** `WmsDbContext.PrepareAuditEntries` auto-generates `AuditLog` +
    `StatusHistory` but forces `OccurredAtUtc = now` and, for `BaseAuditableEntity` added through the context,
    forces `CreatedDate = now`. Therefore:
    - `AuditLog` / `StatusHistory`: constructed directly with explicit backdated `OccurredAtUtc` (they are
      excluded from re-auditing, so no double-entry).
    - `StockMovement` / `Stock`: added via the context (so the existing invariants of the audit pipeline apply),
      then backdated via a targeted raw-SQL `UPDATE` of `CreatedDate` (and `CreatedById` where actor differs),
      since the interceptor would otherwise stamp `now`.
    - The seeder must produce a `StatusHistory` row for each status transition in the sample document chains —
      either directly or by driving the status change through the context so `PrepareAuditEntries` emits it.
  - **Consistency by construction:** compute each `Stock.OnhandQty` from the generated movement ledger
    (Onhand == Σ In − Σ Out ± Σ Adjustment per product+location) instead of hardcoding mismatched numbers.
    Ensure `Location.CurrentQuantity` ≤ `MaxQuantity`, no negative stock, putaway ≤ receiving detail quantity,
    and no duplicate Confirmed receiving per PO. The whole seed set is a "closed book" that respects existing
    service invariants.
  - **Deterministic + idempotent:** check existence by natural unique keys before inserting (Warehouse.Code,
    Product.Sku, Vendor.Name, Customer.Name, PoNumber, OrderNo, usernames…). Re-running the seeder adds nothing
    the second time. User accounts created only if the username does not exist.
  - **Actors:** create users first (admin exists; add 2 WarehouseManager + 4 WarehouseStaff with role + demo
    password), then assign CreatedBy / ConfirmedBy / PickedBy / ApprovedBy from those user IDs so audit fields
    show names. Where a document chain travels through statuses, actors rotate realistically (e.g. manager
    approves, staff confirms/picks).
  - **Backdate window:** movements / audit / status history spread deterministically across ~28–42 days ending
    "now"; ordering newest-first so `StockMovement` table paging (pageSize 10) has dozens of pages.
  - **Images:** `Product.ImageUrl = placeholder URL seeded by SKU` (stable — same SKU → same image; requires
    internet when demoed). No Cloudinary upload in this scope.
  - **Category:** the `Category` entity is flat (no hierarchy). Use 3 or 4 top-level categories fitting the
    product scope; no schema change to add hierarchical categories.
- **Schema changes:** none.
- **API contracts:** none.
- **Specific interactions**
  - `Seed:Enabled` config (appsettings/env) gates seeding; default on only in `Development`.
  - Seeder runs after `Migrate()` inside the same startup scope; logs a seeded-counts summary via `ILogger`.
  - Placeholder image URL scheme is deterministic per SKU (documented in code constant) so no DB state needed
    to derive it.

## Testing Decisions

- **What makes a good test here:** test only externally observable behavior of the seed seam — idempotency
  (run twice → identical counts, no duplicates), stock↔movement reconciliation (Onhand == ledger), FK integrity,
  backdate range (timestamps within the window, descending paging order works), role creation, and presence of
  the expected sample chains (PO statuses, SaleOrder statuses, approved adjustments, StatusHistory rows).
- **Modules tested:** the seeder contract (the one new seam), which transitively exercises master-data creation,
  user/role creation, movement generation, backdating, and document-chain building. Prefer asserting on a real
  in-memory `WmsDbContext` (EF InMemory / SQLite) — the strongest available simulation of the audit pipeline.
- **Prior art:** the existing `WMS.Tests` project (xUnit, 19 tests) with `Fakes.cs` (`FakeCurrentUser`,
  `FakeUnitOfWork`, `MapperFactory`) and per-repository fakes; it already tests Receiving, PutAway, Picking and
  StockMovement paging. The seeder tests follow the same project and style, adding an in-memory-context seam.
  Keep all existing tests green — regression only.

## Out of Scope

- **Level C full transaction chain** beyond the handful of sample documents (no auto-generated huge ledger of
  POs/receivings/pickings beyond the ~4–6 sample chains). The ~300–400 movements are synthetic history for the
  dashboard, not a fully-reconciled giant order history.
- **Shipment / carrier integration** — shipment stays at demo (`IShipmentGateway` demo); no shipment seed data
  beyond what existing SaleOrder→Packed flow implies.
- **Cloudinary / real image upload** — placeholder URLs only.
- **Category hierarchy / sub-categories** — `Category` stays flat.
- **Multi-currency / multi-locale** — VND + Vietnamese only.
- **Serial/IMEI/barcode tracking** — no schema for per-unit identity in this feature.
- **Production environments** — seeding only in `Development`; `Seed:Enabled` OFF in prod by policy.
- **RMA / returns, warehouse transfers, cycle counting, replenishment** — out of scope this feature.

## Further Notes

- Full decision history + verified facts live in `.scratch/demo-real-data/CONTEXT.md` (gitignored scratch
  folder, by convention). The spec is published here in `docs/issues/` (also gitignored).
- Demo credentials are documented (admin + seeded staff/managers share `Admin@123` for demo). These are demo
  accounts, not for production.
- Reset flow: drop/recreate the DB (or rotate it) and re-run — the seeder reproduces a deterministic demo state.
- Numbers (60 SKU, 24/40 locations, 300–400 movements) are sane defaults agreed with the user; keep
  consistency invariants intact if tuned later.