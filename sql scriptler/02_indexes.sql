USE SecilStoreCodeCase;
GO

-- Lookup/Delta sorguları için yardımcı indexler
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Config_App_Active' AND object_id=OBJECT_ID('dbo.Configurations'))
  CREATE INDEX IX_Config_App_Active ON dbo.Configurations (ApplicationNameNorm, NameNorm) INCLUDE (IsActive, UpdatedAt)
  WHERE IsActive = 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Config_App_UpdatedAt' AND object_id=OBJECT_ID('dbo.Configurations'))
  CREATE INDEX IX_Config_App_UpdatedAt ON dbo.Configurations (ApplicationNameNorm, UpdatedAt) INCLUDE (NameNorm, IsActive);
GO

-- Tek aktif kayıt kuralı (benzersiz)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Config_Active' AND object_id=OBJECT_ID('dbo.Configurations'))
  CREATE UNIQUE INDEX UX_Config_Active ON dbo.Configurations (ApplicationNameNorm, NameNorm)
  WHERE IsActive = 1;
GO
