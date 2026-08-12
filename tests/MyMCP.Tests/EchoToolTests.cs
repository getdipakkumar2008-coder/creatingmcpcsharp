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
    }
}
