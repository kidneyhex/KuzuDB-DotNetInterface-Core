using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents a connection to a KuzuDB database.
    /// </summary>
    public class Connection : IDisposable
    {
        private kuzu_connection _connection;
        private Database _database;
        private bool _disposed = false;

        /// <summary>
        /// Gets the database associated with this connection.
        /// </summary>
        public Database Database => _database;

        /// <summary>
        /// Initializes a new instance of the Connection class.
        /// </summary>
        /// <param name="database">The database to connect to.</param>
        internal Connection(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _connection = new kuzu_connection();
            
            var state = kuzunet.kuzu_connection_init(_database.GetNativeDatabase(), _connection);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to initialize database connection.");
            }
        }

        /// <summary>
        /// Executes a query and returns the result.
        /// </summary>
        /// <param name="query">The query string to execute.</param>
        /// <returns>A QueryResult containing the results.</returns>
        public QueryResult Query(string query)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            EnsureNotDisposed();

            var queryResult = new kuzu_query_result();
            var state = kuzunet.kuzu_connection_query(_connection, query, queryResult);
            
            if (state != kuzu_state.KuzuSuccess)
            {
                var errorMessage = kuzunet.kuzu_query_result_get_error_message(queryResult);
                queryResult.Dispose();
                throw new KuzuException($"Query execution failed: {errorMessage}");
            }

            return new QueryResult(queryResult);
        }

        /// <summary>
        /// Prepares a statement for execution.
        /// </summary>
        /// <param name="query">The query string to prepare.</param>
        /// <returns>A PreparedStatement instance.</returns>
        public PreparedStatement Prepare(string query)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            EnsureNotDisposed();

            var preparedStatement = new kuzu_prepared_statement();
            var state = kuzunet.kuzu_connection_prepare(_connection, query, preparedStatement);
            
            if (state != kuzu_state.KuzuSuccess)
            {
                var errorMessage = kuzunet.kuzu_prepared_statement_get_error_message(preparedStatement);
                preparedStatement.Dispose();
                throw new KuzuException($"Statement preparation failed: {errorMessage}");
            }

            return new PreparedStatement(preparedStatement, this);
        }

        /// <summary>
        /// Sets the maximum number of threads for query execution.
        /// </summary>
        /// <param name="numThreads">The number of threads to use.</param>
        public void SetMaxNumThreadsForExecution(ulong numThreads)
        {
            EnsureNotDisposed();
            
            var state = kuzunet.kuzu_connection_set_max_num_thread_for_exec(_connection, numThreads);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to set maximum number of threads for execution.");
            }
        }

        /// <summary>
        /// Gets the maximum number of threads for query execution.
        /// </summary>
        /// <returns>The number of threads used for execution.</returns>
        public ulong GetMaxNumThreadsForExecution()
        {
            EnsureNotDisposed();
            
            var state = kuzunet.kuzu_connection_get_max_num_thread_for_exec(_connection, out ulong numThreads);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to get maximum number of threads for execution.");
            }
            
            return numThreads;
        }

        /// <summary>
        /// Sets the query timeout in milliseconds.
        /// </summary>
        /// <param name="timeoutMs">The timeout in milliseconds.</param>
        public void SetQueryTimeout(ulong timeoutMs)
        {
            EnsureNotDisposed();
            
            var state = kuzunet.kuzu_connection_set_query_timeout(_connection, timeoutMs);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException("Failed to set query timeout.");
            }
        }

        /// <summary>
        /// Interrupts the currently running query.
        /// </summary>
        public void Interrupt()
        {
            EnsureNotDisposed();
            kuzunet.kuzu_connection_interrupt(_connection);
        }

        /// <summary>
        /// Gets the native connection handle for internal use.
        /// </summary>
        internal kuzu_connection GetNativeConnection()
        {
            EnsureNotDisposed();
            return _connection;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Connection));
        }

        /// <summary>
        /// Releases all resources used by the Connection.
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
                _connection?.Dispose();

                _disposed = true;
            }
        }

        ~Connection()
        {
            Dispose(false);
        }
    }
}