using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents query execution summary information.
    /// </summary>
    public class QuerySummary : IDisposable
    {
        private kuzu_query_summary _querySummary;
        private bool _disposed = false;

        /// <summary>
        /// Gets the compilation time in milliseconds.
        /// </summary>
        public double CompilingTime { get; private set; }

        /// <summary>
        /// Gets the execution time in milliseconds.
        /// </summary>
        public double ExecutionTime { get; private set; }

        /// <summary>
        /// Gets the total time (compilation + execution) in milliseconds.
        /// </summary>
        public double TotalTime => CompilingTime + ExecutionTime;

        /// <summary>
        /// Initializes a new instance of the QuerySummary class.
        /// </summary>
        /// <param name="querySummary">The native query summary.</param>
        internal QuerySummary(kuzu_query_summary querySummary)
        {
            _querySummary = querySummary ?? throw new ArgumentNullException(nameof(querySummary));
            
            CompilingTime = kuzunet.kuzu_query_summary_get_compiling_time(_querySummary);
            ExecutionTime = kuzunet.kuzu_query_summary_get_execution_time(_querySummary);
        }

        /// <summary>
        /// Returns a string representation of the query summary.
        /// </summary>
        /// <returns>A string containing the timing information.</returns>
        public override string ToString()
        {
            return $"Compilation: {CompilingTime:F2}ms, Execution: {ExecutionTime:F2}ms, Total: {TotalTime:F2}ms";
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QuerySummary));
        }

        /// <summary>
        /// Releases all resources used by the QuerySummary.
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
                _querySummary?.Dispose();

                _disposed = true;
            }
        }

        ~QuerySummary()
        {
            Dispose(false);
        }
    }
}