-- Stored procedures for all employee data access used by the MCP tools.
-- Rationale (defense-in-depth + least privilege):
--   * The application login is granted EXECUTE on these procedures only.
--   * Direct INSERT/UPDATE/DELETE/SELECT on dbo.Employeedata is revoked for that login,
--     so a compromised app credential cannot run arbitrary DML against the table.
--   * All inputs are strongly typed parameters (no string concatenation anywhere).
USE testdb;
GO

-- Single-employee search. Id (primary key) wins when non-zero; otherwise every provided
-- field must match (AND). At least one field must be supplied by the caller.
CREATE OR ALTER PROCEDURE dbo.usp_SearchEmployee
    @Id         INT           = 0,
    @Name       NVARCHAR(100) = NULL,
    @Department NVARCHAR(100) = NULL,
    @Email      NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 Id, Name, Department, Email, Address, JoiningDate
    FROM dbo.Employeedata
    WHERE (@Id <> 0 AND Id = @Id)
       OR (
            @Id = 0
            AND (@Name IS NULL OR Name = @Name)
            AND (@Department IS NULL OR Department = @Department)
            AND (@Email IS NULL OR Email = @Email)
            AND (@Name IS NOT NULL OR @Department IS NOT NULL OR @Email IS NOT NULL)
          );
END
GO

-- Full list, ordered by name.
CREATE OR ALTER PROCEDURE dbo.usp_GetAllEmployees
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Department, Email, Address, JoiningDate
    FROM dbo.Employeedata
    ORDER BY Name;
END
GO

-- Partial update: only non-NULL parameters change; omitted fields keep their value
-- via COALESCE. Returns the updated row (empty result set if the Id does not exist).
CREATE OR ALTER PROCEDURE dbo.usp_UpdateEmployee
    @Id         INT,
    @Name       NVARCHAR(100) = NULL,
    @Department NVARCHAR(100) = NULL,
    @Email      NVARCHAR(200) = NULL,
    @Address    NVARCHAR(200) = NULL,
    @JoiningDate DATE         = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Employeedata
    SET Name        = COALESCE(@Name, Name),
        Department  = COALESCE(@Department, Department),
        Email       = COALESCE(@Email, Email),
        Address     = COALESCE(@Address, Address),
        JoiningDate = COALESCE(@JoiningDate, JoiningDate)
    OUTPUT inserted.Id, inserted.Name, inserted.Department,
           inserted.Email, inserted.Address, inserted.JoiningDate
    WHERE Id = @Id;
END
GO

-- Delete by primary key. Returns the number of rows deleted (0 or 1).
CREATE OR ALTER PROCEDURE dbo.usp_DeleteEmployee
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Employeedata
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ---------------------------------------------------------------------------
-- Least-privilege grants. Replace [mcp_app] with your actual application login/user.
-- The app should NOT hold table-level DML rights once these procedures exist.
-- ---------------------------------------------------------------------------
-- GRANT EXECUTE ON dbo.usp_SearchEmployee   TO [mcp_app];
-- GRANT EXECUTE ON dbo.usp_GetAllEmployees  TO [mcp_app];
-- GRANT EXECUTE ON dbo.usp_UpdateEmployee   TO [mcp_app];
-- GRANT EXECUTE ON dbo.usp_DeleteEmployee   TO [mcp_app];
-- REVOKE SELECT, INSERT, UPDATE, DELETE ON dbo.Employeedata FROM [mcp_app];
-- GO
