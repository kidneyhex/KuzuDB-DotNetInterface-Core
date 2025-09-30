using KuzuDB.OOWrapper;
using System;

namespace KuzuDB.OOWrapper.Examples
{
    /// <summary>
    /// Example program demonstrating the KuzuDB Object-Oriented Wrapper.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"KuzuDB Version: {KuzuUtilities.GetVersion()}");
                Console.WriteLine($"Storage Version: {KuzuUtilities.GetStorageVersion()}");
                Console.WriteLine();

                // Example 1: Basic database operations
                BasicDatabaseOperations();

                // Example 2: Prepared statements
                PreparedStatementExample();

                // Example 3: Working with different data types
                DataTypesExample();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void BasicDatabaseOperations()
        {
            Console.WriteLine("=== Basic Database Operations ===");

            // Create a temporary database
            string dbPath = Path.Combine(Path.GetTempPath(), "kuzu_example_db");
            CleanupDatabase(dbPath);

            using var database = new Database(dbPath);
            using var connection = database.CreateConnection();

            // Create a simple node table
            ExecuteQuery(connection, "CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))");

            // Insert some data
            ExecuteQuery(connection, "CREATE (:Person {name: 'Alice', age: 30})");
            ExecuteQuery(connection, "CREATE (:Person {name: 'Bob', age: 25})");
            ExecuteQuery(connection, "CREATE (:Person {name: 'Charlie', age: 35})");

            // Query the data
            using var result = connection.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age");

            Console.WriteLine($"Query returned {result.NumTuples} rows with {result.NumColumns} columns:");

            // Print column headers
            for (ulong i = 0; i < result.NumColumns; i++)
            {
                Console.Write($"{result.GetColumnName(i),-15}");
            }
            Console.WriteLine();
            Console.WriteLine(new string('-', (int)(result.NumColumns * 15)));

            // Print data rows
            foreach (var row in result)
            {
                for (ulong i = 0; i < row.ColumnCount; i++)
                {
                    var value = row[i];
                    var displayValue = value.IsNull ? "NULL" : value.ToString();
                    Console.Write($"{displayValue,-15}");
                }
                Console.WriteLine();
            }

            Console.WriteLine();

            // Clean up
            CleanupDatabase(dbPath);
        }

        private static void PreparedStatementExample()
        {
            Console.WriteLine("=== Prepared Statement Example ===");

            string dbPath = Path.Combine(Path.GetTempPath(), "kuzu_prepared_example_db");
            CleanupDatabase(dbPath);

            using var database = new Database(dbPath);
            using var connection = database.CreateConnection();

            // Create table and insert data
            ExecuteQuery(connection, "CREATE NODE TABLE Employee(id INT64, name STRING, salary DOUBLE, PRIMARY KEY(id))");

            // Use prepared statement to insert multiple employees
            using var insertStmt = connection.Prepare("CREATE (:Employee {id: $id, name: $name, salary: $salary})");

            var employees = new[]
            {
                (1, "John Doe", 50000.0),
                (2, "Jane Smith", 60000.0),
                (3, "Mike Johnson", 55000.0),
                (4, "Sarah Wilson", 65000.0)
            };

            foreach (var (id, name, salary) in employees)
            {
                insertStmt.BindInt64("id", id);
                insertStmt.BindString("name", name);
                insertStmt.BindDouble("salary", salary);

                using var insertResult = insertStmt.Execute();
                if (!insertResult.IsSuccess)
                {
                    Console.WriteLine($"Failed to insert employee: {insertResult.ErrorMessage}");
                }
            }

            // Query with prepared statement
            using var queryStmt = connection.Prepare("MATCH (e:Employee) WHERE e.salary > $minSalary RETURN e.name, e.salary ORDER BY e.salary DESC");
            queryStmt.BindDouble("minSalary", 55000.0);

            using var result = queryStmt.Execute();
            Console.WriteLine("Employees with salary > $55,000:");

            foreach (var row in result)
            {
                var name = row["e.name"].GetString();
                var salary = row["e.salary"].GetDouble();
                Console.WriteLine($"  {name}: ${salary:N2}");
            }

            Console.WriteLine();

            // Clean up
            CleanupDatabase(dbPath);
        }

        private static void DataTypesExample()
        {
            Console.WriteLine("=== Data Types Example ===");

            string dbPath = Path.Combine(Path.GetTempPath(), "kuzu_datatypes_example_db");
            CleanupDatabase(dbPath);

            using var database = new Database(dbPath);
            using var connection = database.CreateConnection();

            // Create table with various data types
            ExecuteQuery(connection, @"CREATE NODE TABLE DataTypeTest(
                id INT64,
                name STRING,
                active BOOLEAN,
                score DOUBLE,
                count INT32,
                PRIMARY KEY(id)
            )");

            // Insert test data
            ExecuteQuery(connection, "CREATE (:DataTypeTest {id: 1, name: 'Test Record', active: true, score: 95.5, count: 42})");
            ExecuteQuery(connection, "CREATE (:DataTypeTest {id: 2, name: 'Another Record', active: false, score: 87.2, count: 38})");

            // Query and examine data types
            using var result = connection.Query("MATCH (d:DataTypeTest) RETURN d.id, d.name, d.active, d.score, d.count ORDER BY d.id");

            Console.WriteLine("Column Information:");
            for (ulong i = 0; i < result.NumColumns; i++)
            {
                var columnName = result.GetColumnName(i);
                var dataType = result.GetColumnDataType(i);
                Console.WriteLine($"  {columnName}: {dataType}");
            }
            Console.WriteLine();

            Console.WriteLine("Data with type information:");
            foreach (var row in result)
            {
                Console.WriteLine($"Row data:");
                for (ulong i = 0; i < row.ColumnCount; i++)
                {
                    var value = row[i];
                    var columnName = result.GetColumnName(i);
                    var dataType = value.GetDataType();

                    Console.WriteLine($"  {columnName} ({dataType}): {value}");
                }
                Console.WriteLine();
            }

            // Clean up
            CleanupDatabase(dbPath);
        }

        private static void ExecuteQuery(Connection connection, string query)
        {
            using var result = connection.Query(query);
            if (!result.IsSuccess)
            {
                throw new Exception($"Query failed: {result.ErrorMessage}");
            }
        }

        private static void CleanupDatabase(string dbPath)
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}