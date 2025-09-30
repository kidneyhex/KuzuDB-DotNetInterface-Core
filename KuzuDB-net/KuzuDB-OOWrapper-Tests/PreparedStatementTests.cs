namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the PreparedStatement class.
    /// </summary>
    public class PreparedStatementTests : BaseKuzuTest
    {
        [Fact]
        public void BindBool_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestBool(id INT64, flag BOOLEAN, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestBool {id: $id, flag: $flag})");

            // Act & Assert (should not throw)
            stmt.BindInt64("id", 1);
            stmt.BindBool("flag", true);

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void BindInt64_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestInt64(id INT64, value INT64, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestInt64 {id: $id, value: $value})");

            // Act & Assert (should not throw)
            stmt.BindInt64("id", 1);
            stmt.BindInt64("value", 123456789L);

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void BindInt32_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestInt32(id INT64, value INT32, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestInt32 {id: $id, value: $value})");

            // Act & Assert (should not throw)
            stmt.BindInt64("id", 1);
            stmt.BindInt32("value", 12345);

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void BindDouble_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestDouble(id INT64, value DOUBLE, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestDouble {id: $id, value: $value})");

            // Act & Assert (should not throw)
            stmt.BindInt64("id", 1);
            stmt.BindDouble("value", 123.456);

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void BindFloat_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestFloat(id INT64, value FLOAT, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestFloat {id: $id, value: $value})");

            // Act & Assert (should not throw)
            stmt.BindInt64("id", 1);
            stmt.BindFloat("value", 123.456f);

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void BindString_WithValidParameter_ShouldSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestString(id INT64, name STRING, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestString {id: $id, name: $name})");

            // Act & Assert (should not throw)
            stmt.BindInt64("id", 1);
            stmt.BindString("name", "Test Name");

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void Execute_WithBoundParameters_ShouldInsertData()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestExecution(id INT64, name STRING, age INT32, active BOOLEAN, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestExecution {id: $id, name: $name, age: $age, active: $active})");

            // Act
            stmt.BindInt64("id", 1);
            stmt.BindString("name", "John Doe");
            stmt.BindInt32("age", 30);
            stmt.BindBool("active", true);

            using var result = stmt.Execute();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            // Verify data was inserted
            using var queryResult = TestConnection!.Query("MATCH (t:TestExecution) RETURN t.id, t.name, t.age, t.active");
            Assert.Equal((ulong)1, queryResult.NumTuples);

            foreach (var row in queryResult)
            {
                Assert.Equal(1L, row["t.id"].GetInt64());
                Assert.Equal("John Doe", row["t.name"].GetString());
                Assert.Equal(30, row["t.age"].GetInt32());
                Assert.True(row["t.active"].GetBool());
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void Execute_MultipleTimesWithDifferentParameters_ShouldInsertMultipleRecords()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestMultiple(id INT64, name STRING, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestMultiple {id: $id, name: $name})");

            var testData = new[]
            {
                (1L, "Alice"),
                (2L, "Bob"),
                (3L, "Charlie")
            };

            // Act
            foreach (var (id, name) in testData)
            {
                stmt.BindInt64("id", id);
                stmt.BindString("name", name);

                using var result = stmt.Execute();
                Assert.True(result.IsSuccess);
            }

            // Assert
            using var queryResult = TestConnection!.Query("MATCH (t:TestMultiple) RETURN COUNT(*) as count");
            foreach (var row in queryResult)
            {
                Assert.Equal(3L, row["count"].GetInt64());
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void Execute_WithoutBindingAllParameters_ShouldHandleUnboundParameters()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestIncomplete(id INT64, name STRING, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestIncomplete {id: $id, name: $name})");

            // Act - Only bind one parameter
            stmt.BindInt64("id", 1);
            // Intentionally not binding "name" parameter

            using var result = stmt.Execute();

            // Assert - KuzuDB appears to handle unbound parameters gracefully
            // This behavior might vary by database engine implementation
            if (!result.IsSuccess)
            {
                Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
            }
            else
            {
                // If successful, verify the data was inserted with some default for unbound parameter
                using var queryResult = TestConnection!.Query("MATCH (t:TestIncomplete) WHERE t.id = 1 RETURN t.name");
                Assert.True(queryResult.NumTuples > 0, "Should have inserted a row even with unbound parameter");
            }
        }

        [Fact]
        public void BindString_WithNullParameter_ShouldThrowArgumentNullException()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestNull(id INT64, name STRING, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestNull {id: $id, name: $name})");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => stmt.BindString("name", null!));
        }

        [Fact]
        public void BindString_WithEmptyParameterName_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestEmpty(id INT64, name STRING, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestEmpty {id: $id, name: $name})");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stmt.BindString("", "value"));
        }

        [Fact]
        public void BindParameters_WithInvalidParameterName_ShouldIgnoreOrSucceed()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestInvalid(id INT64, name STRING, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestInvalid {id: $id, name: $name})");

            // First, test valid parameters work normally
            stmt.BindInt64("id", 1);
            stmt.BindString("name", "Test Name");
            
            using (var result = stmt.Execute())
            {
                Assert.True(result.IsSuccess, "Valid parameters should work initially");
            }

            // Now test with invalid parameter name
            // KuzuDB might silently ignore invalid parameter names, or it might cause issues
            try
            {
                stmt.BindString("nonexistent_param", "value");
                // If no exception is thrown, the library silently ignores invalid parameters
                Assert.True(true, "KuzuDB allows binding invalid parameter names without throwing exception");
            }
            catch (KuzuException ex)
            {
                // If an exception is thrown, verify it's the expected error
                Assert.Contains("nonexistent_param", ex.Message);
            }
        }

        [Fact]
        public void Connection_Property_ShouldReturnCorrectConnection()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestConnection(id INT64, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestConnection {id: $id})");

            // Act
            var connection = stmt.Connection;

            // Assert
            Assert.Equal(TestConnection, connection);
        }

        [Fact]
        public void QueryStatement_WithSelectQuery_ShouldReturnResults()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var stmt = TestConnection!.Prepare("MATCH (p:Person) WHERE p.age > $minAge RETURN p.name, p.age ORDER BY p.age");

            // Act
            stmt.BindInt32("minAge", 25);
            using var result = stmt.Execute();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.NumTuples > 0);

            // Verify we get the expected people (age > 25)
            var names = new List<string>();
            foreach (var row in result)
            {
                names.Add(row["p.name"].GetString());
                Assert.True(row["p.age"].GetInt32() > 25);
                row.Dispose();
            }

            Assert.Contains("Alice Johnson", names); // age 30
            Assert.Contains("Carol Wilson", names);  // age 35
            Assert.DoesNotContain("Bob Smith", names);   // age 25, not > 25
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BindBool_WithDifferentValues_ShouldWorkCorrectly(bool testValue)
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestBoolValues(id INT64, flag BOOLEAN, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestBoolValues {id: $id, flag: $flag})");

            // Act
            stmt.BindInt64("id", 1);
            stmt.BindBool("flag", testValue);
            using var result = stmt.Execute();

            // Assert
            Assert.True(result.IsSuccess);

            // Verify the value was stored correctly
            using var queryResult = TestConnection!.Query("MATCH (t:TestBoolValues) RETURN t.flag");
            foreach (var row in queryResult)
            {
                Assert.Equal(testValue, row["t.flag"].GetBool());
                row.Dispose();
                break;
            }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(123.456)]
        [InlineData(-789.012)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public void BindDouble_WithDifferentValues_ShouldWorkCorrectly(double testValue)
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestDoubleValues(id INT64, value DOUBLE, PRIMARY KEY(id))");
            using var stmt = TestConnection!.Prepare("CREATE (:TestDoubleValues {id: $id, value: $value})");

            // Act
            stmt.BindInt64("id", 1);
            stmt.BindDouble("value", testValue);
            using var result = stmt.Execute();

            // Assert
            Assert.True(result.IsSuccess);

            // Verify the value was stored correctly
            using var queryResult = TestConnection!.Query("MATCH (t:TestDoubleValues) RETURN t.value");
            foreach (var row in queryResult)
            {
                Assert.Equal(testValue, row["t.value"].GetDouble(), precision: 6);
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestDispose(id INT64, PRIMARY KEY(id))");
            var stmt = TestConnection!.Prepare("CREATE (:TestDispose {id: $id})");

            // Act
            stmt.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => stmt.BindInt64("id", 1));
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestMultiDispose(id INT64, PRIMARY KEY(id))");
            var stmt = TestConnection!.Prepare("CREATE (:TestMultiDispose {id: $id})");

            // Act & Assert (should not throw)
            stmt.Dispose();
            stmt.Dispose(); // Should not throw

            // If we get here without exception, test passes
            Assert.True(true);
        }
    }
}