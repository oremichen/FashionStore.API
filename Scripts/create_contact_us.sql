CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "ContactUs"
(
    "Id" varchar(50) NOT NULL DEFAULT gen_random_uuid()::text,
    "Address" varchar(500) NOT NULL,
    "ContactPhone" varchar(50) NOT NULL,
    "BusinessPhone" varchar(50) NULL,
    "ContactEmail" varchar(254) NOT NULL,
    "BusinessEmail" varchar(254) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsActive" boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT "PK_ContactUs" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "ContactUsOneActiveUnique"
    ON "ContactUs" ("IsActive")
    WHERE "IsActive" = TRUE;
