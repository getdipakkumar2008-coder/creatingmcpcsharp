using System;
using MyMCP;
using Xunit;

namespace MyMCP.Tests
{
    public class EchoToolTests
    {
        [Fact]
        public void Echo_ReturnsMessagePrefixedWithHelloFromCSharp()
        {
            var result = EchoTool.Echo("world");

            Assert.Equal("Hello from C#: world", result);
        }

        [Fact]
        public void ReverseEcho_ReturnsReversedMessage()
        {
            var result = EchoTool.ReverseEcho("abc");

            Assert.Equal("cba", result);
        }

        [Fact]
        public void GetEmployeefromDB_ThrowsHelpfulError_WhenConnectionStringNotConfigured()
        {
            var previous = Environment.GetEnvironmentVariable("EMPLOYEE_DB_CONNECTION");
            try
            {
                Environment.SetEnvironmentVariable("EMPLOYEE_DB_CONNECTION", null);

                var ex = Assert.Throws<InvalidOperationException>(
                    () => EchoTool.GetEmployeefromDB(new Employee { Id = 1 }));

                Assert.Contains("EMPLOYEE_DB_CONNECTION", ex.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("EMPLOYEE_DB_CONNECTION", previous);
            }
        }

        [Fact]
        public void EmployeeDataEditUpdate_Throws_WhenIdMissing()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => EchoTool.EmployeeDataEditUpdate(new Employee { Name = "Test" }));

            Assert.Contains("Id is required", ex.Message);
        }

        [Fact]
        public void EmployeeDataEditUpdate_Throws_WhenJoiningDateFormatInvalid()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => EchoTool.EmployeeDataEditUpdate(new Employee { Id = 1, JoiningDate = "2021-01-10" }));

            Assert.Contains("MM/DD/YYYY", ex.Message);
        }

        [Fact]
        public void EmployeeDataEditUpdate_ThrowsHelpfulError_WhenConnectionStringNotConfigured()
        {
            var previous = Environment.GetEnvironmentVariable("EMPLOYEE_DB_CONNECTION");
            try
            {
                Environment.SetEnvironmentVariable("EMPLOYEE_DB_CONNECTION", null);

                var ex = Assert.Throws<InvalidOperationException>(
                    () => EchoTool.EmployeeDataEditUpdate(new Employee { Id = 1, Name = "Updated" }));

                Assert.Contains("EMPLOYEE_DB_CONNECTION", ex.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("EMPLOYEE_DB_CONNECTION", previous);
            }
        }

        [Fact]
        public void EmployeDelete_Throws_WhenIdMissing()
        {
            var ex = Assert.Throws<ArgumentException>(() => EchoTool.EmployeDelete(0));

            Assert.Contains("Id is required", ex.Message);
        }

        [Fact]
        public void EmployeDelete_ThrowsHelpfulError_WhenConnectionStringNotConfigured()
        {
            var previous = Environment.GetEnvironmentVariable("EMPLOYEE_DB_CONNECTION");
            try
            {
                Environment.SetEnvironmentVariable("EMPLOYEE_DB_CONNECTION", null);

                var ex = Assert.Throws<InvalidOperationException>(() => EchoTool.EmployeDelete(1));

                Assert.Contains("EMPLOYEE_DB_CONNECTION", ex.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("EMPLOYEE_DB_CONNECTION", previous);
            }
        }
    }
}
