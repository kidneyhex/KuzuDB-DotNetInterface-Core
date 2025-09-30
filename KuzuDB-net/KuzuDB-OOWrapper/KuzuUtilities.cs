using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Provides utility methods for KuzuDB operations.
    /// </summary>
    public static class KuzuUtilities
    {
        /// <summary>
        /// Gets the KuzuDB version.
        /// </summary>
        /// <returns>The version string.</returns>
        public static string GetVersion()
        {
            return kuzunet.kuzu_get_version();
        }

        /// <summary>
        /// Gets the KuzuDB storage version.
        /// </summary>
        /// <returns>The storage version.</returns>
        public static ulong GetStorageVersion()
        {
            return kuzunet.kuzu_get_storage_version();
        }

        /// <summary>
        /// Creates a default system configuration.
        /// </summary>
        /// <returns>A SystemConfig instance with default settings.</returns>
        public static SystemConfig CreateDefaultSystemConfig()
        {
            return new SystemConfig();
        }
    }
}