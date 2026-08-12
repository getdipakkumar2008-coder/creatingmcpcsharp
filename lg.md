# Chat Log — MyMCP Employee Data Work

> Note: SQL Server usernames and passwords have been redacted (`<sql-user>` / `<sql-password>`).
> Server/connection credentials are replaced with placeholders.

## Session summary

### 1. Test `GetEmployeefromDB` against the database
- Verified SQL Server connectivity; `dbo.Employeedata` table was missing, so it was created and seeded from `sql/Employeedata.sql`.
- Ran a test harness calling `EchoTool.GetEmployeefromDB` against the live DB.
- All lookups passed: by Id, Name, Email, Department, case-insensitive name, and a no-match case returning null.

### 2. Add `Address` and `JoiningDate` columns + end-to-end test
- Added `Address NVARCHAR(200)` and `JoiningDate DATE` to `dbo.Employeedata` and seeded values.
- `JoiningDate` surfaced to clients formatted as MM/DD/YYYY.
- Updated `Employee` model and `GetEmployeefromDB` in `EchoTool.cs`.
- Updated `sql/Employeedata.sql` for reproducibility.
- End-to-end test passed; new columns returned correctly.

### 3. `dotnet build` failure
- Build failed with duplicate assembly-attribute errors (CS0579).
- Root cause: a leftover `dbtest/obj` folder from a temporary test project was picked up by the main project's default `**/*.cs` globbing.
- Removed the stray `dbtest` folder; build succeeded (0 warnings, 0 errors).

### 4. List all employees
- Queried and returned all employee rows.

### 5. Which employees are in Marketing
- Returned the single Marketing employee (Neha).

### 6. Add employee "SOUMYATA"
- Inserted new employee (Id 11): SOUMYATA, ITTELECOM, sskaran@company.com, 14 FC Road, Pune, 04/25/2016.

### 7. Add employee "aarya"
- Inserted new employee (Id 12): aarya, ITTELECOM, sskaran@company.com, 14 FC Road, Pune, 04/25/2016.

### 8. Correct name to "AARYA TRIPATHI"
- Updated Id 12 name from "aarya" to "AARYA TRIPATHI".

### 9. Security governance guidance (prevent delete/truncate)
- Recommended: least-privilege logins, remove hard-coded credentials, system-versioning
  for recoverability, backups, and SQL Server Audit.

### 10. Delete record "aaraya@company.com"
- Deleted the single matching row (Id 2, Aaraya — Design). 1 row removed.

### 11. Apply governance controls
- Created two least-privilege SQL logins (usernames redacted):
  - Read-only application login: `db_datareader` + `DENY INSERT, UPDATE, DELETE, ALTER`.
  - Writer login: `GRANT SELECT, INSERT, UPDATE` + `DENY DELETE, ALTER` (can update, cannot delete/truncate).
- Fixed a mistake: `DENY CONTROL` is a superset that also blocked SELECT; replaced with targeted denies.
- Enabled system-versioning (temporal) on `Employeedata` → `Employeedata_History`
  (also blocks TRUNCATE and preserves history).
- Enabled SQL Server Audit (server audit + database audit specification) for DELETE on the table,
  schema/object changes, and permission changes; audit files written to `C:\SQLAudit`.
- Verified enforcement: writer can UPDATE but not DELETE/TRUNCATE; read-only login can SELECT but not write.
- Verified audit captured the denied DELETE attempt and permission changes.
- Removed the hard-coded credential from `EchoTool.cs` error message; switched example to the read-only login placeholder.
- Scripts saved: `sql/Security_Governance.sql`, `sql/Security_Audit.sql`.

### 12. Use read-only login for the app
- Set `EMPLOYEE_DB_CONNECTION` in `.vscode/mcp.json` `env` to the read-only login:
  `Server=<server>;Database=testdb;User Id=<sql-user>;Password=<sql-password>;TrustServerCertificate=True;`

### 13. List SOUMYATA details
- Returned Id 11 details via the read-only login.

### 14. Update Pooja's address
- Updated Pooja (Id 9) address to "700 Salt Lake, Kolkata" via the writer login; previous value retained in history.

### 15. Start logging chat to lg.md
- Created `lg.md` with a chronological, credential-redacted session log.

### 16. Explain `git push` error "src refspec main does not match any"
- Explained: no local `main` ref exists — either no commit has been made yet, or the local branch is named differently (e.g., `master`).

### 17. Line-by-line git fix commands
- Provided step-by-step commands to add `.gitignore`, stage, commit, rename branch to `main`, set remote, and push.

### 18. Show top 5 employees
- Returned the first 5 employees by Id (Id 2 absent due to earlier delete).

### 19. HR and Engineering employees (background execution)
- Listed all Engineering (3) and HR (2) employees via `sqlcmd` with the read-only login.

### 20. Which tool executed for the previous query
- Clarified the terminal `sqlcmd` utility was used, not the MCP tool (which returns a single row only).

### 21. Priya details
- Returned Priya (Id 4) via `sqlcmd`.

### 22. Why not use `GetEmployeefromDB` MCP tool
- Explained the tool wasn't registered yet because the running server was a stale build (exe locked by the running process prevented rebuild/restart).

### 23. Priya details via MCP tool
- After rebuild/restart, called the `GetEmployeefromDB` MCP tool successfully for Priya.

### 24. New tool `GetAllEmployeesData()`
- Added an MCP tool returning all employees ordered by name; verified compilation.

### 25. Get all employee data via MCP tool
- Called `GetAllEmployeesData` MCP tool; returned all 11 employees.

### 26. Background logging on successful tool execution
- Added a fire-and-forget background logger appending `timestamp | toolName | executed successfully`
  to `tool-execution.log`; wired into all tools on their success paths.

### 27. Vikram details via MCP tool
- Returned Vikram (Id 8) via the `GetEmployeefromDB` MCP tool.

### 28. Where is the background log
- Log path: `bin/Debug/net9.0/tool-execution.log` (app base directory); overridable via `TOOL_LOG_PATH`.

### 29. Show all employee data
- Called `GetAllEmployeesData` MCP tool again; returned all 11 employees.

### 30. Push changes to `fix/code-review-findings`
- Committed the `GetAllEmployeesData` + background-logging change (`EchoTool.cs` only, no secrets) and pushed to the branch.

### 31. Continue logging chat to lg.md
- Appended entries 15-31 to this log (credentials remain redacted).

