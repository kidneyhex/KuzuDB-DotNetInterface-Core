using KuzuDB.OOWrapper;
using System;
using System.IO;

namespace KuzuDB.OOWrapper.Examples
{
    /// <summary>
    /// Example program demonstrating the KuzuDB Object-Oriented Wrapper.
    /// This program showcases various graph database operations including schema creation,
    /// data loading, querying, and manipulation using the KuzuDB .NET OO bindings.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== KuzuDB .NET OO Wrapper Examples ===\n");
                Console.WriteLine($"KuzuDB Version: {KuzuUtilities.GetVersion()}");
                Console.WriteLine($"Storage Version: {KuzuUtilities.GetStorageVersion()}");
                Console.WriteLine();

                // Create a temporary database for all examples
                string dbPath = Path.Combine(Path.GetTempPath(), "kuzu_oo_examples_db");
                CleanupDatabase(dbPath);

                using var database = new Database(dbPath);
                using var connection = database.CreateConnection();

                // Example 1: Schema Creation
                Console.WriteLine("1. Creating Schema...");
                CreateSchema(connection);

                // Example 2: Data Loading
                Console.WriteLine("\n2. Loading Sample Data...");
                LoadSampleData(connection);

                // Example 3: Basic Pattern Matching
                Console.WriteLine("\n3. Basic Pattern Matching - Who follows whom:");
                BasicPatternMatching(connection);

                // Example 4: Aggregation Queries
                Console.WriteLine("\n4. Aggregation Examples:");
                AggregationExamples(connection);

                // Example 5: Complex Queries with Multiple Relationships
                Console.WriteLine("\n5. Complex Multi-Relationship Queries:");
                ComplexQueries(connection);

                // Example 6: Data Modification Examples
                Console.WriteLine("\n6. Data Modification Examples:");
                DataModificationExamples(connection);

                // Example 7: Path Queries
                Console.WriteLine("\n7. Path Finding Examples:");
                PathQueries(connection);

                // Example 8: Property Filtering
                Console.WriteLine("\n8. Property Filtering Examples:");
                PropertyFilteringExamples(connection);

                // Example 9: Prepared Statements with Complex Data
                Console.WriteLine("\n9. Advanced Prepared Statement Examples:");
                AdvancedPreparedStatementExample(connection);

                // Example 10: Working with Different Data Types
                Console.WriteLine("\n10. Data Types Example:");
                DataTypesExample(connection);

                Console.WriteLine("\n=== All Examples Completed Successfully ===");

                // Clean up
                CleanupDatabase(dbPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            ExecuteQuery(connection, "CREATE NODE TABLE User(name STRING, age INT64, PRIMARY KEY (name))");
            ExecuteQuery(connection, "CREATE NODE TABLE City(name STRING, population INT64, PRIMARY KEY (name))");
            
            // Create relationship tables
            ExecuteQuery(connection, "CREATE REL TABLE Follows(FROM User TO User, since INT64)");
            ExecuteQuery(connection, "CREATE REL TABLE LivesIn(FROM User TO City)");
            
            Console.WriteLine("  Schema created successfully!");
            Console.WriteLine("  - User nodes: name (STRING), age (INT64)");
            Console.WriteLine("  - City nodes: name (STRING), population (INT64)");
            Console.WriteLine("  - Follows relationships: since (INT64)");
            Console.WriteLine("  - LivesIn relationships");
        }

