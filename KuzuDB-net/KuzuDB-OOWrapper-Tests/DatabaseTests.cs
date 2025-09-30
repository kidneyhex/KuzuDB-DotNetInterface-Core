namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the Database class.
    /// </summary>
    public class DatabaseTests : BaseKuzuTest
    {
        [Fact]
        public void Constructor_WithValidPath_ShouldCreateDatabase()
        {
            // Arrange & Act
            using var database = new Database(TestDatabasePath);

            // Assert
            Assert.NotNull(database);
            Assert.Equal(TestDatabasePath, database.DatabasePath);
        }

        [Fact]
        public void Constructor_WithNullPath_ShouldCreateInMemoryDatabase()
        {
            // Arrange, Act & Assert - Based on actual implementation, null path creates in-memory database
            using var database = new Database(null!);
            Assert.NotNull(database);
            Assert.Equal("", database.DatabasePath); // Empty string for in-memory database
        }

        [Fact]
        public void Constructor_WithEmptyPath_ShouldCreateInMemoryDatabase()
        {
            // Arrange, Act & Assert - Based on actual implementation, empty path creates in-memory database
            using var database = new Database("");
            Assert.NotNull(database);
            Assert.Equal("", database.DatabasePath);
        }

        [Fact]
        public void CreateConnection_ShouldReturnValidConnection()
        {
            // Arrange
            using var database = new Database(TestDatabasePath);

            // Act
            using var connection = database.CreateConnection();

            // Assert
            Assert.NotNull(connection);
            Assert.Equal(database, connection.Database);
        }

        [Fact]
        public void CreateConnection_MultipleCalls_ShouldReturnDifferentInstances()
        {
            // Arrange
            using var database = new Database(TestDatabasePath);

            // Act
            using var connection1 = database.CreateConnection();
            using var connection2 = database.CreateConnection();

            // Assert
            Assert.NotNull(connection1);
            Assert.NotNull(connection2);
            Assert.NotSame(connection1, connection2);
            Assert.Equal(database, connection1.Database);
            Assert.Equal(database, connection2.Database);
        }

        [Fact]
        public void CreateConnection_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var database = new Database(TestDatabasePath);
            database.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => database.CreateConnection());
        }

        [Fact]
        public void DatabasePath_ShouldReturnCorrectPath()
        {
            // Arrange
            var testPath = Path.Combine(Path.GetTempPath(), "test_db_path");

            // Act
            using var database = new Database(testPath);

            // Assert
            Assert.Equal(testPath, database.DatabasePath);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var database = new Database(TestDatabasePath);
            using var connection = database.CreateConnection();

            // Act
            database.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => database.CreateConnection());
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Arrange
            var database = new Database(TestDatabasePath);

            // Act (should not throw)
            database.Dispose();
            database.Dispose(); // Should not throw

            // Assert - if we get here without exception, test passes
            Assert.True(true);
        }

        [Theory]
        [InlineData("simple_db")]
        [InlineData("database_with_underscores")]
        [InlineData("db-with-hyphens")]
        public void Constructor_WithDifferentValidPaths_ShouldSucceed(string pathSuffix)
        {
            // Arrange
            var testPath = Path.Combine(Path.GetTempPath(), $"test_db_{pathSuffix}_{Guid.NewGuid():N}");
            
            try
            {
                // Act
                using var database = new Database(testPath);
                using var connection = database.CreateConnection();

                // Assert
                Assert.NotNull(database);
                Assert.NotNull(connection);
                Assert.Equal(testPath, database.DatabasePath);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testPath))
                    File.Delete(testPath);
            }
        }

        [Fact]
        public void Constructor_WithInMemoryDatabase_ShouldWork()
        {
            // Arrange & Act
            using var database = new Database("");
            using var connection = database.CreateConnection();

            // Assert
            Assert.NotNull(database);
            Assert.NotNull(connection);
            Assert.Equal("", database.DatabasePath);
        }

        [Fact] 
        public void Constructor_WithValidConfig_ShouldCreateDatabase()
        {
            // Arrange
            var config = new SystemConfig();

            // Act
            using var database = new Database(TestDatabasePath, config);

            // Assert
            Assert.NotNull(database);
            Assert.Equal(TestDatabasePath, database.DatabasePath);
        }

        [Fact]
        public void CreateConnection_MultipleConnections_ShouldAllBeValid()
        {
            // Arrange
            using var database = new Database(TestDatabasePath);
            var connections = new List<Connection>();

            try
            {
                // Act - Create multiple connections
                for (int i = 0; i < 5; i++)
                {
                    connections.Add(database.CreateConnection());
                }

                // Assert
                Assert.Equal(5, connections.Count);
                foreach (var connection in connections)
                {
                    Assert.NotNull(connection);
                    Assert.Equal(database, connection.Database);
                }

                // Verify all connections are different instances
                for (int i = 0; i < connections.Count; i++)
                {
                    for (int j = i + 1; j < connections.Count; j++)
                    {
                        Assert.NotSame(connections[i], connections[j]);
                    }
                }
            }
            finally
            {
                // Cleanup
                foreach (var connection in connections)
                {
                    connection.Dispose();
                }
            }
        }
    }
}