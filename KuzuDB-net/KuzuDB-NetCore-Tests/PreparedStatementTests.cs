namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_prepared_statement wrapper class and prepared statement functionality.
    /// </summary>
    public class PreparedStatementTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void PreparedStatement_Prepare_WithValidQuery_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();

            // Act
            using var stmt = new kuzu_prepared_statement();
            var result = kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
            Assert.True(kuzunet.kuzu_prepared_statement_is_success(stmt));
        }

        [Fact]
        public void PreparedStatement_Prepare_WithInvalidQuery_ShouldReturnFailedStatement()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, "INVALID PREPARE SYNTAX", stmt);

            // Assert
            Assert.False(kuzunet.kuzu_prepared_statement_is_success(stmt));
            
            var errorMessage = kuzunet.kuzu_prepared_statement_get_error_message(stmt);
            Assert.False(string.IsNullOrEmpty(errorMessage));
        }

        [Fact]
        public void PreparedStatement_BindBool_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act
            var result = kuzunet.kuzu_prepared_statement_bind_bool(stmt, "active", true);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_BindInt64_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act
            var result = kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 123L);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_BindInt32_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act
            var result = kuzunet.kuzu_prepared_statement_bind_int32(stmt, "age", 30);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_BindDouble_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Company {id: $id, name: $name, revenue: $revenue})", stmt);

            // Act
            var result = kuzunet.kuzu_prepared_statement_bind_double(stmt, "revenue", 123456.789);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_BindFloat_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "MATCH (p:Person {id: 1}), (c:Company {id: 1}) CREATE (p)-[:WorksFor {position: $position, salary: $salary}]->(c)", stmt);

            // Act
            var result = kuzunet.kuzu_prepared_statement_bind_float(stmt, "salary", 75000.0f);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_BindString_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act
            var result = kuzunet.kuzu_prepared_statement_bind_string(stmt, "name", "John Doe");

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_Execute_WithBoundParameters_ShouldInsertData()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act - Bind parameters
            kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 1L);
            kuzunet.kuzu_prepared_statement_bind_string(stmt, "name", "John Doe");
            kuzunet.kuzu_prepared_statement_bind_int32(stmt, "age", 30);
            kuzunet.kuzu_prepared_statement_bind_bool(stmt, "active", true);

            // Execute
            using var result = new kuzu_query_result();
            var executeResult = kuzunet.kuzu_connection_execute(TestConnection!, stmt, result);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, executeResult);
            Assert.True(kuzunet.kuzu_query_result_is_success(result));

            // Verify data was inserted
            using var queryResult = ExecuteQuery("MATCH (p:Person) WHERE p.id = 1 RETURN p.name, p.age, p.active");
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(queryResult));
        }

        [Fact]
        public void PreparedStatement_ExecuteMultipleTimes_WithDifferentParameters_ShouldInsertMultipleRecords()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            var testData = new[]
            {
                (1L, "Alice", 25, true),
                (2L, "Bob", 30, false),
                (3L, "Charlie", 35, true)
            };

            // Act
            foreach (var (id, name, age, active) in testData)
            {
                kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", id);
                kuzunet.kuzu_prepared_statement_bind_string(stmt, "name", name);
                kuzunet.kuzu_prepared_statement_bind_int32(stmt, "age", age);
                kuzunet.kuzu_prepared_statement_bind_bool(stmt, "active", active);

                using var result = new kuzu_query_result();
                var executeResult = kuzunet.kuzu_connection_execute(TestConnection!, stmt, result);
                Assert.Equal(kuzu_state.KuzuSuccess, executeResult);
                Assert.True(kuzunet.kuzu_query_result_is_success(result));
            }

            // Assert
            using var queryResult = ExecuteQuery("MATCH (p:Person) RETURN COUNT(*) as count");
            Assert.Equal((ulong)1, kuzunet.kuzu_query_result_get_num_tuples(queryResult));
            
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(queryResult, flatTuple);
            using var countValue = new kuzu_value();
            kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, countValue);
            kuzunet.kuzu_value_get_int64(countValue, out long count);
            Assert.Equal(3L, count);
        }

        [Theory]
        [InlineData((sbyte)-128)]
        [InlineData((sbyte)0)]
        [InlineData((sbyte)127)]
        public void PreparedStatement_BindInt8_WithDifferentValues_ShouldWorkCorrectly(sbyte testValue)
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestInt8(id INT64, value INT8, PRIMARY KEY(id))");
            
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:TestInt8 {id: $id, value: $value})", stmt);

            // Act
            kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 1L);
            var result = kuzunet.kuzu_prepared_statement_bind_int8(stmt, "value", testValue);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Theory]
        [InlineData((short)-32768)]
        [InlineData((short)0)]
        [InlineData((short)32767)]
        public void PreparedStatement_BindInt16_WithDifferentValues_ShouldWorkCorrectly(short testValue)
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestInt16(id INT64, value INT16, PRIMARY KEY(id))");
            
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:TestInt16 {id: $id, value: $value})", stmt);

            // Act
            kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 1L);
            var result = kuzunet.kuzu_prepared_statement_bind_int16(stmt, "value", testValue);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)127)]
        [InlineData((byte)255)]
        public void PreparedStatement_BindUInt8_WithDifferentValues_ShouldWorkCorrectly(byte testValue)
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestUInt8(id INT64, value UINT8, PRIMARY KEY(id))");
            
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:TestUInt8 {id: $id, value: $value})", stmt);

            // Act
            kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 1L);
            var result = kuzunet.kuzu_prepared_statement_bind_uint8(stmt, "value", testValue);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_BindValue_WithKuzuValue_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act
            using var nameValue = kuzunet.kuzu_value_create_string("Test Name");
            var result = kuzunet.kuzu_prepared_statement_bind_value(stmt, "name", nameValue);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);
        }

        [Fact]
        public void PreparedStatement_QueryStatement_WithSelectQuery_ShouldReturnResults()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "MATCH (p:Person) WHERE p.age > $minAge RETURN p.name, p.age ORDER BY p.age", stmt);

            // Act
            kuzunet.kuzu_prepared_statement_bind_int32(stmt, "minAge", 25);
            
            using var result = new kuzu_query_result();
            var executeResult = kuzunet.kuzu_connection_execute(TestConnection!, stmt, result);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, executeResult);
            Assert.True(kuzunet.kuzu_query_result_is_success(result));
            Assert.True(kuzunet.kuzu_query_result_get_num_tuples(result) > 0);
        }

        [Fact]
        public void PreparedStatement_Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act & Assert (should not throw)
            stmt.Dispose();
        }

        [Fact]
        public void PreparedStatement_MultipleDispose_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabaseWithSchema();
            var stmt = new kuzu_prepared_statement();
            kuzunet.kuzu_connection_prepare(TestConnection!, 
                "CREATE (:Person {id: $id, name: $name, age: $age, active: $active})", stmt);

            // Act & Assert (should not throw)
            stmt.Dispose();
            stmt.Dispose(); // Should not throw
        }
    }
}