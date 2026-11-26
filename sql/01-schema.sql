-- sql/01-schema.sql
-- Tạo cấu trúc cơ sở dữ liệu cho hệ thống EduHub mini

CREATE TABLE members (
    id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email     VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE courses (
    id       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title    VARCHAR(150) NOT NULL,
    capacity INT NOT NULL CONSTRAINT chk_courses_capacity CHECK (capacity > 0)
);

CREATE TABLE registrations (
    id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    member_id INT NOT NULL REFERENCES members(id) ON DELETE CASCADE,
    course_id INT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    status    VARCHAR(20) NOT NULL DEFAULT 'pending'
              CONSTRAINT chk_registrations_status CHECK (status IN ('pending', 'confirmed', 'cancelled')),
    CONSTRAINT uq_registrations_member_course UNIQUE (member_id, course_id)
);
