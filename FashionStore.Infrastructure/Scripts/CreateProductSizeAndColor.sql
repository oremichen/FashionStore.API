CREATE TABLE IF NOT EXISTS "ProductSize" (
    "ProductId" varchar(50) NOT NULL,
    "SizeId" varchar(50) NOT NULL,
    CONSTRAINT "PK_ProductSize" PRIMARY KEY ("ProductId", "SizeId"),
    CONSTRAINT "FK_ProductSize_Products_ProductId" FOREIGN KEY ("ProductId")
        REFERENCES "Products" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProductSize_Sizes_SizeId" FOREIGN KEY ("SizeId")
        REFERENCES "Sizes" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ProductSize_SizeId" ON "ProductSize" ("SizeId");

CREATE TABLE IF NOT EXISTS "ProductColor" (
    "ProductId" varchar(50) NOT NULL,
    "ColorId" varchar(50) NOT NULL,
    CONSTRAINT "PK_ProductColor" PRIMARY KEY ("ProductId", "ColorId"),
    CONSTRAINT "FK_ProductColor_Products_ProductId" FOREIGN KEY ("ProductId")
        REFERENCES "Products" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProductColor_Colors_ColorId" FOREIGN KEY ("ColorId")
        REFERENCES "Colors" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ProductColor_ColorId" ON "ProductColor" ("ColorId");
