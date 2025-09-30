using System;
using System.Collections;
using System.Collections.Generic;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents the result of a query execution.
    /// </summary>
    public class QueryResult : IDisposable, IEnumerable<Row>
    {
        private kuzu_query_result _queryResult;
        private bool _disposed = false;

        /// <summary>
        /// Gets a value indicating whether the query was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the error message if the query failed.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the number of columns in the result.
        /// </summary>
        public ulong NumColumns { get; private set; }

        /// <summary>
        /// Gets the number of tuples (rows) in the result.
        /// </summary>
        public ulong NumTuples { get; private set; }

        /// <summary>
        /// Initializes a new instance of the QueryResult class.
        /// </summary>
        /// <param name="queryResult">The native query result.</param>
        internal QueryResult(kuzu_query_result queryResult)
        {
            _queryResult = queryResult ?? throw new ArgumentNullException(nameof(queryResult));
            
            IsSuccess = kuzunet.kuzu_query_result_is_success(_queryResult);
            if (!IsSuccess)
            {
                ErrorMessage = kuzunet.kuzu_query_result_get_error_message(_queryResult);
            }
            else
            {
                NumColumns = kuzunet.kuzu_query_result_get_num_columns(_queryResult);
                NumTuples = kuzunet.kuzu_query_result_get_num_tuples(_queryResult);
            }
        }

        /// <summary>
        /// Gets the name of the column at the specified index.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column.</param>
        /// <returns>The column name.</returns>
        public string GetColumnName(ulong columnIndex)
        {
            EnsureNotDisposed();
            EnsureSuccess();
            
            if (columnIndex >= NumColumns)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            var state = kuzunet.kuzu_query_result_get_column_name(_queryResult, columnIndex, out string columnName);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to get column name at index {columnIndex}.");
            }
            
            return columnName;
        }

        /// <summary>
        /// Gets the data type of the column at the specified index.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column.</param>
        /// <returns>The column data type.</returns>
        public DataType GetColumnDataType(ulong columnIndex)
        {
            EnsureNotDisposed();
            EnsureSuccess();
            
            if (columnIndex >= NumColumns)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            var logicalType = new kuzu_logical_type();
            var state = kuzunet.kuzu_query_result_get_column_data_type(_queryResult, columnIndex, logicalType);
            if (state != kuzu_state.KuzuSuccess)
            {
                logicalType.Dispose();
                throw new KuzuException($"Failed to get column data type at index {columnIndex}.");
            }
            
            return new DataType(logicalType);
        }

        /// <summary>
        /// Gets the query summary.
        /// </summary>
        /// <returns>A QuerySummary object containing execution metrics.</returns>
        public QuerySummary GetQuerySummary()
        {
            EnsureNotDisposed();
            EnsureSuccess();

            var querySummary = new kuzu_query_summary();
            var state = kuzunet.kuzu_query_result_get_query_summary(_queryResult, querySummary);
            if (state != kuzu_state.KuzuSuccess)
            {
                querySummary.Dispose();
                throw new KuzuException("Failed to get query summary.");
            }
            
            return new QuerySummary(querySummary);
        }

        /// <summary>
        /// Checks if there are more rows available.
        /// </summary>
        /// <returns>True if there are more rows, false otherwise.</returns>
        public bool HasNext()
        {
            EnsureNotDisposed();
            EnsureSuccess();
            return kuzunet.kuzu_query_result_has_next(_queryResult);
        }

        /// <summary>
        /// Gets the next row from the result set.
        /// </summary>
        /// <returns>The next row, or null if no more rows are available.</returns>
        public Row? GetNext()
        {
            EnsureNotDisposed();
            EnsureSuccess();
            
            if (!HasNext())
                return null;

            var flatTuple = new kuzu_flat_tuple();
            var state = kuzunet.kuzu_query_result_get_next(_queryResult, flatTuple);
            if (state != kuzu_state.KuzuSuccess)
            {
                flatTuple.Dispose();
                throw new KuzuException("Failed to get next row.");
            }
            
            return new Row(flatTuple, this);
        }

        /// <summary>
        /// Resets the iterator to the beginning of the result set.
        /// </summary>
        public void ResetIterator()
        {
            EnsureNotDisposed();
            EnsureSuccess();
            kuzunet.kuzu_query_result_reset_iterator(_queryResult);
        }

        /// <summary>
        /// Converts the result to a string representation.
        /// </summary>
        /// <returns>A string representation of the result.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            if (!IsSuccess)
                return $"Error: {ErrorMessage}";
            
            return kuzunet.kuzu_query_result_to_string(_queryResult);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the rows.
        /// </summary>
        /// <returns>An enumerator for the rows.</returns>
        public IEnumerator<Row> GetEnumerator()
        {
            EnsureNotDisposed();
            EnsureSuccess();
            
            ResetIterator();
            while (HasNext())
            {
                var row = GetNext();
                if (row != null)
                    yield return row;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QueryResult));
        }

        private void EnsureSuccess()
        {
            if (!IsSuccess)
                throw new InvalidOperationException($"Cannot perform operation on failed query result: {ErrorMessage}");
        }

        /// <summary>
        /// Releases all resources used by the QueryResult.
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
                _queryResult?.Dispose();

                _disposed = true;
            }
        }

        ~QueryResult()
        {
            Dispose(false);
        }
    }
}