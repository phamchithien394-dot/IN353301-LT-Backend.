# 2.1.2 Hai người giành chỗ cuối: vì sao cần cô lập?

## 1. Tình huống

Một môn học (`courses`, `id = 2`) chỉ còn **1 chỗ** trống. Hai sinh viên cùng đăng ký tại cùng một thời điểm:

- **Phiên A (An)**
- **Phiên B (Bình)**

2. Diễn biến khi KHÔNG khoá (Hình 2.11)

*Hình 2.11. Tình huống tranh chấp khi không khoá*

| Bước | Phiên A (An)                | Phiên B (Bình)              |
|------|-----------------------------|-----------------------------|
| 1    | `SELECT`: còn 1 chỗ         |                             |
| 2    |                             | `SELECT`: còn 1 chỗ         |
| 3    | `INSERT` đăng ký → `COMMIT` |                             |
| 4    |                             | `INSERT` đăng ký → `COMMIT` |

> Bước 4 (phiên của Bình) là bước gây lỗi, được tô đỏ trong slide.

```text
Thời gian ─────────────────────────────────────────────►

Phiên A (An):   SELECT ──────────► INSERT + COMMIT
                (còn 1 chỗ)
Phiên B (Bình):      SELECT ──────────────────► INSERT + COMMIT   ← sai
                     (còn 1 chỗ)
```

3. Hậu quả

**Cả hai cùng thấy "còn 1 chỗ" → khoá có 41/40 người (race condition).**

Nguyên nhân: cả An và Bình đều ra quyết định dựa trên dữ liệu đọc ở bước `SELECT`, trong khi dữ liệu đó đã thay đổi (do phiên kia `INSERT`) trước khi mình ghi. Sức chứa tối đa là 40 nhưng thực tế có 41 người đăng ký.

4. Cách xử lý

**Khoá hàng bằng `SELECT ... FOR UPDATE`, hoặc kiểm tra trong cùng giao dịch.**

```sql
BEGIN;
SELECT capacity FROM courses WHERE id = 2 FOR UPDATE;  -- giữ khoá hàng
-- đếm số đăng ký, còn chỗ thì INSERT
COMMIT;
```

### Giải thích từng dòng

| Câu lệnh | Ý nghĩa |
|----------|---------|
| `BEGIN;` | Bắt đầu giao dịch. |
| `SELECT capacity FROM courses WHERE id = 2 FOR UPDATE;` | Đọc sức chứa của môn học và **khoá hàng** `id = 2`. Giao dịch khác muốn khoá/ghi cùng hàng này phải chờ. |
| `-- đếm số đăng ký, còn chỗ thì INSERT` | Trong khi đang giữ khoá, đếm số người đã đăng ký; nếu còn chỗ thì `INSERT` bản ghi đăng ký. |
| `COMMIT;` | Kết thúc giao dịch và **nhả khoá**. |

5. Diễn biến khi CÓ khoá

| Bước | Phiên A (An) | Phiên B (Bình) |
|------|--------------|----------------|
| 1 | `BEGIN` + `SELECT ... FOR UPDATE` → **lấy được khoá** | |
| 2 | | `BEGIN` + `SELECT ... FOR UPDATE` → **phải chờ** |
| 3 | Đếm số đăng ký, còn chỗ → `INSERT` → `COMMIT` (nhả khoá) | |
| 4 | | Được khoá, đếm lại thấy đã đủ 40 → **không `INSERT`** → `COMMIT`/`ROLLBACK` |

Kết quả: lớp giữ đúng **40/40**, không bị vượt sức chứa.

6. Ví dụ chạy thử (tham khảo)

> Viết theo PostgreSQL.

```sql
-- Tạo bảng
CREATE TABLE courses (
    id       INT PRIMARY KEY,
    name     TEXT NOT NULL,
    capacity INT  NOT NULL
);

CREATE TABLE registrations (
    id         SERIAL PRIMARY KEY,
    course_id  INT  NOT NULL REFERENCES courses(id),
    student    TEXT NOT NULL
);

-- Dữ liệu mẫu: môn id = 2, sức chứa 40, đã có 39 người đăng ký
INSERT INTO courses (id, name, capacity) VALUES (2, 'Backend', 40);
INSERT INTO registrations (course_id, student)
SELECT 2, 'sv' || g FROM generate_series(1, 39) AS g;
```

Mở **hai cửa sổ terminal** (hai phiên) và chạy xen kẽ:

```sql
-- Phiên A (An)                              -- Phiên B (Bình)
BEGIN;
SELECT capacity FROM courses
 WHERE id = 2 FOR UPDATE;
                                             BEGIN;
                                             SELECT capacity FROM courses
                                              WHERE id = 2 FOR UPDATE;  -- bị chờ
SELECT count(*) FROM registrations
 WHERE course_id = 2;                        -- (đang chờ...)
INSERT INTO registrations (course_id, student)
VALUES (2, 'An');
COMMIT;                                      -- nhả khoá
                                             -- Bình được chạy tiếp
                                             SELECT count(*) FROM registrations
                                              WHERE course_id = 2;  -- = 40, hết chỗ
                                             ROLLBACK;
```

7. Tóm tắt

- **Race condition**: hai giao dịch cùng đọc dữ liệu cũ rồi cùng ghi → kết quả sai (41/40).
- **Giải pháp**: khoá hàng bằng `SELECT ... FOR UPDATE`, hoặc kiểm tra điều kiện trong cùng một giao dịch.
- **Mấu chốt**: bước "kiểm tra còn chỗ" và bước "đăng ký" phải được thực hiện **như một khối không bị xen vào**.
