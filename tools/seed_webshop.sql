-- Seed WebShop and Marketplace tables for the OpenMU PlayerWeb economy.
-- Safe to re-run: shop items use deterministic UUIDs (md5 of the item id) + ON CONFLICT DO NOTHING.

-- 1. Marketplace listing table (player-to-player trades)
CREATE TABLE IF NOT EXISTS data."OpenMuWeb_MarketplaceItem" (
    "Id" uuid PRIMARY KEY,
    "ItemId" uuid NOT NULL,
    "SellerAccountId" uuid NOT NULL,
    "PriceWCoin" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "Status" integer NOT NULL DEFAULT 0
);

-- 2. Official WebShop catalogue
CREATE TABLE IF NOT EXISTS config."OpenMuWeb_ShopItem" (
    "Id" uuid PRIMARY KEY,
    "ItemGroup" smallint NOT NULL,
    "ItemNumber" smallint NOT NULL,
    "Level" smallint NOT NULL DEFAULT 0,
    "OptionName" text NOT NULL DEFAULT '',
    "PriceWCoin" integer NOT NULL,
    "Stock" integer NOT NULL DEFAULT 0
);

-- 3. Seed a starter WebShop catalogue using real ItemDefinition rows.
--    Deterministic id = md5(item definition id), so re-running never duplicates.
--    Prices are illustrative WCoin amounts; adjust to the server economy.
INSERT INTO config."OpenMuWeb_ShopItem" ("Id", "ItemGroup", "ItemNumber", "Level", "OptionName", "PriceWCoin", "Stock")
SELECT
    md5(d."Id"::text)::uuid,
    d."Group",
    d."Number",
    0,
    '',
    CASE
        WHEN d."Group" = 12 THEN 150   -- wings / jewels / orbs
        ELSE 120                        -- weapons / armor / shields
    END,
    100
FROM config."ItemDefinition" d
WHERE d."Name" IS NOT NULL
  AND d."Group" IN (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
ON CONFLICT DO NOTHING;
