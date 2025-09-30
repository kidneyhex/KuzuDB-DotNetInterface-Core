namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the Row class.
    /// </summary>
    public class RowTests : BaseKuzuTest
    {
        [Fact]
        public void IndexerByColumnIndex_WithValidIndex_ShouldReturnValue()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name, p.age");
            
            foreach (var row in result)
            {
                using var nameValue = row[0];
                using var ageValue = row[1];

                // Assert
                Assert.NotNull(nameValue);
                Assert.Equal("Alice Johnson", nameValue.GetString());
                Assert.NotNull(ageValue);
                Assert.Equal(30, ageValue.GetInt32());
                
                row.Dispose();
                break; // Only check first row
            }
        }

        [Fact]
        public void IndexerByColumnIndex_WithULongIndex_ShouldReturnValue()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            foreach (var row in result)
            {
                using var value = row[0UL];

                // Assert
                Assert.NotNull(value);
                Assert.Equal("Alice Johnson", value.GetString());
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void IndexerByColumnIndex_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act & Assert
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            foreach (var row in result)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => row[999]);
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void IndexerByColumnName_WithValidName_ShouldReturnValue()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name, p.age");
            
            foreach (var row in result)
            {
                using var nameValue = row["p.name"];
                using var ageValue = row["p.age"];

                // Assert
                Assert.NotNull(nameValue);
                Assert.Equal("Alice Johnson", nameValue.GetString());
                Assert.NotNull(ageValue);
                Assert.Equal(30, ageValue.GetInt32());
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void IndexerByColumnName_WithCaseInsensitive_ShouldReturnValue()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            foreach (var row in result)
            {
                using var value = row["P.NAME"]; // Different case

                // Assert
                Assert.NotNull(value);
                Assert.Equal("Alice Johnson", value.GetString());
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void IndexerByColumnName_WithInvalidName_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act & Assert
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            foreach (var row in result)
            {
                var exception = Assert.Throws<ArgumentException>(() => row["nonexistent_column"]);
                Assert.Contains("Column 'nonexistent_column' not found", exception.Message);
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void IndexerByColumnName_WithNullOrEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act & Assert
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            foreach (var row in result)
            {
                var exceptionNull = Assert.Throws<ArgumentException>(() => row[null!]);
                var exceptionEmpty = Assert.Throws<ArgumentException>(() => row[""]);

                Assert.Contains("Column name cannot be null or empty", exceptionNull.Message);
                Assert.Contains("Column name cannot be null or empty", exceptionEmpty.Message);
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void ColumnCount_ShouldReturnCorrectCount()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name, p.age, p.active");
            
            foreach (var row in result)
            {
                // Assert
                Assert.Equal((ulong)3, row.ColumnCount);
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void ToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            foreach (var row in result)
            {
                var stringResult = row.ToString();

                // Assert
                Assert.False(string.IsNullOrEmpty(stringResult));
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void GetEnumerator_ShouldIterateOverAllColumns()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name, p.age, p.active");
            
            foreach (var row in result)
            {
                var values = new List<Value>();
                var valueCount = 0;

                foreach (var value in row)
                {
                    values.Add(value);
                    valueCount++;
                }

                // Assert
                Assert.Equal(3, valueCount);
                Assert.Equal(3, values.Count);
                
                // Check values
                Assert.Equal("Alice Johnson", values[0].GetString());
                Assert.Equal(30, values[1].GetInt32());
                Assert.True(values[2].GetBool());

                // Dispose all values
                foreach (var value in values)
                {
                    value.Dispose();
                }
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void HandleNullValues_ShouldWorkCorrectly()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery("CREATE NODE TABLE TestNull(id INT64, optional_value STRING, PRIMARY KEY(id))");
            ExecuteQuery("CREATE (:TestNull {id: 1})"); // optional_value will be null

            // Act
            using var result = TestConnection!.Query("MATCH (t:TestNull) RETURN t.id, t.optional_value");
            
            foreach (var row in result)
            {
                using var idValue = row["t.id"];
                using var nullValue = row["t.optional_value"];

                // Assert
                Assert.False(idValue.IsNull);
                Assert.Equal(1L, idValue.GetInt64());
                
                Assert.True(nullValue.IsNull);
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void MultipleDataTypes_ShouldHandleCorrectly()
        {
            // Arrange
            SetupTestDatabase();
            ExecuteQuery(@"CREATE NODE TABLE MultiType(
                id INT64, 
                name STRING, 
                score DOUBLE, 
                active BOOLEAN, 
                count INT32,
                rating FLOAT,
                PRIMARY KEY(id))");
            ExecuteQuery(@"CREATE (:MultiType {
                id: 1, 
                name: 'Test', 
                score: 95.5, 
                active: true, 
                count: 42,
                rating: 4.8})");

            // Act
            using var result = TestConnection!.Query(@"MATCH (m:MultiType) 
                RETURN m.id, m.name, m.score, m.active, m.count, m.rating");
            
            foreach (var row in result)
            {
                // Assert
                Assert.Equal((ulong)6, row.ColumnCount);
                
                Assert.Equal(1L, row["m.id"].GetInt64());
                Assert.Equal("Test", row["m.name"].GetString());
                Assert.Equal(95.5, row["m.score"].GetDouble(), precision: 3);
                Assert.True(row["m.active"].GetBool());
                Assert.Equal(42, row["m.count"].GetInt32());
                Assert.Equal(4.8f, row["m.rating"].GetFloat(), precision: 2);
                
                row.Dispose();
                break;
            }
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            Row? testRow = null;
            foreach (var row in result)
            {
                testRow = row;
                break;
            }

            // Act
            testRow!.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => testRow.ColumnCount);
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            Row? testRow = null;
            foreach (var row in result)
            {
                testRow = row;
                break;
            }

            // Act & Assert (should not throw)
            testRow!.Dispose();
            testRow.Dispose(); // Should not throw

            // If we get here without exception, test passes
            Assert.True(true);
        }

        [Fact]
        public void AccessAfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 1 RETURN p.name");
            
            Row? testRow = null;
            foreach (var row in result)
            {
                testRow = row;
                break;
            }

            // Act
            testRow!.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => testRow[0]);
            Assert.Throws<ObjectDisposedException>(() => testRow["p.name"]);
            Assert.Throws<ObjectDisposedException>(() => testRow.ColumnCount);
            Assert.Throws<ObjectDisposedException>(() => testRow.ToString());
        }
    }
}