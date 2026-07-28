-- ============================================================
-- Full Migration Script for Boosting Hub
-- Run this in SSMS against boostinghub_db (local) or boostinghubdb (Somee)
-- ============================================================

-- ── 1. Add voucher_no column to orders table ──
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.orders') AND name = 'voucher_no')
BEGIN
    ALTER TABLE orders ADD voucher_no NVARCHAR(50) NULL;
    PRINT 'Column voucher_no added to orders.';
END
ELSE
    PRINT 'Column voucher_no already exists.';
GO

-- ── 2. Create unique index on voucher_no ──
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_orders_voucher_no' AND object_id = OBJECT_ID(N'dbo.orders'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_orders_voucher_no
        ON orders (voucher_no)
        WHERE voucher_no IS NOT NULL;
    PRINT 'Index IX_orders_voucher_no created.';
END
ELSE
    PRINT 'Index IX_orders_voucher_no already exists.';
GO

-- ── 3. Reseed manual_payment_proofs identity to start at 0 ──
-- The next inserted row should get ID = 0 instead of 1.
-- DBCC CHECKIDENT will error if the table does not exist, so check first.
IF OBJECT_ID(N'dbo.manual_payment_proofs', N'U') IS NOT NULL
BEGIN
    DBCC CHECKIDENT ('manual_payment_proofs', RESEED, -1);
    PRINT 'manual_payment_proofs identity reseeded. Next insert will get ID = 0.';
END
ELSE
    PRINT 'Table manual_payment_proofs does not exist on this server. Skipping reseed.';
GO

-- ── 4. Verify ──
SELECT 'voucher_no column' AS check_item, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'voucher_no';

SELECT 'IX_orders_voucher_no index' AS check_item
FROM sys.indexes
WHERE name = 'IX_orders_voucher_no' AND object_id = OBJECT_ID(N'dbo.orders');

IF OBJECT_ID(N'dbo.manual_payment_proofs', N'U') IS NOT NULL
    SELECT 'manual_payment_proofs identity seed' AS check_item, IDENT_SEED('manual_payment_proofs') AS seed;
ELSE
    SELECT 'manual_payment_proofs' AS check_item, 'table does not exist' AS seed;
GO
