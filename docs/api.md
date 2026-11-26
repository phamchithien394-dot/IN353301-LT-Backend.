# Thiết kế REST API — EduHub mini

## 2.1. Bảng thiết kế endpoint cho `registrations`

Quy ước áp dụng:
- URI sử dụng danh từ số nhiều (`courses`, `registrations`), **không chứa động từ**.
- Phiên bản hoá API qua tiền tố `/api/v1`.
- Dữ liệu trao đổi định dạng JSON (`application/json`), phản hồi lỗi chuẩn **Problem Details (RFC 9457)** (`application/problem+json`).

| Chức năng | Method | URI | Request Body | Mã thành công | Các mã lỗi có thể gặp |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Xem danh sách đăng ký của một khoá học | `GET` | `/api/v1/courses/{courseId}/registrations` | *(Không có)* | `200 OK` | `400 Bad Request` (`courseId` không hợp lệ), `404 Not Found` (không tìm thấy khoá học) |
| Tạo đăng ký | `POST` | `/api/v1/courses/{courseId}/registrations` | `{ "memberId": 3 }` | `201 Created` | `400 Bad Request` (dữ liệu không hợp lệ), `404 Not Found` (không tìm thấy khoá học hoặc thành viên), `409 Conflict` (thành viên đã đăng ký khoá học này hoặc khoá học đã đủ sĩ số) |
| Xác nhận đăng ký | `PATCH` | `/api/v1/registrations/{registrationId}` | `{ "status": "confirmed" }` | `200 OK` | `400 Bad Request` (`status` hoặc ID không hợp lệ), `404 Not Found` (không tìm thấy phiếu đăng ký), `409 Conflict` (phiếu đã bị huỷ, không thể xác nhận) |
| Huỷ đăng ký | `DELETE` | `/api/v1/registrations/{registrationId}` | *(Không có)* | `204 No Content` | `400 Bad Request` (ID không hợp lệ), `404 Not Found` (không tìm thấy phiếu đăng ký), `409 Conflict` (phiếu đã ở trạng thái `cancelled` trước đó) |

> **Ghi chú thiết kế:** Thao tác *xác nhận đăng ký* sử dụng `PATCH /api/v1/registrations/{registrationId}` với payload `{ "status": "confirmed" }` thay vì thêm động từ `/confirm` vào URI. Thao tác *huỷ đăng ký* sử dụng `DELETE /api/v1/registrations/{registrationId}` để chuyển trạng thái phiếu sang `cancelled` trong cơ sở dữ liệu.

---

## 2.2. JSON Request và Response mẫu cho chức năng Tạo đăng ký

### Request mẫu

```http
POST /api/v1/courses/2/registrations HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
  "memberId": 3
}
```

### Response thành công (`201 Created`)

```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/v1/registrations/7

{
  "id": 7,
  "memberId": 3,
  "courseId": 2,
  "status": "pending"
}
```

### Response lỗi trùng đăng ký (`409 Conflict` — RFC 9457 Problem Details)

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://eduhub.local/errors/duplicate-registration",
  "title": "Duplicate registration",
  "status": 409,
  "detail": "Member 3 has already registered for course 2.",
  "instance": "/api/v1/courses/2/registrations"
}
```
