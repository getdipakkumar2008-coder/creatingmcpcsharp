# MyMCP: Model Context Protocol (MCP) Server for Employee Management

A C# implementation of an **MCP Server** that exposes employee data management tools via the Model Context Protocol. This project demonstrates how to build a secure, database-integrated MCP server with SQL Server backend, least-privilege security, and comprehensive audit logging.

## 🎯 Project Overview

**MyMCP** is a .NET 9.0 console application that:

- **Implements the Model Context Protocol (MCP)** to expose a set of tools that can be called by MCP clients (e.g., AI assistants, integrated development environments, or automation frameworks)
- **Connects to SQL Server** for persistent employee data storage and retrieval
- **Provides employee query and management tools**, including string utilities (Echo, ReverseEcho) and database operations (GetEmployeefromDB)
- **Enforces least-privilege database access** with separate read-only and read-write SQL logins
- **Implements comprehensive audit logging** via SQL Server's built-in audit framework
- **Uses system-versioning (temporal tables)** to maintain full history and prevent accidental data loss

## 🏗️ Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────────────┐
│                    MCP Client                                   │
│          (Claude, IDE, Automation Framework, etc.)              │
└──────────────────────┬──────────────────────────────────────────┘
                       │ (MCP Protocol via Stdio)
                       │
┌──────────────────────▼──────────────────────────────────────────┐
│                 MyMCP Server (Program.cs)                       │
│    ┌─────────────────────────────────────────────────────────┐  │
│    │   Dependency Injection & Host Bootstrap                 │  │
│    │   - AddMcpServer()                                      │  │
│    │   - WithStdioServerTransport()                         │  │
│    │   - WithToolsFromAssembly()                            │  │
│    └─────────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────────┐
│                EchoTool Class (Tools)                           │
│    ┌──────────────────────┬─────────────────────────────────┐   │
│    │ Simple Tools         │ Database Tools                  │   │
│    ├──────────────────────┼─────────────────────────────────┤   │
│    │ • Echo               │ • GetEmployeefromDB             │   │
│    │ • ReverseEcho        │  (SQL Server Backend)           │   │
│    └──────────────────────┴─────────────────────────────────┘   │
└──────────────────────┬──────────────────────────────────────────┘
                       │ (SqlConnection via ADO.NET)
                       │
┌──────────────────────▼──────────────────────────────────────────┐
│              SQL Server Database (testdb)                       │
│    ┌─────────────────────────────────────────────────────────┐  │
│    │  dbo.Employeedata (Main Table)                         │  │
│    │  ├─ Id (INT PK)                                        │  │
│    │  ├─ Name (NVARCHAR(100))                               │  │
│    │  ├─ Department (NVARCHAR(100))                         │  │
│    │  ├─ Email (NVARCHAR(200))                              │  │
│    │  ├─ Address (NVARCHAR(200))                            │  │
│    │  ├─ JoiningDate (DATE)                                 │  │
│    │  └─ SysStart, SysEnd (Temporal)                        │  │
│    │                                                         │  │
│    │  dbo.Employeedata_History (Auto-maintained)            │  │
│    │  └─ Full history of all row changes                    │  │
│    │                                                         │  │
│    │  Security Logins:                                      │  │
│    │  ├─ mcp_app (Read-only) → db_datareader role          │  │
│    │  └─ emp_writer (Insert/Update only)                   │  │
│    │                                                         │  │
│    │  Audit Configuration:                                  │  │
│    │  └─ Employee_Server_Audit (File-based)                │  │
│    └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## 📦 Technology Stack

| Component | Version | Purpose |
|-----------|---------|---------|
| **.NET** | 9.0 | Runtime & Framework |
| **ModelContextProtocol** | 2.1.0 | MCP Protocol Implementation |
| **Microsoft.Extensions.Hosting** | 10.0.11 | Dependency Injection & Host Management |
| **Microsoft.Data.SqlClient** | 7.0.2 | SQL Server Database Access (ADO.NET) |
| **Language** | C# | Primary implementation language |
| **Database** | SQL Server 2019+ | Data persistence layer |

## 📁 Project Structure

```
creatingmcpcsharp/
├── Program.cs                      # MCP Server bootstrap & initialization
├── EchoTool.cs                     # Tools & data models
│   ├── Employee (model)
│   └── EchoTool (static class with [McpServerTool] methods)
├── MyMCP.csproj                    # NuGet dependencies & .NET 9.0 config
├── sql/
│   ├── Employeedata.sql            # Table schema & sample data
│   ├── Security_Governance.sql     # Least-privilege logins & temporal setup
│   └── Security_Audit.sql          # Server/database audit configuration
├── .github/
│   └── copilot-instructions.md     # Copilot configuration
├── .vscode/
│   └── mcp.json                    # MCP server debug configuration
├── lg.md                           # Development session log
└── README.md                       # This file
```

## 🛠️ Building & Running

### Prerequisites

- **.NET 9.0 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **SQL Server 2019 or later** (or SQL Server Express)
- **Access to create databases** on your SQL Server instance
- Environment variable `EMPLOYEE_DB_CONNECTION` set with a valid connection string

