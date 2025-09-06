USE SecilStoreCodeCase;
GO

-- Basit seed (SERVICE-A ve SERVICE-B)
INSERT INTO dbo.Configurations (ApplicationName, Name, [Type], [Value], IsActive, UpdatedAt)
VALUES
('SERVICE-A','SiteName','string','soty.io',1,SYSUTCDATETIME()),
('SERVICE-A','MaxItemCount','int','50',1,SYSUTCDATETIME()),
('SERVICE-A','IsFeatureXOpen','bool','true',1,SYSUTCDATETIME()),

('SERVICE-B','SiteName','string','soty-b.io',1,SYSUTCDATETIME()),
('SERVICE-B','MaxItemCount','int','99',1,SYSUTCDATETIME()),
('SERVICE-B','IsFeatureXOpen','bool','0',1,SYSUTCDATETIME());
GO
