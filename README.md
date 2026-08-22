# WMS - Warehouse Management System

## Giới thiệu

WMS là ứng dụng quản lý kho hàng. Hệ thống theo dõi luồng hàng từ đơn đặt hàng, nhận hàng, cất vào vị trí kho, quản lý tồn kho, đến lấy hàng cho đơn bán.

## Tính năng

- Đăng nhập và phân quyền `Admin`, `WarehouseManager`, `WarehouseStaff`.
- Quản lý người dùng: cập nhật thông tin, khóa/mở khóa tài khoản, đặt lại mật khẩu, cập nhật avatar.
- Quản lý danh mục, sản phẩm, hình ảnh sản phẩm, kho và vị trí trong kho.
- Quản lý đơn đặt hàng: tạo, sửa, duyệt và đóng Purchase Order.
- Nhận hàng: tạo phiếu nhận, kiểm tra số lượng theo PO, xác nhận phiếu nhận và sinh task cất hàng.
- Cất hàng: chọn vị trí đích, phân công nhân viên, bắt đầu/hoàn thành task; cập nhật tồn kho và sức chứa vị trí khi hoàn thành.
- Quản lý tồn kho theo sản phẩm, vị trí và kho.
- Quản lý đơn bán và Picking: tạo phiếu lấy hàng, phân công, xử lý lấy hàng và trừ tồn khi hoàn thành.
- Audit log, lịch sử đổi trạng thái và lịch sử biến động tồn kho.
- Phân trang cho các trang danh sách chính.

## Công nghệ sử dụng

| Phần | Công nghệ |
| --- | --- |
| Frontend | React 19, TypeScript, Vite, Ant Design, TanStack Query, Axios |
| Backend | ASP.NET Core 8, Entity Framework Core, ASP.NET Identity, JWT |
| Database | SQL Server |
| Lưu ảnh | Cloudinary |
| API Documentation | Swagger / OpenAPI |

## Kiến trúc hệ thống

Backend được tổ chức theo Clean Architecture. Luồng phụ thuộc đi từ ngoài vào trong; lớp Domain không phụ thuộc vào framework, database hay dịch vụ bên ngoài.

```text
                         +----------------------+
                         |      WMS.Domain      |
                         | Entity, Enum, Model  |
                         +----------^-----------+
                                    |
                         +----------+-----------+
                         |  WMS.Application     |
                         | Use case, DTO,       |
                         | Service interface    |
                         +----------^-----------+
                                    |
              +---------------------+---------------------+
              |                                           |
+-------------+-------------+               +-------------+-------------+
|     WMS.Infrastructure     |               |        WMS.API           |
| EF Core, Repository,       |               | Controller, Middleware,  |
| Identity, SQL Server,      |               | Swagger, DI, JWT         |
| Cloudinary, Service impl. |               |                          |
+-------------+-------------+               +-------------+-------------+
              |                                           |
              v                                           |
       SQL Server / Cloudinary                    React Frontend
```

### WMS.Domain

Lớp lõi của hệ thống, chứa entity, enum và các model nghiệp vụ như `Product`, `Warehouse`, `Location`, `Stock`, `Receiving`, `Picking` và các trạng thái tương ứng. Lớp này không biết về HTTP, Entity Framework hay Cloudinary.

### WMS.Application

Chứa các use case và quy tắc xử lý nghiệp vụ. Lớp này định nghĩa DTO, service interface, repository interface và các service như nhận hàng, cất hàng, picking, tồn kho. Application chỉ phụ thuộc vào Domain và các abstraction của chính nó.

### WMS.Infrastructure

Chứa phần triển khai kỹ thuật cho các abstraction ở Application: `WmsDbContext`, EF Core migrations, repository SQL Server, ASP.NET Identity, JWT service và Cloudinary service. Đây là lớp giao tiếp với database và dịch vụ bên ngoài.

### WMS.API

