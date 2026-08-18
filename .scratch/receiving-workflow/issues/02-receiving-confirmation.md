# 02 — Confirm receiving với hàng tốt, hỏng và thiếu

**What to build:** Người dùng có thể confirm một receiving draft hoàn chỉnh. Một PO chỉ có tối đa một receiving `Confirmed`; các receiving draft khác của cùng PO không được confirm sau đó. Hàng `Damaged` và `Missing` được chốt đúng nghiệp vụ nhưng không cộng tồn và không tạo PutAway task.

**Blocked by:** 01 — Receiving draft validation và nhiều draft trên PO

**Status:** ready-for-agent

- [ ] Chỉ cho phép confirm khi PO chưa có receiving `Confirmed`.
- [ ] Quy tắc hoàn tất tính đúng tổng số lượng theo PO, kể cả khi có nhiều receiving draft và nhiều detail cùng product.
- [ ] Chốt rõ và triển khai quy tắc `Damaged`/`Missing`: được tính là đã xử lý khi confirm, nhưng không cộng `ReceivedQuantity` tồn kho và không tạo PutAway task.
- [ ] Sau khi confirm thành công, receiving chuyển sang `Confirmed` và PO chuyển sang `Received` khi toàn bộ số lượng đã được xử lý.
- [ ] Các draft còn lại của PO không thể confirm sau khi một receiving đã confirmed.
- [ ] Tách quy tắc tính hoàn tất khỏi service orchestration ở mức đơn giản, phù hợp pattern hiện tại.
- [ ] Có test hoặc kịch bản kiểm chứng cho nhiều draft, duplicate product, `Ok`, `Damaged`, `Missing` và confirm trùng.
