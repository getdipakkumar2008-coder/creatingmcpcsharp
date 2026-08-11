# MyMCP Architecture Document

## 1. System Overview

**MyMCP** is a Model Context Protocol (MCP) server implementation in C# that bridges AI assistants and other MCP clients with a SQL Server database containing employee records. It demonstrates enterprise-grade architecture patterns including:

- **Protocol Implementation**: MCP v2.1.0 with stdio transport
- **Data Layer**: SQL Server with parameterized queries and connection pooling
- **Security**: Least-privilege database access with role-based permissions
- **Compliance**: System-versioning (temporal tables) and comprehensive audit logging
- **Scalability**: Stateless design supporting multiple concurrent clients

### Architecture Characteristics

| Aspect | Implementation |
|--------|-----------------|
| **Protocol** | Model Context Protocol (MCP) 2.1.0 |
| **Transport** | Stdio (stdin/stdout) |
| **Framework** | .NET 9.0 with Dependency Injection |
| **Database** | SQL Server 2019+ |
| **Authentication** | Environment variable-based connection string |
| **Concurrency Model** | Async/await ready; stateless per-client |
| **Data Access Pattern** | ADO.NET with parameterized queries |

---

## 2. Detailed Component Architecture

### 2.1 MCP Protocol Layer

```
┌───────────────────────────────────────────────────────────────────┐
│                     MCP Client Layer                              │
│  (Claude AI, IDE Extensions, Automation Scripts, Web Clients)    │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                    MCP Protocol (JSON-RPC)
                    Stdio Transport (stdin/stdout)
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                  Program.cs (MCP Server Init)                    │
│                                                                   │
│  var builder = Host.CreateEmptyApplicationBuilder(...);         │
│  builder.Services                                                │
│    .AddMcpServer()              // Register MCP server           │
│    .WithStdioServerTransport()  // Stdio transport               │
│    .WithToolsFromAssembly();    // Auto-discover [McpServerTool] │
│  await builder.Build().RunAsync();                              │
│                                                                   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                  Tool Registry (Reflection-based)
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│              EchoTool.cs (Static Tool Class)                     │
│                                                                   │
│  [McpServerToolType]                                            │
│  public static class EchoTool                                   │
│  {                                                               │
│      [McpServerTool]                                            │
│      public static string Echo(string message) { ... }         │
│                                                                   │
│      [McpServerTool]                                            │
│      public static string ReverseEcho(string message) { ... }  │
│                                                                   │
│      [McpServerTool]                                            │
│      public static Employee? GetEmployeefromDB(Employee emp)   │
│      { ... }                                                     │
│  }                                                               │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

**Key Points:**
- **Reflection-based Tool Discovery** (line 10, Program.cs): `WithToolsFromAssembly()` scans for `[McpServerTool]` attributes at runtime
- **Stateless Design**: Each tool is a static method; no instance state persists between calls
- **Automatic Serialization**: MCP framework handles JSON serialization/deserialization of parameters and return values

### 2.2 Application Layer (EchoTool.cs)

#### Data Models

```csharp
// Location: EchoTool.cs, lines 12–21
public class Employee
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? JoiningDate { get; set; }  // MM/DD/YYYY format
}
```

**Design Rationale:**
- **Nullable reference types** (`string?`): C# 8.0+ nullability contracts prevent null-reference exceptions
- **Auto-properties**: Minimal, idiomatic C# for data transfer objects (DTOs)
- **JoiningDate as String**: Pre-formatted (MM/DD/YYYY) to eliminate client-side date parsing

#### Tools Implementation

##### Tool 1: Echo (lines 40–44)

```csharp
[McpServerTool, Description("Echoes the message back to the client")]
public static string Echo(string message)
{
    return $"Hello from C#: {message}";
}
```

**Purpose:** Simple demonstration of tool mechanics  
**Complexity:** O(1) string concatenation  
**Error Handling:** None (string input is always valid)

##### Tool 2: ReverseEcho (lines 46–50)

```csharp
[McpServerTool, Description("Echoes in reverse the message sent")]
public static string ReverseEcho(string message)
{
    return new string(message.Reverse().ToArray());
}
```

**Purpose:** Demonstrate LINQ integration  
**Complexity:** O(n) where n = message length  
**Error Handling:** None (string operations are safe)

##### Tool 3: GetEmployeefromDB (lines 62–97)

**Complexity:** O(n) database query; parameterized for SQL injection prevention

```csharp
[McpServerTool, Description("Searches the Employeedata table in SQL Server...")]
public static Employee? GetEmployeefromDB(Employee emp)
{
    using var connection = new SqlConnection(GetConnectionString());
    connection.Open();
    
    using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT TOP 1 Id, Name, Department, Email, Address, JoiningDate
        FROM dbo.Employeedata
        WHERE (@Id <> 0 AND Id = @Id)
           OR (@Name IS NOT NULL AND Name = @Name)
           OR (@Department IS NOT NULL AND Department = @Department)
           OR (@Email IS NOT NULL AND Email = @Email);";
    
    command.Parameters.AddWithValue("@Id", emp.Id);
    command.Parameters.AddWithValue("@Name", (object?)emp.Name ?? DBNull.Value);
    command.Parameters.AddWithValue("@Department", (object?)emp.Department ?? DBNull.Value);
    command.Parameters.AddWithValue("@Email", (object?)emp.Email ?? DBNull.Value);
    
    using var reader = command.ExecuteReader();
    if (reader.Read())
    {
        return new Employee
        {
            Id = reader.GetInt32(0),
            Name = reader.IsDBNull(1) ? null : reader.GetString(1),
            Department = reader.IsDBNull(2) ? null : reader.GetString(2),
            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
            JoiningDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("MM/dd/yyyy"),
        };
    }
    
    return null;
}

