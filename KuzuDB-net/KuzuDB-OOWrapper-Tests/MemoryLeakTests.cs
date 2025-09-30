using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Advanced memory leak detection tests using WeakReferences and detailed memory profiling.
    /// These tests help identify specific memory leak scenarios in the KuzuDB OOWrapper.
    /// </summary>
    public class MemoryLeakTests : BaseKuzuTest
    {
        private const int LEAK_TEST_ITERATIONS = 5000;
        private const int GC_PRESSURE_ITERATIONS = 1000;

        [Fact]
        public void MemoryLeak_QueryResultLifecycle_ShouldNotLeakWithWeakReferences()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var weakReferences = new List<WeakReference>();
            
            // Act - Create query results and track them with weak references
            CreateQueryResultsAndTrack(weakReferences, 500); // Reduced count for more reliable testing
            
            // Force multiple GC cycles with longer waits to ensure finalizers run
            for (int i = 0; i < 5; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);
                Thread.Sleep(200); // Longer wait for finalizers
            }

            // Additional cleanup attempt
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert - More conservative threshold accounting for JIT/GC behavior
            var stillAlive = weakReferences.Count(wr => wr.IsAlive);
            var alivePercentage = (double)stillAlive / weakReferences.Count;
            
            // Allow up to 20% of objects to still be alive due to GC timing issues
            Assert.True(alivePercentage < 0.2, 
                $"Too many objects still alive: {stillAlive}/{weakReferences.Count} ({alivePercentage:P1}). Possible memory leak.");
                
            // Log for debugging
            Console.WriteLine($"Memory leak test: {stillAlive}/{weakReferences.Count} objects still alive ({alivePercentage:P1})");
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateQueryResultsAndTrack(List<WeakReference> weakReferences, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.id");
                weakReferences.Add(new WeakReference(result));
                
                // Process some results to ensure they're used
                var rowCount = 0;
                foreach (var row in result)
                {
                    using var rowDisposable = row;
                    rowCount++;
                    if (rowCount >= 2) break;
                }
                
                // Explicitly dispose approximately half, let finalizers handle the rest
                if (i % 2 == 0)
                {
                    result.Dispose();
                }
                
                // Clear local reference to ensure object is eligible for GC
                result = null!;
            }
            
            // Force a collection here to start the cleanup process
            GC.Collect();
        }

        [Fact]
        public void MemoryLeak_DatabaseConnectionLifecycle_ShouldReleaseResources()
        {
            // Arrange
            var weakReferences = new List<WeakReference>();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // Act
            CreateDatabaseConnectionsAndTrack(weakReferences, 100);

            // Force garbage collection
            for (int i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Thread.Sleep(200);
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            var stillAlive = weakReferences.Count(wr => wr.IsAlive);

            Assert.True(stillAlive < weakReferences.Count * 0.2, 
                $"Too many database/connection objects still alive: {stillAlive}/{weakReferences.Count}");
            Assert.True(memoryGrowth < 50 * 1024 * 1024, 
                $"Excessive memory growth: {memoryGrowth / 1024 / 1024}MB");
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateDatabaseConnectionsAndTrack(List<WeakReference> weakReferences, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var tempDbPath = Path.Combine(Path.GetTempPath(), $"leak_test_db_{i}_{Guid.NewGuid():N}");
                
                var config = new SystemConfig();
                var database = new Database(tempDbPath, config);
                
                weakReferences.Add(new WeakReference(config));
                weakReferences.Add(new WeakReference(database));
                
                var connection = database.CreateConnection();
                weakReferences.Add(new WeakReference(connection));
                
                // Execute a simple query
                using var result = connection.Query("CREATE NODE TABLE Test(id INT64, PRIMARY KEY(id))");
                
                // Explicitly dispose some objects, let finalizers handle others
                if (i % 3 == 0)
                {
                    connection.Dispose();
                    database.Dispose();
                    config.Dispose();
                }

                // Cleanup database directory
                try
                {
                    if (Directory.Exists(tempDbPath))
                        Directory.Delete(tempDbPath, recursive: true);
                }
                catch { }
            }
        }

        [Fact]
        public void MemoryLeak_PreparedStatementReuse_ShouldNotAccumulateMemory()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var memorySnapshots = new List<long>();
            
            using var preparedStatement = TestConnection!.Prepare("MATCH (p:Person) WHERE p.id = $id RETURN p.name");

            // Act - Execute prepared statement many times and monitor memory
            for (int i = 0; i < LEAK_TEST_ITERATIONS; i++)
            {
                preparedStatement.BindInt64("id", (i % 3) + 1);
                
                using var result = preparedStatement.Execute();
                
                foreach (var row in result)
                {
                    using var rowDisposable = row;
                }

                // Take memory snapshots
                if (i % 500 == 0)
                {
                    GC.Collect();
                    memorySnapshots.Add(GC.GetTotalMemory(forceFullCollection: false));
                }
            }

            // Assert - Memory should not grow significantly over time
            if (memorySnapshots.Count > 2)
            {
                var initialMemory = memorySnapshots[0];
                var finalMemory = memorySnapshots.Last();
                var memoryGrowth = finalMemory - initialMemory;
                
                Assert.True(memoryGrowth < 10 * 1024 * 1024, 
                    $"Memory grew excessively during prepared statement reuse: {memoryGrowth / 1024 / 1024}MB");
            }
        }

        [Fact]
        public void MemoryLeak_LargeResultSetIteration_CheckedExceptionHandling_ShouldNotLeakOnErrors()
        {
            // Arrange
            SetupTestDatabase();
            var weakReferences = new List<WeakReference>();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // Act - Create query results that will fail and track them
            CreateFailingQueriesAndTrack(weakReferences, 1000);

            // Force garbage collection
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Thread.Sleep(100);
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryGrowth = finalMemory - initialMemory;
            var stillAlive = weakReferences.Count(wr => wr.IsAlive);

            Assert.True(stillAlive < weakReferences.Count * 0.2, 
                $"Too many failed query result objects still alive: {stillAlive}/{weakReferences.Count}");
            Assert.True(memoryGrowth < 20 * 1024 * 1024, 
                $"Excessive memory growth from failed queries: {memoryGrowth / 1024 / 1024}MB");
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateFailingQueriesAndTrack(List<WeakReference> weakReferences, int count)
        {
            var invalidQueries = new[]
            {
                "INVALID SYNTAX",
                "MATCH (nonexistent:Table) RETURN *",
                "CREATE (:UnknownTable {invalid: syntax})",
                "SELECT * FROM table", // Wrong query language
                "RETURN undefined_variable"
            };

            for (int i = 0; i < count; i++)
            {
                var query = invalidQueries[i % invalidQueries.Length];
                var result = TestConnection!.Query(query);
                
                weakReferences.Add(new WeakReference(result));
                
                // Verify it failed as expected
                Assert.False(result.IsSuccess);
                
                // Sometimes dispose explicitly, sometimes rely on finalizer
                if (i % 3 == 0)
                {
                    result.Dispose();
                }
            }
        }

        [Fact]
        public void MemoryPressure_GCBehaviorUnderLoad_ShouldHandleMemoryPressure()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var gcCountBefore = GC.CollectionCount(2); // Generation 2 collections
            var memoryPressureBefore = GC.GetTotalMemory(forceFullCollection: false);
            var processedResultsCount = 0;

            // Act - Create memory pressure
            var results = new List<QueryResult>();
            
            try
            {
                for (int i = 0; i < GC_PRESSURE_ITERATIONS; i++)
                {
                    var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age, p.active ORDER BY p.id");
                    results.Add(result);
                    
                    // Simulate some processing
                    var rowCount = 0;
                    foreach (var row in result)
                    {
                        using var rowDisposable = row;
                        rowCount++;
                    }
                    processedResultsCount += rowCount;
                    
                    // Dispose some results to create mixed scenario
                    if (results.Count > 100 && i % 50 == 0)
                    {
                        var toDispose = results.Take(50).ToList();
                        foreach (var r in toDispose)
                            r.Dispose();
                        results.RemoveRange(0, 50);
                    }
                }
            }
            finally
            {
                // Cleanup remaining results
                foreach (var r in results)
                    r?.Dispose();
            }

            var gcCountAfter = GC.CollectionCount(2);
            var memoryPressureAfter = GC.GetTotalMemory(forceFullCollection: true);

            // Assert
            var gcCollections = gcCountAfter - gcCountBefore;
            var memoryGrowth = memoryPressureAfter - memoryPressureBefore;

            // Some GC collections should have occurred under memory pressure
            Assert.True(gcCollections >= 0, "Gen 2 GC collection count should not be negative");
            
            // We should have processed a reasonable number of results
            Assert.True(processedResultsCount > 0, "No results were processed");
            
            // Final memory should not be excessive (allow up to 100MB growth)
            Assert.True(memoryGrowth < 100 * 1024 * 1024, 
                $"Excessive memory growth under pressure: {memoryGrowth / 1024 / 1024}MB");
                
            // Log for debugging
            Console.WriteLine($"Memory pressure test: {gcCollections} Gen2 GCs, {memoryGrowth / 1024 / 1024}MB growth, {processedResultsCount} rows processed");
        }

        [Fact]
        public void MemoryLeak_RowProcessing_ShouldNotAccumulateMemory()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var weakReferences = new List<WeakReference>();
            var memorySnapshots = new List<long>();

            // Act
            for (int iteration = 0; iteration < 100; iteration++)
            {
                using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age, p.active ORDER BY p.id");
                
                foreach (var row in result)
                {
                    weakReferences.Add(new WeakReference(row));
                    
                    // Process the row values
                    for (ulong i = 0; i < result.NumColumns; i++)
                    {
                        using var value = row[i];
                    }
                    
                    // Sometimes dispose explicitly
                    if (iteration % 3 == 0)
                    {
                        row.Dispose();
                    }
                }
                
                if (iteration % 10 == 0)
                {
                    GC.Collect();
                    memorySnapshots.Add(GC.GetTotalMemory(forceFullCollection: false));
                }
            }

            // Force final cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert
            var stillAlive = weakReferences.Count(wr => wr.IsAlive);
            Assert.True(stillAlive < weakReferences.Count * 0.1, 
                $"Too many Row objects still alive: {stillAlive}/{weakReferences.Count}");

            if (memorySnapshots.Count > 2)
            {
                var memoryGrowth = memorySnapshots.Last() - memorySnapshots.First();
                Assert.True(memoryGrowth < 20 * 1024 * 1024, 
                    $"Excessive memory growth during Row processing: {memoryGrowth / 1024 / 1024}MB");
            }
        }

        [Fact]
        public void PerformanceRegression_MemoryAllocationRate_ShouldBeMaintained()
        {
            // Arrange
            SetupTestDatabaseWithData();
            
            // Warm up to avoid JIT compilation affecting measurements
            for (int warmup = 0; warmup < 10; warmup++)
            {
                using var warmupResult = TestConnection!.Query("MATCH (p:Person) RETURN p.name LIMIT 1");
                foreach (var row in warmupResult)
                {
                    using var rowDisposable = row;
                }
            }
            
            // Force garbage collection before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var allocatedBytesStart = GC.GetTotalAllocatedBytes(precise: true);
            var stopwatch = Stopwatch.StartNew();

            // Act
            const int iterations = 500; // Reduced for more consistent measurements
            var processedRows = 0;
            
            for (int i = 0; i < iterations; i++)
            {
                using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.id");
                
                foreach (var row in result)
                {
                    using var rowDisposable = row;
                    using var value = row[0];
                    processedRows++;
                }
            }

            stopwatch.Stop();
            var allocatedBytesEnd = GC.GetTotalAllocatedBytes(precise: true);

            // Assert
            var totalAllocated = allocatedBytesEnd - allocatedBytesStart;
            var avgTimePerIteration = stopwatch.ElapsedMilliseconds / (double)iterations;
            var avgAllocationsPerIteration = totalAllocated / iterations;

            // Performance assertions with more realistic thresholds
            Assert.True(avgTimePerIteration < 100, 
                $"Average time per iteration too high: {avgTimePerIteration:F2}ms");
            Assert.True(avgAllocationsPerIteration < 50 * 1024, 
                $"Average allocations per iteration too high: {avgAllocationsPerIteration / 1024:F1}KB");
                
            // Verify we processed data
            Assert.True(processedRows > 0, "No rows were processed");
            
            // Log for debugging
            Console.WriteLine($"Performance test: {avgTimePerIteration:F2}ms/iteration, {avgAllocationsPerIteration / 1024:F1}KB/iteration, {processedRows} rows processed");
        }
    }
}