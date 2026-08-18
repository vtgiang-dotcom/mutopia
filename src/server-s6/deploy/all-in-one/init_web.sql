CREATE TABLE IF NOT EXISTS data."OpenMuWeb_News" (
    id uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    title text NOT NULL,
    body text NOT NULL,
    author text NOT NULL,
    "creationDate" timestamp(3) NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS data.openmuweb_news (
    id uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    title text NOT NULL,
    body text NOT NULL,
    author text NOT NULL,
    "creationDate" timestamp(3) NOT NULL DEFAULT CURRENT_TIMESTAMP
);
