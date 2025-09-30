using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Represents system configuration options for KuzuDB.
    /// </summary>
    public class SystemConfig : IDisposable
    {
        private kuzu_system_config _config;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the SystemConfig class with default settings.
        /// </summary>
        public SystemConfig()
        {
            _config = kuzunet.kuzu_default_system_config();
        }

        /// <summary>
        /// Gets the native system config handle for internal use.
        /// </summary>
        internal kuzu_system_config GetNativeConfig()
        {
            EnsureNotDisposed();
            return _config;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SystemConfig));
        }

        /// <summary>
        /// Releases all resources used by the SystemConfig.
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
                _config?.Dispose();

                _disposed = true;
            }
        }

        ~SystemConfig()
        {
            Dispose(false);
        }
    }
}