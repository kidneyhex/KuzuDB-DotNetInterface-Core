using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents a KuzuDB database instance.
    /// </summary>
    public class Database : IDisposable
    {
        private kuzu_database _database;
        private kuzu_system_config _config;
        private bool _disposed = false;

        /// <summary>
        /// Gets the database path.
        /// </summary>
        public string DatabasePath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Database class.
        /// </summary>
        /// <param name="databasePath">The path to the database file. If null, a database will exist in memory only.</param>
        /// <param name="config">Optional system configuration. If null, default configuration is used.</param>
        public Database(string databasePath, SystemConfig? config = null)
        {
            DatabasePath = (string.IsNullOrWhiteSpace(databasePath)) ? "": databasePath;
            
            _config = config?.GetNativeConfig() ?? kuzunet.kuzu_default_system_config();
            _database = new kuzu_database();
            
            var state = kuzunet.kuzu_database_init(DatabasePath, _config, _database);
            if (state != kuzu_state.KuzuSuccess)
            {
                throw new KuzuException($"Failed to initialize database at path: {databasePath}");
            }
        }

        /// <summary>
        /// Creates a new connection to this database.
        /// </summary>
        /// <returns>A new Connection instance.</returns>
        public Connection CreateConnection()
        {
            EnsureNotDisposed();
            return new Connection(this);
        }

        /// <summary>
        /// Gets the native database handle for internal use.
        /// </summary>
        internal kuzu_database GetNativeDatabase()
        {
            EnsureNotDisposed();
            return _database;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Database));
        }

        /// <summary>
        /// Releases all resources used by the Database.
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
                _database?.Dispose();
                _config?.Dispose();

                _disposed = true;
            }
        }

        ~Database()
        {
            Dispose(false);
        }
    }
}