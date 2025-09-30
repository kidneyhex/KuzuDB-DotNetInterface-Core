namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Integration tests that verify multiple OO wrapper classes working together.
    /// </summary>
    public class IntegrationTests : BaseKuzuTest
    {
        [Fact]
        public void CompleteWorkflow_DatabaseToQueryResults_ShouldWorkEndToEnd()
        {
            // Arrange & Act - Create database and connection
            using var database = new Database(TestDatabasePath);
            using var connection = database.CreateConnection();

            // Act - Create schema
            using (var result = connection.Query("CREATE NODE TABLE Employee(id INT64, name STRING, salary DOUBLE, active BOOLEAN, PRIMARY KEY(id))"))
            {
                Assert.True(result.IsSuccess);
            }

            // Act - Insert data using prepared statements
            using var insertStmt = connection.Prepare("CREATE (:Employee {id: $id, name: $name, salary: $salary, active: $active})");
            
            var employees = new[]
            {
                (1L, "Alice Johnson", 75000.0, true),
                (2L, "Bob Smith", 65000.0, false),
                (3L, "Carol Wilson", 85000.0, true),
                (4L, "David Brown", 70000.0, true)
            };

            foreach (var (id, name, salary, active) in employees)
            {
                insertStmt.BindInt64("id", id);
                insertStmt.BindString("name", name);
                insertStmt.BindDouble("salary", salary);
                insertStmt.BindBool("active", active);

                using var insertResult = insertStmt.Execute();
                Assert.True(insertResult.IsSuccess);
            }

            // Act - Query data with complex conditions
            using var queryResult = connection.Query(@"
                MATCH (e:Employee) 
                WHERE e.active = true AND e.salary > 70000 
                RETURN e.name, e.salary 
                ORDER BY e.salary DESC");

            // Assert - Verify results
            Assert.True(queryResult.IsSuccess);
            Assert.Equal((ulong)2, queryResult.NumTuples); // Carol and Alice
            Assert.Equal((ulong)2, queryResult.NumColumns);

            var resultData = new List<(string name, double salary)>();
            foreach (var row in queryResult)
            {
                var name = row["e.name"].GetString();
                var salary = row["e.salary"].GetDouble();
                resultData.Add((name, salary));
                row.Dispose();
            }

            Assert.Equal(2, resultData.Count);
            Assert.Equal(("Carol Wilson", 85000.0), resultData[0]); // Highest salary first
            Assert.Equal(("Alice Johnson", 75000.0), resultData[1]);
        }

        [Fact]
        public void ErrorHandling_AcrossMultipleOperations_ShouldBeConsistent()
        {
            // Arrange
            using var database = new Database(TestDatabasePath);
            using var connection = database.CreateConnection();

            // Act & Assert - Invalid schema creation
            using (var result = connection.Query("CREATE INVALID SYNTAX"))
            {
                Assert.False(result.IsSuccess);
                Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
            }

            // Act & Assert - Query on non-existent table
            using (var result = connection.Query("MATCH (n:NonExistentTable) RETURN n"))
            {
                Assert.False(result.IsSuccess);
                Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
            }

            // Act & Assert - Invalid prepared statement
            using (var stmt = connection.Prepare("INVALID PREPARE STATEMENT"))
            {
                Assert.False(stmt.IsSuccess);
                Assert.False(string.IsNullOrEmpty(stmt.ErrorMessage));
            }

            // Act & Assert - Valid operations should still work after errors
            using (var result = connection.Query("CREATE NODE TABLE ErrorTest(id INT64, PRIMARY KEY(id))"))
            {
                Assert.True(result.IsSuccess);
            }

            using (var result = connection.Query("CREATE (:ErrorTest {id: 1})"))
            {
                Assert.True(result.IsSuccess);
            }

            using (var result = connection.Query("MATCH (e:ErrorTest) RETURN COUNT(*) as count"))
            {
                Assert.True(result.IsSuccess);
                foreach (var row in result)
                {
                    Assert.Equal(1L, row["count"].GetInt64());
                    row.Dispose();
                    break;
                }
            }
        }
    }
}