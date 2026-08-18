# Hướng dẫn Set up Docker cho WMS

Tài liệu hướng dẫn chạy toàn bộ hệ thống WMS bằng **Docker Compose** trên máy bất kỳ (Windows / macOS / Linux / VPS) — không cần cài .NET SDK, Node.js hay SQL Server thủ công.

Đối chiếu ticket: [`docs/issues/09-docker-compose-deploy.md`](docs/issues/09-docker-compose-deploy.md) (status: ready-for-agent).

---

## 1. Kiến trúc

```
┌────────────────────────────┐
│  web (Nginx)               │  ← http://localhost:8080  (SPA + proxy /api)
│  - phục vụ frontend build  │
│  - proxy /api/* → api:8080 │
└──────────┬─────────────────┘
           │ /api/*
┌──────────▼─────────────────┐
│  api (ASP.NET Core 8)      │  ← http://api:8080
│  - WMS.API (JWT, Swagger)  │
│  - seed roles + admin      │
└──────────┬─────────────────┘
           │ SQL Server
┌──────────▼─────────────────┐
│  db (SQL Server 2022)      │  ← data bền qua volume mssql-data
└────────────────────────────┘
```

- **3 service**: `db`, `api`, `web` — chạy bằng **một lệnh** `docker compose up -d --build`.
- **Volume** `mssql-data` giữ dữ liệu: restart container **không mất dữ liệu**.
- Frontend build tĩnh, phục vụ qua **Nginx**, proxy `/api` về backend (cùng origin → không vướng CORS).

---

## 2. Yêu cầu

- **Docker**:
  - Windows/macOS: cài [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows cần WSL2).
  - Linux/VPS: Docker Engine + Docker Compose plugin: `sudo apt install docker.io docker-compose-v2` (hoặc theo tài liệu distro).
- Kiểm tra đã cài:

  ```bash
  docker --version
  docker compose version
  ```

> Không cần cài .NET SDK, Node.js hay SQL Server trên máy host.

---

## 3. Cấu trúc file cần tạo

```
D:\GITHUB\WMS\                        (repo root)
├── docker-compose.yml                ← TẠO MỚI
├── .env                              ← TẠO MỚI (hoặc copy từ .env.example)
├── backend\
│   ├── Dockerfile                    ← TẠO MỚI
│   └── .dockerignore                 ← TẠO MỚI
└── frontend\
    ├── Dockerfile                    ← TẠO MỚI
    ├── .dockerignore                 ← TẠO MỚI
    └── nginx.conf                    ← TẠO MỚI
```

---

## 4. Nội dung từng file

### 4.1 `backend/Dockerfile`

Multi-stage: build bằng SDK image → publish → runtime image.

```dockerfile
# ── Build stage ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + projects (tận dụng layer cache cho restore)
COPY WMS-mini/WMS.sln ./
COPY WMS-mini/src/ ./src/
RUN dotnet restore WMS.sln

# Publish API (Release)
RUN dotnet publish WMS-mini/src/WMS.API/WMS.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Container listen trên 8080 (Nginx sẽ proxy tới đây)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WMS.API.dll"]
```

> **Lưu ý về build context**: file này nằm trong `backend/` và `docker-compose.yml` sẽ khai báo `context: ./backend`, nên mọi đường dẫn trong Dockerfile tính từ thư mục `backend/` (ví dụ `WMS-mini/WMS.sln`).

### 4.2 `backend/.dockerignore`

```gitignore
**/bin/
**/obj/
**/.vs/
*.user
*.suo
appsettings.Development.json
appsettings.*.local.json
```

### 4.3 `frontend/Dockerfile`

Build bằng Node 22 (Vite 8 yêu cầu Node ≥ 20.19 / ≥ 22.12) → serve bằng Nginx.

```dockerfile
# ── Build stage ────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

# Cài dependencies trước để tận dụng layer cache
COPY package.json package-lock.json ./
RUN npm ci

# Copy source rồi build
COPY . .
# Trong container, frontend gọi API same-origin qua Nginx proxy /api
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# ── Serve stage ────────────────────────────────────────────
FROM nginx:alpine AS serve
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

> **Tại sao `VITE_API_BASE_URL=/api`?** File `frontend/.env` hiện tại đặt `VITE_API_BASE_URL=http://localhost:5246/api` (chỉ đúng khi dev). Khi build trong Docker, `ENV` (process env) có độ ưu tiên cao hơn file `.env`, nên axios sẽ gọi `/api/...` — đúng đường proxy của Nginx. **Không cần sửa source code.**

### 4.4 `frontend/.dockerignore`

```gitignore
node_modules
dist
.env
.env.*.local
npm-debug.log*
```

### 4.5 `frontend/nginx.conf`

