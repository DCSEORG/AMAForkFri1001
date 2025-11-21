-- Drop and recreate the managed identity user with correct SID
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'mid-AppModAssist-PLACEHOLDER')
BEGIN
    DROP USER [mid-AppModAssist-PLACEHOLDER];
END
GO

CREATE USER [mid-AppModAssist-PLACEHOLDER] FROM EXTERNAL PROVIDER;
GO

ALTER ROLE db_datareader ADD MEMBER [mid-AppModAssist-PLACEHOLDER];
GO

ALTER ROLE db_datawriter ADD MEMBER [mid-AppModAssist-PLACEHOLDER];
GO
