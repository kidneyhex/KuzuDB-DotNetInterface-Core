using System.IO;

namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Base class for KuzuDB NetCore tests providing common test utilities and setup.
    /// </summary>
    public abstract class BaseKuzuNetCoreTest : IDisposable
    {
        protected string TestDatabasePath { get; private set; }
        protected kuzu_database? TestDatabase { get; private set; }
        protected kuzu_connection? TestConnection { get; private set; }
        protected kuzu_system_config? TestSystemConfig { get; private set; }

        protected BaseKuzuNetCoreTest()
        {
            // Create a unique test database path for each test
            TestDatabasePath = Path.Combine(Path.GetTempPath(), $"kuzu_netcore_test_db_{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Sets up a test database and connection using the native API.
        /// </summary>
        protected void SetupTestDatabase()
        {
            CleanupTestDatabase();
            
            TestSystemConfig = kuzunet.kuzu_default_system_config();
            TestDatabase = new kuzu_database();
            
            var initResult = kuzunet.kuzu_database_init(TestDatabasePath, TestSystemConfig, TestDatabase);
            if (initResult != kuzu_state.KuzuSuccess)
            {
                throw new Exception($"Failed to initialize test database: {initResult}");
            }

            TestConnection = new kuzu_connection();
            var connResult = kuzunet.kuzu_connection_init(TestDatabase, TestConnection);
            if (connResult != kuzu_state.KuzuSuccess)
            {
                throw new Exception($"Failed to initialize test connection: {connResult}");
            }
        }

        /// <summary>
        /// Sets up a test database with sample schema.
        /// </summary>
        protected void SetupTestDatabaseWithSchema()
        {
            SetupTestDatabase();
            CreateSampleSchema();
        }

        /// <summary>
        /// Sets up a test database with sample schema and data.
        /// </summary>
        protected void SetupTestDatabaseWithData()
        {
            SetupTestDatabaseWithSchema();
            InsertSampleData();
        }

        /// <summary>
        /// Creates a sample schema for testing.
        /// </summary>
        protected void CreateSampleSchema()
        {
            if (TestConnection == null)
                throw new InvalidOperationException("Test connection not initialized. Call SetupTestDatabase first.");

            ExecuteQuery("CREATE NODE TABLE Person(id INT64, name STRING, age INT32, active BOOLEAN, PRIMARY KEY(id))");
            ExecuteQuery("CREATE NODE TABLE Company(id INT64, name STRING, revenue DOUBLE, PRIMARY KEY(id))");
            ExecuteQuery("CREATE REL TABLE WorksFor(FROM Person TO Company, position STRING, salary FLOAT)");
        }

        /// <summary>
        /// Inserts sample data for testing.
        /// </summary>
        protected void InsertSampleData()
        {
            if (TestConnection == null)
                throw new InvalidOperationException("Test connection not initialized.");

            // Insert persons
            ExecuteQuery("CREATE (:Person {id: 1, name: 'Alice Johnson', age: 30, active: true})");
            ExecuteQuery("CREATE (:Person {id: 2, name: 'Bob Smith', age: 25, active: false})");
            ExecuteQuery("CREATE (:Person {id: 3, name: 'Carol Wilson', age: 35, active: true})");

            // Insert companies
            ExecuteQuery("CREATE (:Company {id: 1, name: 'TechCorp', revenue: 1000000.50})");
            ExecuteQuery("CREATE (:Company {id: 2, name: 'DataSoft', revenue: 750000.75})");

            // Insert relationships
            ExecuteQuery("MATCH (p:Person {id: 1}), (c:Company {id: 1}) CREATE (p)-[:WorksFor {position: 'Developer', salary: 75000.0}]->(c)");
            ExecuteQuery("MATCH (p:Person {id: 2}), (c:Company {id: 2}) CREATE (p)-[:WorksFor {position: 'Analyst', salary: 65000.0}]->(c)");
            ExecuteQuery("MATCH (p:Person {id: 3}), (c:Company {id: 1}) CREATE (p)-[:WorksFor {position: 'Manager', salary: 85000.0}]->(c)");
        }

        /// <summary>
        /// Executes a query and ensures it succeeds.
        /// </summary>
        protected kuzu_query_result ExecuteQuery(string query)
        {
            if (TestConnection == null)
                throw new InvalidOperationException("Test connection not initialized.");

            var result = new kuzu_query_result();
            var state = kuzunet.kuzu_connection_query(TestConnection, query, result);
            
            if (state != kuzu_state.KuzuSuccess || !kuzunet.kuzu_query_result_is_success(result))
            {
                var errorMsg = kuzunet.kuzu_query_result_get_error_message(result);
                result.Dispose();
                throw new Exception($"Query failed: {errorMsg}. Query: {query}");
            }
            
            return result;
        }

        /// <summary>
        /// Executes a query that is expected to fail.
        /// </summary>
        protected kuzu_query_result ExecuteQueryExpectingFailure(string query)
        {
            if (TestConnection == null)
                throw new InvalidOperationException("Test connection not initialized.");

            var result = new kuzu_query_result();
            kuzunet.kuzu_connection_query(TestConnection, query, result);
            return result;
        }

        /// <summary>
        /// Cleans up the test database directory.
        /// </summary>
        protected void CleanupTestDatabase()
        {
            try
            {
                if (Directory.Exists(TestDatabasePath))
                {
                    Directory.Delete(TestDatabasePath, recursive: true);
                }
                else if (File.Exists(TestDatabasePath))
                {
                    File.Delete(TestDatabasePath);
                }

                // Clean up any additional files/directories that might be created with similar names
                var directory = Path.GetDirectoryName(TestDatabasePath);
                var fileName = Path.GetFileName(TestDatabasePath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, fileName + "*");
                    foreach (var file in files)
                    {
                        try { File.Delete(file); } catch { /* Ignore cleanup errors */ }
                    }

                    var directories = Directory.GetDirectories(directory, fileName + "*");
                    foreach (var dir in directories)
                    {
                        try { Directory.Delete(dir, recursive: true); } catch { /* Ignore cleanup errors */ }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log warning but don't fail the test
                Console.WriteLine($"Warning: Could not clean up test database: {ex.Message}");
            }
        }

        public virtual void Dispose()
        {
            try
            {
                TestConnection?.Dispose();
                TestDatabase?.Dispose();
                TestSystemConfig?.Dispose();
                CleanupTestDatabase();
            }
            catch (Exception ex)
            {
                // Log but don't throw exceptions during disposal
                Console.WriteLine($"Warning: Error during test cleanup: {ex.Message}");
            }
        }
    }
}