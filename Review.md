# Code Review — MyMCP

Reviewer: Senior Architect pass · Scope: full repository (no diff available — single initial commit) · Date: 2026-08-11

> **Status (2026-08-11, branch `fix/code-review-findings`): all findings below addressed.** See commit(s) on this branch for the fixes: secrets untracked/parameterized (#1 — credentials still need manual rotation on the actual SQL Server, see note), search semantics changed from OR to Id-or-AND (#2), SqlException now wrapped with a clear error (#3), dead code removed (#4, #5), and a new `tests/MyMCP.Tests` project with unit tests added (#6).

## Summary

MyMCP is a small .NET 9 MCP (Model Context Protocol) server exposing three tools (`Echo`, `ReverseEcho`, `GetEmployeefromDB`) over stdio, backed by a SQL Server `Employeedata` table. The SQL scripts show real security awareness (parameterized queries, least-privilege logins, temporal tables, server auditing). However, the repo currently **leaks live-looking database credentials in two committed files**, which is the top-priority finding and should be treated as an incident, not a style nit. Beyond that, the codebase is small enough that its main risks are: a hardcoded/duplicated data model, a query with permissive matching semantics, no error handling around the DB call, and no tests.

## Findings

### 1. Critical — Plaintext credentials committed to git (`.vscode/mcp.json`, `sql/Security_Governance.sql`)
- `.vscode/mcp.json:12` embeds `User Id=mcp_app;Password=McpApp#2026!Rd` directly in the MCP server launch config, and it **is tracked in git** (`git ls-files` confirms it, despite `.gitignore` also listing it — the ignore was added after the file was already committed, so it has no effect).
- `sql/Security_Governance.sql:28,38` hardcodes the same password plus a second one (`EmpWriter#2026!Wr`) in `CREATE LOGIN` statements.
- `EchoTool.cs` correctly reads the connection string from `EMPLOYEE_DB_CONNECTION` and has a comment saying "never store real credentials in source" — the intent is right, but `.vscode/mcp.json` undermines it directly.
- **Why it matters**: once committed, these credentials are permanently in git history even if the file is deleted or edited later; anyone with repo access (or a future public push) can extract them.
- **Fix**: rotate both SQL logins immediately, remove `.vscode/mcp.json` from git tracking (`git rm --cached`), parameterize the SQL scripts (e.g., `sqlcmd -v` variables or a secrets manager) instead of literal passwords, and consider scrubbing git history (`git filter-repo`/BFG) if this repo is or will be shared.

### 2. Medium — `GetEmployeefromDB` query semantics are surprising (`EchoTool.cs:69-75`)
The `WHERE` clause ORs every field together, so passing e.g. `Department = "Engineering"` alone (with `Id` defaulting to `0`, per the C# `Employee.Id` default) returns `TOP 1` **arbitrary** engineering employee rather than a meaningful "search," and combining multiple fields doesn't narrow the search — it broadens it (any field match wins). A caller intending "find Id=3 in Department=Engineering" instead gets whichever field matches first. Given the tool description says "Searches ... by any provided field," this is arguably intentional as an OR-search, but it's a foot-gun combined with `TOP 1` silently discarding ambiguity.
- **Suggestion**: clarify intended semantics (AND vs OR) in the description, and if multiple matches are possible, either return a list or make it explicit that only the first is returned.

### 3. Medium — No error handling around DB access (`EchoTool.cs:63-97`)
`GetEmployeefromDB` lets `SqlException` (bad connection string, network failure, auth failure) propagate uncaught. In an MCP tool context this will likely surface as an opaque failure to the calling agent rather than an actionable message. Worth catching and returning/raising a clearer error, especially since the connection string itself throws `InvalidOperationException` with a helpful message today — the DB call path has no equivalent care.

### 4. Low — Duplicated employee data (`EchoTool.cs:26-38` vs `sql/Employeedata.sql:21-31`)
The in-memory `Employees` list is a byte-for-byte duplicate (minus `Address`/`JoiningDate`) of the seed data in `Employeedata.sql`, but it's dead code — no tool references `Employees` (the only consumer, `GetEmployee`, is commented out at `EchoTool.cs:52-60`). Either delete the in-memory list and the commented-out method, or keep one as an explicit fallback/demo path with a comment explaining why both exist.

### 5. Low — Commented-out code left in source (`EchoTool.cs:52-60`)
Dead code should be removed rather than commented out; git history preserves it if needed later.

### 6. Low — No automated tests
There's no test project. For a tool whose main risk surface is a hand-built SQL `WHERE` clause and connection-string handling, even a small integration test (e.g., using LocalDB or a test double for `SqlConnection`) would catch regressions in the matching logic called out in #2.

### 7. Informational — SQL scripts are otherwise solid
`Security_Audit.sql` and `Security_Governance.sql` show good practices: parameterized queries in C#, `DENY` instead of relying only on `GRANT`, temporal versioning for recoverability, and server/database audit specs. The only real problem is the credential handling (#1) — the design pattern (least-privilege `mcp_app` read-only + `emp_writer` write-only logins) is good and worth keeping once secrets are externalized.

## Priority order
1. Rotate leaked credentials and stop committing secrets (#1) — do this first, independent of any other cleanup.
2. Clarify/fix search semantics in `GetEmployeefromDB` (#2).
3. Add error handling for the DB path (#3).
4. Remove dead code (#4, #5).
5. Add minimal test coverage (#6).
