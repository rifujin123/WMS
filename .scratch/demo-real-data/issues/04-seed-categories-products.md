# 04 — Seed categories + products (60 SKU, ảnh placeholder)

**What to build:** Seed danh mục phẳng + 60 sản phẩm điện thoại / laptop / phụ kiện với thương hiệu thật,
tên tiếng Việt, giá VND, ảnh placeholder ổn định theo SKU (`Product.ImageUrl`).

**Blocked by:** 01 — Seed scaffold + gating

**Status:** ready-for-agent

- [ ] 3–4 `Category` phẳng phù hợp scope: Điện thoại, Laptop, Phụ kiện (có thể thêm Máy tính bảng).
- [ ] **60** `Product` rải đều các category: SKU có cấu trúc (brand-model-color-storage), tên tiếng Việt thị trường VN.
- [ ] Thương hiệu thật (Apple, Samsung, Xiaomi, Dell, Lenovo, HP, Logitech, Anker…); giá VND hợp lý (điện thoại ~5–40tr, laptop ~8–60tr, phụ kiện vài trăm nghìn).
- [ ] `Unit` hợp lý (Cái/Chiếc/Bộ), `Dimension` có giá trị.
- [ ] `ImageUrl` = placeholder stable theo SKU (cùng SKU → cùng ảnh; không cần Cloudinary; chấp nhận phụ thuộc internet khi demo).
- [ ] Idempotent theo `Product.Sku` (unique index đã có) và `Category.Name`.
- [ ] Test: đúng 60 sản phẩm; SKU unique; giá > 0; mọi sản phẩm có Category; URL ảnh deterministic theo SKU.

**Verified facts:** `Product`(Sku/Name/CategoryId/Unit/Price/Dimension/ImageUrl), `Product.Sku` unique;
`Category` phẳng (không phân cấp); Products page fallback icon xám khi ImageUrl trống.