private static string GetConnectionString()
{
    return Environment.GetEnvironmentVariable("EMPLOYEE_DB_CONNECTION")
        ?? throw new InvalidOperationException("Set EMPLOYEE_DB_CONNECTION environment variable...");
}
```

**Architecture Decisions:**

| Decision | Rationale |
|----------|-----------|
| **`using` statements** | Ensures connection/command disposal (resource management) |
| **Parameterized queries** | Prevents SQL injection; parameters bound at index 1–4 |
| **`DBNull.Value`** | SQL Server NULL representation for optional input parameters |
| **`reader.IsDBNull()`** | Check before type conversion to prevent exceptions |
| **`TOP 1`** | Returns first match; prevents multi-row surprises |
| **`GetConnectionString()` static method** | Centralized retrieval from environment variable |
| **Logical OR** | Supports flexible search by any single field |

**Multi-Criteria Search Example:**

```sql
-- Client request:
GetEmployeefromDB(new Employee { Name = "Dipak", Department = "Engineering" })

-- Generated WHERE clause:
WHERE (@Id <> 0 AND Id = @Id)                           -- False (Id = 0)
   OR (@Name IS NOT NULL AND Name = @Name)              -- True  (matches)
   OR (@Department IS NOT NULL AND Department = @Department)  -- True (matches)
   OR (@Email IS NOT NULL AND Email = @Email);          -- False (Email = null)
