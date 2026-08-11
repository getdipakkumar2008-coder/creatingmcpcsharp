-- Server-level audit: writes audit records to C:\SQLAudit
USE master;
GO
IF EXISTS (SELECT 1 FROM sys.server_audits WHERE name = 'Employee_Server_Audit')
BEGIN
    ALTER SERVER AUDIT Employee_Server_Audit WITH (STATE = OFF);
    DROP SERVER AUDIT Employee_Server_Audit;
END
GO
CREATE SERVER AUDIT Employee_Server_Audit
    TO FILE (FILEPATH = 'C:\SQLAudit\', MAXSIZE = 50 MB, MAX_ROLLOVER_FILES = 10)
    WITH (ON_FAILURE = CONTINUE);
GO
ALTER SERVER AUDIT Employee_Server_Audit WITH (STATE = ON);
GO

-- Database-level specification: audit DELETE on the table, plus schema/object
-- changes (ALTER/DROP) and permission changes.
USE testdb;
GO
IF EXISTS (SELECT 1 FROM sys.database_audit_specifications WHERE name = 'EmployeeData_Audit_Spec')
BEGIN
    ALTER DATABASE AUDIT SPECIFICATION EmployeeData_Audit_Spec WITH (STATE = OFF);
    DROP DATABASE AUDIT SPECIFICATION EmployeeData_Audit_Spec;
END
GO
CREATE DATABASE AUDIT SPECIFICATION EmployeeData_Audit_Spec
    FOR SERVER AUDIT Employee_Server_Audit
    ADD (DELETE ON OBJECT::dbo.Employeedata BY public),
    ADD (SCHEMA_OBJECT_CHANGE_GROUP),
    ADD (SCHEMA_OBJECT_PERMISSION_CHANGE_GROUP),
    ADD (DATABASE_OBJECT_PERMISSION_CHANGE_GROUP),
    ADD (DATABASE_PERMISSION_CHANGE_GROUP)
    WITH (STATE = ON);
GO
