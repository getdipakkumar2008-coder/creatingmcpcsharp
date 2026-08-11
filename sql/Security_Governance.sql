USE testdb;
GO

/* ------------------------------------------------------------------ */
/* 1. System-versioning (temporal) for full history + recoverability. */
/*    Also blocks TRUNCATE while versioning is ON.                    */
/* ------------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employeedata' AND temporal_type = 2)
BEGIN
    ALTER TABLE dbo.Employeedata ADD
        SysStart DATETIME2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
            CONSTRAINT DF_Employeedata_SysStart DEFAULT SYSUTCDATETIME(),
        SysEnd   DATETIME2(7) GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL
            CONSTRAINT DF_Employeedata_SysEnd   DEFAULT CONVERT(DATETIME2(7), '9999-12-31 23:59:59.9999999'),
        PERIOD FOR SYSTEM_TIME (SysStart, SysEnd);

    ALTER TABLE dbo.Employeedata
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Employeedata_History));
END
GO

/* ------------------------------------------------------------------ */
/* 2. Least-privilege logins.                                         */
/* ------------------------------------------------------------------ */

-- Read-only login used by the MCP application.
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'mcp_app')
    CREATE LOGIN mcp_app WITH PASSWORD = 'McpApp#2026!Rd', CHECK_POLICY = ON;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_app')
    CREATE USER mcp_app FOR LOGIN mcp_app;
ALTER ROLE db_datareader ADD MEMBER mcp_app;
-- Note: do NOT deny CONTROL here - CONTROL is a superset and would also block SELECT.
DENY INSERT, UPDATE, DELETE, ALTER ON dbo.Employeedata TO mcp_app;
GO

-- Writer login: can add/update employees but NOT delete or truncate/alter.
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'emp_writer')
    CREATE LOGIN emp_writer WITH PASSWORD = 'EmpWriter#2026!Wr', CHECK_POLICY = ON;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'emp_writer')
    CREATE USER emp_writer FOR LOGIN emp_writer;
GRANT SELECT, INSERT, UPDATE ON dbo.Employeedata TO emp_writer;
-- Targeted denies only; CONTROL is intentionally omitted so SELECT/UPDATE still work.
DENY DELETE, ALTER ON dbo.Employeedata TO emp_writer;
GO