Là điểm vào của backend. API nhận HTTP request từ frontend, kiểm tra JWT/RBAC, gọi service Application và trả response. Đây cũng là nơi đăng ký dependency injection, middleware, CORS và Swagger.

### Frontend

Frontend React giao tiếp với `WMS.API` qua HTTP/JSON và JWT. `ProtectedRoute` kiểm soát quyền truy cập màn hình ở phía client để hỗ trợ trải nghiệm người dùng; backend vẫn là lớp kiểm tra quyền cuối cùng bằng `[Authorize]`.

## Cấu trúc thư mục

```text
WMS/
├── frontend/                         # React + Vite
│   ├── src/components/               # Component dùng lại
│   ├── src/pages/                    # Các trang nghiệp vụ
│   ├── src/services/                 # Gọi API
│   ├── src/hooks/                    # React Query hooks
│   └── src/router/                   # Route và phân quyền frontend
│
└── backend/WMS-mini/
    ├── src/WMS.API/                  # API, controller, middleware
    ├── src/WMS.Application/          # Service, DTO, interface
    ├── src/WMS.Domain/               # Entity, enum
    └── src/WMS.Infrastructure/       # EF Core, repository, Identity
```

## Yêu cầu môi trường

- Node.js 20 trở lên.
- .NET SDK 8.
- SQL Server hoặc SQL Server LocalDB.
- Tài khoản Cloudinary nếu dùng upload ảnh sản phẩm hoặc avatar.

## Cài đặt và chạy dự án

### Chạy backend

```powershell
Copy-Item backend/WMS-mini/src/WMS.API/appsettings.Development.example.json backend/WMS-mini/src/WMS.API/appsettings.Development.json
cd backend/WMS-mini
dotnet restore
dotnet run --project src/WMS.API
```

API local chạy tại `http://localhost:5246`.

### Chạy frontend

```powershell
Copy-Item frontend/.env.example frontend/.env
cd frontend
npm install
npm run dev
```

Frontend local chạy tại `http://localhost:5173`.

### Kiểm tra source

```powershell
cd frontend
npm run lint
npm run build
```

```powershell
cd backend/WMS-mini
dotnet build WMS.sln
dotnet test WMS.sln
```

Hiện solution chưa có project test tự động. Lệnh `dotnet test` được giữ để dùng khi bổ sung test project.

## Database

Dự án dùng SQL Server với Entity Framework Core migrations.

- Connection string mặc định được cấu hình trong `appsettings.json`.
- Khi chạy API, `db.Database.Migrate()` được gọi để áp dụng migration.
- Các ràng buộc chính gồm SKU sản phẩm, mã kho, mã PO, mã receiving, mã picking và cặp `ProductId + LocationId` trong tồn kho.
- Trang danh sách API dùng page size mặc định là 10.

## Deployment

Frontend đã được deploy tại:

- [https://wms-boie.onrender.com/](https://wms-boie.onrender.com/)

## API Documentation

Swagger chỉ được bật khi backend chạy trong môi trường `Development`.

- Swagger local: [http://localhost:5246/swagger](http://localhost:5246/swagger)
- API local: `http://localhost:5246/api/...`

Một số nhóm API chính:

| Nhóm | Endpoint gốc |
| --- | --- |
| Xác thực | `/api/Auth` |
| Người dùng | `/api/Users` |
| Sản phẩm | `/api/Products` |
| Danh mục | `/api/Categories` |
| Kho và vị trí | `/api/Warehouses`, `/api/Locations` |
| Đơn đặt hàng | `/api/PurchaseOrders` |
| Nhận hàng | `/api/Receivings` |
| Cất hàng | `/api/PutAwayTasks` |
| Tồn kho | `/api/Stocks` |
| Đơn bán và lấy hàng | `/api/SaleOrders`, `/api/Pickings` |

RMA và Association Rules mới có entity/DTO ở backend, chưa có API hoặc UI hoàn chỉnh.
