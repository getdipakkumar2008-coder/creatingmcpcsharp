using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MyMCP
{
    public class Employee
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        // Formatted as MM/DD/YYYY.
        public string? JoiningDate { get; set; }
    }

    [McpServerToolType]
    public static class EchoTool
    {
        [McpServerTool, Description("Echoes the message back to the client")]
        public static string Echo(string message)
        {
            var result = $"Hello from C#: {message}";
            LogToolExecution(nameof(Echo));
            return result;
        }

        [McpServerTool, Description("Echoes in reverse the message sent")]
        public static string ReverseEcho(string message)
        {
            var result = new string(message.Reverse().ToArray());
            LogToolExecution(nameof(ReverseEcho));
            return result;
        }

        // Match semantics: if Id is provided (non-zero) it is used alone, since it is the
        // primary key. Otherwise every provided field (Name/Department/Email) must ALL
        // match (AND, not OR) - passing multiple fields narrows the search rather than
        // widening it. At least one field must be provided.
        [McpServerTool, Description("Searches the Employeedata table in SQL Server for a single employee. " +
            "If Id is provided it is matched alone (primary key lookup); otherwise all of the provided " +
            "fields among Name, Department, and Email must match (AND). At least one field is required.")]
        public static Employee? GetEmployeefromDB(Employee emp)
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString());
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT TOP 1 Id, Name, Department, Email, Address, JoiningDate
                    FROM dbo.Employeedata
                    WHERE (@Id <> 0 AND Id = @Id)
                       OR (
                            @Id = 0
                            AND (@Name IS NULL OR Name = @Name)
                            AND (@Department IS NULL OR Department = @Department)
                            AND (@Email IS NULL OR Email = @Email)
                            AND (@Name IS NOT NULL OR @Department IS NOT NULL OR @Email IS NOT NULL)
                          );";

                command.Parameters.AddWithValue("@Id", emp.Id);
                command.Parameters.AddWithValue("@Name", (object?)emp.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Department", (object?)emp.Department ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)emp.Email ?? DBNull.Value);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var found = new Employee
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                        JoiningDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("MM/dd/yyyy"),
                    };
                    LogToolExecution(nameof(GetEmployeefromDB));
                    return found;
                }

                LogToolExecution(nameof(GetEmployeefromDB));
                return null;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"Employee lookup failed due to a database error: {ex.Message}", ex);
            }
        }

        [McpServerTool, Description("Returns the full list of employees from the Employeedata table in SQL Server, ordered by name.")]
        public static List<Employee> GetAllEmployeesData()
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString());
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Name, Department, Email, Address, JoiningDate
                    FROM dbo.Employeedata
                    ORDER BY Name;";

                var employees = new List<Employee>();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    employees.Add(new Employee
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                        JoiningDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("MM/dd/yyyy"),
                    });
                }

                LogToolExecution(nameof(GetAllEmployeesData));
                return employees;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"Employee list retrieval failed due to a database error: {ex.Message}", ex);
            }
        }

        // Fire-and-forget append so logging never blocks or breaks a tool response.
        private static readonly object LogLock = new();

        private static void LogToolExecution(string toolName)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    var path = Environment.GetEnvironmentVariable("TOOL_LOG_PATH")
                        ?? Path.Combine(AppContext.BaseDirectory, "tool-execution.log");
                    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {toolName} | executed successfully{Environment.NewLine}";
                    lock (LogLock)
                    {
                        File.AppendAllText(path, line);
                    }
                }
                catch
                {
                    // Logging must never affect tool behavior.
                }
            });
        }

        // Connection string is read from the EMPLOYEE_DB_CONNECTION environment variable to avoid hardcoding credentials.
        // Use the least-privilege read-only login (mcp_app); never store real credentials in source.
        private static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("EMPLOYEE_DB_CONNECTION")
                ?? throw new InvalidOperationException(
                    "Set the EMPLOYEE_DB_CONNECTION environment variable to a connection string, e.g. " +
                    "\"Server=<host>;Database=testdb;User Id=mcp_app;Password=<secret>;TrustServerCertificate=True;\"");
        }

    }
}