        private static void LoadSampleData(Connection connection)
        {
            // Insert users
            ExecuteQuery(connection, "CREATE (:User {name: 'Adam', age: 30})");
            ExecuteQuery(connection, "CREATE (:User {name: 'Karissa', age: 40})");
            ExecuteQuery(connection, "CREATE (:User {name: 'Zhang', age: 50})");
            ExecuteQuery(connection, "CREATE (:User {name: 'Noura', age: 25})");

            // Insert cities
            ExecuteQuery(connection, "CREATE (:City {name: 'Waterloo', population: 150000})");
            ExecuteQuery(connection, "CREATE (:City {name: 'Kitchener', population: 200000})");
            ExecuteQuery(connection, "CREATE (:City {name: 'Guelph', population: 75000})");

            // Create follows relationships
            ExecuteQuery(connection, "MATCH (a:User {name: 'Adam'}), (b:User {name: 'Karissa'}) CREATE (a)-[:Follows {since: 2020}]->(b)");
            ExecuteQuery(connection, "MATCH (a:User {name: 'Adam'}), (b:User {name: 'Zhang'}) CREATE (a)-[:Follows {since: 2020}]->(b)");
            ExecuteQuery(connection, "MATCH (a:User {name: 'Karissa'}), (b:User {name: 'Zhang'}) CREATE (a)-[:Follows {since: 2021}]->(b)");
            ExecuteQuery(connection, "MATCH (a:User {name: 'Zhang'}), (b:User {name: 'Noura'}) CREATE (a)-[:Follows {since: 2022}]->(b)");

            // Create lives-in relationships
            ExecuteQuery(connection, "MATCH (u:User {name: 'Adam'}), (c:City {name: 'Waterloo'}) CREATE (u)-[:LivesIn]->(c)");
            ExecuteQuery(connection, "MATCH (u:User {name: 'Karissa'}), (c:City {name: 'Waterloo'}) CREATE (u)-[:LivesIn]->(c)");
            ExecuteQuery(connection, "MATCH (u:User {name: 'Zhang'}), (c:City {name: 'Kitchener'}) CREATE (u)-[:LivesIn]->(c)");
            ExecuteQuery(connection, "MATCH (u:User {name: 'Noura'}), (c:City {name: 'Guelph'}) CREATE (u)-[:LivesIn]->(c)");

            Console.WriteLine("  Sample data loaded successfully!");
            Console.WriteLine("  - 4 users with ages 25-50");
            Console.WriteLine("  - 3 cities with populations 75K-200K");
            Console.WriteLine("  - 4 follow relationships");
            Console.WriteLine("  - 4 lives-in relationships");
        }

        private static void BasicPatternMatching(Connection connection)
        {
            using var result = connection.Query("MATCH (a:User)-[f:Follows]->(b:User) RETURN a.name, f.since, b.name ORDER BY f.since, a.name");

            Console.WriteLine("  Follow relationships:");
            PrintQueryResults(result, "    ");
        }

        private static void AggregationExamples(Connection connection)
        {
            // Count followers per user
            Console.WriteLine("  Followers count per user:");
            using var followersResult = connection.Query(
                "MATCH (a:User)<-[f:Follows]-(b:User) RETURN a.name, COUNT(*) AS follower_count ORDER BY follower_count DESC");
            PrintQueryResults(followersResult, "    ");

            // Average age by city
            Console.WriteLine("\n  Average age by city:");
            using var avgAgeResult = connection.Query(
                "MATCH (u:User)-[:LivesIn]->(c:City) RETURN c.name, AVG(u.age) AS avg_age ORDER BY avg_age DESC");
            PrintQueryResults(avgAgeResult, "    ");

            // Total population across all cities
            Console.WriteLine("\n  Total population across all cities:");
            using var totalPopResult = connection.Query(
                "MATCH (c:City) RETURN SUM(c.population) AS total_population");
            PrintQueryResults(totalPopResult, "    ");
        }

