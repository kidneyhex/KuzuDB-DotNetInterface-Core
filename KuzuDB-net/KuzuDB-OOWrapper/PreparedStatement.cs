using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents a prepared statement that can be executed multiple times with different parameters.
    /// </summary>
    public class PreparedStatement : IDisposable
    {
        private kuzu_prepared_statement _preparedStatement;
        private Connection _connection;
        private bool _disposed = false;

        /// <summary>
        /// Gets a value indicating whether the statement preparation was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the error message if the statement preparation failed.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PreparedStatement class.
        /// </summary>
        /// <param name="preparedStatement">The native prepared statement.</param>
        /// <param name="connection">The connection used to create this statement.</param>
        internal PreparedStatement(kuzu_prepared_statement preparedStatement, Connection connection)
        {
            _preparedStatement = preparedStatement ?? throw new ArgumentNullException(nameof(preparedStatement));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            
            IsSuccess = kuzunet.kuzu_prepared_statement_is_success(_preparedStatement);
            if (!IsSuccess)
            {
                ErrorMessage = kuzunet.kuzu_prepared_statement_get_error_message(_preparedStatement);
            }
        }

        /// <summary>
        /// Executes the prepared statement.
        /// </summary>
        /// <returns>A QueryResult containing the results.</returns>
        public QueryResult Execute()
        {
            EnsureNotDisposed();
            EnsureSuccess();

            var queryResult = new kuzu_query_result();
            var state = kuzunet.kuzu_connection_execute(_connection.GetNativeConnection(), _preparedStatement, queryResult);
            
            if (state != kuzu_state.KuzuSuccess)
            {
                var errorMessage = kuzunet.kuzu_query_result_get_error_message(queryResult);
                queryResult.Dispose();
                throw new KuzuException($"Statement execution failed: {errorMessage}");
            }

            return new QueryResult(queryResult);
        }

        /// <summary>
        /// Binds a boolean parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The boolean value to bind.</param>
        public void BindBool(string paramName, bool value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_bool(_preparedStatement, paramName, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind boolean parameter '{paramName}'.");
            }
        }

        /// <summary>
        /// Binds an integer parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The integer value to bind.</param>
        public void BindInt64(string paramName, long value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_int64(_preparedStatement, paramName, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind int64 parameter '{paramName}'.");
            }
        }

        /// <summary>
        /// Binds an integer parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The integer value to bind.</param>
        public void BindInt32(string paramName, int value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_int32(_preparedStatement, paramName, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind int32 parameter '{paramName}'.");
            }
        }

        /// <summary>
        /// Binds a double parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The double value to bind.</param>
        public void BindDouble(string paramName, double value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_double(_preparedStatement, paramName, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind double parameter '{paramName}'.");
            }
        }

        /// <summary>
        /// Binds a float parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The float value to bind.</param>
        public void BindFloat(string paramName, float value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_float(_preparedStatement, paramName, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind float parameter '{paramName}'.");
            }
        }

        /// <summary>
        /// Binds a string parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The string value to bind.</param>
        public void BindString(string paramName, string value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_string(_preparedStatement, paramName, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind string parameter '{paramName}'.");
            }
        }

        /// <summary>
        /// Binds a value parameter to the statement.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="value">The value to bind.</param>
        public void BindValue(string paramName, Value value)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(paramName));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            EnsureNotDisposed();
            EnsureSuccess();

            var state = kuzunet.kuzu_prepared_statement_bind_value(_preparedStatement, paramName, value.GetNativeValue());
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to bind value parameter '{paramName}'.");
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PreparedStatement));
        }

        private void EnsureSuccess()
        {
            if (!IsSuccess)
                throw new InvalidOperationException($"Cannot perform operation on failed prepared statement: {ErrorMessage}");
        }

        /// <summary>
        /// Releases all resources used by the PreparedStatement.
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
                _preparedStatement?.Dispose();

                _disposed = true;
            }
        }

        ~PreparedStatement()
        {
            Dispose(false);
        }
    }
}