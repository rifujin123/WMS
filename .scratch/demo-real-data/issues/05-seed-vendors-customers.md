# 05 — Seed vendors + customers (8–12 NCC, 15–20 khách)

**What to build:** Seed danh mục nhà cung cấp (8–12) và khách hàng (15–20) để form Đơn mua chọn NCC, Đơn bán
chọn khách hàng, và các trang master có dữ liệu.

**Blocked by:** 01 — Seed scaffold + gating

**Status:** ready-for-agent

- [ ] 8–12 `Vendor`: tên công ty (nhà phân phối ủy quyền / đại lý), ContactName tiếng Việt, Phone/Email/Address VN thực tế.
- [ ] 15–20 `Customer`: doanh nghiệp + khách lẻ, ContactName, Phone/Email/Address VN.
- [ ] Idempotent theo `Name` (unique index đã có trên Vendor.Name và Customer.Name).
- [ ] Test: đúng count (8–12 / 15–20); tên unique; contact/phone/email có giá trị cơ bản.

**Verified facts:** `Vendor`/`Customer`(Name/ContactName/Phone/Email/Address), đều có unique index trên Name;
Đơn mua chọn vendor, Đơn bán chọn customer từ danh mục.