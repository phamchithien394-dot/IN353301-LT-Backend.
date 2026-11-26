# IN4533 - Bài tập 02: EduHub mini (PostgreSQL & REST API)

- **Họ và tên:** `<Phạm Chí Thiện>`
- **MSSV:** `<0024417997>`
- **Học phần:** Lập trình Back End (`IN4533`)

---

## 1. Cấu trúc thư mục

```text
.
├── docs/
│   ├── api.md              # Thiết kế REST API & JSON mẫu (Phần 2)
│   ├── requests.md         # Các lệnh curl kiểm thử API (Phần 3.1)
│   └── images/             # Ảnh chụp màn hình kết nối PostgreSQL & kiểm thử API
├── sql/
│   ├── 01-schema.sql       # Tạo 3 bảng members, courses, registrations
│   ├── 02-seed.sql         # Dữ liệu mẫu (5 thành viên, 4 khoá học, 6 phiếu)
│   └── 03-queries.sql      # 3 câu truy vấn SELECT / JOIN / GROUP BY
├── src/                    # ASP.NET Core Minimal API (.NET 10 + Npgsql)
│   ├── EduHubApi.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json.example
│   └── Properties/launchSettings.json
├── .gitignore
└── README.md
```

---

## 2. Cách cài đặt PostgreSQL (Mục 1.1)

Hệ thống sử dụng **PostgreSQL 18.6** cài đặt trực tiếp trên Windows (service `postgresql-x64-18`, cổng mặc định `5432`, công cụ dòng lệnh `psql` tại `C:\Program Files\PostgreSQL\18\bin\psql.exe` và pgAdmin).

Kiểm tra kết nối và tạo cơ sở dữ liệu `eduhub_mini`:

```powershell
# Mở kết nối với user postgres (nhập mật khẩu đã thiết lập khi cài đặt)
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432
```

Trong phiên `psql`, tạo database mới và chuyển vào database đó:

```sql
CREATE DATABASE eduhub_mini;
\c eduhub_mini
```

---

## 3. Khởi tạo schema, nạp dữ liệu và chạy truy vấn (Mục 1.2 – 1.4)

Chạy lần lượt 3 tệp SQL từ thư mục gốc của kho mã:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d eduhub_mini -f sql/01-schema.sql
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d eduhub_mini -f sql/02-seed.sql
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d eduhub_mini -f sql/03-queries.sql
```

---

## 4. Chạy ASP.NET Core Minimal API (Mục 3.2)

### Cấu hình chuỗi kết nối an toàn

> **Lưu ý bảo mật:** Không commit mật khẩu hoặc thông tin kết nối thật lên GitHub. File `src/appsettings.Development.json` đã được đưa vào `.gitignore`.

Sao chép file mẫu và điền mật khẩu PostgreSQL của bạn trên máy cục bộ:

```powershell
Copy-Item src/appsettings.Development.json.example src/appsettings.Development.json
```

Hoặc truyền qua biến môi trường khi chạy:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=eduhub_mini;Username=postgres;Password=<MAT_KHAU_CUA_BAN>"
```

### Khởi chạy API

```powershell
dotnet run --project src/EduHubApi.csproj
```

API lắng nghe tại `http://localhost:5000`.

### Kiểm thử nhanh bằng `curl.exe`

```powershell
curl.exe -i http://localhost:5000/api/v1/courses/2/registrations
curl.exe -i -X POST http://localhost:5000/api/v1/courses/2/registrations -H "Content-Type: application/json" -d "{\"memberId\": 3}"
```

Xem đầy đủ các lệnh kiểm thử trong [docs/requests.md](docs/requests.md).
