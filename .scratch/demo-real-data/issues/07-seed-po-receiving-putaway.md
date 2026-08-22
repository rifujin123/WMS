# 07 — Seed chuỗi document: PO → Receiving → PutAway (3–4)

**What to build:** Seed 3–4 chuỗi nhập hàng hoàn chỉnh để trang PO/Receiving/PutAway và `StatusHistory` có nội
dung kể chuyện nghiệp vụ thật: Đơn mua (Pending→Approved→Received→Closed) → Receiving (đủ/đúng) → PutAway
(hoàn thành, cộng tồn tại vị trí), với StatusHistory/AuditLog cho từng bước chuyển trạng thái.

**Blocked by:** 05 (vendors), 06 (stock/movement base)

**Status:** ready-for-agent

- [ ] 3–4 `PurchaseOrder` (PoNumber unique, `PO-…`) với vendor thật, detail từ sản phẩm seed, tổng giá VND hợp lý.
- [ ] Mỗi PO đi qua đủ trạng thái → `Closed`; phải tôn trọng ràng buộc hiện có (đơn vị quantity ≤ số còn lại trên PO).
- [ ] `Receiving` với `ReceivingNo` unique; **1 PO chỉ 1 Confirmed receiving** (ràng buộc filter-index đã có); detail Ok/Hỏng hợp lý; số `ReceivedQuantity` đúng.
- [ ] `PutAwayTask` hoàn thành hết cho các dòng nhận Ok → cộng `Stock` + `Location.CurrentQuantity` + ghi `StockMovement` In đúng (khớp ledger ticket 06 — không đếm kép).
- [ ] `StatusHistory` đầy đủ cho mỗi chuyển trạng thái (Pending→Approved→Received→Closed, PO) với `OccurredAtUtc` quá khứ khớp cửa sổ; actors = manager (duyệt) / staff (xác nhận, cất).
- [ ] `AuditLog` tương ứng (Created/Updated/Deleted) cho các entity trong chuỗi; backdate đúng quy tắc.
- [ ] Không vi phạm invariant: putaway ≤ receiving detail; capacity location; không dư thừa.
- [ ] Test: PO closed đúng; 1-confirmed-receiving giữ; tồn kho/movement cộng dồn khớp (không double-count); StatusHistory đủ bước.

**Verified facts:** `PurchaseOrder`(PoNumber unique) + `PurchaseOrderDetail`(RowVersion rowversion);
`Receiving`(ReceivingNo unique; filter-index `[Status]=1` unique per PO); `PutAwayTask` complete → cộng stock +
movement In; `PrepareAuditEntries` sinh StatusHistory khi `Status` đổi.