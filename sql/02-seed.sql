-- sql/02-seed.sql
-- Dữ liệu mẫu: 5 thành viên (>= 4), 4 khoá học (>= 3), 6 phiếu đăng ký (>= 6)
-- Đảm bảo có đủ 3 trạng thái (pending, confirmed, cancelled) và có thành viên chưa đăng ký khoá nào

INSERT INTO members (full_name, email) VALUES
    ('Nguyễn Văn An', 'an.nguyen@eduhub.vn'),
    ('Trần Thị Bình', 'binh.tran@eduhub.vn'),
    ('Lê Minh Châu', 'chau.le@eduhub.vn'),
    ('Phạm Quốc Dũng', 'dung.pham@eduhub.vn'),
    ('Hoàng Thị Em', 'em.hoang@eduhub.vn');

INSERT INTO courses (title, capacity) VALUES
    ('Lập trình Back End với ASP.NET Core', 30),
    ('Cơ sở dữ liệu PostgreSQL', 25),
    ('Thiết kế RESTful API', 20),
    ('Docker cơ bản cho lập trình viên', 15);

-- Ghi chú: Thành viên 5 (Hoàng Thị Em) chưa đăng ký khoá nào;
-- Khoá 4 (Docker cơ bản) chưa có phiếu nào;
-- Thành viên 3 (Lê Minh Châu) chưa đăng ký Khoá 2 (phù hợp với ví dụ POST tạo đăng ký id = 7).
INSERT INTO registrations (member_id, course_id, status) VALUES
    (1, 1, 'confirmed'),
    (2, 1, 'pending'),
    (1, 2, 'confirmed'),
    (2, 2, 'cancelled'),
    (3, 1, 'confirmed'),
    (4, 3, 'pending');
