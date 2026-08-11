using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        private static readonly List<Employee> Employees = new()
        {
            new Employee { Id = 1, Name = "Dipak", Department = "Engineering", Email = "dipak@company.com" },
            new Employee { Id = 2, Name = "Aaraya", Department = "Design", Email = "aaraya@company.com" },
            new Employee { Id = 3, Name = "Rohan", Department = "Engineering", Email = "rohan@company.com" },
            new Employee { Id = 4, Name = "Priya", Department = "HR", Email = "priya@company.com" },
            new Employee { Id = 5, Name = "Sneha", Department = "Finance", Email = "sneha@company.com" },
            new Employee { Id = 6, Name = "Amit", Department = "Engineering", Email = "amit@company.com" },
            new Employee { Id = 7, Name = "Neha", Department = "Marketing", Email = "neha@company.com" },
            new Employee { Id = 8, Name = "Vikram", Department = "Sales", Email = "vikram@company.com" },
            new Employee { Id = 9, Name = "Pooja", Department = "HR", Email = "pooja@company.com" },
            new Employee { Id = 10, Name = "Karan", Department = "Finance", Email = "karan@company.com" },
        };

        [McpServerTool, Description("Echoes the message back to the client")]
        public static string Echo(string message)
        {
            return $"Hello from C#: {message}";
        }

        [McpServerTool, Description("Echoes in reverse the message sent")]
        public static string ReverseEcho(string message)
        {
            return new string(message.Reverse().ToArray());
        }

        // [McpServerTool, Description("Searches the 10-employee list by any provided field (Id, Name, Department, or Email) and returns the matching employee's full details")]
        // public static Employee? GetEmployee(Employee emp)
        // {
        //     return Employees.FirstOrDefault(e =>
        //         (emp.Id != 0 && e.Id == emp.Id) ||
        //         (!string.IsNullOrWhiteSpace(emp.Name) && string.Equals(e.Name, emp.Name, StringComparison.OrdinalIgnoreCase)) ||
        //         (!string.IsNullOrWhiteSpace(emp.Department) && string.Equals(e.Department, emp.Department, StringComparison.OrdinalIgnoreCase)) ||
        //         (!string.IsNullOrWhiteSpace(emp.Email) && string.Equals(e.Email, emp.Email, StringComparison.OrdinalIgnoreCase)));
        // }

 [McpServerTool, Description("Searches the Employeedata table in SQL Server by any provided field (Id, Name, Department, or Email) and returns the matching employee's full details")]
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
