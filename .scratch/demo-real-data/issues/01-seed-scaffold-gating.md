# 01 — Seed scaffold + gating (ISeedDemoData, DI, Seed:Enabled)

**What to build:** Hạ tầng seed dữ liệu demo: interface + implementation root đăng ký DI, được API host gọi
đúng 1 lần sau `Migrate()` trong `Development`, gate bằng cờ `Seed:Enabled`. Nền tảng để các ticket 02–08
bám vào.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] Interface ra `IDemoDataSeeder` (contract: `Task<SeedSummary> SeedAsync(CancellationToken)`; `SeedSummary` = counts đã seed theo nhóm).
- [ ] Implementation trong `WMS.Infrastructure`, đăng ký DI (scoped).
- [ ] API host: sau `Migrate()` (scope startup), gọi seeder khi `app.Environment.IsDevelopment() && Seed:Enabled != false`.
- [ ] Config key `Seed:Enabled` — mặc định `true` trong Development (appsettings.Development.json), không bật ở prod.
- [ ] Base idempotency: nếu dữ liệu đã tồn tại (ví dụ đếm Warehouse > 0) thì skip toàn bộ (tránh chạy lại midway khi DB đã có từ đợt trước).
- [ ] `ILogger` log tóm tắt counts sau khi seed xong.
- [ ] Test (single seam — in-memory DbContext): gating bật/tắt đúng; chạy 2 lần không duplicate; skip khi đã có dữ liệu.

**Verified facts (CONTEXT.md):** không có sẵn seeder nào ngoài roles+admin tại `Program.cs:198–225`; convention
ticket ở `.scratch/receiving-workflow/issues/01-receiving-draft-validation.md`.