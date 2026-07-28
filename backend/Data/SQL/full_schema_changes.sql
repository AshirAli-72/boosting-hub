-- ============================================================
-- Full Schema Changes for Boosting Hub
-- Run this in SSMS against boostinghub_db (local) or boostinghubdb (Somee)
-- All statements are idempotent (safe to run multiple times)
-- ============================================================

-- ── 1. Create social_media_accounts table ──
IF OBJECT_ID(N'dbo.social_media_accounts', N'U') IS NULL
BEGIN
    CREATE TABLE social_media_accounts (
        id          INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        user_id     INT            NOT NULL,
        platform    NVARCHAR(100)  NOT NULL,
        username    NVARCHAR(200)  NOT NULL,
        profile_url NVARCHAR(500)  NULL,
        created_at  DATETIME2      NOT NULL,
        CONSTRAINT FK_social_media_accounts_users FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE INDEX IX_social_media_accounts_user_id ON social_media_accounts (user_id);
    CREATE UNIQUE INDEX IX_social_media_accounts_user_id_platform ON social_media_accounts (user_id, platform);

    PRINT 'Table social_media_accounts created.';
END
ELSE
    PRINT 'Table social_media_accounts already exists.';
GO

-- ── 2. Create packages table ──
IF OBJECT_ID(N'dbo.packages', N'U') IS NULL
BEGIN
    CREATE TABLE packages (
        id          INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        platform    NVARCHAR(200)  NOT NULL,
        service     NVARCHAR(200)  NOT NULL,
        price       DECIMAL(18,2)  NOT NULL,
        currency    NVARCHAR(10)   NOT NULL,
        is_active   BIT            NOT NULL DEFAULT 1,
        created_at  DATETIME2      NOT NULL,
        updated_at  DATETIME2      NULL
    );

    CREATE INDEX IX_packages_platform_service ON packages (platform, service);

    PRINT 'Table packages created.';
END
ELSE
    PRINT 'Table packages already exists.';
GO

-- ── 3. Create manual_payment_proofs table ──
IF OBJECT_ID(N'dbo.manual_payment_proofs', N'U') IS NULL
BEGIN
    CREATE TABLE manual_payment_proofs (
        id              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        order_id        INT            NOT NULL,
        paid_amount     DECIMAL(18,2)  NOT NULL,
        paid_voucher    NVARCHAR(500)  NULL,
        submit_date     DATETIME2      NOT NULL,
        payment_method  NVARCHAR(50)   NOT NULL,
        status          INT            NOT NULL DEFAULT 1,
        CONSTRAINT FK_manual_payment_proofs_orders FOREIGN KEY (order_id) REFERENCES orders(id)
    );

    CREATE INDEX IX_manual_payment_proofs_order_id ON manual_payment_proofs (order_id);
    CREATE INDEX IX_manual_payment_proofs_status ON manual_payment_proofs (status);

    PRINT 'Table manual_payment_proofs created.';
END
ELSE
    PRINT 'Table manual_payment_proofs already exists.';
GO

-- ── 4. Add voucher_no column to orders (if missing) ──
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.orders') AND name = 'voucher_no')
BEGIN
    ALTER TABLE orders ADD voucher_no NVARCHAR(50) NULL;
    PRINT 'Column voucher_no added to orders.';
END
ELSE
    PRINT 'Column voucher_no already exists.';
GO

-- ── 5. Create unique filtered index on orders.voucher_no (if missing) ──
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

-- ── 6. Add account_number and bank_name to accounts (if missing) ──
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.accounts') AND name = 'account_number')
BEGIN
    ALTER TABLE accounts ADD account_number NVARCHAR(100) NULL;
    PRINT 'Column account_number added to accounts.';
END
ELSE
    PRINT 'Column account_number already exists.';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.accounts') AND name = 'bank_name')
BEGIN
    ALTER TABLE accounts ADD bank_name NVARCHAR(100) NULL;
    PRINT 'Column bank_name added to accounts.';
END
ELSE
    PRINT 'Column bank_name already exists.';
GO

-- ── 7. Reseed manual_payment_proofs identity to start at 0 ──
IF OBJECT_ID(N'dbo.manual_payment_proofs', N'U') IS NOT NULL
BEGIN
    DBCC CHECKIDENT ('manual_payment_proofs', RESEED, -1);
    PRINT 'manual_payment_proofs identity reseeded. Next insert will get ID = 0.';
END
ELSE
    PRINT 'Table manual_payment_proofs does not exist. Skipping reseed.';
GO

-- ── 7. Verify ──
PRINT '';
PRINT '=== Verification ===';

IF OBJECT_ID(N'dbo.social_media_accounts', N'U') IS NOT NULL
    PRINT 'social_media_accounts: EXISTS';
ELSE
    PRINT 'social_media_accounts: MISSING';

IF OBJECT_ID(N'dbo.packages', N'U') IS NOT NULL
    PRINT 'packages: EXISTS';
ELSE
    PRINT 'packages: MISSING';

IF OBJECT_ID(N'dbo.manual_payment_proofs', N'U') IS NOT NULL
    PRINT 'manual_payment_proofs: EXISTS';
ELSE
    PRINT 'manual_payment_proofs: MISSING';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.orders') AND name = 'voucher_no')
    PRINT 'orders.voucher_no: EXISTS';
ELSE
    PRINT 'orders.voucher_no: MISSING';

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_orders_voucher_no' AND object_id = OBJECT_ID(N'dbo.orders'))
    PRINT 'IX_orders_voucher_no: EXISTS';
ELSE
    PRINT 'IX_orders_voucher_no: MISSING';

IF OBJECT_ID(N'dbo.manual_payment_proofs', N'U') IS NOT NULL
    SELECT 'manual_payment_proofs identity seed' AS check_item, IDENT_SEED('manual_payment_proofs') AS seed;
GO