### Build

```bash
dotnet restore
dotnet build
```

### Run

Ensure the environment variable is set:

```bash
# Windows
set EMPLOYEE_DB_CONNECTION=Server=localhost;Database=testdb;User Id=mcp_app;Password=<password>;TrustServerCertificate=True;

# macOS/Linux
export EMPLOYEE_DB_CONNECTION=Server=localhost;Database=testdb;User Id=mcp_app;Password=<password>;TrustServerCertificate=True;

dotnet run
```

The MCP server will start and listen for client connections via **stdio** transport.

## 📊 Data Model

### Employee Table Schema

```sql
CREATE TABLE dbo.Employeedata (
    Id            INT            NOT NULL PRIMARY KEY,
    Name          NVARCHAR(100)  NULL,
    Department    NVARCHAR(100)  NULL,
    Email         NVARCHAR(200)  NULL,
    Address       NVARCHAR(200)  NULL,
    JoiningDate   DATE           NULL,
    SysStart      DATETIME2(7)   GENERATED ALWAYS AS ROW START (Temporal),
    SysEnd        DATETIME2(7)   GENERATED ALWAYS AS ROW END (Temporal)
);
```

**Key Features:**
- **Temporal Versioning**: All changes tracked in `dbo.Employeedata_History` automatically
- **Joining Date Formatting**: Stored as DATE; formatted as MM/DD/YYYY when returned to clients
- **Nullable Fields**: All fields except Id and temporal columns are nullable
- **Sample Data**: 10 pre-seeded employees from diverse departments (Engineering, Design, HR, Finance, Marketing, Sales)

### C# Model (EchoTool.cs, lines 12–21)

```csharp
public class Employee
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? JoiningDate { get; set; }  // Formatted as MM/DD/YYYY
}
```

## 🔧 Available Tools

### 1. Echo

**Description:** Echoes the message back to the client  
**Signature:** `Echo(string message) → string`  
**Example:**
```
Input:  "Hello"
Output: "Hello from C#: Hello"
```

### 2. ReverseEcho

**Description:** Echoes in reverse the message sent  
**Signature:** `ReverseEcho(string message) → string`  
**Example:**
```
Input:  "Hello"
Output: "olleH"
```

### 3. GetEmployeefromDB

**Description:** Searches the `dbo.Employeedata` SQL Server table by any provided field (Id, Name, Department, or Email) and returns the matching employee's full details, including Address and JoiningDate.  
**Signature:** `GetEmployeefromDB(Employee emp) → Employee?`  
**Parameters:**
- `emp.Id`: (Optional) Search by employee ID
- `emp.Name`: (Optional) Search by employee name (case-insensitive)
- `emp.Department`: (Optional) Search by department (case-insensitive)
- `emp.Email`: (Optional) Search by email (case-insensitive)

**Returns:** First matching employee record or `null` if no match found

**Database Query:**
```sql
SELECT TOP 1 Id, Name, Department, Email, Address, JoiningDate
FROM dbo.Employeedata
WHERE (@Id <> 0 AND Id = @Id)
   OR (@Name IS NOT NULL AND Name = @Name)
   OR (@Department IS NOT NULL AND Department = @Department)
   OR (@Email IS NOT NULL AND Email = @Email);
```

**Implementation Details (EchoTool.cs, lines 62–97):**
- Uses parameterized queries to prevent SQL injection
- Null handling with `DBNull.Value` for optional fields
- JoiningDate formatted as MM/DD/YYYY before returning to clients
- Connection string retrieved from environment variable `EMPLOYEE_DB_CONNECTION`

## 🔐 Security Architecture

### Least-Privilege Database Logins

#### 1. Read-Only Application Login (`mcp_app`)

**Purpose:** Used by the MCP application for safe, read-only employee queries  
**Permissions:**
- Role: `db_datareader`
- Explicit Denies: `INSERT, UPDATE, DELETE, ALTER` on `dbo.Employeedata`

**Script:** `sql/Security_Governance.sql` (lines 26–34)

#### 2. Writer Login (`emp_writer`)

**Purpose:** Reserved for administrative updates (INSERT, UPDATE only; no DELETE)  
**Permissions:**
- Grants: `SELECT, INSERT, UPDATE` on `dbo.Employeedata`
- Explicit Denies: `DELETE, ALTER` (prevents accidental data loss)

**Script:** `sql/Security_Governance.sql` (lines 36–44)

### Temporal Versioning (System-Versioning)

**Purpose:** Maintains full history and prevents TRUNCATE attacks  
**Implementation:**
- Automatically added columns: `SysStart`, `SysEnd` (DATETIME2(7))
- Shadow table: `dbo.Employeedata_History` (auto-maintained)
- Every UPDATE/DELETE logs the previous state to history
- TRUNCATE is blocked while versioning is enabled

**Script:** `sql/Security_Governance.sql` (lines 5–19)

### SQL Server Audit

