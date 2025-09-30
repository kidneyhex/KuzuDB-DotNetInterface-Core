using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Stress tests and memory leak detection tests for KuzuDB NetCore wrapper.
    /// These tests verify system stability under high load and proper resource cleanup.
    /// </summary>
    public class StressTests : BaseKuzuNetCoreTest
    {
        private const int STRESS_ITERATIONS = 1000;
        private const int MEMORY_PRESSURE_ITERATIONS = 10000;
        private const int CONCURRENT_OPERATIONS = 50;

        [Fact]
        public void StressTest_ManyQueryExecutions_ShouldNotLeakMemory()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
            var processedRows = 0;

            // Act
            for (int i = 0; i < STRESS_ITERATIONS; i++)
            {
                using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.id");
                
                // Iterate through results to ensure they're fully processed
                while (kuzunet.kuzu_query_result_has_next(result))
                {
                    using var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                    processedRows++;
                }

                // Force garbage collection every 100 iterations to check for memory growth
                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            
            // Allow for some reasonable memory growth (less than 20MB for better leak detection)
            Assert.True(memoryGrowth < 20 * 1024 * 1024, 
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB, which suggests a memory leak");
                
            // Verify we actually processed data
            Assert.True(processedRows > 0, "No rows were processed");
            
            // Log for debugging
            Console.WriteLine($"Stress test processed {processedRows} rows with {memoryGrowth / 1024 / 1024}MB memory growth");
        }

        [Fact]
        public void StressTest_QueryResultDisposal_ShouldReleaseResourcesProperly()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // Act
            for (int i = 0; i < MEMORY_PRESSURE_ITERATIONS; i++)
            {
                var result = ExecuteQuery("MATCH (p:Person) RETURN p.name, p.age, p.active ORDER BY p.id");
                
                // Sometimes dispose explicitly, sometimes let finalizer handle it
                if (i % 2 == 0)
                {
                    result.Dispose();
                }
                // Let finalizer handle the rest

                if (i % 1000 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            // Final cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            
            Assert.True(memoryGrowth < 50 * 1024 * 1024, 
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB, indicating potential memory leak in disposal");
        }

        [Fact]
        public void StressTest_ConcurrentQueries_ShouldHandleParallelLoad()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var exceptions = new ConcurrentBag<Exception>();
            var successful = 0;

            // Act
            Parallel.For(0, CONCURRENT_OPERATIONS, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, i =>
            {
                try
                {
                    // Each thread uses its own connection to avoid concurrency issues
                    using var systemConfig = kuzunet.kuzu_default_system_config();
                    using var database = new kuzu_database();
                    
                    var tempDbPath = Path.Combine(Path.GetTempPath(), $"concurrent_db_{i}_{Guid.NewGuid():N}");
                    var dbResult = kuzunet.kuzu_database_init(tempDbPath, systemConfig, database);
                    
                    if (dbResult == kuzu_state.KuzuSuccess)
                    {
                        using var connection = new kuzu_connection();
                        var connResult = kuzunet.kuzu_connection_init(database, connection);
                        
                        if (connResult == kuzu_state.KuzuSuccess)
                        {
                            // Set up schema and data for this thread's database
                            using var createResult = new kuzu_query_result();
                            var createState = kuzunet.kuzu_connection_query(connection, 
                                "CREATE NODE TABLE Person(id INT64, name STRING, age INT32, PRIMARY KEY(id))", createResult);
                                
                            if (createState == kuzu_state.KuzuSuccess && kuzunet.kuzu_query_result_is_success(createResult))
                            {
                                // Insert test data
                                using var insertResult = new kuzu_query_result();
                                var insertState = kuzunet.kuzu_connection_query(connection, 
                                    "CREATE (:Person {id: 1, name: 'Test User', age: 30})", insertResult);
                                    
                                if (insertState == kuzu_state.KuzuSuccess && kuzunet.kuzu_query_result_is_success(insertResult))
                                {
                                    // Execute multiple queries per thread
                                    for (int j = 0; j < 10; j++)
                                    {
                                        using var result = new kuzu_query_result();
                                        var queryResult = kuzunet.kuzu_connection_query(connection, 
                                            "MATCH (p:Person) RETURN p.name ORDER BY p.id", result);
                                        
                                        if (queryResult == kuzu_state.KuzuSuccess && 
                                            kuzunet.kuzu_query_result_is_success(result))
                                        {
                                            Interlocked.Increment(ref successful);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Cleanup database directory
                    try
                    {
                        if (Directory.Exists(tempDbPath))
                            Directory.Delete(tempDbPath, recursive: true);
                        else if (File.Exists(tempDbPath))
                            File.Delete(tempDbPath);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.True(exceptions.IsEmpty, $"Concurrent operations had exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");
            Assert.True(successful > 0, "No successful concurrent operations completed");
        }

        [Fact]
        public void StressTest_LargeResultSets_ShouldHandleEfficiently()
        {
            // Arrange
            SetupTestDatabase();
            
            // Create a larger dataset
            ExecuteQuery("CREATE NODE TABLE LargeTable(id INT64, value STRING, data DOUBLE, PRIMARY KEY(id))");
            
            // Insert many records in batches
            for (int batch = 0; batch < 10; batch++)
            {
                var batchQuery = "CREATE ";
                for (int i = 0; i < 100; i++)
                {
                    var id = batch * 100 + i;
                    batchQuery += $"(:LargeTable {{id: {id}, value: 'data_{id}', data: {id * 1.5}}})";
                    if (i < 99) batchQuery += ", ";
                }
                ExecuteQuery(batchQuery);
            }

            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // Act
            using var result = ExecuteQuery("MATCH (l:LargeTable) RETURN l.id, l.value, l.data ORDER BY l.id");
            
            var processedRows = 0;
            while (kuzunet.kuzu_query_result_has_next(result))
            {
                using var flatTuple = new kuzu_flat_tuple();
                kuzunet.kuzu_query_result_get_next(result, flatTuple);
                processedRows++;
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(forceFullCollection: false);

            // Assert
            Assert.Equal(1000, processedRows); // Should have processed 1000 rows
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, // Should complete within 10 seconds
                $"Large result set processing took too long: {stopwatch.ElapsedMilliseconds}ms");
                
            var memoryUsed = finalMemory - initialMemory;
            Assert.True(memoryUsed < 100 * 1024 * 1024, // Should use less than 100MB
                $"Large result set used too much memory: {memoryUsed / 1024 / 1024}MB");
        }

        [Fact]
        public void StressTest_PreparedStatementReuse_ShouldBeEfficient()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // Act
            using var preparedStatement = new kuzu_prepared_statement();
            var prepareResult = kuzunet.kuzu_connection_prepare(TestConnection!, 
                "MATCH (p:Person) WHERE p.id = $id RETURN p.name, p.age", preparedStatement);
            
            Assert.Equal(kuzu_state.KuzuSuccess, prepareResult);

            for (int i = 0; i < STRESS_ITERATIONS; i++)
            {
                // Bind parameter with proper validation
                var bindResult = kuzunet.kuzu_prepared_statement_bind_int64(preparedStatement, "id", (i % 3) + 1);
                Assert.Equal(kuzu_state.KuzuSuccess, bindResult);

                // Execute
                using var result = new kuzu_query_result();
                var executeResult = kuzunet.kuzu_connection_execute(TestConnection!, preparedStatement, result);
                
                Assert.Equal(kuzu_state.KuzuSuccess, executeResult);
                Assert.True(kuzunet.kuzu_query_result_is_success(result));

                // Process results
                while (kuzunet.kuzu_query_result_has_next(result))
                {
                    using var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                }

                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            
            Assert.True(memoryGrowth < 5 * 1024 * 1024, 
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB during prepared statement reuse");
        }

        [Fact]
        public void StressTest_ConnectionRecreation_ShouldNotLeakResources()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // Act
            for (int i = 0; i < 100; i++)
            {
                using var systemConfig = kuzunet.kuzu_default_system_config();
                using var database = new kuzu_database();
                
                var tempDbPath = Path.Combine(Path.GetTempPath(), $"stress_db_{i}_{Guid.NewGuid():N}");
                var dbResult = kuzunet.kuzu_database_init(tempDbPath, systemConfig, database);
                Assert.Equal(kuzu_state.KuzuSuccess, dbResult);

                using var connection = new kuzu_connection();
                var connResult = kuzunet.kuzu_connection_init(database, connection);
                Assert.Equal(kuzu_state.KuzuSuccess, connResult);

                // Create schema and insert data
                using var createResult = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection, 
                    "CREATE NODE TABLE TempTable(id INT64, name STRING, PRIMARY KEY(id))", createResult);
                
                using var insertResult = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection, 
                    "CREATE (:TempTable {id: 1, name: 'test'})", insertResult);

                using var queryResult = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection, 
                    "MATCH (t:TempTable) RETURN t.id, t.name", queryResult);

                // Cleanup database directory
                try
                {
                    if (Directory.Exists(tempDbPath))
                        Directory.Delete(tempDbPath, recursive: true);
                    else if (File.Exists(tempDbPath))
                        File.Delete(tempDbPath);
                }
                catch { /* Ignore cleanup errors */ }

                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            
            Assert.True(memoryGrowth < 20 * 1024 * 1024, 
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB during connection recreation");
        }

        [Fact]
        public void StressTest_ComplexQueryExecution_ShouldHandleIntensiveOperations()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var stopwatch = Stopwatch.StartNew();

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                // Complex query with joins, aggregations, and sorting
                using var result = ExecuteQuery(@"
                    MATCH (p:Person)-[w:WorksFor]->(c:Company)
                    WITH p, c, w, p.age * w.salary as weighted_value
                    WHERE p.active = true
                    RETURN p.name, c.name, COUNT(*) as relationships, 
                           AVG(w.salary) as avg_salary, 
                           SUM(weighted_value) as total_weighted
                    ORDER BY avg_salary DESC
                ");

                Assert.True(kuzunet.kuzu_query_result_is_success(result));
                
                while (kuzunet.kuzu_query_result_has_next(result))
                {
                    using var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                }
            }

            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds < 30000, 
                $"Complex query stress test took too long: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void MemoryLeakTest_FinalizerCleanup_ShouldReleaseUnmanagedResources()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
            var weakRefs = new List<WeakReference>();

            // Act - Create objects without explicit disposal to test finalizers
            CreateQueryResultsWithoutDisposalAndTrack(weakRefs, 500);
            
            // Force finalizer execution with multiple attempts
            for (int i = 0; i < 5; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);
                Thread.Sleep(200); // Give finalizers adequate time
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            var stillAlive = weakRefs.Count(wr => wr.IsAlive);
            var alivePercentage = (double)stillAlive / weakRefs.Count;
            
            Assert.True(memoryGrowth < 30 * 1024 * 1024, 
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB, finalizers may not be cleaning up properly");
                
            // Allow for some objects to still be alive due to GC timing
            Assert.True(alivePercentage < 0.3, 
                $"Too many objects still alive: {stillAlive}/{weakRefs.Count} ({alivePercentage:P1}), finalizers may not be working properly");
                
            // Log results for debugging
            Console.WriteLine($"Finalizer test: {memoryGrowth / 1024 / 1024}MB growth, {stillAlive}/{weakRefs.Count} objects still alive");
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateQueryResultsWithoutDisposalAndTrack(List<WeakReference> weakRefs, int count)
        {
            // This method intentionally doesn't dispose query results to test finalizer cleanup
            for (int i = 0; i < count; i++)
            {
                var result = new kuzu_query_result();
                weakRefs.Add(new WeakReference(result));
                
                kuzunet.kuzu_connection_query(TestConnection!, "MATCH (p:Person) RETURN p.name LIMIT 1", result);
                
                if (kuzunet.kuzu_query_result_is_success(result) && kuzunet.kuzu_query_result_has_next(result))
                {
                    var flatTuple = new kuzu_flat_tuple();
                    weakRefs.Add(new WeakReference(flatTuple));
                    
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                    
                    // Clear local references
                    flatTuple = null!;
                }
                
                result = null!; // Clear local reference
                // Intentionally not disposing to test finalizer
            }
        }

        [Fact]
        public void StressTest_ErrorHandling_ShouldBeRobustUnderLoad()
        {
            // Arrange
            SetupTestDatabaseWithData(); // Ensure we have proper schema and data
            var successCount = 0;
            var errorCount = 0;
            var totalQueries = 500;

            // Act
            for (int i = 0; i < totalQueries; i++)
            {
                // Mix valid and invalid queries - ensure we get both success and failure cases
                string query;
                bool shouldSucceed;
                
                if (i % 3 == 0)
                {
                    query = "INVALID QUERY SYNTAX"; // Invalid syntax
                    shouldSucceed = false;
                }
                else if (i % 5 == 0)
                {
                    query = "MATCH (nonexistent:NonexistentTable) RETURN *"; // Valid syntax but nonexistent table
                    shouldSucceed = false;
                }
                else
                {
                    query = "MATCH (p:Person) RETURN COUNT(*)"; // Valid query
                    shouldSucceed = true;
                }
                
                using var result = new kuzu_query_result();
                var queryState = kuzunet.kuzu_connection_query(TestConnection!, query, result);
                
                bool actualSuccess = queryState == kuzu_state.KuzuSuccess && kuzunet.kuzu_query_result_is_success(result);
                
                if (actualSuccess)
                {
                    successCount++;
                    
                    // For successful queries, verify we can process results
                    if (shouldSucceed)
                    {
                        while (kuzunet.kuzu_query_result_has_next(result))
                        {
                            using var flatTuple = new kuzu_flat_tuple();
                            kuzunet.kuzu_query_result_get_next(result, flatTuple);
                        }
                    }
                }
                else
                {
                    errorCount++;
                    // For failed queries, verify we can get error message
                    var errorMsg = kuzunet.kuzu_query_result_get_error_message(result);
                    Assert.False(string.IsNullOrEmpty(errorMsg), "Error queries should provide error messages");
                }
            }

            // Assert
            Assert.True(successCount > 0, "No successful queries executed");
            Assert.True(errorCount > 0, "No error queries were detected - test may not be testing error scenarios properly");
            Assert.Equal(totalQueries, successCount + errorCount);
            
            // Verify we got a reasonable mix (at least 20% of each type)
            var successRate = (double)successCount / totalQueries;
            var errorRate = (double)errorCount / totalQueries;
            
            Assert.True(successRate >= 0.2, $"Success rate too low: {successRate:P1}");
            Assert.True(errorRate >= 0.2, $"Error rate too low: {errorRate:P1}");
            
            // Log for debugging
            Console.WriteLine($"Error handling test: {successCount} successes, {errorCount} errors ({successRate:P1} success rate)");
        }

        [Fact]
        public void PerformanceTest_QueryExecutionTime_ShouldBeMaintainedUnderLoad()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var executionTimes = new List<long>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                using var result = ExecuteQuery("MATCH (p:Person)-[w:WorksFor]->(c:Company) RETURN p.name, c.name, w.salary ORDER BY w.salary DESC");
                
                while (kuzunet.kuzu_query_result_has_next(result))
                {
                    using var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                }
                
                stopwatch.Stop();
                executionTimes.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var averageTime = executionTimes.Average();
            var maxTime = executionTimes.Max();
            var minTime = executionTimes.Min();

            Assert.True(averageTime < 1000, $"Average execution time too high: {averageTime}ms");
            Assert.True(maxTime < 5000, $"Maximum execution time too high: {maxTime}ms");
            
            // Check for performance degradation - max shouldn't be much higher than average
            Assert.True(maxTime < averageTime * 10, 
                $"Performance degradation detected: max={maxTime}ms, avg={averageTime}ms");
        }

        [Fact]
        public void MultipleConnections_ToSameDatabase_ShouldWork()
        {
            // Arrange - Single database, multiple connections
            SetupTestDatabaseWithData();
            
            using var systemConfig2 = kuzunet.kuzu_default_system_config();
            using var connection2 = new kuzu_connection();
            
            // Try to create second connection to same database
            var connResult = kuzunet.kuzu_connection_init(TestDatabase!, connection2);
            
            if (connResult == kuzu_state.KuzuSuccess)
            {
                // KuzuDB supports multiple connections - test concurrent reads
                var exceptions = new ConcurrentBag<Exception>();
                var connection1Results = new ConcurrentBag<int>();
                var connection2Results = new ConcurrentBag<int>();
                var successfulReads = 0;

                // Test concurrent reads from both connections
                var tasks = new[]
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            // Connection 1 performs multiple reads
                            for (int i = 0; i < 10; i++)
                            {
                                using var result = new kuzu_query_result();
                                var queryResult = kuzunet.kuzu_connection_query(TestConnection!, 
                                    "MATCH (p:Person) RETURN COUNT(*) as count", result);
                                
                                if (queryResult == kuzu_state.KuzuSuccess && 
                                    kuzunet.kuzu_query_result_is_success(result))
                                {
                                    // Process result to get count
                                    if (kuzunet.kuzu_query_result_has_next(result))
                                    {
                                        using var flatTuple = new kuzu_flat_tuple();
                                        kuzunet.kuzu_query_result_get_next(result, flatTuple);
                                        using var countValue = new kuzu_value();
                                        kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, countValue);
                                        kuzunet.kuzu_value_get_int64(countValue, out long count);
                                        connection1Results.Add((int)count);
                                        Interlocked.Increment(ref successfulReads);
                                    }
                                }
                                
                                // Small delay to allow interleaving
                                Thread.Sleep(10);
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }),
                    
                    Task.Run(() =>
                    {
                        try
                        {
                            // Connection 2 performs multiple reads
                            for (int i = 0; i < 10; i++)
                            {
                                using var result = new kuzu_query_result();
                                var queryResult = kuzunet.kuzu_connection_query(connection2, 
                                    "MATCH (c:Company) RETURN COUNT(*) as count", result);
                                
                                if (queryResult == kuzu_state.KuzuSuccess && 
                                    kuzunet.kuzu_query_result_is_success(result))
                                {
                                    // Process result to get count
                                    if (kuzunet.kuzu_query_result_has_next(result))
                                    {
                                        using var flatTuple = new kuzu_flat_tuple();
                                        kuzunet.kuzu_query_result_get_next(result, flatTuple);
                                        using var countValue = new kuzu_value();
                                        kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, countValue);
                                        kuzunet.kuzu_value_get_int64(countValue, out long count);
                                        connection2Results.Add((int)count);
                                        Interlocked.Increment(ref successfulReads);
                                    }
                                }
                                
                                // Small delay to allow interleaving
                                Thread.Sleep(10);
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }),
                    
                    Task.Run(() =>
                    {
                        try
                        {
                            // Both connections perform complex reads simultaneously
                            for (int i = 0; i < 5; i++)
                            {
                                // Connection 1: Complex query with joins
                                using var result1 = new kuzu_query_result();
                                var query1Result = kuzunet.kuzu_connection_query(TestConnection!, 
                                    "MATCH (p:Person)-[w:WorksFor]->(c:Company) RETURN p.name, c.name ORDER BY p.id", result1);
                                
                                if (query1Result == kuzu_state.KuzuSuccess && 
                                    kuzunet.kuzu_query_result_is_success(result1))
                                {
                                    var rowCount = 0;
                                    while (kuzunet.kuzu_query_result_has_next(result1))
                                    {
                                        using var flatTuple = new kuzu_flat_tuple();
                                        kuzunet.kuzu_query_result_get_next(result1, flatTuple);
                                        rowCount++;
                                    }
                                    connection1Results.Add(rowCount);
                                    Interlocked.Increment(ref successfulReads);
                                }
                                
                                // Connection 2: Different complex query
                                using var result2 = new kuzu_query_result();
                                var query2Result = kuzunet.kuzu_connection_query(connection2, 
                                    "MATCH (p:Person) WHERE p.active = true RETURN p.name, p.age ORDER BY p.age", result2);
                                
                                if (query2Result == kuzu_state.KuzuSuccess && 
                                    kuzunet.kuzu_query_result_is_success(result2))
                                {
                                    var rowCount = 0;
                                    while (kuzunet.kuzu_query_result_has_next(result2))
                                    {
                                        using var flatTuple = new kuzu_flat_tuple();
                                        kuzunet.kuzu_query_result_get_next(result2, flatTuple);
                                        rowCount++;
                                    }
                                    connection2Results.Add(rowCount);
                                    Interlocked.Increment(ref successfulReads);
                                }
                                
                                Thread.Sleep(20);
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    })
                };

                // Wait for all concurrent operations to complete
                Task.WaitAll(tasks);

                // Assert concurrent reads succeeded
                Assert.True(exceptions.IsEmpty, 
                    $"Concurrent read operations had exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");
                Assert.True(successfulReads > 0, "No successful concurrent reads completed");
                Assert.True(connection1Results.Count > 0, "Connection 1 performed no successful reads");
                Assert.True(connection2Results.Count > 0, "Connection 2 performed no successful reads");

                // Verify data consistency - Person count should be consistent
                var personCounts = connection1Results.Where(r => r == 3).ToList(); // Expecting 3 persons from sample data
                Assert.True(personCounts.Count > 0, "Person count queries should return consistent results");

                // Verify Company count should be consistent 
                var companyCounts = connection2Results.Where(r => r == 2).ToList(); // Expecting 2 companies from sample data
                Assert.True(companyCounts.Count > 0, "Company count queries should return consistent results");

                // Test prepared statements with multiple connections
                using var stmt1 = new kuzu_prepared_statement();
                using var stmt2 = new kuzu_prepared_statement();
                
                var prep1Result = kuzunet.kuzu_connection_prepare(TestConnection!, 
                    "MATCH (p:Person) WHERE p.id = $id RETURN p.name", stmt1);
                var prep2Result = kuzunet.kuzu_connection_prepare(connection2, 
                    "MATCH (c:Company) WHERE c.id = $id RETURN c.name", stmt2);
                
                if (prep1Result == kuzu_state.KuzuSuccess && prep2Result == kuzu_state.KuzuSuccess)
                {
                    // Test concurrent prepared statement execution
                    var preparedTasks = new[]
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                for (int id = 1; id <= 3; id++)
                                {
                                    kuzunet.kuzu_prepared_statement_bind_int64(stmt1, "id", id);
                                    using var result = new kuzu_query_result();
                                    var execResult = kuzunet.kuzu_connection_execute(TestConnection!, stmt1, result);
                                    
                                    if (execResult == kuzu_state.KuzuSuccess && 
                                        kuzunet.kuzu_query_result_is_success(result))
                                    {
                                        Interlocked.Increment(ref successfulReads);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        }),
                        
                        Task.Run(() =>
                        {
                            try
                            {
                                for (int id = 1; id <= 2; id++)
                                {
                                    kuzunet.kuzu_prepared_statement_bind_int64(stmt2, "id", id);
                                    using var result = new kuzu_query_result();
                                    var execResult = kuzunet.kuzu_connection_execute(connection2, stmt2, result);
                                    
                                    if (execResult == kuzu_state.KuzuSuccess && 
                                        kuzunet.kuzu_query_result_is_success(result))
                                    {
                                        Interlocked.Increment(ref successfulReads);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        })
                    };
                    
                    Task.WaitAll(preparedTasks);
                    
                    Assert.True(exceptions.IsEmpty, 
                        "Concurrent prepared statement execution should not throw exceptions");
                }

                // Log success for debugging
                Console.WriteLine($"? KuzuDB supports multiple connections - {successfulReads} successful concurrent operations");
            }
            else
            {
                // KuzuDB doesn't support multiple connections - verify the error
                Assert.NotEqual(kuzu_state.KuzuSuccess, connResult);
                
                // Verify the original connection still works
                using var verifyResult = new kuzu_query_result();
                var verifyState = kuzunet.kuzu_connection_query(TestConnection!, 
                    "MATCH (p:Person) RETURN COUNT(*) as count", verifyResult);
                
                Assert.Equal(kuzu_state.KuzuSuccess, verifyState);
                Assert.True(kuzunet.kuzu_query_result_is_success(verifyResult));
                
                // Log for debugging
                Console.WriteLine("? KuzuDB confirmed to not support multiple connections to same database");
                Console.WriteLine("? Original connection remains functional after failed second connection attempt");
            }
        }
    }
}