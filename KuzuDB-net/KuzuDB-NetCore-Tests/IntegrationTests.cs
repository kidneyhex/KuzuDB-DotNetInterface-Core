namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Integration tests that verify multiple NetCore wrapper classes working together.
    /// </summary>
    public class IntegrationTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void CompleteWorkflow_DatabaseToQueryResults_ShouldWorkEndToEnd()
        {
            // Arrange & Act - Create database and connection
            SetupTestDatabase();

            // Act - Create schema
            using var schemaResult = ExecuteQuery("CREATE NODE TABLE Employee(id INT64, name STRING, salary DOUBLE, active BOOLEAN, PRIMARY KEY(id))");
            Assert.True(kuzunet.kuzu_query_result_is_success(schemaResult));

            // Act - Insert data using prepared statements
            using var insertStmt = new kuzu_prepared_statement();
            var prepareResult = kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Employee {id: $id, name: $name, salary: $salary, active: $active})", insertStmt);
            Assert.Equal(kuzu_state.KuzuSuccess, prepareResult);
            Assert.True(kuzunet.kuzu_prepared_statement_is_success(insertStmt));
            
            var employees = new[]
            {
                (1L, "Alice Johnson", 75000.0, true),
                (2L, "Bob Smith", 65000.0, false),
                (3L, "Carol Wilson", 85000.0, true),
                (4L, "David Brown", 70000.0, true)
            };

            foreach (var (id, name, salary, active) in employees)
            {
                kuzunet.kuzu_prepared_statement_bind_int64(insertStmt, "id", id);
                kuzunet.kuzu_prepared_statement_bind_string(insertStmt, "name", name);
                kuzunet.kuzu_prepared_statement_bind_double(insertStmt, "salary", salary);
                kuzunet.kuzu_prepared_statement_bind_bool(insertStmt, "active", active);

                using var insertResult = new kuzu_query_result();
                var executeResult = kuzunet.kuzu_connection_execute(TestConnection!, insertStmt, insertResult);
                Assert.Equal(kuzu_state.KuzuSuccess, executeResult);
                Assert.True(kuzunet.kuzu_query_result_is_success(insertResult));
            }

            // Act - Query data with complex conditions
            using var queryResult = ExecuteQuery(@"
                MATCH (e:Employee) 
                WHERE e.active = true AND e.salary > 70000 
                RETURN e.name, e.salary 
                ORDER BY e.salary DESC");

            // Assert - Verify results
            Assert.True(kuzunet.kuzu_query_result_is_success(queryResult));
            Assert.Equal((ulong)2, kuzunet.kuzu_query_result_get_num_tuples(queryResult)); // Carol and Alice
            Assert.Equal((ulong)2, kuzunet.kuzu_query_result_get_num_columns(queryResult));

            var resultData = new List<(string name, double salary)>();
            while (kuzunet.kuzu_query_result_has_next(queryResult))
            {
                using var flatTuple = new kuzu_flat_tuple();
                kuzunet.kuzu_query_result_get_next(queryResult, flatTuple);
                
                using var nameValue = new kuzu_value();
                using var salaryValue = new kuzu_value();
                
                kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, nameValue);
                kuzunet.kuzu_flat_tuple_get_value(flatTuple, 1, salaryValue);
                
                kuzunet.kuzu_value_get_string(nameValue, out string name);
                kuzunet.kuzu_value_get_double(salaryValue, out double salary);
                
                resultData.Add((name, salary));
            }

            Assert.Equal(2, resultData.Count);
            Assert.Equal(("Carol Wilson", 85000.0), resultData[0]); // Highest salary first
            Assert.Equal(("Alice Johnson", 75000.0), resultData[1]);
        }

        [Fact]
        public void ErrorHandling_AcrossMultipleOperations_ShouldBeConsistent()
        {
            // Arrange
            SetupTestDatabase();

            // Act & Assert - Invalid schema creation
            using var invalidSchemaResult = ExecuteQueryExpectingFailure("CREATE INVALID SYNTAX");
            Assert.False(kuzunet.kuzu_query_result_is_success(invalidSchemaResult));
            Assert.False(string.IsNullOrEmpty(kuzunet.kuzu_query_result_get_error_message(invalidSchemaResult)));

            // Act & Assert - Query on non-existent table
            using var invalidQueryResult = ExecuteQueryExpectingFailure("MATCH (n:NonExistentTable) RETURN n");
            Assert.False(kuzunet.kuzu_query_result_is_success(invalidQueryResult));
            Assert.False(string.IsNullOrEmpty(kuzunet.kuzu_query_result_get_error_message(invalidQueryResult)));

            // Act & Assert - Invalid prepared statement
            using var invalidStmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, "INVALID PREPARE STATEMENT", invalidStmt);
            Assert.False(kuzunet.kuzu_prepared_statement_is_success(invalidStmt));
            Assert.False(string.IsNullOrEmpty(kuzunet.kuzu_prepared_statement_get_error_message(invalidStmt)));

            // Act & Assert - Valid operations should still work after errors
            using var validResult1 = ExecuteQuery("CREATE NODE TABLE ErrorTest(id INT64, PRIMARY KEY(id))");
            Assert.True(kuzunet.kuzu_query_result_is_success(validResult1));

            using var validResult2 = ExecuteQuery("CREATE (:ErrorTest {id: 1})");
            Assert.True(kuzunet.kuzu_query_result_is_success(validResult2));

            using var validResult3 = ExecuteQuery("MATCH (e:ErrorTest) RETURN COUNT(*) as count");
            Assert.True(kuzunet.kuzu_query_result_is_success(validResult3));
            
            // Verify count result
            Assert.True(kuzunet.kuzu_query_result_has_next(validResult3));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(validResult3, flatTuple);
            using var countValue = new kuzu_value();
            kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, countValue);
            kuzunet.kuzu_value_get_int64(countValue, out long count);
            Assert.Equal(1L, count);
        }

        [Fact]
        public void ComplexDataTypes_AndOperations_ShouldWorkTogether()
        {
            // Arrange
            SetupTestDatabase();

            // Create schema with various data types
            ExecuteQuery(@"CREATE NODE TABLE ComplexTest(
                id INT64, 
                name STRING, 
                age INT32, 
                score DOUBLE, 
                rating FLOAT,
                active BOOLEAN, 
                created_date DATE,
                PRIMARY KEY(id))");

            // Act - Insert data with various types using prepared statement
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                @"CREATE (:ComplexTest {
                    id: $id, name: $name, age: $age, score: $score, 
                    rating: $rating, active: $active, created_date: $created_date})", stmt);

            // Bind various data types
            kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 1L);
            kuzunet.kuzu_prepared_statement_bind_string(stmt, "name", "Test User");
            kuzunet.kuzu_prepared_statement_bind_int32(stmt, "age", 25);
            kuzunet.kuzu_prepared_statement_bind_double(stmt, "score", 95.75);
            kuzunet.kuzu_prepared_statement_bind_float(stmt, "rating", 4.5f);
            kuzunet.kuzu_prepared_statement_bind_bool(stmt, "active", true);
            
            using var date = new kuzu_date_t();
            kuzunet.kuzu_prepared_statement_bind_date(stmt, "created_date", date);

            using var insertResult = new kuzu_query_result();
            var executeResult = kuzunet.kuzu_connection_execute(TestConnection!, stmt, insertResult);
            Assert.Equal(kuzu_state.KuzuSuccess, executeResult);

            // Query and verify all data types
            using var queryResult = ExecuteQuery("MATCH (c:ComplexTest) RETURN c.id, c.name, c.age, c.score, c.rating, c.active");
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(queryResult));
            Assert.Equal((ulong)6, kuzunet.kuzu_query_result_get_num_columns(queryResult));

            // Verify data types from query result
            using var idType = new kuzu_logical_type();
            using var nameType = new kuzu_logical_type();
            using var ageType = new kuzu_logical_type();
            using var scoreType = new kuzu_logical_type();
            using var ratingType = new kuzu_logical_type();
            using var activeType = new kuzu_logical_type();

            kuzunet.kuzu_query_result_get_column_data_type(queryResult, 0, idType);
            kuzunet.kuzu_query_result_get_column_data_type(queryResult, 1, nameType);
            kuzunet.kuzu_query_result_get_column_data_type(queryResult, 2, ageType);
            kuzunet.kuzu_query_result_get_column_data_type(queryResult, 3, scoreType);
            kuzunet.kuzu_query_result_get_column_data_type(queryResult, 4, ratingType);
            kuzunet.kuzu_query_result_get_column_data_type(queryResult, 5, activeType);

            Assert.Equal(kuzu_data_type_id.KUZU_INT64, kuzunet.kuzu_data_type_get_id(idType));
            Assert.Equal(kuzu_data_type_id.KUZU_STRING, kuzunet.kuzu_data_type_get_id(nameType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(ageType));
            Assert.Equal(kuzu_data_type_id.KUZU_DOUBLE, kuzunet.kuzu_data_type_get_id(scoreType));
            Assert.Equal(kuzu_data_type_id.KUZU_FLOAT, kuzunet.kuzu_data_type_get_id(ratingType));
            Assert.Equal(kuzu_data_type_id.KUZU_BOOL, kuzunet.kuzu_data_type_get_id(activeType));
        }

        [Fact]
        public void MultipleConnections_ShouldWorkIndependently()
        {
            // Arrange
            var systemConfig1 = kuzunet.kuzu_default_system_config();
            var systemConfig2 = kuzunet.kuzu_default_system_config();
            var database1 = new kuzu_database();
            var database2 = new kuzu_database();
            var connection1 = new kuzu_connection();
            var connection2 = new kuzu_connection();

            var path1 = TestDatabasePath + "_conn1";
            var path2 = TestDatabasePath + "_conn2";

            try
            {
                // Act - Initialize two separate database connections
                kuzunet.kuzu_database_init(path1, systemConfig1, database1);
                kuzunet.kuzu_database_init(path2, systemConfig2, database2);
                kuzunet.kuzu_connection_init(database1, connection1);
                kuzunet.kuzu_connection_init(database2, connection2);

                // Create different schemas in each database
                using var result1 = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection1, "CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id))", result1);
                Assert.True(kuzunet.kuzu_query_result_is_success(result1));

                using var result2 = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection2, "CREATE NODE TABLE Company(id INT64, name STRING, PRIMARY KEY(id))", result2);
                Assert.True(kuzunet.kuzu_query_result_is_success(result2));

                // Insert different data in each database
                using var insertResult1 = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection1, "CREATE (:Person {id: 1, name: 'Alice'})", insertResult1);
                Assert.True(kuzunet.kuzu_query_result_is_success(insertResult1));

                using var insertResult2 = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection2, "CREATE (:Company {id: 1, name: 'TechCorp'})", insertResult2);
                Assert.True(kuzunet.kuzu_query_result_is_success(insertResult2));

                // Verify each database has its own data
                using var queryResult1 = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection1, "MATCH (p:Person) RETURN COUNT(*) as count", queryResult1);
                Assert.True(kuzunet.kuzu_query_result_is_success(queryResult1));

                using var queryResult2 = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection2, "MATCH (c:Company) RETURN COUNT(*) as count", queryResult2);
                Assert.True(kuzunet.kuzu_query_result_is_success(queryResult2));

                // Both should have 1 record each
                Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(queryResult1));
                Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(queryResult2));
            }
            finally
            {
                // Cleanup
                connection1?.Dispose();
                connection2?.Dispose();
                database1?.Dispose();
                database2?.Dispose();
                systemConfig1?.Dispose();
                systemConfig2?.Dispose();
            }
        }

        [Fact]
        public void QuerySummary_AndPerformanceMetrics_ShouldProvideInsights()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act - Execute a complex query
            using var result = ExecuteQuery(@"
                MATCH (p:Person)-[w:WorksFor]->(c:Company) 
                WHERE p.active = true 
                RETURN p.name, c.name, w.salary 
                ORDER BY w.salary DESC");

            // Get query summary
            using var summary = new kuzu_query_summary();
            var summaryResult = kuzunet.kuzu_query_result_get_query_summary(result, summary);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, summaryResult);
            
            var compilingTime = kuzunet.kuzu_query_summary_get_compiling_time(summary);
            var executionTime = kuzunet.kuzu_query_summary_get_execution_time(summary);
            
            Assert.True(compilingTime >= 0, "Compiling time should be non-negative");
            Assert.True(executionTime >= 0, "Execution time should be non-negative");
        }
    }
}