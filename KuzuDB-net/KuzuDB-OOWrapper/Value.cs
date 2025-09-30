using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents a value in KuzuDB.
    /// </summary>
    public class Value : IDisposable
    {
        private kuzu_value _value;
        private bool _disposed = false;

        /// <summary>
        /// Gets a value indicating whether this value is null.
        /// </summary>
        public bool IsNull { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Value class.
        /// </summary>
        /// <param name="value">The native value.</param>
        internal Value(kuzu_value value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            IsNull = kuzunet.kuzu_value_is_null(_value);
        }

        /// <summary>
        /// Creates a null value.
        /// </summary>
        /// <returns>A null Value instance.</returns>
        public static Value CreateNull()
        {
            var nativeValue = kuzunet.kuzu_value_create_null();
            return new Value(nativeValue);
        }

        /// <summary>
        /// Creates a boolean value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A Value instance containing the boolean.</returns>
        public static Value CreateBool(bool value)
        {
            var nativeValue = kuzunet.kuzu_value_create_bool(value);
            return new Value(nativeValue);
        }

        /// <summary>
        /// Creates an integer value.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A Value instance containing the integer.</returns>
        public static Value CreateInt64(long value)
        {
            var nativeValue = kuzunet.kuzu_value_create_int64(value);
            return new Value(nativeValue);
        }

        /// <summary>
        /// Creates an integer value.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A Value instance containing the integer.</returns>
        public static Value CreateInt32(int value)
        {
            var nativeValue = kuzunet.kuzu_value_create_int32(value);
            return new Value(nativeValue);
        }

        /// <summary>
        /// Creates a double value.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>A Value instance containing the double.</returns>
        public static Value CreateDouble(double value)
        {
            var nativeValue = kuzunet.kuzu_value_create_double(value);
            return new Value(nativeValue);
        }

        /// <summary>
        /// Creates a float value.
        /// </summary>
        /// <param name="value">The float value.</param>
        /// <returns>A Value instance containing the float.</returns>
        public static Value CreateFloat(float value)
        {
            var nativeValue = kuzunet.kuzu_value_create_float(value);
            return new Value(nativeValue);
        }

        /// <summary>
        /// Creates a string value.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>A Value instance containing the string.</returns>
        public static Value CreateString(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
                
            var nativeValue = kuzunet.kuzu_value_create_string(value);
            return new Value(nativeValue);
        }

        /// <summary>
        /// Gets the boolean value.
        /// </summary>
        /// <returns>The boolean value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or not a boolean.</exception>
        public bool GetBool()
        {
            EnsureNotDisposed();
            if (IsNull)
                throw new InvalidOperationException("Cannot get boolean value from null.");

            var state = kuzunet.kuzu_value_get_bool(_value, out bool result);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get boolean value.");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the int64 value.
        /// </summary>
        /// <returns>The int64 value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or not an int64.</exception>
        public long GetInt64()
        {
            EnsureNotDisposed();
            if (IsNull)
                throw new InvalidOperationException("Cannot get int64 value from null.");

            var state = kuzunet.kuzu_value_get_int64(_value, out long result);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get int64 value.");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the int32 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or not an int32.</exception>
        public int GetInt32()
        {
            EnsureNotDisposed();
            if (IsNull)
                throw new InvalidOperationException("Cannot get int32 value from null.");

            var state = kuzunet.kuzu_value_get_int32(_value, out int result);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get int32 value.");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the double value.
        /// </summary>
        /// <returns>The double value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or not a double.</exception>
        public double GetDouble()
        {
            EnsureNotDisposed();
            if (IsNull)
                throw new InvalidOperationException("Cannot get double value from null.");

            var state = kuzunet.kuzu_value_get_double(_value, out double result);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get double value.");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the float value.
        /// </summary>
        /// <returns>The float value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or not a float.</exception>
        public float GetFloat()
        {
            EnsureNotDisposed();
            if (IsNull)
                throw new InvalidOperationException("Cannot get float value from null.");

            var state = kuzunet.kuzu_value_get_float(_value, out float result);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get float value.");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the string value.
        /// </summary>
        /// <returns>The string value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or not a string.</exception>
        public string GetString()
        {
            EnsureNotDisposed();
            if (IsNull)
                throw new InvalidOperationException("Cannot get string value from null.");

            var state = kuzunet.kuzu_value_get_string(_value, out string result);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get string value.");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the data type of this value.
        /// </summary>
        /// <returns>The data type.</returns>
        public DataType GetDataType()
        {
            EnsureNotDisposed();
            
            var logicalType = new kuzu_logical_type();
            kuzunet.kuzu_value_get_data_type(_value, logicalType);
            
            return new DataType(logicalType);
        }

        /// <summary>
        /// Converts the value to a string representation.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            return kuzunet.kuzu_value_to_string(_value);
        }

        /// <summary>
        /// Gets the native value handle for internal use.
        /// </summary>
        internal kuzu_value GetNativeValue()
        {
            EnsureNotDisposed();
            return _value;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Value));
        }

        /// <summary>
        /// Releases all resources used by the Value.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources
                _value?.Dispose();

                _disposed = true;
            }
        }

        ~Value()
        {
            Dispose(false);
        }
    }
}