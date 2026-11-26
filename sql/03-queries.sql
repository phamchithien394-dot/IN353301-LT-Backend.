-- sql/03-queries.sql

-- (a) Lấy họ tên thành viên và tên khoá học của các phiếu có trạng thái confirmed, sắp xếp theo họ tên tăng dần
SELECT m.full_name, c.title
FROM registrations r
JOIN members m ON m.id = r.member_id
JOIN courses c ON c.id = r.course_id
WHERE r.status = 'confirmed'
ORDER BY m.full_name ASC, c.title ASC;

-- (b) Thống kê số phiếu đăng ký của từng khoá học, kể cả khoá chưa có phiếu nào
-- Dùng LEFT JOIN thay vì JOIN (INNER JOIN) để giữ lại các khoá học chưa có bản ghi nào trong registrations (COUNT(r.id) trả về 0)
SELECT c.id, c.title, COUNT(r.id) AS so_dang_ky
FROM courses c
LEFT JOIN registrations r ON r.course_id = c.id
GROUP BY c.id, c.title
ORDER BY c.id ASC;

-- (c) Liệt kê các thành viên chưa đăng ký khoá học nào
SELECT m.id, m.full_name, m.email
FROM members m
LEFT JOIN registrations r ON r.member_id = m.id
WHERE r.id IS NULL
ORDER BY m.id ASC;
