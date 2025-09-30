namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_connection wrapper class and connection functionality.
    /// </summary>
    public class ConnectionTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void Connection_Initialization_WithValidDatabase_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();

            // Assert - Setup should have succeeded
            Assert.NotNull(TestConnection);
        }

        [Fact]
        public void Connection_Query_WithValidQuery_ShouldReturnSuccessfulResult()
        {
            // Arrange
            SetupTestDatabaseWithSchema();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN COUNT(*) as count");

            // Assert
            Assert.True(kuzunet.kuzu_query_result_is_success(result));
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(result)); // COUNT(*) always returns 1 tuple with the count result
        }

        [Fact]
        public void Connection_Query_WithInvalidQuery_ShouldReturnFailedResult()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var result = ExecuteQueryExpectingFailure("INVALID QUERY SYNTAX");

            // Assert
            Assert.False(kuzunet.kuzu_query_result_is_success(result));
            
            var errorMessage = kuzunet.kuzu_query_result_get_error_message(result);
            Assert.False(string.IsNullOrEmpty(errorMessage));
        }

        [Fact]
        public void Connection_SetMaxNumThreadsForExecution_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            const ulong expectedThreads = 4;

            // Act
            var result = kuzunet.kuzu_connection_set_max_num_thread_for_exec(TestConnection!, expectedThreads);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void Connection_GetMaxNumThreadsForExecution_ShouldReturnPositiveValue()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            var result = kuzunet.kuzu_connection_get_max_num_thread_for_exec(TestConnection!, out ulong numThreads);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
            Assert.True(numThreads > 0);
        }

        [Fact]
        public void Connection_SetAndGetMaxNumThreads_ShouldWorkTogether()
        {
            // Arrange
            SetupTestDatabase();
            const ulong expectedThreads = 2;

            // Act
            var setResult = kuzunet.kuzu_connection_set_max_num_thread_for_exec(TestConnection!, expectedThreads);
            var getResult = kuzunet.kuzu_connection_get_max_num_thread_for_exec(TestConnection!, out ulong actualThreads);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, setResult);
            Assert.Equal(kuzu_state.KuzuSuccess, getResult);
            Assert.Equal(expectedThreads, actualThreads);
        }

        [Fact]
        public void Connection_SetQueryTimeout_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            const ulong timeoutMs = 5000;

            // Act
            var result = kuzunet.kuzu_connection_set_query_timeout(TestConnection!, timeoutMs);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void Connection_Interrupt_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert (should not throw)
            kuzunet.kuzu_connection_interrupt(TestConnection!);
        }

        [Fact]
        public void Connection_SchemaCreation_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert
            using var result1 = ExecuteQuery("CREATE NODE TABLE TestTable(id INT64, name STRING, PRIMARY KEY(id))");
            Assert.True(kuzunet.kuzu_query_result_is_success(result1));

            using var result2 = ExecuteQuery("CREATE (:TestTable {id: 1, name: 'test'})");
            Assert.True(kuzunet.kuzu_query_result_is_success(result2));

            using var result3 = ExecuteQuery("MATCH (t:TestTable) RETURN t.id, t.name");
            Assert.True(kuzunet.kuzu_query_result_is_success(result3));
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(result3));
            Assert.Equal((ulong)2, kuzunet.kuzu_query_result_get_num_columns(result3));
        }

        [Fact]
        public void Connection_ComplexQuery_WithJoins_ShouldReturnCorrectStructure()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery(@"
                MATCH (p:Person)-[w:WorksFor]->(c:Company) 
                RETURN p.name, p.age, c.name, w.position, w.salary 
                ORDER BY p.id");

            // Assert
            Assert.True(kuzunet.kuzu_query_result_is_success(result));
            Assert.Equal((ulong)5, kuzunet.kuzu_query_result_get_num_columns(result));
            Assert.Equal((ulong)3, kuzunet.kuzu_query_result_get_num_tuples(result)); // 3 work relationships

            // Check column names
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 0, out string col0));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 1, out string col1));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 2, out string col2));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 3, out string col3));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 4, out string col4));

            Assert.Equal("p.name", col0);
            Assert.Equal("p.age", col1);
            Assert.Equal("c.name", col2);
            Assert.Equal("w.position", col3);
            Assert.Equal("w.salary", col4);
        }

        [Fact]
        public void Connection_Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabase();
            var connection = TestConnection;

            // Act
            connection!.Dispose();

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public void Connection_MultipleDispose_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabase();
            var connection = TestConnection;

            // Act & Assert (should not throw)
            connection!.Dispose();
            connection.Dispose(); // Should not throw
        }
    }
}