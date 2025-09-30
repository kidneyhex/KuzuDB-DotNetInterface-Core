namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_database wrapper class and database initialization functionality.
    /// </summary>
    public class DatabaseTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void Database_Initialization_WithValidPath_ShouldSucceed()
        {
            // Arrange
            var systemConfig = kuzunet.kuzu_default_system_config();
            var database = new kuzu_database();

            // Act
            var result = kuzunet.kuzu_database_init(TestDatabasePath, systemConfig, database);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);

            // Cleanup
            database.Dispose();
            systemConfig.Dispose();
        }

        [Fact]
        public void Database_Initialization_WithInMemoryPath_ShouldSucceed()
        {
            // Arrange
            var systemConfig = kuzunet.kuzu_default_system_config();
            var database = new kuzu_database();

            // Act - Using empty string for in-memory database
            var result = kuzunet.kuzu_database_init("", systemConfig, database);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result);

            // Cleanup
            database.Dispose();
            systemConfig.Dispose();
        }

        [Fact]
        public void Database_Dispose_ShouldCleanupResources()
        {
            // Arrange
            var systemConfig = kuzunet.kuzu_default_system_config();
            var database = new kuzu_database();
            kuzunet.kuzu_database_init(TestDatabasePath, systemConfig, database);

            // Act & Assert (should not throw)
            database.Dispose();
            systemConfig.Dispose();
        }

        [Fact]
        public void Database_DoubleDispose_ShouldNotThrow()
        {
            // Arrange
            var systemConfig = kuzunet.kuzu_default_system_config();
            var database = new kuzu_database();
            kuzunet.kuzu_database_init(TestDatabasePath, systemConfig, database);

            // Act & Assert (should not throw)
            database.Dispose();
            database.Dispose(); // Should not throw

            systemConfig.Dispose();
        }

        [Fact]
        public void Database_SystemConfig_Creation_ShouldReturnValidConfig()
        {
            // Act
            var systemConfig = kuzunet.kuzu_default_system_config();

            // Assert
            Assert.NotNull(systemConfig);

            // Cleanup
            systemConfig.Dispose();
        }

        [Fact]
        public void Database_MultipleInstances_ShouldWorkIndependently()
        {
            // Arrange
            var systemConfig1 = kuzunet.kuzu_default_system_config();
            var systemConfig2 = kuzunet.kuzu_default_system_config();
            var database1 = new kuzu_database();
            var database2 = new kuzu_database();
            
            var path1 = TestDatabasePath + "_1";
            var path2 = TestDatabasePath + "_2";

            // Act
            var result1 = kuzunet.kuzu_database_init(path1, systemConfig1, database1);
            var result2 = kuzunet.kuzu_database_init(path2, systemConfig2, database2);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, result1);
            Assert.Equal(kuzu_state.KuzuSuccess, result2);

            // Cleanup
            database1.Dispose();
            database2.Dispose();
            systemConfig1.Dispose();
            systemConfig2.Dispose();
        }

        [Fact]
        public void Database_Version_ShouldReturnVersionString()
        {
            // Act
            var version = kuzunet.kuzu_get_version();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }

        [Fact]
        public void Database_StorageVersion_ShouldReturnVersionNumber()
        {
            // Act
            var storageVersion = kuzunet.kuzu_get_storage_version();

            // Assert
            Assert.True(storageVersion > 0);
        }
    }
}