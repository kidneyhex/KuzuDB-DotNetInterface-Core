namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the Connection class.
    /// </summary>
    public class ConnectionTests : BaseKuzuTest
    {
        [Fact]
        public void Query_WithValidQuery_ShouldReturnSuccessfulResult()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestNode(id INT64, PRIMARY KEY(id))");
            ExecuteQuery("CREATE (:TestNode {id: 1})");

            // Act
            using var result = TestConnection!.Query("MATCH (n:TestNode) RETURN n.id");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal((ulong)1, result.NumTuples);
        }

        [Fact]
        public void Query_WithInvalidQuery_ShouldReturnFailedResult()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var result = TestConnection!.Query("INVALID QUERY SYNTAX");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public void Query_WithNullQuery_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => TestConnection!.Query(null!));
            Assert.Contains("Query cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Query_WithEmptyQuery_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => TestConnection!.Query(""));
            Assert.Contains("Query cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Prepare_WithValidQuery_ShouldReturnPreparedStatement()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestNode(id INT64, name STRING, PRIMARY KEY(id))");

            // Act
            using var stmt = TestConnection!.Prepare("CREATE (:TestNode {id: $id, name: $name})");

            // Assert
            Assert.NotNull(stmt);
            Assert.True(stmt.IsSuccess);
        }

        [Fact]
        public void Prepare_WithInvalidQuery_ShouldReturnFailedPreparedStatement()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var stmt = TestConnection!.Prepare("INVALID PREPARE SYNTAX");

            // Assert
            Assert.NotNull(stmt);
            Assert.False(stmt.IsSuccess);
            Assert.False(string.IsNullOrEmpty(stmt.ErrorMessage));
        }

        [Fact]
        public void Prepare_WithNullQuery_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => TestConnection!.Prepare(null!));
            Assert.Contains("Query cannot be null or empty", exception.Message);
        }

        [Fact]
        public void SetMaxNumThreadsForExecution_WithValidValue_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert (should not throw)
            TestConnection!.SetMaxNumThreadsForExecution(4);
        }

        [Fact]
        public void GetMaxNumThreadsForExecution_ShouldReturnPositiveValue()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            var numThreads = TestConnection!.GetMaxNumThreadsForExecution();

            // Assert
            Assert.True(numThreads > 0);
        }

        [Fact]
        public void SetAndGetMaxNumThreadsForExecution_ShouldWorkTogether()
        {
            // Arrange
            SetupTestDatabase();
            const ulong expectedThreads = 2;

            // Act
            TestConnection!.SetMaxNumThreadsForExecution(expectedThreads);
            var actualThreads = TestConnection.GetMaxNumThreadsForExecution();

            // Assert
            Assert.Equal(expectedThreads, actualThreads);
        }

        [Fact]
        public void SetQueryTimeout_WithValidValue_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert (should not throw)
            TestConnection!.SetQueryTimeout(5000); // 5 seconds
        }

        [Fact]
        public void Interrupt_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert (should not throw)
            TestConnection!.Interrupt();
        }

        [Fact]
        public void Database_Property_ShouldReturnCorrectDatabase()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            var database = TestConnection!.Database;

            // Assert
            Assert.Equal(TestDatabase, database);
        }

        [Fact]
        public void Query_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            SetupTestDatabase();
            TestConnection!.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => TestConnection.Query("MATCH (n) RETURN n"));
        }

        [Fact]
        public void Prepare_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            SetupTestDatabase();
            TestConnection!.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => TestConnection.Prepare("MATCH (n) RETURN n"));
        }

        [Fact]
        public void SetMaxNumThreadsForExecution_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            SetupTestDatabase();
            TestConnection!.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => TestConnection.SetMaxNumThreadsForExecution(1));
        }

        [Fact]
        public void ComplexQuery_WithMultipleOperations_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query(@"
                MATCH (p:Person)-[w:WorksFor]->(c:Company) 
                WHERE p.active = true AND c.revenue > 500000 
                RETURN p.name, p.age, c.name, w.position, w.salary 
                ORDER BY w.salary DESC");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.True(result.NumTuples > 0);
            Assert.Equal((ulong)5, result.NumColumns);
        }

        [Fact]
        public void Query_WithSchemaOperations_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert - Create table
            using (var result = TestConnection!.Query("CREATE NODE TABLE TestTable(id INT64, value STRING, PRIMARY KEY(id))"))
            {
                Assert.True(result.IsSuccess);
            }

            // Act & Assert - Insert data
            using (var result = TestConnection.Query("CREATE (:TestTable {id: 1, value: 'test'})"))
            {
                Assert.True(result.IsSuccess);
            }

            // Act & Assert - Query data
            using (var result = TestConnection.Query("MATCH (t:TestTable) RETURN t.id, t.value"))
            {
                Assert.True(result.IsSuccess);
                Assert.Equal((ulong)1, result.NumTuples);
            }
        }
    }
}