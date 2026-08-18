CREATE TABLE IF NOT EXISTS data."OpenMuWeb_News" (
    "id"           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "title"        TEXT NOT NULL,
    "body"         TEXT NOT NULL,
    "author"       TEXT NOT NULL DEFAULT 'Admin',
    "creationDate" TIMESTAMP NOT NULL DEFAULT now()
);

-- Insert welcome news if empty
INSERT INTO data."OpenMuWeb_News" ("title", "body", "author")
SELECT 'Chào mừng đến với MU Online S6E3!', 'Máy chủ MU Online Season 6 Episode 3 chính thức khai mở. Chúc các chiến binh có những giờ phút chơi game vui vẻ!', 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM data."OpenMuWeb_News");
