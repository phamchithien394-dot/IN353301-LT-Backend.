# Kiểm thử API bằng `curl`

Tài liệu gồm các lệnh `curl` kiểm thử cho cả 4 chức năng ở mục 2.1 (giả định API chạy tại `http://localhost:5000` sau khi đã nạp dữ liệu từ `sql/01-schema.sql` và `sql/02-seed.sql`).

> **Lưu ý trên Windows PowerShell:** Trong PowerShell, `curl` là alias của `Invoke-WebRequest`, nên hãy dùng `curl.exe` hoặc chạy trong Git Bash / Command Prompt để dùng cú pháp `-H`, `-d` bên dưới.

---

## 1. Xem danh sách đăng ký của một khoá học (`GET`)

- **Mã trạng thái dự kiến:** `200 OK`

```bash
curl -i http://localhost:5000/api/v1/courses/2/registrations
```

---

## 2. Tạo đăng ký mới (`POST`)

Thành viên `3` (Lê Minh Châu) chưa đăng ký khoá học `2` (Cơ sở dữ liệu PostgreSQL) trong dữ liệu seed.

- **Mã trạng thái dự kiến:** `201 Created`

```bash
curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations \
  -H "Content-Type: application/json" \
  -d '{"memberId": 3}'
```

### Kiểm thử trường hợp lỗi trùng đăng ký

Chạy lại đúng lệnh trên lần thứ 2:

- **Mã trạng thái dự kiến:** `409 Conflict` (`application/problem+json`)

```bash
curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations \
  -H "Content-Type: application/json" \
  -d '{"memberId": 3}'
```

---

## 3. Xác nhận đăng ký (`PATCH`)

Xác nhận phiếu đăng ký có `id = 2` (đang ở trạng thái `pending` trong dữ liệu seed) hoặc `id = 7` vừa tạo ở bước 2.

- **Mã trạng thái dự kiến:** `200 OK`

```bash
curl -i -X PATCH http://localhost:5000/api/v1/registrations/2 \
  -H "Content-Type: application/json" \
  -d '{"status": "confirmed"}'
```

---

## 4. Huỷ đăng ký (`DELETE`)

Huỷ phiếu đăng ký có `id = 6` (đang ở trạng thái `pending` trong dữ liệu seed).

- **Mã trạng thái dự kiến:** `204 No Content`

```bash
curl -i -X DELETE http://localhost:5000/api/v1/registrations/6
```

---

## 5. Kiểm thử trường hợp không tìm thấy khoá học (`404 Not Found`)

- **Mã trạng thái dự kiến:** `404 Not Found` (`application/problem+json`)

```bash
curl -i http://localhost:5000/api/v1/courses/999/registrations
```
