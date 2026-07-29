-- Run this in SSMS against your database
-- Renames payment_proofs_table back to manual_payment_proofs

IF OBJECT_ID('dbo.payment_proofs_table', 'U') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.payment_proofs_table', 'manual_payment_proofs';
    PRINT 'Table renamed: payment_proofs_table -> manual_payment_proofs';
END
ELSE
    PRINT 'Table payment_proofs_table does not exist.';
