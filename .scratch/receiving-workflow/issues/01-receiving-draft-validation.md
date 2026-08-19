# 01 — Receiving draft validation và nhiều draft trên PO

**What to build:** Staff có thể lưu nhiều receiving ở trạng thái `Draft` cho cùng một PO đã duyệt. Backend kiểm tra dữ liệu nhận hàng đầy đủ và chặn dữ liệu không hợp lệ trước khi lưu.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] Chỉ cho phép tạo và cập nhật receiving khi PO đang ở trạng thái `Approved` và chưa có receiving `Confirmed`.
- [ ] Cho phép nhiều receiving `Draft` trên cùng một PO.
- [ ] Chặn `ActualQuantity` bằng 0 hoặc số âm ở backend.
- [ ] Chặn product không thuộc PO.
- [ ] Gộp các detail trùng `ProductId` trước khi kiểm tra và chặn tổng số lượng vượt số lượng còn lại của PO.
- [ ] Validation áp dụng giống nhau cho tạo mới và cập nhật draft.
- [ ] Có test hoặc kịch bản kiểm chứng cho nhiều draft, duplicate product, quantity âm/0 và quantity vượt PO.
