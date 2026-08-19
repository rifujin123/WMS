# 03 — Lưu và kiểm tra ảnh hóa đơn trên receiving draft

**What to build:** Khi tạo hoặc cập nhật receiving draft, người dùng có thể gửi ảnh hóa đơn đã upload. Backend kiểm tra URL ảnh hợp lệ và lưu URL cùng receiving; luồng tạo receiving thủ công không có ảnh vẫn hoạt động.

**Blocked by:** 01 — Receiving draft validation và nhiều draft trên PO

**Status:** ready-for-agent

- [ ] Request tạo/cập nhật receiving nhận được `InvoiceImageUrl` tùy chọn.
- [ ] Backend validate URL không rỗng khi được gửi và đúng định dạng URL được hệ thống upload sử dụng.
- [ ] URL hợp lệ được lưu và trả lại trong receiving response.
- [ ] URL không hợp lệ bị từ chối khi tạo hoặc cập nhật draft.
- [ ] Không có invoice image vẫn tạo/cập nhật receiving thủ công bình thường.
- [ ] Không cho client bypass validation quantity hoặc product bằng cách gửi dữ liệu scan đã chỉnh sửa.
- [ ] Có test hoặc kịch bản kiểm chứng URL hợp lệ, URL sai và manual fallback.