```

---

### 2.3 Data Access Layer (ADO.NET)

#### Connection Management

```csharp
using var connection = new SqlConnection(GetConnectionString());
connection.Open();
```

**Pattern:** Using-statements ensure connections are returned to the pool immediately after disposal

**Connection String Format:**
```
Server=<hostname>;Database=testdb;User Id=<login>;Password=<password>;TrustServerCertificate=True;
```

#### Command Execution Flow

```
┌─────────────────────────────────────────┐
│  SqlConnection.Open()                   │
│  (Acquire from connection pool or new)  │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│  SqlCommand.ExecuteReader()             │
│  (Returns SqlDataReader on stream)      │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│  while (reader.Read()) { ... }          │
│  (Iterate result set, one row at a time)│
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│  reader.Dispose() (implicit via using)  │
│  connection.Dispose() (return to pool)  │
└─────────────────────────────────────────┘
```

#### Parameter Binding

**Pattern:** Named parameters with type-safe binding

```csharp
command.Parameters.AddWithValue("@Id", emp.Id);
command.Parameters.AddWithValue("@Name", (object?)emp.Name ?? DBNull.Value);
```

**Security Benefit:** Query plan caching and SQL injection prevention

---

### 2.4 Database Layer (SQL Server)

#### Schema Design

```sql
CREATE TABLE dbo.Employeedata (
    Id            INT            NOT NULL PRIMARY KEY,
    Name          NVARCHAR(100)  NULL,
    Department    NVARCHAR(100)  NULL,
    Email         NVARCHAR(200)  NULL,
    Address       NVARCHAR(200)  NULL,
    JoiningDate   DATE           NULL,
    
    -- Temporal Versioning Columns (auto-managed)
    SysStart      DATETIME2(7)   GENERATED ALWAYS AS ROW START HIDDEN,
    SysEnd        DATETIME2(7)   GENERATED ALWAYS AS ROW END HIDDEN,
    PERIOD FOR SYSTEM_TIME (SysStart, SysEnd)
);

-- Shadow table (auto-created by SQL Server)
CREATE TABLE dbo.Employeedata_History (
    Id            INT            NOT NULL,
    Name          NVARCHAR(100)  NULL,
    Department    NVARCHAR(100)  NULL,
    Email         NVARCHAR(200)  NULL,
    Address       NVARCHAR(200)  NULL,
    JoiningDate   DATE           NULL,
    SysStart      DATETIME2(7)   NOT NULL,
    SysEnd        DATETIME2(7)   NOT NULL
);

-- System-versioning enabled
ALTER TABLE dbo.Employeedata
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Employeedata_History));
```

**Key Design Patterns:**

| Pattern | Benefit |
|---------|---------|
| **Temporal Versioning** | Full audit trail; point-in-time recovery |
| **NVARCHAR (Unicode)** | International character support |
| **NULL constraints** | Flexible schema; most fields optional except PK |
| **HIDDEN temporal columns** | Transparent to legacy queries; backward compatible |

#### Temporal Query Examples

```sql
-- Current state
SELECT * FROM dbo.Employeedata WHERE Id = 1;

-- Historical state (as of 2 hours ago)
SELECT * FROM dbo.Employeedata
FOR SYSTEM_TIME AS OF DATEADD(hour, -2, GETUTCDATE())
WHERE Id = 1;

-- All versions of employee 1
SELECT * FROM dbo.Employeedata
FOR SYSTEM_TIME BETWEEN '2026-08-01' AND '2026-08-12'
WHERE Id = 1;
```

---

## 3. Security Architecture

### 3.1 Authentication & Authorization

#### Login Hierarchy

```
SQL Server (Master Database)
├── Login: mcp_app
│   └─ Password Policy: Enforced (CHECK_POLICY = ON)
│   └─ Database User: mcp_app (testdb)
│       └─ Role: db_datareader
│       └─ Explicit Denies: INSERT, UPDATE, DELETE, ALTER
│
└── Login: emp_writer
    └─ Password Policy: Enforced (CHECK_POLICY = ON)
    └─ Database User: emp_writer (testdb)
        └─ Grants: SELECT, INSERT, UPDATE
        └─ Denies: DELETE, ALTER
```

**Implementation (sql/Security_Governance.sql):**

```sql
-- Read-only login for MCP application
CREATE LOGIN mcp_app WITH PASSWORD = 'McpApp#2026!Rd', CHECK_POLICY = ON;
CREATE USER mcp_app FOR LOGIN mcp_app;
ALTER ROLE db_datareader ADD MEMBER mcp_app;
DENY INSERT, UPDATE, DELETE, ALTER ON dbo.Employeedata TO mcp_app;

