# 06 — Seed stock + backdated movements (ledger khớp tồn kho)

**What to build:** Seed tồn kho và lịch sử biến động: ~300–400 `StockMovement` + `Stock.OnhandQty` **tính từ
ledger** (consistency by construction), rải 4–6 tuần, kèm ~600 `AuditLog`. Đây là phần khó nhất của dự án vì
vướng ràng buộc backdate của `WmsDbContext`.

**Blocked by:** 02 (users — for actors), 03 (warehouses+locations), 04 (products)

**Status:** ready-for-agent

- [ ] Sinh ledger movements (In / Out / Adjustment) cho các sản phẩm × vị trí, tổng **300–400** dòng, rải **28–42 ngày** trước `now`.
- [ ] `Stock.OnhandQty = Σ In − Σ Out ± Σ Adjustment` cho từng (product, location) — **không hardcode**; đảm bảo không tồn âm.
- [ ] `Stock.ReservedQty` hợp lý (có thể > 0 cho các đơn đang Picking — bổ sung/đồng bộ từ ticket 08).
- [ ] `Location.CurrentQuantity` = tổng hàng tại vị trí, **≤ MaxQuantity** (capacity).
- [ ] **Backdate**: `AuditLog`/`StatusHistory` dựng trực tiếp với `OccurredAtUtc` quá khứ (được exclude khỏi re-audit).
  `Stock`/`StockMovement` là `BaseAuditableEntity` → interceptor ép `CreatedDate=now` → backdate qua raw-SQL
  `UPDATE CreatedDate/CreatedById` sau khi add (đúng quyết định trong SPEC).
- [ ] Actors: gán `CreatedById`/`ActorUserId` từ user seed (manager/staff) — xen kẽ tự nhiên.
- [ ] `MovementType` + Notes mô tả trung thực (nhận hàng / bán / điều chỉnh).
- [ ] Idempotent: không tạo kép khi chạy lại (dựa trên base idempotency + quy tắc unique).
- [ ] Test (single seam — in-memory context): khớp Onhand == ledger; không âm; capacity ≤ max; tổng dòng trong
  khoảng; timestamp thuộc cửa sổ 4–6 tuần; thứ tự newest-first cho paging dashboard.

**Verified facts (CONTEXT.md):** `PrepareAuditEntries` ép `OccurredAtUtc=now` + `CreatedDate=now`; `AuditLog`/
`StatusHistory` exclude khỏi re-audit; `Stock` có unique (ProductId,LocationId); `StockMovement` là
`BaseAuditableEntity`.