```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    # Proxy API về backend container (giữ nguyên URI /api/...)
    location /api/ {
        proxy_pass http://api:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # SPA fallback: mọi route không phải file thật → index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache tĩnh
    location ~* \.(js|css|png|jpg|jpeg|gif|svg|ico|woff2?)$ {
        expires 7d;
        add_header Cache-Control "public, max-age=604800";
    }
}
```

> `proxy_pass http://api:8080;` **không** có trailing slash → giữ nguyên đường dẫn gốc `/api/Auth/login`, khớp với route `api/[controller]` của backend.

### 4.6 `docker-compose.yml` (repo root)

```yaml
name: wms

services:
  # ── SQL Server 2022 ──────────────────────────────────────
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD}
      MSSQL_PID: Developer
    ports:
      - "1433:1433"          # mở ra host nếu muốn dùng SSMS/Azure Data Studio
    volumes:
      - mssql-data:/var/opt/mssql   # ← dữ liệu bền qua restart
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q 'SELECT 1' -b -o /dev/null"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

  # ── Backend API (.NET 8) ─────────────────────────────────
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      # ⚠️ Bắt buộc: Program.cs ném exception nếu thiếu Jwt/Cloudinary
      Jwt__Key: ${JWT_KEY}
      Jwt__Issuer: ${JWT_ISSUER:-WMS}
      Jwt__Audience: ${JWT_AUDIENCE:-WMS}
      Cloudinary__CloudName: ${CLOUDINARY_CLOUD_NAME}
      Cloudinary__ApiKey: ${CLOUDINARY_API_KEY}
      Cloudinary__ApiSecret: ${CLOUDINARY_API_SECRET}
      # ⚠️ Trỏ tới container db, KHÔNG phải localdb
      ConnectionStrings__DefaultConnection: "Server=db,1433;Database=WMS;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;"
    depends_on:
      db:
        condition: service_healthy
    # (tùy chọn) mở API ra host để debug Swagger:
    # ports:
    #   - "5246:8080"

  # ── Frontend (Nginx) ─────────────────────────────────────
  web:
    build:
      context: ./frontend
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: /api
    ports:
      - "8080:80"
    depends_on:
      - api

volumes:
  mssql-data:
```

### 4.7 `.env` (repo root) — tạo bản sao thành `.env.example` để commit

```dotenv
# ── SQL Server ────────────────────────────────────────────
# Yêu cầu mạnh: ≥ 8 ký tự, 3/4 loại (hoa, thường, số, ký tự đặc biệt)
MSSQL_SA_PASSWORD=Wms@12345!

# ── JWT ───────────────────────────────────────────────────
# Lấy từ appsettings.Development.json (mục Jwt:Key) — nên đổi key riêng khi deploy thật
JWT_KEY=d46f7c5e2b3a9d8c1e0f6b7a8d9c2e3f4a5b6c7d8e9f0a1b2c3d4e5f6a7b8c9d
JWT_ISSUER=YourIssuer
JWT_AUDIENCE=YourAudience

# ── Cloudinary (BẮT BUỘC — app crash nếu thiếu) ──────────
# Lấy từ appsettings.Development.json (mục Cloudinary) hoặc tài khoản Cloudinary của bạn
CLOUDINARY_CLOUD_NAME=dtsfahg4d
CLOUDINARY_API_KEY=775617431467893
CLOUDINARY_API_SECRET=qwO39FuNCpapJj8qcIX48AzVCP4
```

> ⚠️ `.env` chứa secret → thêm vào `.gitignore` (hiện gitignore đã chặn `**/.env`). Commit **`.env.example`** với giá trị mẫu, không commit `.env` thật.

---

## 5. Migration database ⚠️ (quan trọng)

`Program.cs` **hiện chưa tự chạy migration** — seeding roles + tài khoản `admin` sẽ fail nếu bảng chưa tồn tại. Có 2 cách:

### Cách A — Khuyến nghị: thêm auto-migrate vào `Program.cs` (đổi ~5 dòng)

Trong `backend/WMS-mini/src/WMS.API/Program.cs`, ngay **trước** khối seed (trước dòng `// Seed roles and default admin`), thêm:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    db.Database.Migrate(); // áp dụng migration chưa chạy
}
```

Lúc này `docker compose up -d --build` tự migrate → không cần thao tác thủ công. (`WmsDbContext` và `Microsoft.EntityFrameworkCore` đã được `using` sẵn ở đầu file.)

### Cách B — Không sửa code: chạy migrate một lần thủ công

Chạy migration **sau khi `db` healthy, trước khi `api` start lần đầu** (chạy trên máy host có .NET SDK 8, hoặc bất kỳ máy nào):

```bash
# Từ thư mục repo root, khi db đã chạy:
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"        # PowerShell: $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
cd backend/WMS-mini
dotnet ef database update \
  --project src/WMS.API/WMS.API.csproj \
  --connection "Server=localhost,1433;Database=WMS;User Id=sa;Password=Wms@12345!;TrustServerCertificate=True;"
