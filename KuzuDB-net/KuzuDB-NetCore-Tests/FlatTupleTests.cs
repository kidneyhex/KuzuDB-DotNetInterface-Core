namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_flat_tuple wrapper class and flat tuple functionality.
    /// </summary>
    public class FlatTupleTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void FlatTuple_GetValue_WithValidIndex_ShouldReturnValue()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name ORDER BY p.id LIMIT 1");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act
            using var value = new kuzu_value();
            var getValueResult = kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, value);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, getValueResult);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_string(value, out string name));
            Assert.Equal("Alice Johnson", name);
        }

        [Fact]
        public void FlatTuple_GetValue_WithMultipleColumns_ShouldReturnCorrectValues()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name, p.age, p.active ORDER BY p.id LIMIT 1");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert - Check name
            using var nameValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, nameValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_string(nameValue, out string name));
            Assert.Equal("Alice Johnson", name);

            // Act & Assert - Check age
            using var ageValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 1, ageValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int32(ageValue, out int age));
            Assert.Equal(30, age);

            // Act & Assert - Check active
            using var activeValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 2, activeValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_bool(activeValue, out bool active));
            Assert.True(active);
        }

        [Fact]
        public void FlatTuple_ToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name ORDER BY p.id LIMIT 1");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act
            var stringResult = kuzunet.kuzu_flat_tuple_to_string(flatTuple);

            // Assert
            Assert.False(string.IsNullOrEmpty(stringResult));
            Assert.Contains("Alice Johnson", stringResult);
        }

        [Fact]
        public void FlatTuple_WithNumericValues_ShouldReturnCorrectTypes()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (c:Company) RETURN c.id, c.revenue ORDER BY c.id LIMIT 1");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert - Check ID (INT64)
            using var idValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, idValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int64(idValue, out long id));
            Assert.Equal(1L, id);

            // Act & Assert - Check revenue (DOUBLE)
            using var revenueValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 1, revenueValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_double(revenueValue, out double revenue));
            Assert.Equal(1000000.50, revenue, precision: 2);
        }

        [Fact]
        public void FlatTuple_WithJoinQuery_ShouldReturnComplexData()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery(@"
                MATCH (p:Person)-[w:WorksFor]->(c:Company) 
                RETURN p.name, c.name, w.salary 
                ORDER BY p.id LIMIT 1");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert - Check person name
            using var personNameValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, personNameValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_string(personNameValue, out string personName));
            Assert.Equal("Alice Johnson", personName);

            // Act & Assert - Check company name
            using var companyNameValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 1, companyNameValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_string(companyNameValue, out string companyName));
            Assert.Equal("TechCorp", companyName);

            // Act & Assert - Check salary
            using var salaryValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 2, salaryValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_float(salaryValue, out float salary));
            Assert.Equal(75000.0f, salary, precision: 1);
        }

        [Fact]
        public void FlatTuple_WithAggregationQuery_ShouldReturnAggregatedValues()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (p:Person) RETURN COUNT(*) as count, AVG(p.age) as avg_age");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert - Check count
            using var countValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, countValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int64(countValue, out long count));
            Assert.Equal(3L, count);

            // Act & Assert - Check average age
            using var avgAgeValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 1, avgAgeValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_double(avgAgeValue, out double avgAge));
            Assert.Equal(30.0, avgAge, precision: 1); // (30 + 25 + 35) / 3 = 30
        }

        [Fact]
        public void FlatTuple_WithNullValues_ShouldHandleNullsCorrectly()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestNull(id INT64, optional_field STRING, PRIMARY KEY(id))");
            ExecuteQuery("CREATE (:TestNull {id: 1})"); // optional_field will be null
            
            using var result = ExecuteQuery("MATCH (t:TestNull) RETURN t.id, t.optional_field");
            
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            using var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert - Check non-null ID
            using var idValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, idValue));
            Assert.False(kuzunet.kuzu_value_is_null(idValue));
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int64(idValue, out long id));
            Assert.Equal(1L, id);

            // Act & Assert - Check null field
            using var nullFieldValue = new kuzu_value();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_flat_tuple_get_value(flatTuple, 1, nullFieldValue));
            Assert.True(kuzunet.kuzu_value_is_null(nullFieldValue));
        }

        [Fact]
        public void FlatTuple_Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name LIMIT 1");
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            
            var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert (should not throw)
            flatTuple.Dispose();
        }

        [Fact]
        public void FlatTuple_MultipleDispose_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name LIMIT 1");
            Assert.True(kuzunet.kuzu_query_result_has_next(result));
            
            var flatTuple = new kuzu_flat_tuple();
            kuzunet.kuzu_query_result_get_next(result, flatTuple);

            // Act & Assert (should not throw)
            flatTuple.Dispose();
            flatTuple.Dispose(); // Should not throw
        }
    }
}