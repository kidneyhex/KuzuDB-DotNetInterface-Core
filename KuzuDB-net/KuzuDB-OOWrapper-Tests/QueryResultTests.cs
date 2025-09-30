namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the QueryResult class.
    /// </summary>
    public class QueryResultTests : BaseKuzuTest
    {
        [Fact]
        public void IsSuccess_WithSuccessfulQuery_ShouldReturnTrue()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void IsSuccess_WithFailedQuery_ShouldReturnFalse()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var result = TestConnection!.Query("INVALID QUERY");

            // Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ErrorMessage_WithFailedQuery_ShouldReturnMessage()
        {
            // Arrange
            SetupTestDatabase();

            // Act
            using var result = TestConnection!.Query("INVALID QUERY");

            // Assert
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public void ErrorMessage_WithSuccessfulQuery_ShouldReturnEmptyOrNull()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Assert
            Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public void NumColumns_WithKnownQuery_ShouldReturnCorrectCount()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age, p.active");

            // Assert
            Assert.Equal((ulong)3, result.NumColumns);
        }

        [Fact]
        public void NumTuples_WithKnownData_ShouldReturnCorrectCount()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Assert
            Assert.Equal((ulong)3, result.NumTuples); // We inserted 3 persons
        }

        [Fact]
        public void GetColumnName_WithValidIndex_ShouldReturnCorrectName()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age");

            // Assert
            Assert.Equal("p.name", result.GetColumnName(0));
            Assert.Equal("p.age", result.GetColumnName(1));
        }

        [Fact]
        public void GetColumnName_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetColumnName(999));
        }

        [Fact]
        public void GetColumnDataType_WithValidIndex_ShouldReturnDataType()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age");
            using var nameDataType = result.GetColumnDataType(0);
            using var ageDataType = result.GetColumnDataType(1);

            // Assert
            Assert.NotNull(nameDataType);
            Assert.NotNull(ageDataType);
        }

        [Fact]
        public void GetColumnDataType_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            SetupTestDatabaseWithData();
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetColumnDataType(999));
        }

        [Fact]
        public void Enumeration_ShouldIterateOverAllRows()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var expectedRowCount = 3; // We inserted 3 persons

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name ORDER BY p.id");
            var rowCount = 0;
            var names = new List<string>();

            foreach (var row in result)
            {
                rowCount++;
                names.Add(row[0].GetString());
                row.Dispose(); // Manually dispose for testing
            }

            // Assert
            Assert.Equal(expectedRowCount, rowCount);
            Assert.Equal(expectedRowCount, names.Count);
            Assert.Contains("Alice Johnson", names);
            Assert.Contains("Bob Smith", names);
            Assert.Contains("Carol Wilson", names);
        }

        [Fact]
        public void Enumeration_WithEmptyResult_ShouldNotIterate()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) WHERE p.id = 999 RETURN p.name");
            var rowCount = 0;

            foreach (var row in result)
            {
                rowCount++;
                row.Dispose();
            }

            // Assert
            Assert.Equal(0, rowCount);
        }

        [Fact]
        public void ToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name LIMIT 1");
            var stringResult = result.ToString();

            // Assert
            Assert.False(string.IsNullOrEmpty(stringResult));
        }

        [Fact]
        public void MultipleEnumerations_ShouldWorkCorrectly()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name ORDER BY p.id");
            
            var firstPass = new List<string>();
            foreach (var row in result)
            {
                firstPass.Add(row[0].GetString());
                row.Dispose();
            }

            var secondPass = new List<string>();
            foreach (var row in result)
            {
                secondPass.Add(row[0].GetString());
                row.Dispose();
            }

            // Assert
            Assert.Equal(3, firstPass.Count);
            Assert.Equal(3, secondPass.Count);
            
            // Check that both passes contain the same elements (order should be the same due to ORDER BY)
            for (int i = 0; i < firstPass.Count; i++)
            {
                Assert.Equal(firstPass[i], secondPass[i]);
            }
        }

        [Fact]
        public void ComplexQuery_WithJoins_ShouldReturnCorrectStructure()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query(@"
                MATCH (p:Person)-[w:WorksFor]->(c:Company) 
                RETURN p.name, p.age, c.name, w.position, w.salary 
                ORDER BY p.id");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal((ulong)5, result.NumColumns);
            Assert.Equal((ulong)3, result.NumTuples); // 3 work relationships

            // Check column names
            Assert.Equal("p.name", result.GetColumnName(0));
            Assert.Equal("p.age", result.GetColumnName(1));
            Assert.Equal("c.name", result.GetColumnName(2));
            Assert.Equal("w.position", result.GetColumnName(3));
            Assert.Equal("w.salary", result.GetColumnName(4));
        }

        [Fact]
        public void AggregationQuery_ShouldReturnCorrectResults()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Act
            using var result = TestConnection!.Query("MATCH (p:Person) RETURN COUNT(*) AS person_count");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal((ulong)1, result.NumColumns);
            Assert.Equal((ulong)1, result.NumTuples);
            Assert.Equal("person_count", result.GetColumnName(0));

            // Check the actual count
            foreach (var row in result)
            {
                Assert.Equal(3L, row[0].GetInt64());
                row.Dispose();
                break; // Only one row expected
            }
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Act
            result.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => result.NumColumns);
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name");

            // Act & Assert (should not throw)
            result.Dispose();
            result.Dispose(); // Should not throw

            // If we get here without exception, test passes
            Assert.True(true);
        }
    }
}