```

Sau đó `docker compose up -d --build` bình thường (lần sau không cần migrate lại nếu đã có `__EFMigrationsHistory`).

---

## 6. Các bước chạy

```bash
# 1. Tạo .env (xem mục 4.7)
# 2. Tạo đủ các file ở mục 4
# 3. Build + chạy (lần đầu sẽ tải image, mất vài phút)
docker compose up -d --build

# 4. Kiểm tra trạng thái (chờ db healthy, api/web running)
docker compose ps
docker compose logs -f api        # xem log backend (migration, seed admin...)

# 5. Mở web
#    http://localhost:8080
```

**Truy cập sau khi chạy:**

| Thứ | URL / Thông tin |
|---|---|
| Web (SPA) | http://localhost:8080 |
| API Swagger | http://localhost:5246/swagger (nếu mở port `5246:8080` và `ASPNETCORE_ENVIRONMENT=Development`) |
| SQL Server (SSMS/ADS) | `localhost,1433` — user `sa`, password trong `.env` |
| Tài khoản mặc định | `admin` / `Admin@123` (backend tự seed khi start) |

**Kiểm tra nhanh luồng chính:** đăng nhập `admin` → tạo kho/vị trí → nhập kho (Receiving) → cất hàng (PutAway) → tồn kho (Stock) → đơn bán → picking → shipment.

**Các lệnh quản lý:**

```bash
docker compose logs -f            # log tất cả service
docker compose restart api        # restart riêng backend
docker compose down               # dừng (giữ volume dữ liệu)
docker compose down -v            # dừng VÀ XÓA volume dữ liệu (reset toàn bộ)
docker compose ps                 # trạng thái + port
```

**Reset từ đầu** (khi cần demo sạch): `docker compose down -v` rồi `docker compose up -d --build`.

---

## 7. Xử lý sự cố thường gặp

| Triệu chứng | Nguyên nhân & cách xử lý |
|---|---|
| `api` restart liên tục, log có `Cloudinary:CloudName is not configured.` | Thiếu `CLOUDINARY_*` trong `.env` → bổ sung rồi `docker compose up -d` lại. |
| Log `Jwt:Key is not configured.` | Thiếu `JWT_KEY` trong `.env`. |
| Log `A network-related or instance-specific error...` khi kết nối `db` | Chưa đúng connection string (`Server=db,1433`) hoặc `api` start trước khi `db` sẵn sàng — kiểm tra `depends_on: condition: service_healthy`, xem `docker compose ps` có cột HEALTHY không. |
| Lỗi `Invalid object name 'dbo.AspNetRoles'` (hoặc bảng không tồn tại) | **Chưa migrate** — làm theo mục 5. |
| `SA password does not meet SQL Server password policy` | Đổi `MSSQL_SA_PASSWORD` thành mật khẩu mạnh (≥8 ký tự, 3/4 loại ký tự). |
| Cổng bị chiếm (`port is already allocated`) | Đổi port host trong compose, ví dụ `"8081:80"`. |
| Web mở lên trắng / 404 khi vào route con (ví dụ `/login`) | Thiếu SPA fallback trong nginx.conf (`try_files ... /index.html`) — kiểm tra mục 4.5. |
| Gọi API lỗi 404 | Kiểm tra nginx proxy `/api/` (mục 4.5) và route backend `api/[controller]`. |
| Build frontend fail vì Node version | Vite 8 cần Node ≥ 20.19 / ≥ 22.12 — dùng `node:22-alpine` như Dockerfile mẫu. |

---

## 8. Ghi chú thêm

- **CORS**: policy `Frontend` trong `Program.cs` chỉ cho phép `http://localhost:5173`, nhưng với kiến trúc Nginx proxy `/api` cùng origin thì **không ảnh hưởng** — không cần sửa.
- **Swagger**: chỉ bật khi `ASPNETCORE_ENVIRONMENT=Development`. Muốn xem Swagger khi demo: đổi thành `Development` và mở port `5246:8080`.
- **Cloudinary**: dùng cho upload ảnh sản phẩm. Nếu không có tài khoản, đăng ký miễn phí tại [cloudinary.com](https://cloudinary.com) rồi điền 3 giá trị vào `.env`.
- **Bảo mật khi deploy thật (VPS)**: đổi `JWT_KEY`, `MSSQL_SA_PASSWORD`, secret Cloudinary; không mở port `1433` ra internet; cân nhắc HTTPS (Nginx + certbot hoặc reverse proxy).
