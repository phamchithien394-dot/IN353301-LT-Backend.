-- CÂU 1
SELECT c.title
FROM courses c
JOIN registrations r ON c.id = r.course_id
JOIN members m ON r.member_id = m.id
WHERE m.full_name = 'Nguyễn An';

-- CÂU 2
SELECT c.id, c.title, COUNT(r.member_id) AS total_members
FROM courses c
LEFT JOIN registrations r ON c.id = r.course_id
GROUP BY c.id, c.title
ORDER BY total_members DESC, c.title;

-- CÂU 3
BEGIN;

INSERT INTO registrations (member_id, course_id)
SELECT m.id, c.id
FROM members m, courses c
WHERE m.full_name = 'Lê Chi' AND c.title = 'Cơ sở dữ liệu'
ON CONFLICT (member_id, course_id) DO NOTHING;

COMMIT;

SELECT m.full_name, c.title, r.status, r.registered_at
FROM registrations r
JOIN members m ON r.member_id = m.id
JOIN courses c ON r.course_id = c.id
WHERE m.full_name = 'Lê Chi';