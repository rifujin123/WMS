# 02 — Seed users & roles (2 manager + 4 staff)

**What to build:** Seed các user demo để mọi trường actor (`CreatedBy`/`ConfirmedBy`/`PickedBy`/`ApprovedBy`)
hiển thị người thật, không phải null: 2 `WarehouseManager` + 4 `WarehouseStaff` (admin đã tồn tại từ Program.cs seed
hiện tại).

**Blocked by:** 01 — Seed scaffold + gating

**Status:** ready-for-agent

- [ ] Đảm bảo roles tồn tại: `Admin`, `WarehouseManager`, `WarehouseStaff` (Program.cs đã tạo — không tạo kép).
- [ ] Tạo 2 user `WarehouseManager` + 4 user `WarehouseStaff`: username + email + FullName tiếng Việt, `CreatedAt = UtcNow`.
- [ ] Password demo dùng config `Seed:DemoPassword` (default `Admin@123`) — không hardcode ý nghĩa khác biệt; chỉ dùng demo.
- [ ] Gán `CreatedById = admin` cho các user seed (actor hợp lý).
- [ ] Idempotent theo username (`UserManager.FindByNameAsync` — đã tồn tại thì skip).
- [ ] Ghi chú tài khoản demo (username / vai trò) vào log hoặc README dev.
- [ ] Test: đủ 6 user + đúng vai trò; chạy lại không tạo kép; password đăng nhập được (đã hash đúng).