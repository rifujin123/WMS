# 03 — Seed warehouses + locations (HCM 24, HN 40)

**What to build:** Seed 2 kho và các vị trí để luồng nhận/cất/lấy có dữ liệu địa điểm thật: kho HCM 24 vị trí,
kho HN 40 vị trí, mã vị trí theo Aisle-Rack-Level, đủ capacity/type.

**Blocked by:** 01 — Seed scaffold + gating

**Status:** ready-for-agent

- [ ] 2 `Warehouse`: HCM (Code `WH-HCM`, tên tiếng Việt, địa chỉ thật) + HN (Code `WH-HN`).
- [ ] Kho HCM: **24** vị trí; kho HN: **40** vị trí.
- [ ] Mã vị trí theo chuẩn hiện có (Lane-Aisle-Rack-Level, ví dụ `A-01-01`), viết hoa, unique trong kho; đúng schema `LocationFormModal` (`Aisle/Rack/Level`).
- [ ] `LocationType` phân bổ hợp lý: Storage chủ yếu + vài Picking / Receiving / Shipping.
- [ ] `MaxQuantity` thực tế (phiện thoại/laptop — lớn hơn 0, hợp lý theo loại vị trí); `CurrentQuantity = 0` ban đầu.
- [ ] Idempotent theo `Warehouse.Code` + `Location.Code`.
- [ ] Test: đúng count 24/40; mã vị trí unique; capacity > 0; LocationType hợp lệ.

**Verified facts:** `Location`(WarehouseId/Code/Aisle/Rack/Level/LocationType/MaxQuantity/CurrentQuantity);
`Warehouse.Code` unique index; LocationFormModal placeholder `A-01-01`.