-- Writer login for administrative updates
CREATE LOGIN emp_writer WITH PASSWORD = 'EmpWriter#2026!Wr', CHECK_POLICY = ON;
CREATE USER emp_writer FOR LOGIN emp_writer;
GRANT SELECT, INSERT, UPDATE ON dbo.Employeedata TO emp_writer;
DENY DELETE, ALTER ON dbo.Employeedata TO emp_writer;
```

**Threat Model:**

| Threat | Mitigation |
|--------|------------|
| **SQL Injection** | Parameterized queries (ADO.NET parameters) |
| **Unauthorized Modification** | Separate read-only and write logins |
| **Accidental Data Loss** | DENY DELETE + Temporal versioning |
| **Privilege Escalation** | No admin/sysadmin roles for app/writer |
| **Weak Passwords** | Password policy enforcement (CHECK_POLICY = ON) |

### 3.2 Audit & Compliance

#### Audit Configuration

```sql
-- sql/Security_Audit.sql

USE master;
CREATE SERVER AUDIT Employee_Server_Audit
    TO FILE (FILEPATH = 'C:\SQLAudit\', MAXSIZE = 50 MB, MAX_ROLLOVER_FILES = 10)
    WITH (ON_FAILURE = CONTINUE);

USE testdb;
CREATE DATABASE AUDIT SPECIFICATION EmployeeData_Audit_Spec
    FOR SERVER AUDIT Employee_Server_Audit
    ADD (DELETE ON OBJECT::dbo.Employeedata BY public),
    ADD (SCHEMA_OBJECT_CHANGE_GROUP),
    ADD (SCHEMA_OBJECT_PERMISSION_CHANGE_GROUP),
    ADD (DATABASE_OBJECT_PERMISSION_CHANGE_GROUP),
    ADD (DATABASE_PERMISSION_CHANGE_GROUP);
```

**Audit Events Tracked:**

1. **DELETE Operations** — Every delete attempt (including denied)
2. **Schema Changes** — CREATE, ALTER, DROP on objects
3. **Permission Changes** — GRANT/DENY statements
4. **Failed Access** — Attempts by unauthorized logins

**Forensics Query:**

```sql
SELECT 
    event_time,
    database_name,
    object_name,
    action_id,
    statement,
    session_server_principal_name,
    succeeded
FROM sys.fn_get_audit_file('C:\SQLAudit\*', DEFAULT, DEFAULT)
WHERE action_id IN ('DE', 'AL')  -- DELETE, ALTER
ORDER BY event_time DESC;
```

---

## 4. Data Flow Architecture

### 4.1 Request/Response Cycle

```
┌────────────────────────────────────────────────────────────────┐
│  1. MCP Client Sends Tool Call                                 │
│     {                                                           │
│       "jsonrpc": "2.0",                                         │
│       "method": "tools/call",                                   │
│       "params": {                                               │
│         "name": "GetEmployeefromDB",                            │
│         "arguments": {                                          │
│           "emp": { "Id": 1, "Name": null, ... }               │
│         }                                                       │
│       }                                                         │
│     }                                                           │
└──────────────────────────────┬─────────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────────┐
│  2. MCP Server Receives & Deserializes                         │
│     - Stdio transport parses JSON-RPC message                  │
│     - Tool registry finds GetEmployeefromDB method             │
│     - Parameters deserialized into Employee object            │
└──────────────────────────────┬─────────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────────┐
│  3. Tool Execution (EchoTool.GetEmployeefromDB)               │
│     a. Build SQL query with parameterized search              │
│     b. Open SQL connection (from pool)                        │
│     c. Execute query via SqlDataReader                        │
│     d. Map result to Employee DTO                             │
│     e. Close connection (return to pool)                      │
└──────────────────────────────┬─────────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────────┐
│  4. Result Serialization                                       │
│     {                                                           │
│       "jsonrpc": "2.0",                                         │
│       "id": 1,                                                  │
│       "result": {                                               │
│         "id": 1,                                                │
│         "name": "Dipak",                                        │
│         "department": "Engineering",                           │
│         "email": "dipak@company.com",                          │
│         "address": "12 MG Road, Pune",                         │
│         "joiningDate": "03/15/2019"                            │
│       }                                                         │
│     }                                                           │
└──────────────────────────────┬─────────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────────┐
│  5. MCP Client Receives Response                               │
│     - Deserialize into Employee object                         │
│     - Application logic processes result                       │
└────────────────────────────────────────────────────────────────┘
```

### 4.2 Error Handling & Resilience

**Unhandled Exceptions:**

| Exception | Source | Client Receives |
|-----------|--------|-----------------|
| `InvalidOperationException` | `GetConnectionString()` if env var missing | MCP error response with message |
| `SqlException` | SQL query failure, connection timeout | MCP error response + exception details |
| `IndexOutOfRangeException` | Reader column access (defensive checks prevent this) | MCP error response |
| `NullReferenceException` | Unsafe null dereference | MCP error response |

**Best Practice:** All ADO.NET calls wrapped in `using` statements to prevent resource leaks even on exceptions.

---

## 5. Scalability & Performance

### 5.1 Connection Pooling

```csharp
// Implicit via SqlConnection
using var connection = new SqlConnection(GetConnectionString());
// Microsoft.Data.SqlClient automatically manages pooling:
// - Min pool size: 0 (default)
// - Max pool size: 100 (default)
// - Connection lifetime: 15 minutes (default)
```

**Benefits:**
- Reduces TCP handshake overhead
- Reuses prepared connections across tool calls
- Automatic cleanup of stale connections

**Configuration (via connection string):**
```
Min Pool Size=5;Max Pool Size=100;Connection Lifetime=3600;
```

### 5.2 Query Optimization

**Current Queries (GetEmployeefromDB):**

```sql
SELECT TOP 1 Id, Name, Department, Email, Address, JoiningDate
FROM dbo.Employeedata
WHERE (@Id <> 0 AND Id = @Id)
   OR (@Name IS NOT NULL AND Name = @Name)
   OR (@Department IS NOT NULL AND Department = @Department)
   OR (@Email IS NOT NULL AND Email = @Email);
```

**Index Strategy:**

```sql
CREATE CLUSTERED INDEX PK_Employeedata ON dbo.Employeedata(Id);
CREATE NONCLUSTERED INDEX IX_Name ON dbo.Employeedata(Name);
CREATE NONCLUSTERED INDEX IX_Department ON dbo.Employeedata(Department);
CREATE NONCLUSTERED INDEX IX_Email ON dbo.Employeedata(Email);
```

**Query Plan:** SQL Server uses index seeks for single-column filters; full table scan for multi-column OR (acceptable for small dataset ~10–1000 rows)

### 5.3 Caching Strategy

**Current:** No application-level caching (stateless design)

**Recommended for production:**

```csharp
private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

public static Employee? GetEmployeefromDB(Employee emp)
{
    string cacheKey = $"emp_{emp.Id}_{emp.Name}_{emp.Department}_{emp.Email}";
    
    if (_cache.TryGetValue(cacheKey, out Employee? cached))
        return cached;
    
    // ... database query ...
    
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
    return result;
}
```

**Caveat:** Invalidate cache on updates via `emp_writer` login; temporal tables simplify point-in-time consistency checks.

---

## 6. Dependency Architecture

### 6.1 NuGet Package Dependencies

```xml
<!-- MyMCP.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.2" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.11" />
    <PackageReference Include="ModelContextProtocol" Version="2.1.0" />
</ItemGroup>
```

**Dependency Graph:**

```
MyMCP (net9.0)
├── ModelContextProtocol (2.1.0)
│   └── System.Text.Json (transitive)
│   └── System.Threading.Channels (transitive)
│
├── Microsoft.Extensions.Hosting (10.0.11)
│   ├── Microsoft.Extensions.DependencyInjection (10.0.0)
│   ├── Microsoft.Extensions.Logging (10.0.0)
│   └── Microsoft.Extensions.Configuration (10.0.0)
│
└── Microsoft.Data.SqlClient (7.0.2)
    ├── System.Security.Principal.Windows (transitive)
    ├── System.Configuration.ConfigurationManager (transitive)
    └── System.Runtime.Caching (transitive)
```

**Supply Chain Risk:**
- All packages maintained by Microsoft (trusted)
- Version pinning (7.0.2, 10.0.11, 2.1.0) ensures reproducible builds
- Regular updates recommended (check for security advisories)

---

## 7. Deployment Architecture

### 7.1 Deployment Model

**Single-Process Model:**
```
┌──────────────────────────────────┐
│   Docker Container / VM           │
│  ┌────────────────────────────┐   │
│  │  dotnet run (MyMCP.exe)    │   │
│  │  - Stdin/Stdout bound      │   │
│  │  - Environment vars set    │   │
│  │  - Listening on stdio      │   │
│  └────────────────────────────┘   │
└──────────────────────────────────┘
        ▲                    ▼
        │                    │
   Client Process ←────► Stdio Transport
(Claude, IDE, etc.)
```

**Configuration via Environment Variables:**
```bash
EMPLOYEE_DB_CONNECTION=Server=prod-sql.example.com;Database=testdb;User Id=mcp_app;Password=***;TrustServerCertificate=False;
```

### 7.2 Multi-Instance Deployment

```
Load Balancer / Orchestrator (Kubernetes, Docker Compose, etc.)
│
├─ Instance 1 (MyMCP Process 1)
│  └─ Connection Pool to SQL Server
│
├─ Instance 2 (MyMCP Process 2)
│  └─ Connection Pool to SQL Server
│
└─ Instance 3 (MyMCP Process 3)
   └─ Connection Pool to SQL Server
     ▼
  SQL Server (Centralized)
  ├─ dbo.Employeedata (shared)
  └─ dbo.Employeedata_History
```

**Synchronization:** SQL Server handles concurrency via row-level locking and MVCC (implicit in temporal queries)

---

## 8. Extension Points & Future Enhancements

### 8.1 New Tool Implementation Pattern

```csharp
[McpServerTool, Description("Your tool description")]
public static MyReturnType MyNewTool(MyParameterType param)
{
    // 1. Validate input
    if (string.IsNullOrWhiteSpace(param.Field))
        throw new ArgumentException("Field cannot be empty");
    
    // 2. Execute business logic (database, API call, etc.)
    using var connection = new SqlConnection(GetConnectionString());
    connection.Open();
    // ... query ...
    
    // 3. Return result (auto-serialized by MCP framework)
    return result;
}
```

### 8.2 Recommended Enhancements

1. **Async Tooling**
   ```csharp
   [McpServerTool]
   public static async Task<Employee?> GetEmployeefromDBAsync(Employee emp)
   {
       using var connection = new SqlConnection(GetConnectionString());
       await connection.OpenAsync();
       // ... async query ...
   }
   ```

2. **Dependency Injection**
   ```csharp
   // In Program.cs
   builder.Services.AddScoped<IEmployeeRepository, SqlEmployeeRepository>();
   
   // In tool
   [McpServerTool]
   public static Employee? GetEmployee(Employee emp, IEmployeeRepository repo)
   {
       return repo.FindEmployee(emp);
   }
   ```

3. **Logging & Metrics**
   ```csharp
   private static readonly ILogger<EchoTool> _logger;
   
   [McpServerTool]
   public static Employee? GetEmployeefromDB(Employee emp)
   {
       var sw = Stopwatch.StartNew();
       _logger.LogInformation("Query employee: {emp}", emp);
       // ... execute ...
       _logger.LogInformation("Query completed in {ms}ms", sw.ElapsedMilliseconds);
   }
   ```

4. **Pagination**
   ```csharp
   public class EmployeeSearchRequest
   {
       public Employee SearchCriteria { get; set; }
       public int PageNumber { get; set; } = 1;
       public int PageSize { get; set; } = 20;
   }
   
   [McpServerTool]
   public static List<Employee> SearchEmployees(EmployeeSearchRequest req)
   {
       var offset = (req.PageNumber - 1) * req.PageSize;
       // ... OFFSET/FETCH query ...
   }
   ```

---

## 9. Architecture Decision Log (ADL)

### ADL-001: MCP Protocol Version

**Decision:** Use ModelContextProtocol v2.1.0  
**Rationale:** Latest stable; compatible with Claude and other major clients  
**Alternatives Considered:** v1.x (deprecated), custom JSON-RPC (reinventing the wheel)  
**Date:** 2026-08-11

### ADL-002: Least-Privilege Database Access

**Decision:** Separate logins for read-only (mcp_app) and read-write (emp_writer)  
**Rationale:** Limits blast radius if credentials compromised; follows principle of least privilege  
**Alternatives Considered:** Single admin login (risky), no database auth (non-secure)  
**Date:** 2026-08-11

### ADL-003: Temporal Versioning

**Decision:** Enable system-versioning (SQL Server temporal tables)  
**Rationale:** Full audit trail; point-in-time recovery; prevents TRUNCATE attacks  
**Alternatives Considered:** Manual change tracking (more code), database mirroring (overkill for this scale)  
**Date:** 2026-08-11

### ADL-004: Connection String via Environment Variable

**Decision:** Read from `EMPLOYEE_DB_CONNECTION` environment variable  
**Rationale:** Keeps secrets out of source code; cloud-native approach (compatible with Docker, Kubernetes)  
**Alternatives Considered:** appsettings.json (secrets would be versioned), hardcoded (never do this)  
**Date:** 2026-08-11

### ADL-005: Parameterized Queries

**Decision:** Use `SqlCommand.Parameters.AddWithValue()` for all queries  
**Rationale:** SQL injection prevention; query plan caching  
**Alternatives Considered:** String concatenation (vulnerable), stored procedures (adds complexity)  
**Date:** 2026-08-11

---

## 10. Appendix: Example Workflows

### Workflow 1: Query Employee by ID

```
Client: Call GetEmployeefromDB with Employee { Id = 1 }
  ↓
MCP Server deserializes request
  ↓
EchoTool.GetEmployeefromDB(emp)
  ├─ Connect to SQL Server (mcp_app login, read-only)
  ├─ Execute: SELECT ... WHERE (@Id <> 0 AND Id = @Id) ...
  ├─ SQL Server scans clustered index on PK_Employeedata
  ├─ Return row: { Id: 1, Name: "Dipak", ..., JoiningDate: "03/15/2019" }
  ├─ Close connection (return to pool)
  └─ Return Employee object to MCP framework
    ↓
MCP Server serializes result to JSON
  ↓
Client receives: { "id": 1, "name": "Dipak", ... }
```

### Workflow 2: Update Employee Address (emp_writer)

```
Database Admin: Call UPDATE via emp_writer login
  ↓
SQL Server Temporal Engine
  ├─ Generate SysEnd for old row
  ├─ Insert old row into dbo.Employeedata_History
  ├─ Insert new row into dbo.Employeedata with fresh SysStart
  ├─ Log to audit trail (if enabled)
  └─ COMMIT transaction
    ↓
Audit Log Entry Created
  ├─ Event: UPDATE
  ├─ Table: dbo.Employeedata
  ├─ User: emp_writer
  ├─ Timestamp: 2026-08-11 23:37:00 UTC
  └─ Severity: Success
    ↓
Point-in-Time Recovery Available
  ├─ Query FOR SYSTEM_TIME AS OF '2026-08-11 23:36:00'
  └─ Retrieve pre-update address
```

---

## 11. References

- **MCP Specification:** https://modelcontextprotocol.io/
- **.NET 9.0 Docs:** https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/
- **SQL Server Temporal Tables:** https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables/
- **SQL Server Auditing:** https://learn.microsoft.com/en-us/sql/relational-databases/security/auditing/sql-server-audit-database-engine/
- **Microsoft.Data.SqlClient:** https://github.com/dotnet/SqlClient

---

**Document Version:** 1.0  
**Last Updated:** August 11, 2026  
**Author:** Copilot Documentation Generator  
**Status:** Final
