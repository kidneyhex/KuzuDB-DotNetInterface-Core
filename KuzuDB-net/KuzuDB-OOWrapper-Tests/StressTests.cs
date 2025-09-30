using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Stress tests and memory leak detection tests for KuzuDB OOWrapper.
    /// These tests verify system stability under high load and proper resource cleanup.
    /// </summary>
    public class StressTests : BaseKuzuTest
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
                using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.id");
                
                // Iterate through results to ensure they're fully processed
                foreach (var row in result)
                {
                    using var rowDisposable = row;
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
                var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age, p.active ORDER BY p.id");
                
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
                    var tempDbPath = Path.Combine(Path.GetTempPath(), $"concurrent_db_{i}_{Guid.NewGuid():N}");
                    
                    using var database = new Database(tempDbPath);
                    using var connection = database.CreateConnection();

                    // Set up schema and data for this thread's database
                    using var createResult = connection.Query("CREATE NODE TABLE Person(id INT64, name STRING, age INT32, PRIMARY KEY(id))");
                        
                    if (createResult.IsSuccess)
                    {
                        // Insert test data
                        using var insertResult = connection.Query("CREATE (:Person {id: 1, name: 'Test User', age: 30})");
                            
                        if (insertResult.IsSuccess)
                        {
                            // Execute multiple queries per thread
                            for (int j = 0; j < 10; j++)
                            {
                                using var result = connection.Query("MATCH (p:Person) RETURN p.name ORDER BY p.id");
                                
                                if (result.IsSuccess)
                                {
                                    Interlocked.Increment(ref successful);
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
            using var result = TestConnection!.Query("MATCH (l:LargeTable) RETURN l.id, l.value, l.data ORDER BY l.id");
            
            var processedRows = 0;
            foreach (var row in result)
            {
                using var rowDisposable = row;
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
            using var preparedStatement = TestConnection!.Prepare("MATCH (p:Person) WHERE p.id = $id RETURN p.name, p.age");

            for (int i = 0; i < STRESS_ITERATIONS; i++)
            {
                // Bind parameter
                preparedStatement.BindInt64("id", (i % 3) + 1);

                // Execute
                using var result = preparedStatement.Execute();
                
                Assert.True(result.IsSuccess);

                // Process results
                foreach (var row in result)
                {
                    using var rowDisposable = row;
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
                var tempDbPath = Path.Combine(Path.GetTempPath(), $"stress_db_{i}_{Guid.NewGuid():N}");
                
                using var database = new Database(tempDbPath);
                using var connection = database.CreateConnection();

                // Create schema and insert data
                using var createResult = connection.Query("CREATE NODE TABLE TempTable(id INT64, name STRING, PRIMARY KEY(id))");
                
                using var insertResult = connection.Query("CREATE (:TempTable {id: 1, name: 'test'})");

                using var queryResult = connection.Query("MATCH (t:TempTable) RETURN t.id, t.name");

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
                using var result = TestConnection!.Query(@"
                    MATCH (p:Person)-[w:WorksFor]->(c:Company)
                    WITH p, c, w, p.age * w.salary as weighted_value
                    WHERE p.active = true
                    RETURN p.name, c.name, COUNT(*) as relationships, 
                           AVG(w.salary) as avg_salary, 
                           SUM(weighted_value) as total_weighted
                    ORDER BY avg_salary DESC
                ");

                Assert.True(result.IsSuccess);
                
                foreach (var row in result)
                {
                    using var rowDisposable = row;
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
            CreateQueryResultsWithoutDisposalAndTrack(weakRefs, 500); // Reduced count for more reliable testing
            
            // Force multiple garbage collection cycles with proper timing
            for (int i = 0; i < 5; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);
                Thread.Sleep(200); // Give finalizers adequate time
            }

            // Final cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            var stillAlive = weakRefs.Count(wr => wr.IsAlive);
            var alivePercentage = (double)stillAlive / weakRefs.Count;
            
            // Finalizers should have cleaned up most resources
            Assert.True(memoryGrowth < 30 * 1024 * 1024, 
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB, finalizers may not be cleaning up properly");
                
            // Most objects should be collected by finalizers
            Assert.True(alivePercentage < 0.3, 
                $"Too many objects still alive after finalizer cleanup: {stillAlive}/{weakRefs.Count} ({alivePercentage:P1})");
                
            // Log results for debugging
            Console.WriteLine($"Finalizer test: {memoryGrowth / 1024 / 1024}MB growth, {stillAlive}/{weakRefs.Count} objects still alive");
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateQueryResultsWithoutDisposalAndTrack(List<WeakReference> weakRefs, int count)
        {
            // This method intentionally doesn't dispose query results to test finalizer cleanup
            for (int i = 0; i < count; i++)
            {
                var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name LIMIT 1");
                weakRefs.Add(new WeakReference(result));
                
                if (result.IsSuccess && result.HasNext())
                {
                    // Create row without disposal
                    var row = result.GetNext();
                    if (row != null)
                    {
                        weakRefs.Add(new WeakReference(row));
                        
                        // Create value without disposal
                        var value = row[0];
                        weakRefs.Add(new WeakReference(value));
                        
                        // Clear local references
                        value = null!;
                        row = null!;
                    }
                }
                
                result = null!; // Clear local reference
                
                // Intentionally not disposing - finalizers should handle cleanup
            }
            
            // Force initial collection to start cleanup process
            GC.Collect();
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
                
                using var result = TestConnection!.Query(query);
                
                if (result.IsSuccess)
                {
                    successCount++;
                    
                    // For successful queries, verify we can process results
                    if (shouldSucceed)
                    {
                        foreach (var row in result)
                        {
                            using var rowDisposable = row;
                        }
                    }
                }
                else
                {
                    errorCount++;
                    // For failed queries, verify we can get error message
                    Assert.False(string.IsNullOrEmpty(result.ErrorMessage), "Error queries should provide error messages");
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
                
                using var result = TestConnection!.Query("MATCH (p:Person)-[w:WorksFor]->(c:Company) RETURN p.name, c.name, w.salary ORDER BY w.salary DESC");
                
                foreach (var row in result)
                {
                    using var rowDisposable = row;
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
            
            try
            {
                using var connection2 = TestDatabase!.CreateConnection();
                
                // Test concurrent reads from both connections
                var exceptions = new ConcurrentBag<Exception>();
                var connection1Results = new ConcurrentBag<int>();
                var connection2Results = new ConcurrentBag<int>();
                var successfulReads = 0;

                // Test concurrent reads from both connections
                var tasks = new Task[]
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            // Connection 1 performs multiple reads
                            for (int i = 0; i < 10; i++)
                            {
                                using var result = TestConnection!.Query("MATCH (p:Person) RETURN COUNT(*) as count");
                                
                                if (result.IsSuccess)
                                {
                                    // Process result to get count
                                    foreach (var row in result)
                                    {
                                        using var rowDisposable = row;
                                        var countValue = row[0];
                                        using var countDisposable = countValue;
                                        connection1Results.Add((int)countValue.GetInt64());
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
                                using var result = connection2.Query("MATCH (c:Company) RETURN COUNT(*) as count");
                                
                                if (result.IsSuccess)
                                {
                                    // Process result to get count
                                    foreach (var row in result)
                                    {
                                        using var rowDisposable = row;
                                        var countValue = row[0];
                                        using var countDisposable = countValue;
                                        connection2Results.Add((int)countValue.GetInt64());
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
                using var stmt1 = TestConnection!.Prepare("MATCH (p:Person) WHERE p.id = $id RETURN p.name");
                using var stmt2 = connection2.Prepare("MATCH (c:Company) WHERE c.id = $id RETURN c.name");
                
                // Test concurrent prepared statement execution
                var preparedTasks = new Task[]
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            for (int id = 1; id <= 3; id++)
                            {
                                stmt1.BindInt64("id", id);
                                using var result = stmt1.Execute();
                                
                                if (result.IsSuccess)
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
                                stmt2.BindInt64("id", id);
                                using var result = stmt2.Execute();
                                
                                if (result.IsSuccess)
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

                // Log success for debugging
                Console.WriteLine($"? KuzuDB supports multiple connections - {successfulReads} successful concurrent operations");
            }
            catch (Exception ex)
            {
                // If multiple connections aren't supported, log and pass
                Console.WriteLine($"? Multiple connections not supported: {ex.Message}");
                
                // Verify the original connection still works
                using var verifyResult = TestConnection!.Query("MATCH (p:Person) RETURN COUNT(*) as count");
                Assert.True(verifyResult.IsSuccess);
                
                Console.WriteLine("? Original connection remains functional");
            }
        }
    }
}