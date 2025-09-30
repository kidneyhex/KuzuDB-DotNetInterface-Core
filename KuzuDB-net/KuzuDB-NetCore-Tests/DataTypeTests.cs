namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_logical_type and data type functionality.
    /// </summary>
    public class DataTypeTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void DataType_Create_WithBasicTypes_ShouldSucceed()
        {
            // Act & Assert for different basic types
            using var boolType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_BOOL, null!, 0, boolType);
            Assert.Equal(kuzu_data_type_id.KUZU_BOOL, kuzunet.kuzu_data_type_get_id(boolType));

            using var intType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_INT32, null!, 0, intType);
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(intType));

            using var stringType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_STRING, null!, 0, stringType);
            Assert.Equal(kuzu_data_type_id.KUZU_STRING, kuzunet.kuzu_data_type_get_id(stringType));
        }

        [Theory]
        [InlineData(kuzu_data_type_id.KUZU_BOOL)]
        [InlineData(kuzu_data_type_id.KUZU_INT8)]
        [InlineData(kuzu_data_type_id.KUZU_INT16)]
        [InlineData(kuzu_data_type_id.KUZU_INT32)]
        [InlineData(kuzu_data_type_id.KUZU_INT64)]
        [InlineData(kuzu_data_type_id.KUZU_UINT8)]
        [InlineData(kuzu_data_type_id.KUZU_UINT16)]
        [InlineData(kuzu_data_type_id.KUZU_UINT32)]
        [InlineData(kuzu_data_type_id.KUZU_UINT64)]
        [InlineData(kuzu_data_type_id.KUZU_FLOAT)]
        [InlineData(kuzu_data_type_id.KUZU_DOUBLE)]
        [InlineData(kuzu_data_type_id.KUZU_STRING)]
        [InlineData(kuzu_data_type_id.KUZU_DATE)]
        [InlineData(kuzu_data_type_id.KUZU_TIMESTAMP)]
        [InlineData(kuzu_data_type_id.KUZU_INTERVAL)]
        public void DataType_Create_WithDifferentTypes_ShouldReturnCorrectId(kuzu_data_type_id expectedType)
        {
            // Act
            using var dataType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(expectedType, null!, 0, dataType);

            // Assert
            Assert.Equal(expectedType, kuzunet.kuzu_data_type_get_id(dataType));
        }

        [Fact]
        public void DataType_Clone_ShouldCreateIdenticalCopy()
        {
            // Arrange
            using var originalType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_STRING, null!, 0, originalType);

            // Act
            using var clonedType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_clone(originalType, clonedType);

            // Assert
            Assert.Equal(kuzunet.kuzu_data_type_get_id(originalType), kuzunet.kuzu_data_type_get_id(clonedType));
        }

        [Fact]
        public void DataType_Equals_WithSameTypes_ShouldReturnTrue()
        {
            // Arrange
            using var type1 = new kuzu_logical_type();
            using var type2 = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_INT32, null!, 0, type1);
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_INT32, null!, 0, type2);

            // Act
            var areEqual = kuzunet.kuzu_data_type_equals(type1, type2);

            // Assert
            Assert.True(areEqual);
        }

        [Fact]
        public void DataType_Equals_WithDifferentTypes_ShouldReturnFalse()
        {
            // Arrange
            using var type1 = new kuzu_logical_type();
            using var type2 = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_INT32, null!, 0, type1);
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_STRING, null!, 0, type2);

            // Act
            var areEqual = kuzunet.kuzu_data_type_equals(type1, type2);

            // Assert
            Assert.False(areEqual);
        }

        [Fact]
        public void DataType_GetId_ShouldReturnCorrectTypeId()
        {
            // Arrange
            using var dataType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_DOUBLE, null!, 0, dataType);

            // Act
            var typeId = kuzunet.kuzu_data_type_get_id(dataType);

            // Assert
            Assert.Equal(kuzu_data_type_id.KUZU_DOUBLE, typeId);
        }

        [Fact]
        public void DataType_CreateArray_ShouldSetNumElementsCorrectly()
        {
            // Arrange
            using var elementType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_INT32, null!, 0, elementType);

            // Act
            using var arrayType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_ARRAY, elementType, 10, arrayType);

            // Assert
            Assert.Equal(kuzu_data_type_id.KUZU_ARRAY, kuzunet.kuzu_data_type_get_id(arrayType));
            
            var result = kuzunet.kuzu_data_type_get_num_elements_in_array(arrayType, out ulong numElements);
            Assert.Equal(kuzu_state.KuzuSuccess, result);
            Assert.Equal(10ul, numElements);
        }

        [Fact]
        public void DataType_FromQueryResult_ShouldReturnCorrectTypes()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN p.id, p.name, p.age, p.active");

            // Assert - Check column data types
            using var idType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 0, idType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT64, kuzunet.kuzu_data_type_get_id(idType));

            using var nameType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 1, nameType));
            Assert.Equal(kuzu_data_type_id.KUZU_STRING, kuzunet.kuzu_data_type_get_id(nameType));

            using var ageType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 2, ageType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(ageType));

            using var activeType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 3, activeType));
            Assert.Equal(kuzu_data_type_id.KUZU_BOOL, kuzunet.kuzu_data_type_get_id(activeType));
        }

        [Fact]
        public void DataType_FromValue_ShouldReturnCorrectType()
        {
            // Arrange
            using var value = kuzunet.kuzu_value_create_double(123.456);

            // Act
            using var dataType = new kuzu_logical_type();
            kuzunet.kuzu_value_get_data_type(value, dataType);

            // Assert
            Assert.Equal(kuzu_data_type_id.KUZU_DOUBLE, kuzunet.kuzu_data_type_get_id(dataType));
        }

        [Fact]
        public void DataType_WithComplexQuery_ShouldHandleAggregationTypes()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = ExecuteQuery("MATCH (p:Person) RETURN COUNT(*) as count, AVG(p.age) as avg_age, MIN(p.age) as min_age, MAX(p.age) as max_age");

            // Assert - Check aggregation result types
            using var countType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 0, countType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT64, kuzunet.kuzu_data_type_get_id(countType));

            using var avgType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 1, avgType));
            Assert.Equal(kuzu_data_type_id.KUZU_DOUBLE, kuzunet.kuzu_data_type_get_id(avgType));

            using var minType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 2, minType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(minType));

            using var maxType = new kuzu_logical_type();
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_query_result_get_column_data_type(result, 3, maxType));
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(maxType));
        }

        [Fact]
        public void DataType_Dispose_ShouldCleanupResources()
        {
            // Arrange
            var dataType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_STRING, null!, 0, dataType);

            // Act & Assert (should not throw)
            dataType.Dispose();
        }

        [Fact]
        public void DataType_MultipleDispose_ShouldNotThrow()
        {
            // Arrange
            var dataType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_STRING, null!, 0, dataType);

            // Act & Assert (should not throw)
            dataType.Dispose();
            dataType.Dispose(); // Should not throw
        }
    }
}