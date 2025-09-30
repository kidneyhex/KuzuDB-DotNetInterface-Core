namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_query_result wrapper class and query result functionality.
    /// </summary>
    public class QueryResultTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void QueryResult_IsSuccess_WithSuccessfulQuery_ShouldReturnTrue()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name");

            // Assert
            Assert.True(kuzunet.kuzu_query_result_is_success(result));
        }

        [Fact]
        public void QueryResult_IsSuccess_WithFailedQuery_ShouldReturnFalse()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var result = ExecuteQueryExpectingFailure("INVALID QUERY");

            // Assert
            Assert.False(kuzunet.kuzu_query_result_is_success(result));
        }

        [Fact]
        public void QueryResult_GetErrorMessage_WithFailedQuery_ShouldReturnMessage()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var result = ExecuteQueryExpectingFailure("INVALID QUERY SYNTAX");

            // Assert
            var errorMessage = kuzunet.kuzu_query_result_get_error_message(result);
            Assert.False(string.IsNullOrEmpty(errorMessage));
        }

        [Fact]
        public void QueryResult_GetNumColumns_WithKnownQuery_ShouldReturnCorrectCount()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name, p.age, p.active");

            // Assert
            Assert.Equal((ulong)3, kuzunet.kuzu_query_result_get_num_columns(result));
        }

        [Fact]
        public void QueryResult_GetNumTuples_WithKnownData_ShouldReturnCorrectCount()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name");

            // Assert
            Assert.Equal((ulong)3, kuzunet.kuzu_query_result_get_num_tuples(result)); // We inserted 3 persons
        }

        [Fact]
        public void QueryResult_GetColumnName_WithValidIndex_ShouldReturnCorrectName()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name, p.age");

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 0, out string col0Name));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 1, out string col1Name));
            
            Assert.Equal("p.name", col0Name);
            Assert.Equal("p.age", col1Name);
        }

        [Fact]
        public void QueryResult_GetColumnDataType_WithValidIndex_ShouldReturnDataType()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name, p.age");
            using var nameDataType = new kuzu_logical_type();
            using var ageDataType = new kuzu_logical_type();

            var nameResult = kuzunet.kuzu_query_result_get_column_data_type(result, 0, nameDataType);
            var ageResult = kuzunet.kuzu_query_result_get_column_data_type(result, 1, ageDataType);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, nameResult);
            Assert.Equal(kuzu_state.KuzuSuccess, ageResult);
            
            Assert.Equal(kuzu_data_type_id.KUZU_STRING, kuzunet.kuzu_data_type_get_id(nameDataType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(ageDataType));
        }

        [Fact]
        public void QueryResult_HasNext_WithResults_ShouldReturnTrueInitially()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name LIMIT 1");

            // Assert
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
        }

        [Fact]
        public void QueryResult_GetNext_WithResults_ShouldReturnFlatTuple()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name ORDER BY p.id LIMIT 1");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            
            using var flatTuple = new kuzu_flat_tuple();
            var getNextResult = kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, getNextResult);
        }

        [Fact]
        public void QueryResult_ResetIterator_ShouldAllowMultipleIterations()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name ORDER BY p.id");
            
            // First iteration
            var firstPassCount = 0;
            while (kuzunet.kuzu_query_result_has_next(result))
            {
                using var flatTuple = new kuzu_flat_tuple();
                kuzunet.kuzu_query_result_get_next(result, flatTuple);
                firstPassCount++;
            }

            // Reset and second iteration
            kuzunet.kuzu_query_result_reset_iterator(result);
            var secondPassCount = 0;
            while (kuzunet.kuzu_query_result_has_next(result))
            {
                using var flatTuple = new kuzu_flat_tuple();
                kuzunet.kuzu_query_result_get_next(result, flatTuple);
                secondPassCount++;
            }

            // Assert
            Assert.Equal(3, firstPassCount);
            Assert.Equal(3, secondPassCount);
        }

        [Fact]
        public void QueryResult_ToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name LIMIT 1");
            var stringResult = kuzunet.kuzu_query_result_to_string(result);

            // Assert
            Assert.False(string.IsNullOrEmpty(stringResult));
        }

        [Fact]
        public void QueryResult_GetQuerySummary_ShouldReturnSummaryInfo()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name");
            using var summary = new kuzu_query_summary();
            var summaryResult = kuzunet.kuzu_query_result_get_query_summary(result, summary);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, summaryResult);
            
            var compilingTime = kuzunet.kuzu_query_summary_get_compiling_time(summary);
            var executionTime = kuzunet.kuzu_query_summary_get_execution_time(summary);
            
            Assert.True(compilingTime >= 0);
            Assert.True(executionTime >= 0);
        }

        [Fact]
        public void QueryResult_WithEmptyResult_ShouldHaveZeroTuples()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) WHERE p.id = 999 RETURN p.name");

            // Assert
            Assert.True(kuzunet.kuzu_query_result_is_success(result));
            Assert.Equal((ulong)0, kuzunet.kuzu_query_result_get_num_tuples(result));
            Assert.False(kuzunet.kuzu_query_result_has_next(result));
        }

        [Fact]
        public void QueryResult_AggregationQuery_ShouldReturnCorrectResults()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN COUNT(*) AS person_count");

            // Assert
            Assert.True(kuzunet.kuzu_query_result_is_success(result));
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_columns(result));
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(result));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_name(result, 0, out string colName));
            Assert.Equal("person_count", colName);
        }

        [Fact]
        public void QueryResult_Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var result = ExecuteQuery("MATCH (p:Person) RETURN p.name");

            // Act & Assert (should not throw)
            result.Dispose();
        }

        [Fact]
        public void QueryResult_MultipleDispose_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var result = ExecuteQuery("MATCH (p:Person) RETURN p.name");

            // Act & Assert (should not throw)
            result.Dispose();
            result.Dispose(); // Should not throw
        }
    }
}