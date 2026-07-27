USE [boostinghub_db];
GO

-- ============================================================
-- Wallet decimal precision fix: decimal(18,2) -> decimal(18,6)
-- Prevents currency conversion rounding loss (60 PKR -> 58 PKR bug)
-- ============================================================
ALTER TABLE [dbo].[wallets] ALTER COLUMN [total_balance] DECIMAL(18,6) NOT NULL;
ALTER TABLE [dbo].[wallets] ALTER COLUMN [withdrawn] DECIMAL(18,6) NOT NULL;

-- Fix existing wallets with empty currency
UPDATE [dbo].[wallets] SET [currency] = 'USD' WHERE [currency] = '' OR [currency] IS NULL;
GO

-- ============================================================
-- STEP 1: DROP ALL FOREIGN KEY CONSTRAINTS
-- ============================================================

DECLARE @sql NVARCHAR(MAX) = '';

SELECT @sql = @sql + 'ALTER TABLE [' + OBJECT_SCHEMA_NAME(parent_object_id) + '].[' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];' + CHAR(13)
FROM sys.foreign_keys;

EXEC sp_executesql @sql;
GO

-- ============================================================
-- STEP 2: DROP ALL TABLES
-- ============================================================

DECLARE @drop NVARCHAR(MAX) = '';

SELECT @drop = @drop + 'DROP TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '];' + CHAR(13)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME NOT IN ('__EFMigrationsHistory')
  AND TABLE_SCHEMA = 'dbo';

EXEC sp_executesql @drop;
GO

