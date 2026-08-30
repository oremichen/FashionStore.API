BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "Orders"
(
    "Id"               varchar(50)  NOT NULL DEFAULT gen_random_uuid()::text,
    "UserId"           varchar(450) NOT NULL,
    "AddressId"        varchar(50)  NOT NULL,
    "Email"            varchar(320) NOT NULL,
    "DeliveryMethod"   varchar(30)  NOT NULL,
    "Subtotal"         numeric(18,2) NOT NULL,
    "DeliveryFee"      numeric(18,2) NOT NULL,
    "Total"            numeric(18,2) NOT NULL,
    "Currency"         varchar(3)   NOT NULL DEFAULT 'NGN',
    "Status"           varchar(30)  NOT NULL DEFAULT 'PendingPayment',
    "PaymentReference" varchar(100) NOT NULL,
    "PaymentStatus"    varchar(30)  NOT NULL DEFAULT 'pending',
    "CreatedAt"        timestamptz  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PaidAt"           timestamptz  NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Orders_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_Orders_Amounts"
        CHECK ("Subtotal" >= 0 AND "DeliveryFee" >= 0 AND "Total" >= 0)
);

CREATE TABLE IF NOT EXISTS "OrderItems"
(
    "Id"          varchar(50)   NOT NULL DEFAULT gen_random_uuid()::text,
    "OrderId"     varchar(50)   NOT NULL,
    "ProductId"   varchar(50)   NOT NULL,
    "VariantId"   varchar(50)   NULL,
    "ProductName" varchar(250)  NOT NULL,
    "UnitPrice"   numeric(18,2) NOT NULL,
    "Quantity"    integer       NOT NULL,
    "LineTotal"   numeric(18,2) NOT NULL,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderItems_Orders_OrderId"
        FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_OrderItems_UnitPrice" CHECK ("UnitPrice" >= 0),
    CONSTRAINT "CK_OrderItems_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_OrderItems_LineTotal" CHECK ("LineTotal" >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_PaymentReference"
    ON "Orders" ("PaymentReference");

CREATE INDEX IF NOT EXISTS "IX_Orders_UserId"
    ON "Orders" ("UserId");

CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId"
    ON "OrderItems" ("OrderId");

COMMIT;
