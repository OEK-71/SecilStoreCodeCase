USE SecilStoreCodeCase;
GO

-- Şema ve predicate fonksiyonu
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name='sec') EXEC('CREATE SCHEMA sec;');
GO

IF OBJECT_ID('sec.AppPredicate','IF') IS NOT NULL DROP FUNCTION sec.AppPredicate;
GO
CREATE FUNCTION sec.AppPredicate(@ApplicationName NVARCHAR(200))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
  SELECT 1 AS Allowed
  WHERE UPPER(LTRIM(RTRIM(@ApplicationName))) = CAST(SESSION_CONTEXT(N'AppName') AS NVARCHAR(200));
GO

-- Security Policy
IF OBJECT_ID('sec.ConfigRlsPolicy','SP') IS NOT NULL
  DROP SECURITY POLICY sec.ConfigRlsPolicy;
GO
CREATE SECURITY POLICY sec.ConfigRlsPolicy
ADD FILTER PREDICATE sec.AppPredicate(ApplicationName) ON dbo.Configurations,
ADD BLOCK  PREDICATE sec.AppPredicate(ApplicationName) ON dbo.Configurations
WITH (STATE = ON);
GO

-- Test:
-- EXEC sys.sp_set_session_context @key=N'AppName', @value=N'SERVICE-A';
-- SELECT * FROM dbo.Configurations;  -- sadece A görünür
