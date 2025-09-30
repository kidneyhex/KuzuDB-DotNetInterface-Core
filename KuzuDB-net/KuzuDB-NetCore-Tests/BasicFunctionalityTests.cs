namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Basic smoke tests to verify the KuzuDB NetCore wrapper is functional.
    /// </summary>
    public class BasicFunctionalityTests
    {
        [Fact]
        public void KuzuDB_Version_ShouldReturnValidVersion()
        {
            // Act
            var version = kuzunet.kuzu_get_version();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            Assert.Contains(".", version); // Version should contain dots (e.g., "1.2.3")
        }

        [Fact]
        public void KuzuDB_StorageVersion_ShouldReturnValidNumber()
        {
            // Act
            var storageVersion = kuzunet.kuzu_get_storage_version();

            // Assert
            Assert.True(storageVersion > 0, "Storage version should be greater than 0");
        }

        [Fact]
        public void KuzuDB_DefaultSystemConfig_ShouldCreateValidConfig()
        {
            // Act
            using var config = kuzunet.kuzu_default_system_config();

            // Assert
            Assert.NotNull(config);
        }

        [Fact]
        public void KuzuDB_NativeClasses_ShouldBeInstantiable()
        {
            // Act & Assert - Should be able to create instances of all main classes
            using var database = new kuzu_database();
            using var connection = new kuzu_connection();
            using var queryResult = new kuzu_query_result();
            using var preparedStatement = new kuzu_prepared_statement();
            using var flatTuple = new kuzu_flat_tuple();
            using var value = new kuzu_value();
            using var logicalType = new kuzu_logical_type();
            using var querySummary = new kuzu_query_summary();

            // If we get here without exceptions, all classes are instantiable
            Assert.True(true);
        }

        [Fact]
        public void KuzuDB_BasicWorkflow_ShouldWork()
        {
            // This is a minimal integration test to ensure basic functionality works
            var tempPath = Path.Combine(Path.GetTempPath(), $"kuzu_basic_test_{Guid.NewGuid():N}");
            
            try
            {
                // Arrange
                using var systemConfig = kuzunet.kuzu_default_system_config();
                using var database = new kuzu_database();
                using var connection = new kuzu_connection();

                // Act - Initialize database and connection
                var dbInitResult = kuzunet.kuzu_database_init(tempPath, systemConfig, database);
                var connInitResult = kuzunet.kuzu_connection_init(database, connection);

                // Assert
                Assert.Equal(kuzu_state.KuzuSuccess, dbInitResult);
                Assert.Equal(kuzu_state.KuzuSuccess, connInitResult);

                // Act - Execute a simple query
                using var result = new kuzu_query_result();
                var queryResult = kuzunet.kuzu_connection_query(connection, 
                    "CREATE NODE TABLE Test(id INT64, PRIMARY KEY(id))", result);

                // Assert
                Assert.Equal(kuzu_state.KuzuSuccess, queryResult);
                Assert.True(kuzunet.kuzu_query_result_is_success(result));
            }
            finally
            {
                // Cleanup
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Theory]
        [InlineData(kuzu_state.KuzuSuccess)]
        [InlineData(kuzu_state.KuzuError)]
        public void KuzuState_EnumValues_ShouldBeAccessible(kuzu_state expectedState)
        {
            // Act & Assert - Should be able to access enum values without errors
            Assert.True(Enum.IsDefined(typeof(kuzu_state), expectedState));
        }

        [Theory]
        [InlineData(kuzu_data_type_id.KUZU_BOOL)]
        [InlineData(kuzu_data_type_id.KUZU_INT32)]
        [InlineData(kuzu_data_type_id.KUZU_INT64)]
        [InlineData(kuzu_data_type_id.KUZU_STRING)]
        [InlineData(kuzu_data_type_id.KUZU_DOUBLE)]
        [InlineData(kuzu_data_type_id.KUZU_FLOAT)]
        [InlineData(kuzu_data_type_id.KUZU_DATE)]
        [InlineData(kuzu_data_type_id.KUZU_TIMESTAMP)]
        public void DataTypeId_EnumValues_ShouldBeAccessible(kuzu_data_type_id expectedType)
        {
            // Act & Assert - Should be able to access enum values without errors
            Assert.True(Enum.IsDefined(typeof(kuzu_data_type_id), expectedType));
        }
    }
}