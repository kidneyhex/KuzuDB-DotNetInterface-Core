using System;
using System.Collections;
using System.Collections.Generic;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents a row of data from a query result.
    /// </summary>
    public class Row : IDisposable, IEnumerable<Value>
    {
        private kuzu_flat_tuple _flatTuple;
        private QueryResult _queryResult;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the Row class.
        /// </summary>
        /// <param name="flatTuple">The native flat tuple.</param>
        /// <param name="queryResult">The query result this row belongs to.</param>
        internal Row(kuzu_flat_tuple flatTuple, QueryResult queryResult)
        {
            _flatTuple = flatTuple ?? throw new ArgumentNullException(nameof(flatTuple));
            _queryResult = queryResult ?? throw new ArgumentNullException(nameof(queryResult));
        }

        /// <summary>
        /// Gets the value at the specified column index.
        /// </summary>
        /// <param name="columnIndex">The zero-based column index.</param>
        /// <returns>The value at the specified column.</returns>
        public Value this[ulong columnIndex]
        {
            get
            {
                EnsureNotDisposed();
                
                if (columnIndex >= _queryResult.NumColumns)
                    throw new ArgumentOutOfRangeException(nameof(columnIndex));

                var value = new kuzu_value();
                var state = kuzunet.kuzu_flat_tuple_get_value(_flatTuple, columnIndex, value);
                if (state != kuzu_state.KuzuSuccess)
                {
                    value.Dispose();
                    throw new KuzuException($"Failed to get value at column index {columnIndex}.");
                }
                
                return new Value(value);
            }
        }

        /// <summary>
        /// Gets the value at the specified column index.
        /// </summary>
        /// <param name="columnIndex">The zero-based column index.</param>
        /// <returns>The value at the specified column.</returns>
        public Value this[int columnIndex] => this[(ulong)columnIndex];

        /// <summary>
        /// Gets the value for the specified column name.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <returns>The value for the specified column.</returns>
        public Value this[string columnName]
        {
            get
            {
                if (string.IsNullOrEmpty(columnName))
                    throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

                // Find the column index by name
                for (ulong i = 0; i < _queryResult.NumColumns; i++)
                {
                    if (_queryResult.GetColumnName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return this[i];
                    }
                }
                
                throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            }
        }

        /// <summary>
        /// Gets the number of columns in this row.
        /// </summary>
        public ulong ColumnCount 
        { 
            get
            {
                EnsureNotDisposed();
                return _queryResult.NumColumns;
            }
        }

        /// <summary>
        /// Converts the row to a string representation.
        /// </summary>
        /// <returns>A string representation of the row.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            return kuzunet.kuzu_flat_tuple_to_string(_flatTuple);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the values in the row.
        /// </summary>
        /// <returns>An enumerator for the values.</returns>
        public IEnumerator<Value> GetEnumerator()
        {
            EnsureNotDisposed();
            
            for (ulong i = 0; i < ColumnCount; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Row));
        }

        /// <summary>
        /// Releases all resources used by the Row.
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
                _flatTuple?.Dispose();

                _disposed = true;
            }
        }

        ~Row()
        {
            Dispose(false);
        }
    }
}