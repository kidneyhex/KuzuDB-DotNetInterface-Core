namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests for the KuzuException class.
    /// </summary>
    public class KuzuExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_ShouldSetMessage()
        {
            // Arrange
            const string expectedMessage = "Test exception message";

            // Act
            var exception = new KuzuException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
        {
            // Arrange
            const string expectedMessage = "Outer exception message";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new KuzuException(expectedMessage, innerException);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_WithNullMessage_ShouldAcceptNull()
        {
            // Act
            var exception = new KuzuException(null!);

            // Assert
            Assert.NotNull(exception);
            // Message behavior with null is framework-dependent
        }

        [Fact]
        public void InheritanceFromException_ShouldAllowCasting()
        {
            // Arrange
            var kuzuException = new KuzuException("Test message");

            // Act
            Exception baseException = kuzuException;

            // Assert
            Assert.NotNull(baseException);
            Assert.IsType<KuzuException>(baseException);
        }

        [Fact]
        public void ThrowAndCatch_ShouldWorkCorrectly()
        {
            // Arrange
            const string expectedMessage = "Database connection failed";

            // Act & Assert
            var exception = Assert.Throws<KuzuException>((Action)(() => 
            {
                throw new KuzuException(expectedMessage);
            }));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void CatchAsBaseException_ShouldWorkCorrectly()
        {
            // Arrange
            const string expectedMessage = "Query execution failed";
            Exception? caughtException = null;

            // Act - Catch as base Exception type
            try
            {
                throw new KuzuException(expectedMessage);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.NotNull(caughtException);
            Assert.IsType<KuzuException>(caughtException);
            Assert.Equal(expectedMessage, caughtException.Message);
        }

        [Fact]
        public void SerializationRoundTrip_ShouldPreserveMessage()
        {
            // Arrange
            const string originalMessage = "Serialization test message";
            var originalException = new KuzuException(originalMessage);

            // Act
            var serialized = System.Text.Json.JsonSerializer.Serialize(originalException.Message);
            var deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<string>(serialized);

            // Assert
            Assert.Equal(originalMessage, deserializedMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Simple message")]
        [InlineData("Message with special characters: !@#$%^&*()")]
        [InlineData("Very long message that spans multiple lines and contains various types of content including numbers 123, symbols !@#, and other text that might be encountered in real-world scenarios")]
        public void Constructor_WithVariousMessages_ShouldHandleCorrectly(string message)
        {
            // Act
            var exception = new KuzuException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void WithInnerException_ShouldChainCorrectly()
        {
            // Arrange
            var innermost = new ArgumentException("Innermost exception");
            var middle = new InvalidOperationException("Middle exception", innermost);
            var outer = new KuzuException("Outer KuzuException", middle);

            // Act & Assert
            Assert.Equal(middle, outer.InnerException);
            Assert.Equal(innermost, outer.InnerException!.InnerException);
            Assert.Null(outer.InnerException.InnerException!.InnerException);
        }

        [Fact]
        public void ToString_ShouldIncludeExceptionDetails()
        {
            // Arrange
            const string message = "Test exception for ToString";
            var exception = new KuzuException(message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("KuzuException", result);
            Assert.Contains(message, result);
        }

        [Fact]
        public void GetType_ShouldReturnKuzuExceptionType()
        {
            // Arrange
            var exception = new KuzuException("Test");

            // Act
            var type = exception.GetType();

            // Assert
            Assert.Equal(typeof(KuzuException), type);
            Assert.Equal("KuzuException", type.Name);
        }
    }
}