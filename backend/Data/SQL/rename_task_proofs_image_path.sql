-- Rename proof_type to image_path in task_proofs table
-- Also change type from nvarchar(50) to nvarchar(500) to accommodate file paths

EXEC sp_rename 'task_proofs.proof_type', 'image_path', 'COLUMN';

ALTER TABLE [task_proofs]
ALTER COLUMN [image_path] nvarchar(500) NOT NULL;
