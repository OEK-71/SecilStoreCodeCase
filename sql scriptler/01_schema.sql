
---

# sql/01_schema.sql

```sql
-- 1) Database (yoksa)
-- CREATE DATABASE SecilStoreCodeCase;
-- GO
USE SecilStoreCodeCase;
GO

-- 2) Configurations tablosu
IF OBJECT_ID('dbo.Configurations','U') IS NULL
BEGIN
  CREATE TABLE dbo.Configurations
  (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    ApplicationName  NVARCHAR(200) NOT NULL,
    Name             NVARCHAR(200) NOT NULL,
    [Type]           NVARCHAR(20)  NOT NULL, -- 'string' | 'int' | 'bool' | 'double'
    [Value]          NVARCHAR(MAX) NOT NULL,
    IsActive         BIT           NOT NULL CONSTRAINT DF_Configurations_IsActive DEFAULT(1),
    UpdatedAt        DATETIME2(3)  NOT NULL CONSTRAINT DF_Configurations_UpdatedAt DEFAULT (SYSUTCDATETIME())
  );
END
GO

-- (İsteğe bağlı) Normalize edilmiş computed kolonlar (filtered index için tavsiye)
IF COL_LENGTH('dbo.Configurations','ApplicationNameNorm') IS NULL
ALTER TABLE dbo.Configurations ADD ApplicationNameNorm AS UPPER(LTRIM(RTRIM(ApplicationName))) PERSISTED;
IF COL_LENGTH('dbo.Configurations','NameNorm') IS NULL
ALTER TABLE dbo.Configurations ADD NameNorm AS UPPER(LTRIM(RTRIM(Name))) PERSISTED;
GO