-- ============================================================
-- STEP 3: CREATE ALL 17 TABLES (exact match to C# models)
-- ============================================================

-- ── users ───────────────────────────────────────────────────
CREATE TABLE [users] (
    [id]                 int             NOT NULL IDENTITY,
    [name]               nvarchar(200)   NULL,
    [email]              nvarchar(255)   NULL,
    [phone]              nvarchar(50)    NULL,
    [password]           nvarchar(500)   NULL,
    [status]             int             NOT NULL DEFAULT 1,
    [email_verified_at]  datetime2       NULL,
    [remember_token]     nvarchar(500)   NULL,
    [email_change_token] nvarchar(500)   NULL,
    [created_at]         datetime2       NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([id])
);
GO
CREATE UNIQUE INDEX [IX_users_email] ON [users] ([email]) WHERE [email] IS NOT NULL;
CREATE UNIQUE INDEX [IX_users_phone] ON [users] ([phone]) WHERE [phone] IS NOT NULL;
GO

-- ── roles ───────────────────────────────────────────────────
CREATE TABLE [roles] (
    [id]            int             NOT NULL IDENTITY,
    [role_title]    nvarchar(100)   NOT NULL,
    [description]   nvarchar(max)   NULL,
    [created_at]    datetime2       NOT NULL,
    CONSTRAINT [PK_roles] PRIMARY KEY ([id])
);
GO
CREATE UNIQUE INDEX [IX_roles_role_title] ON [roles] ([role_title]);
GO

-- ── permissions ─────────────────────────────────────────────
CREATE TABLE [permissions] (
    [id]            int             NOT NULL IDENTITY,
    [names]         nvarchar(255)   NULL,
    [slugs]         nvarchar(255)   NULL,
    [created_at]    datetime2       NOT NULL,
    CONSTRAINT [PK_permissions] PRIMARY KEY ([id])
);
GO
CREATE UNIQUE INDEX [IX_permissions_slugs] ON [permissions] ([slugs]) WHERE [slugs] IS NOT NULL;
GO

-- ── roles_has_permissions ───────────────────────────────────
CREATE TABLE [roles_has_permissions] (
    [id]            int NOT NULL IDENTITY,
    [role_id]       int NOT NULL,
    [permission_id] int NOT NULL,
    CONSTRAINT [PK_roles_has_permissions] PRIMARY KEY ([id]),
    CONSTRAINT [FK_roles_has_permissions_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_roles_has_permissions_permissions_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [permissions] ([id]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_roles_has_permissions_permission_id] ON [roles_has_permissions] ([permission_id]);
CREATE UNIQUE INDEX [IX_roles_has_permissions_role_id_permission_id] ON [roles_has_permissions] ([role_id], [permission_id]);
GO

-- ── user_has_roles ──────────────────────────────────────────
CREATE TABLE [user_has_roles] (
    [id]        int NOT NULL IDENTITY,
    [user_id]   int NOT NULL,
    [role_id]   int NOT NULL,
    CONSTRAINT [PK_user_has_roles] PRIMARY KEY ([id]),
    CONSTRAINT [FK_user_has_roles_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_user_has_roles_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([id]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_user_has_roles_role_id] ON [user_has_roles] ([role_id]);
CREATE UNIQUE INDEX [IX_user_has_roles_user_id_role_id] ON [user_has_roles] ([user_id], [role_id]);
GO

-- ── accounts ────────────────────────────────────────────────
CREATE TABLE [accounts] (
    [id]            int             NOT NULL IDENTITY,
    [user_id]       int             NOT NULL,
    [account_title] nvarchar(200)   NOT NULL,
    [mobile_number] nvarchar(50)    NOT NULL,
    [cnic]          nvarchar(50)    NOT NULL,
    [is_default]    bit             NOT NULL,
    [status]        nvarchar(20)    NOT NULL DEFAULT N'1',
    [created_at]    datetime2       NOT NULL,
    [updated_at]    datetime2       NOT NULL,
    CONSTRAINT [PK_accounts] PRIMARY KEY ([id]),
    CONSTRAINT [FK_accounts_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
);
GO
CREATE INDEX [IX_accounts_user_id] ON [accounts] ([user_id]);
GO

-- ── orders ──────────────────────────────────────────────────
CREATE TABLE [orders] (
    [id]                int             NOT NULL IDENTITY,
    [full_name]         nvarchar(max)   NULL,
    [email]             nvarchar(max)   NULL,
    [platform]          nvarchar(500)   NOT NULL,
    [service]           nvarchar(500)   NOT NULL,
    [social_media_url]  nvarchar(500)   NULL,
    [description]       nvarchar(1000)  NULL,
    [quantity]          nvarchar(max)   NULL,
    [total_amount]      decimal(18,2)   NOT NULL DEFAULT 0,
    [currency]          nvarchar(10)    NOT NULL DEFAULT N'',
    [status]            int             NOT NULL DEFAULT 2,
    [created_at]        datetime2       NOT NULL,
    [attachment]        nvarchar(500)   NULL,
    [reject_reason]     nvarchar(1000)  NULL,
    CONSTRAINT [PK_orders] PRIMARY KEY ([id])
);
GO
CREATE INDEX [IX_orders_status] ON [orders] ([status]);
GO

-- ── task_generate ───────────────────────────────────────────
CREATE TABLE [task_generate] (
    [id]            int             NOT NULL IDENTITY,
    [order_id]      int             NOT NULL,
    [platform]      nvarchar(200)   NOT NULL,
    [service]       nvarchar(200)   NOT NULL,
    [quantity]      int             NOT NULL,
    [url]           nvarchar(500)   NOT NULL,
    [reward]        decimal(18,2)   NOT NULL,
    [created_at]    datetime2       NOT NULL,
    [expiry_date]   datetime2       NULL,
    [status]        nvarchar(50)    NOT NULL DEFAULT N'1',
    CONSTRAINT [PK_task_generate] PRIMARY KEY ([id]),
    CONSTRAINT [FK_task_generate_orders_order_id] FOREIGN KEY ([order_id]) REFERENCES [orders] ([id]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_task_generate_order_id] ON [task_generate] ([order_id]);
CREATE INDEX [IX_task_generate_status] ON [task_generate] ([status]);
GO

-- ── notifications ───────────────────────────────────────────
CREATE TABLE [notifications] (
    [id]            int             NOT NULL IDENTITY,
    [user_id]       int             NOT NULL,
    [type]          nvarchar(100)   NOT NULL,
    [title]         nvarchar(200)   NOT NULL,
    [message]       nvarchar(max)   NOT NULL,
    [data]          nvarchar(max)   NULL,
    [is_read]       bit             NOT NULL,
    [read_at]       datetime2       NULL,
    [created_at]    datetime2       NOT NULL,
    CONSTRAINT [PK_notifications] PRIMARY KEY ([id]),
    CONSTRAINT [FK_notifications_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
);
GO
CREATE INDEX [IX_notifications_user_id] ON [notifications] ([user_id]);
CREATE INDEX [IX_notifications_user_id_is_read] ON [notifications] ([user_id], [is_read]);
GO

-- ── wallets ─────────────────────────────────────────────────
CREATE TABLE [wallets] (
    [id]            int             NOT NULL IDENTITY,
    [user_id]       int             NOT NULL,
    [total_balance] decimal(18,2)   NOT NULL,
    [currency]      nvarchar(10)    NOT NULL,
    [withdrawn]     decimal(18,2)   NOT NULL,
    [created_at]    datetime2       NOT NULL,
    [status]        int             NOT NULL DEFAULT 1,
    CONSTRAINT [PK_wallets] PRIMARY KEY ([id]),
    CONSTRAINT [FK_wallets_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
GO
CREATE UNIQUE INDEX [IX_wallets_user_id] ON [wallets] ([user_id]);
GO

-- ── transactions ────────────────────────────────────────────
CREATE TABLE [transactions] (
    [id]             int             NOT NULL IDENTITY,
    [wallet_id]      int             NOT NULL,
    [user_id]        int             NOT NULL,
    [type]           nvarchar(50)    NOT NULL,
    [amount]         decimal(18,2)   NOT NULL,
    [balance_after]  decimal(18,2)   NOT NULL,
    [description]    nvarchar(500)   NOT NULL,
    [reference_type] nvarchar(100)   NULL,
    [reference_id]   int             NULL,
    [status]         nvarchar(50)    NOT NULL DEFAULT N'1',
    [created_at]     datetime2       NOT NULL,
    CONSTRAINT [PK_transactions] PRIMARY KEY ([id]),
    CONSTRAINT [FK_transactions_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id]),
    CONSTRAINT [FK_transactions_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
);
GO
CREATE INDEX [IX_transactions_wallet_id] ON [transactions] ([wallet_id]);
CREATE INDEX [IX_transactions_user_id] ON [transactions] ([user_id]);
CREATE INDEX [IX_transactions_created_at] ON [transactions] ([created_at]);
GO

-- ── task_proofs ─────────────────────────────────────────────
CREATE TABLE [task_proofs] (
    [id]                    int             NOT NULL IDENTITY,
    [user_id]               int             NOT NULL,
    [task_id]               int             NOT NULL,
    [proof_url]             nvarchar(2048)  NOT NULL,
    [proof_type]            nvarchar(50)    NOT NULL,
    [date]                  datetime2       NOT NULL,
    [status]                nvarchar(50)    NOT NULL DEFAULT N'2',
    [verification_status]   nvarchar(50)    NOT NULL DEFAULT N'4',
    [reject_reason]         nvarchar(1000)  NULL,
    CONSTRAINT [PK_task_proofs] PRIMARY KEY ([id]),
    CONSTRAINT [FK_task_proofs_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]),
    CONSTRAINT [FK_task_proofs_task_generate_task_id] FOREIGN KEY ([task_id]) REFERENCES [task_generate] ([id])
);
GO
CREATE INDEX [IX_task_proofs_user_id_task_id] ON [task_proofs] ([user_id], [task_id]);
CREATE INDEX [IX_task_proofs_task_id] ON [task_proofs] ([task_id]);
CREATE INDEX [IX_task_proofs_verification_status] ON [task_proofs] ([verification_status]);
GO

-- ── task_complete ───────────────────────────────────────────
CREATE TABLE [task_complete] (
    [id]        int             NOT NULL IDENTITY,
    [user_id]   int             NOT NULL,
    [task_id]   int             NOT NULL,
    [proof_id]  int             NULL,
    [date]      datetime2       NOT NULL,
    [status]    nvarchar(50)    NOT NULL DEFAULT N'2',
    CONSTRAINT [PK_task_complete] PRIMARY KEY ([id]),
    CONSTRAINT [FK_task_complete_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]),
    CONSTRAINT [FK_task_complete_task_generate_task_id] FOREIGN KEY ([task_id]) REFERENCES [task_generate] ([id]),
    CONSTRAINT [FK_task_complete_task_proofs_proof_id] FOREIGN KEY ([proof_id]) REFERENCES [task_proofs] ([id])
);
GO
CREATE INDEX [IX_task_complete_user_id] ON [task_complete] ([user_id]);
CREATE INDEX [IX_task_complete_task_id] ON [task_complete] ([task_id]);
GO

-- ── accepted_tasks ──────────────────────────────────────────
CREATE TABLE [accepted_tasks] (
    [id]            int             NOT NULL IDENTITY,
    [user_id]       int             NOT NULL,
    [task_id]       int             NOT NULL,
    [accepted_at]   datetime2       NOT NULL,
    [status]        nvarchar(50)    NOT NULL DEFAULT N'1',
    CONSTRAINT [PK_accepted_tasks] PRIMARY KEY ([id]),
    CONSTRAINT [FK_accepted_tasks_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]),
    CONSTRAINT [FK_accepted_tasks_task_generate_task_id] FOREIGN KEY ([task_id]) REFERENCES [task_generate] ([id])
);
GO
CREATE INDEX [IX_accepted_tasks_user_id] ON [accepted_tasks] ([user_id]);
CREATE INDEX [IX_accepted_tasks_task_id] ON [accepted_tasks] ([task_id]);
CREATE UNIQUE INDEX [IX_accepted_tasks_user_id_task_id] ON [accepted_tasks] ([user_id], [task_id]);
GO

-- ── activity_logs ───────────────────────────────────────────
CREATE TABLE [activity_logs] (
    [id]            int             NOT NULL IDENTITY,
    [user_id]       int             NULL,
    [user_name]     nvarchar(200)   NULL,
    [user_email]    nvarchar(255)   NULL,
    [user_role]     nvarchar(20)    NOT NULL,
    [event]         nvarchar(50)    NOT NULL,
    [description]   nvarchar(500)   NULL,
    [subject_type]  nvarchar(50)    NOT NULL,
    [subject_id]    int             NULL,
    [subject_name]  nvarchar(200)   NULL,
    [old_values]    nvarchar(max)   NULL,
    [new_values]    nvarchar(max)   NULL,
    [ip_address]    nvarchar(50)    NULL,
    [created_at]    datetime2       NOT NULL,
    CONSTRAINT [PK_activity_logs] PRIMARY KEY ([id]),
    CONSTRAINT [FK_activity_logs_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
);
GO
CREATE INDEX [IX_activity_logs_user_id] ON [activity_logs] ([user_id]);
CREATE INDEX [IX_activity_logs_subject_type_subject_id] ON [activity_logs] ([subject_type], [subject_id]);
CREATE INDEX [IX_activity_logs_created_at] ON [activity_logs] ([created_at]);
CREATE INDEX [IX_activity_logs_event] ON [activity_logs] ([event]);
GO

-- ── social_media_accounts ───────────────────────────────────
CREATE TABLE [social_media_accounts] (
    [id]            int             NOT NULL IDENTITY,
    [user_id]       int             NOT NULL,
    [platform]      nvarchar(100)   NOT NULL,
    [username]      nvarchar(200)   NOT NULL,
    [profile_url]   nvarchar(500)   NULL,
    [created_at]    datetime2       NOT NULL,
    CONSTRAINT [PK_social_media_accounts] PRIMARY KEY ([id]),
    CONSTRAINT [FK_social_media_accounts_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_social_media_accounts_user_id] ON [social_media_accounts] ([user_id]);
CREATE UNIQUE INDEX [IX_social_media_accounts_user_id_platform] ON [social_media_accounts] ([user_id], [platform]);
GO

-- ── packages ────────────────────────────────────────────────
CREATE TABLE [packages] (
    [id]            int             NOT NULL IDENTITY,
    [platform]      nvarchar(200)   NOT NULL,
    [service]       nvarchar(200)   NOT NULL,
    [price]         decimal(18,2)   NOT NULL,
    [currency]      nvarchar(10)    NOT NULL,
    [is_active]     bit             NOT NULL DEFAULT 1,
    [created_at]    datetime2       NOT NULL,
    [updated_at]    datetime2       NULL,
    CONSTRAINT [PK_packages] PRIMARY KEY ([id])
);
GO
CREATE INDEX [IX_packages_platform_service] ON [packages] ([platform], [service]);
GO

-- ============================================================
-- DONE - All 17 tables created, matching C# models exactly
-- ============================================================
