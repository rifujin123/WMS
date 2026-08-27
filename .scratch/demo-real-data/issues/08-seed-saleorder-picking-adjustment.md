# 08 — Seed chuỗi document: SaleOrder → Picking → Packed + StockAdjustment

**What to build:** Seed 2–3 chuỗi xuất hàng hoàn chỉnh (New→Allocated→Picking→Packed) và 1–2 StockAdjustment
được duyệt, để trang Đơn bán/Picking/Điều chỉnh tồn và `StatusHistory` có nội dung, đồng thời ledger tồn
khớp (Out movements + Adjustment).

**Blocked by:** 07 (PO/Receiving/PutAway chain — cùng ledger base)

**Status:** ready-for-agent

- [ ] 2–3 `SaleOrder` (OrderNo unique, `SO-…`) với customer, detail sản phẩm, staff sales, tổng giá VND.
- [ ] Mỗi SaleOrder đi qua `New → Allocated → Picking → Packed`; `Picking` với `PickingNo` unique, QtyPicked == required.
- [ ] Khi Picking hoàn thành: trừ `Stock.OnhandQty` + ghi nhận/reserve đúng + ghi `StockMovement` Out (khớp ledger ticket 06 — không đếm kép), giữ không tồn âm.
- [ ] `StatusHistory` đầy đủ cho SaleOrder/Picking (New→Allocated→Picking→Packed) với `OccurredAtUtc` quá khứ; actors = staff (lấy hàng) / manager.
- [ ] 1–2 `StockAdjustment` (`AdjustmentNo` unique) từ Draft → Approved đúng service rule (`ApprovedById/Date`); detail: CountedQty chênh lệch thực → ghi `StockMovement` Adjustment + cập nhật Stock/Location đến counted.
- [ ] AuditLog tương ứng cho toàn chuỗi; backdate đúng quy tắc (SPEC).
- [ ] Idempotent theo OrderNo/AdjustmentNo/PickingNo.
- [ ] Test: chuỗi đạt Packed; adjustment approved; ledger khớp (Onhand sau Out/Adjustment); không âm; StatusHistory đủ bước; ReservedQty nhất quán.

**Verified facts:** `SaleOrder`(OrderNo unique) + `SaleOrderDetail`; `Picking`(PickingNo unique) +
`PickingDetail`; `StockAdjustment`(AdjustmentNo unique, Draft→Approved, `ApproveAsync` logic); `StockMovement`
MovementType Adjustment.