using System.IO;

namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Base class for KuzuDB OO Wrapper tests providing common test utilities.
    /// </summary>
    public abstract class BaseKuzuTest : IDisposable
    {
        protected string TestDatabasePath { get; private set; }
        protected Database? TestDatabase { get; private set; }
        protected Connection? TestConnection { get; private set; }

        protected BaseKuzuTest()
        {
            // Create a unique test database path for each test
            TestDatabasePath = Path.Combine(Path.GetTempPath(), $"kuzu_test_db_{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Sets up a test database and connection.
        /// </summary>
        protected void SetupTestDatabase()
        {
            CleanupTestDatabase();
            TestDatabase = new Database(TestDatabasePath);
            TestConnection = TestDatabase.CreateConnection();
        }

        /// <summary>
        /// Sets up a test database with sample schema and data.
        /// </summary>
        protected void SetupTestDatabaseWithData()
        {
            SetupTestDatabase();
            CreateSampleSchema();
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
        protected void ExecuteQuery(string query)
        {
            if (TestConnection == null)
                throw new InvalidOperationException("Test connection not initialized.");

            using var result = TestConnection.Query(query);
            if (!result.IsSuccess)
            {
                throw new Exception($"Query failed: {result.ErrorMessage}. Query: {query}");
            }
        }

        /// <summary>
        /// Executes a query and returns the result.
        /// </summary>
        protected QueryResult ExecuteQueryWithResult(string query)
        {
            if (TestConnection == null)
                throw new InvalidOperationException("Test connection not initialized.");

            var result = TestConnection.Query(query);
            if (!result.IsSuccess)
            {
                result.Dispose();
                throw new Exception($"Query failed: {result.ErrorMessage}. Query: {query}");
            }
            return result;
        }

        /// <summary>
        /// Cleans up the test database directory.
        /// </summary>
        protected void CleanupTestDatabase()
        {
            try
            {
                // Dispose resources first to ensure files are closed
                TestConnection?.Dispose();
                TestDatabase?.Dispose();
                TestConnection = null;
                TestDatabase = null;

                // KuzuDB might create either a file or a directory, so check for both
                if (Directory.Exists(TestDatabasePath))
                {
                    Directory.Delete(TestDatabasePath, recursive: true);
                }
                else if (File.Exists(TestDatabasePath))
                {
                    File.Delete(TestDatabasePath);
                }

                // Also clean up any additional files/directories that might be created with similar names
                var directory = Path.GetDirectoryName(TestDatabasePath);
                var fileName = Path.GetFileName(TestDatabasePath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, fileName + "*");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore individual file cleanup errors
                        }
                    }

                    var directories = Directory.GetDirectories(directory, fileName + "*");
                    foreach (var dir in directories)
                    {
                        try
                        {
                            Directory.Delete(dir, recursive: true);
                        }
                        catch
                        {
                            // Ignore individual directory cleanup errors
                        }
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