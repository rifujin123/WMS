# 09 — Test suite (single seam) + CI green + docs

**What to build:** Hoàn thiện bộ test cho toàn bộ feature theo **single seam** (contract seeder → in-memory
`WmsDbContext`), đảm bảo CI (backend build + test) xanh, và cập nhật docs ghi nhận tính năng + tài khoản demo.

**Blocked by:** 07, 08 (cần feature đủ trước khi test toàn bộ)

**Status:** ready-for-agent

- [ ] Test integration dùng in-memory `WmsDbContext` (EF InMemory / SQLite) cho toàn seeder: chạy `SeedAsync` lên DB trống rồi assert.
- [ ] Idempotency: chạy 2 lần → count không đổi, không duplicate.
- [ ] Consistency: Onhand == Σ movements (từng product+location); không tồn âm; capacity ≤ max.
- [ ] Backdate: timestamp trong cửa sổ 4–6 tuần; thứ tự newest-first (paging dashboard).
- [ ] Master data: đếm đúng (2 kho, 24/40 vị trí, 60 SKU, 8–12 NCC, 15–20 KH, 6 user, roles).
- [ ] Chuỗi mẫu: PO Closed, SaleOrder Packed, Adjustment Approved; StatusHistory có đủ bước; 1-confirmed-receiving giữ.
- [ ] Giữ **19 test hiện có** xanh (regression); dùng `dotnet test` bình thường như project `WMS.Tests`.
- [ ] Đảm bảo `dotnet build -m:1 --no-restore` + `dotnet test` + (nếu đụng frontend) `npx tsc -b`/`npm run lint`/`npm run build` đều xanh.
- [ ] Cập nhật `docs/issues/README.md`: ghi nhận tính năng "dữ liệu demo thật" + tài khoản demo + cách reset/disable.
- [ ] Chạy `/code-review` toàn bộ diff trước khi commit.

**Prior art:** `backend/WMS-mini/tests/WMS.Tests/` (`Fakes.cs`: FakeCurrentUser/FakeUnitOfWork/MapperFactory),
19 test hiện có; workflow `.github/workflows/ci.yml`.