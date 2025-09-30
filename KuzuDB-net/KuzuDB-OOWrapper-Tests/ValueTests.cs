namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the Value class.
    /// </summary>
    public class ValueTests : BaseKuzuTest
    {
        [Fact]
        public void CreateNull_ShouldReturnNullValue()
        {
            // Act
            using var value = Value.CreateNull();

            // Assert
            Assert.NotNull(value);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void CreateBool_WithTrue_ShouldReturnBoolValue()
        {
            // Act
            using var value = Value.CreateBool(true);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.True(value.GetBool());
        }

        [Fact]
        public void CreateBool_WithFalse_ShouldReturnBoolValue()
        {
            // Act
            using var value = Value.CreateBool(false);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.False(value.GetBool());
        }

        [Fact]
        public void CreateInt64_ShouldReturnCorrectValue()
        {
            // Arrange
            const long expected = 12345678901234L;

            // Act
            using var value = Value.CreateInt64(expected);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.Equal(expected, value.GetInt64());
        }

        [Fact]
        public void CreateInt32_ShouldReturnCorrectValue()
        {
            // Arrange
            const int expected = 123456;

            // Act
            using var value = Value.CreateInt32(expected);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.Equal(expected, value.GetInt32());
        }

        [Fact]
        public void CreateDouble_ShouldReturnCorrectValue()
        {
            // Arrange
            const double expected = 123.456789;

            // Act
            using var value = Value.CreateDouble(expected);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.Equal(expected, value.GetDouble(), precision: 6);
        }

        [Fact]
        public void CreateFloat_ShouldReturnCorrectValue()
        {
            // Arrange
            const float expected = 123.456f;

            // Act
            using var value = Value.CreateFloat(expected);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.Equal(expected, value.GetFloat(), precision: 3);
        }

        [Fact]
        public void CreateString_WithValidString_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expected = "Hello, World!";

            // Act
            using var value = Value.CreateString(expected);

            // Assert
            Assert.NotNull(value);
            Assert.False(value.IsNull);
            Assert.Equal(expected, value.GetString());
        }

        [Fact]
        public void CreateString_WithNullString_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Value.CreateString(null!));
        }

        [Fact]
        public void GetBool_OnNullValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => value.GetBool());
            Assert.Contains("Cannot get boolean value from null", exception.Message);
        }

        [Fact]
        public void GetInt64_OnNullValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => value.GetInt64());
            Assert.Contains("Cannot get int64 value from null", exception.Message);
        }

        [Fact]
        public void GetInt32_OnNullValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => value.GetInt32());
            Assert.Contains("Cannot get int32 value from null", exception.Message);
        }

        [Fact]
        public void GetDouble_OnNullValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => value.GetDouble());
            Assert.Contains("Cannot get double value from null", exception.Message);
        }

        [Fact]
        public void GetFloat_OnNullValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => value.GetFloat());
            Assert.Contains("Cannot get float value from null", exception.Message);
        }

        [Fact]
        public void GetString_OnNullValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => value.GetString());
            Assert.Contains("Cannot get string value from null", exception.Message);
        }

        [Fact]
        public void GetDataType_ShouldReturnValidDataType()
        {
            // Arrange
            using var value = Value.CreateInt64(123);

            // Act
            using var dataType = value.GetDataType();

            // Assert
            Assert.NotNull(dataType);
        }

        [Fact]
        public void ToString_WithValidValue_ShouldReturnStringRepresentation()
        {
            // Arrange
            using var value = Value.CreateString("test");

            // Act
            var result = value.ToString();

            // Assert
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void ToString_WithNullValue_ShouldReturnStringRepresentation()
        {
            // Arrange
            using var value = Value.CreateNull();

            // Act
            var result = value.ToString();

            // Assert
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateBool_WithDifferentValues_ShouldWorkCorrectly(bool testValue)
        {
            // Act
            using var value = Value.CreateBool(testValue);

            // Assert
            Assert.Equal(testValue, value.GetBool());
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(1L)]
        [InlineData(-1L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void CreateInt64_WithDifferentValues_ShouldWorkCorrectly(long testValue)
        {
            // Act
            using var value = Value.CreateInt64(testValue);

            // Assert
            Assert.Equal(testValue, value.GetInt64());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void CreateInt32_WithDifferentValues_ShouldWorkCorrectly(int testValue)
        {
            // Act
            using var value = Value.CreateInt32(testValue);

            // Assert
            Assert.Equal(testValue, value.GetInt32());
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(123.456)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public void CreateDouble_WithDifferentValues_ShouldWorkCorrectly(double testValue)
        {
            // Act
            using var value = Value.CreateDouble(testValue);

            // Assert
            Assert.Equal(testValue, value.GetDouble(), precision: 6);
        }

        [Theory]
        [InlineData("")]
        [InlineData("simple")]
        [InlineData("string with spaces")]
        [InlineData("string_with_underscores")]
        [InlineData("string-with-hyphens")]
        [InlineData("string123with456numbers")]
        [InlineData("string with special chars: !@#$%^&*()")]
        public void CreateString_WithDifferentValues_ShouldWorkCorrectly(string testValue)
        {
            // Act
            using var value = Value.CreateString(testValue);

            // Assert
            Assert.Equal(testValue, value.GetString());
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var value = Value.CreateString("test");

            // Act
            value.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => value.GetString());
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Arrange
            var value = Value.CreateString("test");

            // Act & Assert (should not throw)
            value.Dispose();
            value.Dispose(); // Should not throw

            // If we get here without exception, test passes
            Assert.True(true);
        }
    }
}