**Purpose:** Compliance, forensics, and change tracking  
**Audit Events Captured:**
- **DELETE on dbo.Employeedata** — All delete attempts (including denials)
- **SCHEMA_OBJECT_CHANGE_GROUP** — ALTER/DROP on tables and indexes
- **Database permission changes** — GRANT/DENY statements
- **Server permission changes** — Logins, roles, and server-level grants

**Audit Target:** File-based audit to `C:\SQLAudit\` (configurable)  
**Script:** `sql/Security_Audit.sql` (lines 1–35)

## 🚀 Getting Started

### 1. Set Up SQL Server Database

```bash
# Connect to SQL Server and run the setup scripts in order:
sqlcmd -S DESKTOP-5G6NQEK -d testdb -i sql/Employeedata.sql
sqlcmd -S DESKTOP-5G6NQEK -i sql/Security_Governance.sql
sqlcmd -S DESKTOP-5G6NQEK -d testdb -i sql/Security_Audit.sql
```

*Adjust server name, database name, and audit path as needed.*

### 2. Configure Environment Variable

```bash
# Windows
setx EMPLOYEE_DB_CONNECTION "Server=DESKTOP-5G6NQEK;Database=testdb;User Id=mcp_app;Password=<pwd>;TrustServerCertificate=True;"

# macOS/Linux
export EMPLOYEE_DB_CONNECTION="Server=localhost;Database=testdb;User Id=mcp_app;Password=<pwd>;TrustServerCertificate=True;"
```

### 3. Build & Run

```bash
dotnet build
dotnet run
```

### 4. Connect a Client

Use any MCP-compatible client (e.g., Claude Desktop, VS Code extension, or custom CLI) to:
- Call `Echo` or `ReverseEcho` for string utilities
- Call `GetEmployeefromDB` with an Employee object to fetch database records

## 📝 Usage Examples

### Query Employee by ID

```csharp
// Client calls GetEmployeefromDB with:
new Employee { Id = 1 }

// Returns:
// {
//   Id: 1,
//   Name: "Dipak",
//   Department: "Engineering",
//   Email: "dipak@company.com",
//   Address: "12 MG Road, Pune",
//   JoiningDate: "03/15/2019"
// }
```

### Query Employee by Department

```csharp
// Client calls GetEmployeefromDB with:
new Employee { Department = "Marketing" }

// Returns:
// {
//   Id: 7,
//   Name: "Neha",
//   Department: "Marketing",
//   Email: "neha@company.com",
//   Address: "9 Marine Dr, Chennai",
//   JoiningDate: "12/01/2019"
// }
```

### Reverse a String

```csharp
// Client calls ReverseEcho with:
"Hello, World!"

// Returns:
// "!dlroW ,olleH"
```

## 📋 Development Session Log

For detailed development history, including database setup, schema evolution, security governance implementation, and end-to-end testing, see [`lg.md`](lg.md).

**Key Milestones:**
1. Initial MCP server setup and tool definitions
2. SQL Server connectivity and table creation
3. Addition of Address and JoiningDate columns
4. Implementation of least-privilege logins
5. Enablement of temporal versioning and system audit
6. End-to-end testing and verification

## 🐛 Troubleshooting

### Connection String Not Found

**Error:** `Set the EMPLOYEE_DB_CONNECTION environment variable...`

**Solution:** Ensure the environment variable is set and the connection string is valid.

```bash
echo $EMPLOYEE_DB_CONNECTION  # Verify it's set
```

### Database Not Found

**Error:** `Cannot open database 'testdb' requested by the login...`

**Solution:** Create the database and run `sql/Employeedata.sql`:

```bash
sqlcmd -S <server> -Q "CREATE DATABASE testdb;"
sqlcmd -S <server> -d testdb -i sql/Employeedata.sql
```

### Access Denied

**Error:** `Login failed for user 'mcp_app'...`

**Solution:** Ensure the login exists and has the correct permissions by running:

```bash
sqlcmd -S <server> -d testdb -i sql/Security_Governance.sql
```

### Build Fails with Duplicate Assembly Attributes

**Error:** `CS0579: Duplicate 'System.Reflection.AssemblyXyz' attribute`

**Solution:** Remove stray folders (e.g., `dbtest/obj`) that may be picked up by the globbing pattern:

```bash
rm -rf dbtest
dotnet clean
dotnet build
```

## 📄 License

Not specified. Please add a license file if distributing.

## 👨‍💻 Author

- **Developer:** Dipak Kumar
- **Repository:** [getdipakkumar2008-coder/creatingmcpcsharp](https://github.com/getdipakkumar2008-coder/creatingmcpcsharp)

## 🔗 References

- [Model Context Protocol (MCP) Specification](https://modelcontextprotocol.io/)
- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/)
- [Microsoft.Data.SqlClient Documentation](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace/)
- [SQL Server Security Best Practices](https://learn.microsoft.com/en-us/sql/relational-databases/security/sql-server-security-best-practices/)
- [SQL Server Temporal Tables](https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables/)

---

**Last Updated:** August 11, 2026  
**Status:** Active Development
