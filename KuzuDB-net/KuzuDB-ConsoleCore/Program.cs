using static kuzunet;

namespace KuzuDB_ConsoleCore
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDB .NET Core Examples ===\n");

            using kuzu_database db = new();
            using kuzu_connection conn = new();
            using kuzu_system_config config = kuzu_default_system_config();

            var state = kuzu_database_init("", config, db);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Could not create DB");
                return;
            }

            state = kuzu_connection_init(db, conn);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Could not connect to DB");
                return;
            }

            // Example 1: Schema Creation
            Console.WriteLine("1. Creating Schema...");
            CreateSchema(conn);

            // Example 2: Data Loading
            Console.WriteLine("\n2. Loading Data from CSV files...");
            LoadData(conn);

            // Example 3: Basic Pattern Matching
            Console.WriteLine("\n3. Basic Pattern Matching - Who follows whom:");
            BasicPatternMatching(conn);

            // Example 4: Aggregation Queries
            Console.WriteLine("\n4. Aggregation Examples:");
            AggregationExamples(conn);

            // Example 5: Complex Queries with Multiple Relationships
            Console.WriteLine("\n5. Complex Multi-Relationship Queries:");
            ComplexQueries(conn);

            // Example 6: Data Modification Examples
            Console.WriteLine("\n6. Data Modification Examples:");
            DataModificationExamples(conn);

            // Example 7: Path Queries
            Console.WriteLine("\n7. Path Finding Examples:");
            PathQueries(conn);

            // Example 8: Property Filtering
            Console.WriteLine("\n8. Property Filtering Examples:");
            PropertyFilteringExamples(conn);

            Console.WriteLine("\n=== All Examples Completed ===");
        }

        private static void CreateSchema(kuzu_connection conn)
        {
            PerformNonQuery(conn, "CREATE NODE TABLE User(name STRING, age INT64, PRIMARY KEY (name))");
            PerformNonQuery(conn, "CREATE NODE TABLE City(name STRING, population INT64, PRIMARY KEY (name))");
            PerformNonQuery(conn, "CREATE REL TABLE Follows(FROM User TO User, since INT64)");
            PerformNonQuery(conn, "CREATE REL TABLE LivesIn(FROM User TO City)");
            Console.WriteLine("Schema created successfully!");
        }

        private static void LoadData(kuzu_connection conn)
        {
            PerformNonQuery(conn, "COPY User FROM \"csv/users.csv\"");
            PerformNonQuery(conn, "COPY City FROM \"csv/cities.csv\"");
            PerformNonQuery(conn, "COPY Follows FROM \"csv/follows.csv\"");
            PerformNonQuery(conn, "COPY LivesIn FROM \"csv/lives-in.csv\"");
            Console.WriteLine("Data loaded successfully!");
        }

        private static void BasicPatternMatching(kuzu_connection conn)
        {
            using kuzu_query_result result = new();
            var state = PerformQuery(conn, "MATCH (a:User)-[f:Follows]->(b:User) RETURN a.name, f.since, b.name;", result);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Error performing MATCH");
                return;
            }

            while (kuzu_query_result_has_next(result))
            {
                string name, name2;
                long since;

                using kuzu_flat_tuple tuple = new();
                kuzu_query_result_get_next(result, tuple);

                using (kuzu_value value = new())
                {
                    kuzu_flat_tuple_get_value(tuple, 0, value);
                    kuzu_value_get_string(value, out name);
                }

                using (kuzu_value value = new())
                {
                    kuzu_flat_tuple_get_value(tuple, 1, value);
                    kuzu_value_get_int64(value, out since);
                }

                using (kuzu_value value = new())
                {
                    kuzu_flat_tuple_get_value(tuple, 2, value);
                    kuzu_value_get_string(value, out name2);
                }

                Console.WriteLine($"  {name} follows {name2} since {since}");
            }
        }

        private static void AggregationExamples(kuzu_connection conn)
        {
            // Count followers per user
            Console.WriteLine("  Followers count per user:");
            ExecuteSimpleQuery(conn, 
                "MATCH (a:User)<-[f:Follows]-(b:User) RETURN a.name, COUNT(*) AS follower_count ORDER BY follower_count DESC;");

            // Average age by city
            Console.WriteLine("\n  Average age by city:");
            ExecuteSimpleQuery(conn, 
                "MATCH (u:User)-[:LivesIn]->(c:City) RETURN c.name, AVG(u.age) AS avg_age ORDER BY avg_age DESC;");

            // Total population
            Console.WriteLine("\n  Total population across all cities:");
            ExecuteSimpleQuery(conn, 
                "MATCH (c:City) RETURN SUM(c.population) AS total_population;");
        }

        private static void ComplexQueries(kuzu_connection conn)
        {
            // Users who follow someone in the same city
            Console.WriteLine("  Users who follow someone in the same city:");
            ExecuteSimpleQuery(conn,
                @"MATCH (u1:User)-[:Follows]->(u2:User), 
                        (u1)-[:LivesIn]->(c:City)<-[:LivesIn]-(u2) 
                  RETURN u1.name, u2.name, c.name;");

            // Cities with users who have followers
            Console.WriteLine("\n  Cities with popular users (who have followers):");
            ExecuteSimpleQuery(conn,
                @"MATCH (u:User)<-[:Follows]-(), (u)-[:LivesIn]->(c:City) 
                  RETURN DISTINCT c.name, c.population 
                  ORDER BY c.population DESC;");
        }

        private static void DataModificationExamples(kuzu_connection conn)
        {
            // Add a new user
            Console.WriteLine("  Adding a new user 'Emma' age 28:");
            PerformNonQuery(conn, "CREATE (u:User {name: 'Emma', age: 28})");

            // Add a new city
            Console.WriteLine("  Adding a new city 'Toronto' with population 2800000:");
            PerformNonQuery(conn, "CREATE (c:City {name: 'Toronto', population: 2800000})");

            // Create relationships
            Console.WriteLine("  Creating relationships for Emma:");
            PerformNonQuery(conn, "MATCH (u:User {name: 'Emma'}), (c:City {name: 'Toronto'}) CREATE (u)-[:LivesIn]->(c)");
            PerformNonQuery(conn, "MATCH (u1:User {name: 'Emma'}), (u2:User {name: 'Adam'}) CREATE (u1)-[:Follows {since: 2024}]->(u2)");

            // Update user age
            Console.WriteLine("  Updating Adam's age to 31:");
            PerformNonQuery(conn, "MATCH (u:User {name: 'Adam'}) SET u.age = 31");

            // Show updated data
            Console.WriteLine("\n  Updated user list:");
            ExecuteSimpleQuery(conn, "MATCH (u:User) RETURN u.name, u.age ORDER BY u.name;");
        }

        private static void PathQueries(kuzu_connection conn)
        {
            // Find paths of length 2 in the follow network
            Console.WriteLine("  Follow chains (paths of length 2):");
            ExecuteSimpleQuery(conn,
                @"MATCH (u1:User)-[:Follows]->(u2:User)-[:Follows]->(u3:User) 
                  RETURN u1.name, u2.name, u3.name;");

            // Find mutual followers
            Console.WriteLine("\n  Mutual follow relationships:");
            ExecuteSimpleQuery(conn,
                @"MATCH (u1:User)-[:Follows]->(u2:User)-[:Follows]->(u1) 
                  RETURN u1.name, u2.name;");
        }

        private static void PropertyFilteringExamples(kuzu_connection conn)
        {
            // Users older than 30
            Console.WriteLine("  Users older than 30:");
            ExecuteSimpleQuery(conn, "MATCH (u:User) WHERE u.age > 30 RETURN u.name, u.age ORDER BY u.age;");

            // Cities with population greater than 100,000
            Console.WriteLine("\n  Large cities (population > 100,000):");
            ExecuteSimpleQuery(conn, "MATCH (c:City) WHERE c.population > 100000 RETURN c.name, c.population ORDER BY c.population DESC;");

            // Recent follows (since 2021)
            Console.WriteLine("\n  Recent follows (since 2021):");
            ExecuteSimpleQuery(conn,
                @"MATCH (u1:User)-[f:Follows]->(u2:User) 
                  WHERE f.since >= 2021 
                  RETURN u1.name, u2.name, f.since 
                  ORDER BY f.since DESC;");
        }

        private static void ExecuteSimpleQuery(kuzu_connection conn, string query)
        {
            using kuzu_query_result result = new();
            var state = PerformQuery(conn, query, result);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine($"    Error executing query: {query}");
                Console.WriteLine($"    Error message: {kuzu_query_result_get_error_message(result)}");
                return;
            }

            // Check if query was successful
            if (!kuzu_query_result_is_success(result))
            {
                Console.WriteLine($"    Query failed: {kuzu_query_result_get_error_message(result)}");
                return;
            }

            int rowCount = 0;
            try
            {
                while (kuzu_query_result_has_next(result))
                {
                    using kuzu_flat_tuple tuple = new();
                    var nextState = kuzu_query_result_get_next(result, tuple);
                    
                    if (nextState != kuzu_state.KuzuSuccess)
                    {
                        Console.WriteLine($"    Error getting next tuple: {nextState}");
                        break;
                    }
                    
                    var values = new List<string>();
                    
                    // Get the number of columns to avoid accessing invalid indices
                    ulong numColumns = kuzu_query_result_get_num_columns(result);
                    
                    for (uint i = 0; i < numColumns; i++)
                    {
                        try
                        {
                            string valueStr = GetValueAsString(tuple, i);
                            values.Add(valueStr);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"    Error accessing column {i}: {ex.Message}");
                            values.Add($"ERROR_COL_{i}");
                        }
                    }
                    
                    Console.WriteLine($"    {string.Join(" | ", values)}");
                    rowCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Fatal error during query execution: {ex.Message}");
                Console.WriteLine($"    Query: {query}");
            }
            
            if (rowCount == 0)
            {
                Console.WriteLine("    (No results)");
            }
        }

        private static string GetValueAsString(kuzu_flat_tuple tuple, uint columnIndex)
        {
            using kuzu_value value = new();
            
            // Get the value from the tuple
            var state = kuzu_flat_tuple_get_value(tuple, columnIndex, value);
            if (state != kuzu_state.KuzuSuccess)
            {
                return $"GET_ERROR_{columnIndex}";
            }

            try
            {
                // Check if the value is null first with proper error handling
                bool isNull;
                try
                {
                    isNull = kuzu_value_is_null(value);
                }
                catch (Exception)
                {
                    // If kuzu_value_is_null fails, assume the value is corrupted
                    return "CORRUPTED_VALUE";
                }

                if (isNull)
                {
                    return "NULL";
                }

                // Try to get the value as different types, starting with string
                try
                {
                    var stringState = kuzu_value_get_string(value, out string stringVal);
                    if (stringState == kuzu_state.KuzuSuccess)
                    {
                        return stringVal ?? "NULL_STRING";
                    }
                }
                catch (Exception)
                {
                    // Continue to try other types
                }

                try
                {
                    var longState = kuzu_value_get_int64(value, out long longVal);
                    if (longState == kuzu_state.KuzuSuccess)
                    {
                        return longVal.ToString();
                    }
                }
                catch (Exception)
                {
                    // Continue to try other types
                }

                try
                {
                    var doubleState = kuzu_value_get_double(value, out double doubleVal);
                    if (doubleState == kuzu_state.KuzuSuccess)
                    {
                        return doubleVal.ToString("F2");
                    }
                }
                catch (Exception)
                {
                    // Continue to try other types
                }

                try
                {
                    var boolState = kuzu_value_get_bool(value, out bool boolVal);
                    if (boolState == kuzu_state.KuzuSuccess)
                    {
                        return boolVal.ToString();
                    }
                }
                catch (Exception)
                {
                    // Continue to try other types
                }

                // If all type conversions fail, try to get string representation
                try
                {
                    string toString = kuzu_value_to_string(value);
                    return toString ?? "UNKNOWN_TYPE";
                }
                catch (Exception)
                {
                    return "CONVERSION_ERROR";
                }
            }
            catch (Exception ex)
            {
                return $"EXCEPTION_{ex.GetType().Name}";
            }
        }

        private static void PerformNonQuery(kuzu_connection conn, string query)
        {
            using kuzu_query_result result = new();

            var state = kuzu_connection_query(conn, query, result);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine($"Could not perform: {query}");
                Console.WriteLine($"Error: {kuzu_query_result_get_error_message(result)}");
                return;
            }

            if (!kuzu_query_result_is_success(result))
            {
                Console.WriteLine($"Query failed: {query}");
                Console.WriteLine($"Error: {kuzu_query_result_get_error_message(result)}");
                return;
            }
        }

        private static kuzu_state PerformQuery(kuzu_connection conn, string query, kuzu_query_result result)
        {
            return kuzu_connection_query(conn, query, result);
        }
    }
}
