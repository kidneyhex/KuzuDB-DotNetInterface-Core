using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents a data type in KuzuDB.
    /// </summary>
    public class DataType : IDisposable
    {
        private kuzu_logical_type _logicalType;
        private bool _disposed = false;

        /// <summary>
        /// Gets the data type ID.
        /// </summary>
        public kuzu_data_type_id TypeId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the DataType class.
        /// </summary>
        /// <param name="logicalType">The native logical type.</param>
        internal DataType(kuzu_logical_type logicalType)
        {
            _logicalType = logicalType ?? throw new ArgumentNullException(nameof(logicalType));
            TypeId = kuzunet.kuzu_data_type_get_id(_logicalType);
        }

        /// <summary>
        /// Creates a data type for the specified type ID.
        /// </summary>
        /// <param name="typeId">The data type ID.</param>
        /// <returns>A DataType instance.</returns>
        public static DataType Create(kuzu_data_type_id typeId)
        {
            var logicalType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_create(typeId, null, 0, logicalType);
            return new DataType(logicalType);
        }

        /// <summary>
        /// Checks if this data type equals another data type.
        /// </summary>
        /// <param name="other">The other data type to compare with.</param>
        /// <returns>True if the data types are equal, false otherwise.</returns>
        public bool Equals(DataType other)
        {
            if (other == null)
                return false;
                
            EnsureNotDisposed();
            other.EnsureNotDisposed();
            
            return kuzunet.kuzu_data_type_equals(_logicalType, other._logicalType);
        }

        /// <summary>
        /// Gets the number of elements in an array type.
        /// </summary>
        /// <returns>The number of elements, or null if not an array type.</returns>
        public ulong? GetNumElementsInArray()
        {
            EnsureNotDisposed();
            
            var state = kuzunet.kuzu_data_type_get_num_elements_in_array(_logicalType, out ulong numElements);
            if (state != kuzu_state.KuzuSuccess)
            {
                return null;
            }
            
            return numElements;
        }

        /// <summary>
        /// Clones this data type.
        /// </summary>
        /// <returns>A new DataType instance that is a copy of this one.</returns>
        public DataType Clone()
        {
            EnsureNotDisposed();
            
            var clonedType = new kuzu_logical_type();
            kuzunet.kuzu_data_type_clone(_logicalType, clonedType);
            
            return new DataType(clonedType);
        }

        /// <summary>
        /// Gets the native logical type handle for internal use.
        /// </summary>
        internal kuzu_logical_type GetNativeLogicalType()
        {
            EnsureNotDisposed();
            return _logicalType;
        }

        /// <summary>
        /// Returns a string representation of the data type.
        /// </summary>
        /// <returns>A string describing the data type.</returns>
        public override string ToString()
        {
            return TypeId.ToString();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DataType));
        }

        /// <summary>
        /// Releases all resources used by the DataType.
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
                _logicalType?.Dispose();

                _disposed = true;
            }
        }

        ~DataType()
        {
            Dispose(false);
        }
    }
}