        private static void ComplexQueries(Connection connection)
        {
            // Users who follow someone in the same city
            Console.WriteLine("  Users who follow someone in the same city:");
            using var sameCityResult = connection.Query(
                @"MATCH (u1:User)-[:Follows]->(u2:User), 
                        (u1)-[:LivesIn]->(c:City)<-[:LivesIn]-(u2) 
                  RETURN u1.name, u2.name, c.name");
            PrintQueryResults(sameCityResult, "    ");

            // Cities with users who have followers
            Console.WriteLine("\n  Cities with popular users (who have followers):");
            using var popularCitiesResult = connection.Query(
                @"MATCH (u:User)<-[:Follows]-(), (u)-[:LivesIn]->(c:City) 
                  RETURN DISTINCT c.name, c.population 
                  ORDER BY c.population DESC");
            PrintQueryResults(popularCitiesResult, "    ");
        }

        private static void DataModificationExamples(Connection connection)
        {
            // Add a new user
            Console.WriteLine("  Adding a new user 'Emma' age 28:");
            ExecuteQuery(connection, "CREATE (u:User {name: 'Emma', age: 28})");

            // Add a new city
            Console.WriteLine("  Adding a new city 'Toronto' with population 2800000:");
            ExecuteQuery(connection, "CREATE (c:City {name: 'Toronto', population: 2800000})");

            // Create relationships for Emma
            Console.WriteLine("  Creating relationships for Emma:");
            ExecuteQuery(connection, "MATCH (u:User {name: 'Emma'}), (c:City {name: 'Toronto'}) CREATE (u)-[:LivesIn]->(c)");
            ExecuteQuery(connection, "MATCH (u1:User {name: 'Emma'}), (u2:User {name: 'Adam'}) CREATE (u1)-[:Follows {since: 2024}]->(u2)");

            // Update user age
            Console.WriteLine("  Updating Adam's age to 31:");
            ExecuteQuery(connection, "MATCH (u:User {name: 'Adam'}) SET u.age = 31");

            // Show updated data
            Console.WriteLine("\n  Updated user list:");
            using var updatedUsersResult = connection.Query("MATCH (u:User) RETURN u.name, u.age ORDER BY u.name");
            PrintQueryResults(updatedUsersResult, "    ");
        }

        private static void PathQueries(Connection connection)
        {
            // Find paths of length 2 in the follow network
            Console.WriteLine("  Follow chains (paths of length 2):");
            using var followChainsResult = connection.Query(
                @"MATCH (u1:User)-[:Follows]->(u2:User)-[:Follows]->(u3:User) 
                  RETURN u1.name, u2.name, u3.name");
            PrintQueryResults(followChainsResult, "    ");

            // Find mutual followers
            Console.WriteLine("\n  Mutual follow relationships:");
            using var mutualResult = connection.Query(
                @"MATCH (u1:User)-[:Follows]->(u2:User)-[:Follows]->(u1) 
                  RETURN u1.name, u2.name");
            
            if (mutualResult.NumTuples == 0)
            {
                Console.WriteLine("    (No mutual follow relationships found)");
            }
            else
            {
                PrintQueryResults(mutualResult, "    ");
            }
        }

        private static void PropertyFilteringExamples(Connection connection)
        {
            // Users older than 30
            Console.WriteLine("  Users older than 30:");
            using var olderUsersResult = connection.Query(
                "MATCH (u:User) WHERE u.age > 30 RETURN u.name, u.age ORDER BY u.age");
            PrintQueryResults(olderUsersResult, "    ");

            // Cities with population greater than 100,000
            Console.WriteLine("\n  Large cities (population > 100,000):");
            using var largeCitiesResult = connection.Query(
                "MATCH (c:City) WHERE c.population > 100000 RETURN c.name, c.population ORDER BY c.population DESC");
            PrintQueryResults(largeCitiesResult, "    ");

            // Recent follows (since 2021)
            Console.WriteLine("\n  Recent follows (since 2021):");
            using var recentFollowsResult = connection.Query(
                @"MATCH (u1:User)-[f:Follows]->(u2:User) 
                  WHERE f.since >= 2021 
                  RETURN u1.name, u2.name, f.since 
                  ORDER BY f.since DESC");
            PrintQueryResults(recentFollowsResult, "    ");
        }

        private static void AdvancedPreparedStatementExample(Connection connection)
        {
            Console.WriteLine("  Using prepared statements for bulk operations:");

            // Create a table for bulk insert example
            ExecuteQuery(connection, "CREATE NODE TABLE Product(id INT64, name STRING, price DOUBLE, in_stock BOOLEAN, PRIMARY KEY(id))");

            // Prepare insert statement
            using var insertStmt = connection.Prepare("CREATE (:Product {id: $id, name: $name, price: $price, in_stock: $in_stock})");

            var products = new[]
            {
                (1, "Laptop", 999.99, true),
                (2, "Mouse", 29.99, true),
                (3, "Keyboard", 79.99, false),
                (4, "Monitor", 299.99, true),
                (5, "Headphones", 199.99, true)
            };

            Console.WriteLine($"  Inserting {products.Length} products using prepared statement:");
            foreach (var (id, name, price, inStock) in products)
            {
                insertStmt.BindInt64("id", id);
                insertStmt.BindString("name", name);
                insertStmt.BindDouble("price", price);
                insertStmt.BindBool("in_stock", inStock);

                using var insertResult = insertStmt.Execute();
                if (!insertResult.IsSuccess)
                {
                    Console.WriteLine($"    Failed to insert product {name}: {insertResult.ErrorMessage}");
                }
            }

            // Query products with prepared statement
            Console.WriteLine("\n  Products with price > $100 (using prepared statement):");
            using var queryStmt = connection.Prepare("MATCH (p:Product) WHERE p.price > $minPrice AND p.in_stock = $inStock RETURN p.name, p.price ORDER BY p.price DESC");
            queryStmt.BindDouble("minPrice", 100.0);
            queryStmt.BindBool("inStock", true);

            using var result = queryStmt.Execute();
            PrintQueryResults(result, "    ");
        }

        private static void DataTypesExample(Connection connection)
        {
            Console.WriteLine("  Demonstrating various data types:");

            // Create a comprehensive data types table
            ExecuteQuery(connection, @"CREATE NODE TABLE DataTypeDemo(
                id INT64,
                name STRING,
                active BOOLEAN,
                score DOUBLE,
                count INT32,
                rating FLOAT,
                PRIMARY KEY(id)
            )");

            // Insert data with various types
            ExecuteQuery(connection, "CREATE (:DataTypeDemo {id: 1, name: 'Sample A', active: true, score: 95.5, count: 42, rating: 4.8})");
            ExecuteQuery(connection, "CREATE (:DataTypeDemo {id: 2, name: 'Sample B', active: false, score: 87.2, count: 38, rating: 3.9})");
            ExecuteQuery(connection, "CREATE (:DataTypeDemo {id: 3, name: 'Sample C', active: true, score: 92.1, count: 45, rating: 4.5})");

            // Query and examine data types
            using var result = connection.Query("MATCH (d:DataTypeDemo) RETURN d.id, d.name, d.active, d.score, d.count, d.rating ORDER BY d.id");

            Console.WriteLine("  Column information:");
            for (ulong i = 0; i < result.NumColumns; i++)
            {
                var columnName = result.GetColumnName(i);
                var dataType = result.GetColumnDataType(i);
                Console.WriteLine($"    {columnName}: {dataType}");
            }

            Console.WriteLine("\n  Data with detailed type information:");
            foreach (var row in result)
            {
                Console.WriteLine($"    Row {row[0]}:");
                for (ulong i = 0; i < row.ColumnCount; i++)
                {
                    var value = row[i];
                    var columnName = result.GetColumnName(i);
                    var dataType = value.GetDataType();

                    string displayValue;
                    if (value.IsNull)
                    {
                        displayValue = "NULL";
                    }
                    else
                    {
                        // Try to get the most appropriate string representation
                        try
                        {
                            displayValue = dataType.ToString().ToLower() switch
                            {
                                var dt when dt.Contains("bool") => value.GetBool().ToString(),
                                var dt when dt.Contains("int64") => value.GetInt64().ToString(),
                                var dt when dt.Contains("int32") => value.GetInt32().ToString(),
                                var dt when dt.Contains("double") => value.GetDouble().ToString("F2"),
                                var dt when dt.Contains("float") => value.GetFloat().ToString("F1"),
                                var dt when dt.Contains("string") => $"\"{value.GetString()}\"",
                                _ => value.ToString()
                            };
                        }
                        catch
                        {
                            displayValue = value.ToString();
                        }
                    }

                    Console.WriteLine($"      {columnName} ({dataType}): {displayValue}");
                }
                Console.WriteLine();
            }
        }

        private static void PrintQueryResults(QueryResult result, string indent = "")
        {
            if (result.NumTuples == 0)
            {
                Console.WriteLine($"{indent}(No results)");
                return;
            }

            // Print column headers
            var columnNames = new string[result.NumColumns];
            for (ulong i = 0; i < result.NumColumns; i++)
            {
                columnNames[i] = result.GetColumnName(i);
            }

            // Print results
            foreach (var row in result)
            {
                var values = new List<string>();
                for (ulong i = 0; i < row.ColumnCount; i++)
                {
                    var value = row[i];
                    if (value.IsNull)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        // Try to format the value appropriately
                        try
                        {
                            var dataType = value.GetDataType().ToString().ToLower();
                            if (dataType.Contains("double"))
                            {
                                values.Add(value.GetDouble().ToString("F2"));
                            }
                            else if (dataType.Contains("float"))
                            {
                                values.Add(value.GetFloat().ToString("F1"));
                            }
                            else
                            {
                                values.Add(value.ToString());
                            }
                        }
                        catch
                        {
                            values.Add(value.ToString());
                        }
                    }
                }
                Console.WriteLine($"{indent}{string.Join(" | ", values)}");
            }
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
            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not clean up database file: {ex.Message}");
            }
        }
    }
}