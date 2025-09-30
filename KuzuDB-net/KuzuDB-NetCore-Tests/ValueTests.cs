namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests for kuzu_value wrapper class and value handling functionality.
    /// </summary>
    public class ValueTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void Value_CreateNull_ShouldCreateNullValue()
        {
            // Act
            using var value = kuzunet.kuzu_value_create_null();

            // Assert
            Assert.NotNull(value);
            Assert.True(kuzunet.kuzu_value_is_null(value));
        }

        [Fact]
        public void Value_CreateNullWithDataType_ShouldCreateTypedNullValue()
        {
            // Arrange
            using var dataType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_STRING, null!, 0, dataType);

            // Act
            using var value = kuzunet.kuzu_value_create_null_with_data_type(dataType);

            // Assert
            Assert.NotNull(value);
            Assert.True(kuzunet.kuzu_value_is_null(value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Value_CreateBool_WithDifferentValues_ShouldWorkCorrectly(bool testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_bool(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_bool(value, out bool result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData((sbyte)-128)]
        [InlineData((sbyte)0)]
        [InlineData((sbyte)127)]
        public void Value_CreateInt8_WithDifferentValues_ShouldWorkCorrectly(sbyte testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_int8(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int8(value, out sbyte result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData((short)-32768)]
        [InlineData((short)0)]
        [InlineData((short)32767)]
        public void Value_CreateInt16_WithDifferentValues_ShouldWorkCorrectly(short testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_int16(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int16(value, out short result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData(-2147483648)]
        [InlineData(0)]
        [InlineData(2147483647)]
        public void Value_CreateInt32_WithDifferentValues_ShouldWorkCorrectly(int testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_int32(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int32(value, out int result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData(-9223372036854775808)]
        [InlineData(0L)]
        [InlineData(9223372036854775807)]
        public void Value_CreateInt64_WithDifferentValues_ShouldWorkCorrectly(long testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_int64(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int64(value, out long result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)127)]
        [InlineData((byte)255)]
        public void Value_CreateUInt8_WithDifferentValues_ShouldWorkCorrectly(byte testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_uint8(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_uint8(value, out byte result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData((ushort)0)]
        [InlineData((ushort)32767)]
        [InlineData((ushort)65535)]
        public void Value_CreateUInt16_WithDifferentValues_ShouldWorkCorrectly(ushort testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_uint16(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_uint16(value, out ushort result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(2147483647u)]
        [InlineData(4294967295u)]
        public void Value_CreateUInt32_WithDifferentValues_ShouldWorkCorrectly(uint testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_uint32(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_uint32(value, out uint result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData(0ul)]
        [InlineData(9223372036854775807ul)]
        [InlineData(18446744073709551615ul)]
        public void Value_CreateUInt64_WithDifferentValues_ShouldWorkCorrectly(ulong testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_uint64(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_uint64(value, out ulong result));
            Assert.Equal(testValue, result);
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(123.456f)]
        [InlineData(-789.012f)]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        public void Value_CreateFloat_WithDifferentValues_ShouldWorkCorrectly(float testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_float(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_float(value, out float result));
            Assert.Equal(testValue, result, precision: 6);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(123.456)]
        [InlineData(-789.012)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public void Value_CreateDouble_WithDifferentValues_ShouldWorkCorrectly(double testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_double(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_double(value, out double result));
            Assert.Equal(testValue, result, precision: 15);
        }

        [Theory]
        [InlineData("")]
        [InlineData("simple")]
        [InlineData("string with spaces")]
        [InlineData("string_with_underscores")]
        [InlineData("string-with-hyphens")]
        [InlineData("string123with456numbers")]
        [InlineData("string with special chars: !@#$%^&*()")]
        public void Value_CreateString_WithDifferentValues_ShouldWorkCorrectly(string testValue)
        {
            // Act
            using var value = kuzunet.kuzu_value_create_string(testValue);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_string(value, out string result));
            Assert.Equal(testValue, result);
        }

        [Fact]
        public void Value_CreateDate_ShouldWorkCorrectly()
        {
            // Arrange
            using var date = new kuzu_date_t();

            // Act
            using var value = kuzunet.kuzu_value_create_date(date);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
        }

        [Fact]
        public void Value_CreateTimestamp_ShouldWorkCorrectly()
        {
            // Arrange
            using var timestamp = new kuzu_timestamp_t();

            // Act
            using var value = kuzunet.kuzu_value_create_timestamp(timestamp);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
        }

        [Fact]
        public void Value_CreateInterval_ShouldWorkCorrectly()
        {
            // Arrange
            using var interval = new kuzu_interval_t();

            // Act
            using var value = kuzunet.kuzu_value_create_interval(interval);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
        }

        [Fact]
        public void Value_Clone_ShouldCreateIdenticalCopy()
        {
            // Arrange
            using var originalValue = kuzunet.kuzu_value_create_string("test string");

            // Act
            using var clonedValue = kuzunet.kuzu_value_clone(originalValue);

            // Assert
            Assert.NotNull(clonedValue);
            Assert.False(kuzunet.kuzu_value_is_null(clonedValue));
            
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_string(clonedValue, out string result));
            Assert.Equal("test string", result);
        }

        [Fact]
        public void Value_Copy_ShouldCopyValueToAnother()
        {
            // Arrange
            using var sourceValue = kuzunet.kuzu_value_create_int32(42);
            using var targetValue = kuzunet.kuzu_value_create_int32(0);

            // Act - Based on typical copy semantics, the target should be the first parameter
            kuzunet.kuzu_value_copy(targetValue, sourceValue);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, kuzunet.kuzu_value_get_int32(targetValue, out int result));
            Assert.Equal(42, result);
        }

        [Fact]
        public void Value_SetNull_ShouldChangeNullStatus()
        {
            // Arrange
            using var value = kuzunet.kuzu_value_create_int32(42);
            Assert.False(kuzunet.kuzu_value_is_null(value));

            // Act
            kuzunet.kuzu_value_set_null(value, true);

            // Assert
            Assert.True(kuzunet.kuzu_value_is_null(value));

            // Act - Set back to not null
            kuzunet.kuzu_value_set_null(value, false);

            // Assert
            Assert.False(kuzunet.kuzu_value_is_null(value));
        }

        [Fact]
        public void Value_ToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            using var value = kuzunet.kuzu_value_create_string("test value");

            // Act
            var result = kuzunet.kuzu_value_to_string(value);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Value_GetDataType_ShouldReturnCorrectType()
        {
            // Arrange
            using var value = kuzunet.kuzu_value_create_int32(42);
            using var dataType = new kuzu_logical_type();

            // Act
            kuzunet.kuzu_value_get_data_type(value, dataType);

            // Assert
            Assert.Equal(kuzu_data_type_id.KUZU_INT32, kuzunet.kuzu_data_type_get_id(dataType));
        }

        [Fact]
        public void Value_CreateDefault_WithDataType_ShouldCreateDefaultValue()
        {
            // Arrange
            using var dataType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(kuzu_data_type_id.KUZU_INT32, null!, 0, dataType);

            // Act
            using var value = kuzunet.kuzu_value_create_default(dataType);

            // Assert
            Assert.NotNull(value);
            Assert.False(kuzunet.kuzu_value_is_null(value));
        }

        [Fact]
        public void Value_Int128_FromAndToString_ShouldWorkCorrectly()
        {
            // Arrange
            const string testValue = "123456789012345678901234567890";
            using var int128Value = new kuzu_int128_t();

            // Act
            var fromStringResult = kuzunet.kuzu_int128_t_from_string(testValue, int128Value);
            var toStringResult = kuzunet.kuzu_int128_t_to_string(int128Value, out string result);

            // Assert
            Assert.Equal(kuzu_state.KuzuSuccess, fromStringResult);
            Assert.Equal(kuzu_state.KuzuSuccess, toStringResult);
            Assert.Equal(testValue, result);
        }

        [Fact]
        public void Value_Dispose_ShouldCleanupResources()
        {
            // Arrange
            var value = kuzunet.kuzu_value_create_string("test");

            // Act & Assert (should not throw)
            value.Dispose();
        }

        [Fact]
        public void Value_MultipleDispose_ShouldNotThrow()
        {
            // Arrange
            var value = kuzunet.kuzu_value_create_string("test");

            // Act & Assert (should not throw)
            value.Dispose();
            value.Dispose(); // Should not throw
        }
    }
}