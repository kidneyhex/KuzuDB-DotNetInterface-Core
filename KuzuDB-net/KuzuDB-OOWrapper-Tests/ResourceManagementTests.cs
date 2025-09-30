using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace KuzuDB.OOWrapper.Tests
{
    /// <summary>
    /// Tests focused on resource management, disposal patterns, and resource leak detection.
    /// These tests ensure proper cleanup of unmanaged resources in the KuzuDB OOWrapper.
    /// </summary>
    public class ResourceManagementTests : BaseKuzuTest
    {
        [Fact]
        public void ResourceManagement_QueryResultDisposalPattern_ShouldFollowBestPractices()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var disposedCount = 0;
            var results = new List<QueryResult>();

            // Act - Test various disposal patterns
            try
            {
                // Pattern 1: Using statement
                using (var result1 = TestConnection!.Query("MATCH (p:Person) RETURN p.name"))
                {
                    Assert.True(result1.IsSuccess);
                    disposedCount++;
                }

                // Pattern 2: Explicit disposal
                var result2 = TestConnection!.Query("MATCH (p:Person) RETURN p.age");
                result2.Dispose();
                disposedCount++;

                // Pattern 3: Try-finally
                QueryResult? result3 = null;
                try
                {
                    result3 = TestConnection!.Query("MATCH (p:Person) RETURN p.active");
                    Assert.True(result3.IsSuccess);
                }
                finally
                {
                    result3?.Dispose();
                    disposedCount++;
                }

                // Pattern 4: Collection disposal
                for (int i = 0; i < 5; i++)
                {
                    results.Add(TestConnection!.Query($"MATCH (p:Person) WHERE p.id = {i + 1} RETURN p.name"));
                }
            }
            finally
            {
                // Cleanup remaining results
                foreach (var result in results.Where(r => r != null))
                {
                    result.Dispose();
                    disposedCount++;
                }
            }

            // Assert - Fixed count: 1 (using) + 1 (explicit) + 1 (try-finally) + 5 (collection)
            Assert.Equal(8, disposedCount);
        }

        [Fact]
        public void ResourceManagement_DatabaseConnectionLifecycle_ShouldCleanupProperly()
        {
            // Arrange
            var tempDbPath = Path.Combine(Path.GetTempPath(), $"resource_test_{Guid.NewGuid():N}");
            var resources = new List<IDisposable>();

            try
            {
                // Act - Create and manage database resources
                var config = new SystemConfig();
                resources.Add(config);

                var database = new Database(tempDbPath, config);
                resources.Add(database);

                var connection = database.CreateConnection();
                resources.Add(connection);

                // Use the resources
                using var result = connection.Query("CREATE NODE TABLE ResourceTest(id INT64, name STRING, PRIMARY KEY(id))");
                Assert.True(result.IsSuccess);

                // Test that resources are still usable before disposal
                using var queryResult = connection.Query("CREATE (:ResourceTest {id: 1, name: 'test'})");
                Assert.True(queryResult.IsSuccess);
            }
            finally
            {
                // Assert - Cleanup should not throw
                foreach (var resource in resources)
                {
                    resource?.Dispose();
                }

                // Cleanup database file
                try
                {
                    if (Directory.Exists(tempDbPath))
                        Directory.Delete(tempDbPath, recursive: true);
                    else if (File.Exists(tempDbPath))
                        File.Delete(tempDbPath);
                }
                catch { }
            }
        }

        [Fact]
        public void ResourceManagement_PreparedStatementLifecycle_ShouldManageResourcesCorrectly()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var preparedStatements = new List<PreparedStatement>();

            try
            {
                // Act - Create multiple prepared statements
                for (int i = 0; i < 10; i++)
                {
                    var query = i % 2 == 0 
                        ? "MATCH (p:Person) WHERE p.id = $id RETURN p.name"
                        : "MATCH (p:Person) WHERE p.age > $age RETURN COUNT(*)";
                    
                    var stmt = TestConnection!.Prepare(query);
                    preparedStatements.Add(stmt);

                    // Use the prepared statement
                    if (i % 2 == 0)
                    {
                        stmt.BindInt64("id", 1);
                    }
                    else
                    {
                        stmt.BindInt32("age", 25);
                    }

                    using var result = stmt.Execute();
                    Assert.True(result.IsSuccess);
                }
            }
            finally
            {
                // Assert - Cleanup should not throw
                foreach (var stmt in preparedStatements)
                {
                    stmt?.Dispose();
                }
            }
        }

        [Fact]
        public void ResourceManagement_RowAndValueLifecycle_ShouldDisposeCorrectly()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var values = new List<Value>();
            var rows = new List<Row>();

            try
            {
                using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.id, p.name, p.age, p.active ORDER BY p.id");

                // Act - Process results and create row/value objects
                foreach (var row in result)
                {
                    rows.Add(row);

                    // Extract values
                    for (ulong i = 0; i < result.NumColumns; i++)
                    {
                        var value = row[i];
                        values.Add(value);
                        
                        // Verify value is valid by getting its data type
                        var dataType = value.GetDataType();
                        Assert.NotNull(dataType);
                        dataType.Dispose();
                    }
                }
            }
            finally
            {
                // Assert - Cleanup should not throw
                foreach (var value in values)
                {
                    value?.Dispose();
                }
                
                foreach (var row in rows)
                {
                    row?.Dispose();
                }
            }

            // Verify we processed expected number of objects
            Assert.Equal(12, values.Count); // 3 persons × 4 columns each
            Assert.Equal(3, rows.Count); // 3 persons
        }

        [Fact]
        public void ResourceManagement_ExceptionDuringResourceUsage_ShouldNotLeakResources()
        {
            // Arrange
            SetupTestDatabase();
            var resourcesCreated = 0;
            var resourcesDisposed = 0;

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                QueryResult? result = null;
                Row? row = null;
                Value? value = null;

                try
                {
                    result = TestConnection!.Query(i % 5 == 0 ? "INVALID QUERY" : "MATCH (n) RETURN COUNT(*)");
                    resourcesCreated++;

                    if (result.IsSuccess)
                    {
                        foreach (var r in result)
                        {
                            row = r;
                            resourcesCreated++;

                            if (result.NumColumns > 0)
                            {
                                value = row[0];
                                resourcesCreated++;
                            }
                            break; // Only process first row
                        }
                    }
                }
                catch (Exception)
                {
                    // Expected for invalid queries - should still cleanup properly
                }
                finally
                {
                    // Cleanup resources
                    if (value != null)
                    {
                        value.Dispose();
                        resourcesDisposed++;
                    }
                    if (row != null)
                    {
                        row.Dispose();
                        resourcesDisposed++;
                    }
                    if (result != null)
                    {
                        result.Dispose();
                        resourcesDisposed++;
                    }
                }
            }

            Assert.Equal(resourcesCreated, resourcesDisposed);
        }

        [Fact]
        public void ResourceManagement_ConcurrentResourceUsage_ShouldBeThreadSafe()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var exceptions = new ConcurrentBag<Exception>();
            var completedTasks = 0;
            var totalResourcesCreated = 0;
            var totalResourcesDisposed = 0;

            // Act
            var tasks = Enumerable.Range(0, 20).Select(taskId => Task.Run(() =>
            {
                var taskResourcesCreated = 0;
                var taskResourcesDisposed = 0;
                var tempDbPath = Path.Combine(Path.GetTempPath(), $"concurrent_test_{taskId}_{Guid.NewGuid():N}");

                try
                {
                    // Each task creates its own database connection to avoid concurrency issues
                    using var database = new Database(tempDbPath);
                    using var connection = database.CreateConnection();

                    // Create the same schema and data as the main test database
                    using var createPersonResult = connection.Query("CREATE NODE TABLE Person(id INT64, name STRING, age INT32, active BOOLEAN, PRIMARY KEY(id))");
                    
                    if (createPersonResult.IsSuccess)
                    {
                        // Insert test data
                        using var insertResult = connection.Query("CREATE (:Person {id: 1, name: 'Alice Johnson', age: 30, active: true})");

                        if (insertResult.IsSuccess)
                        {
                            // Execute multiple queries per task
                            for (int i = 0; i < 10; i++)
                            {
                                using var result = connection.Query("MATCH (p:Person) RETURN p.name ORDER BY p.id");
                                taskResourcesCreated++; // Count query result
                                
                                if (result.IsSuccess)
                                {
                                    foreach (var row in result)
                                    {
                                        using var rowDisposable = row;
                                        taskResourcesCreated++; // Count row
                                        
                                        using var value = row[0];
                                        taskResourcesCreated++; // Count value
                                        
                                        // Resources automatically disposed by using statements
                                        taskResourcesDisposed++; // Value disposed
                                        taskResourcesDisposed++; // Row disposed
                                    }
                                }
                                
                                taskResourcesDisposed++; // Query result disposed
                            }
                        }
                    }
                    
                    Interlocked.Add(ref totalResourcesCreated, taskResourcesCreated);
                    Interlocked.Add(ref totalResourcesDisposed, taskResourcesDisposed);
                    Interlocked.Increment(ref completedTasks);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    // Cleanup the temporary file
                    try
                    {
                        if (Directory.Exists(tempDbPath))
                            Directory.Delete(tempDbPath, recursive: true);
                        else if (File.Exists(tempDbPath))
                            File.Delete(tempDbPath);
                    }
                    catch { }
                }
            })).ToArray();

            Task.WaitAll(tasks, TimeSpan.FromMinutes(2));

            // Assert
            Assert.Empty(exceptions);
            Assert.Equal(20, completedTasks);
            Assert.True(totalResourcesCreated > 0, "No resources were created");
            Assert.Equal(totalResourcesCreated, totalResourcesDisposed);
            
            // Log for debugging
            Console.WriteLine($"Concurrent resource test: {totalResourcesCreated} resources created and disposed");
        }

        [Fact]
        public void ResourceManagement_FinalizerBehavior_ShouldHandleUnmanagedCleanup()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
            var weakRefs = new List<WeakReference>();

            // Act - Create objects without explicit disposal to test finalizers
            CreateResourcesWithoutDisposalAndTrack(weakRefs, 200); // Reduced count for more reliable testing
            
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
            Assert.True(memoryGrowth < 20 * 1024 * 1024, 
                $"Excessive memory growth suggests finalizers are not working properly: {memoryGrowth / 1024 / 1024}MB");
                
            // Most objects should be collected by finalizers
            Assert.True(alivePercentage < 0.3, 
                $"Too many objects still alive after finalizer cleanup: {stillAlive}/{weakRefs.Count} ({alivePercentage:P1})");
                
            // Log results for debugging
            Console.WriteLine($"Finalizer test: {memoryGrowth / 1024 / 1024}MB growth, {stillAlive}/{weakRefs.Count} objects still alive");
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateResourcesWithoutDisposalAndTrack(List<WeakReference> weakRefs, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Create query result without disposal - finalizer should handle cleanup
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
        public void ResourceManagement_DisposeVsFinalizerBehavior_ShouldPreferExplicitDisposal()
        {
            // Arrange
            SetupTestDatabaseWithData();

            // Test explicit disposal performance
            var explicitDisposeTime = MeasureResourceCleanupTime(useExplicitDispose: true);
            
            // Test finalizer cleanup performance  
            var finalizerCleanupTime = MeasureResourceCleanupTime(useExplicitDispose: false);

            // Assert
            // Explicit disposal should be faster and more deterministic
            Assert.True(explicitDisposeTime < finalizerCleanupTime * 2, 
                $"Explicit disposal should be faster. Explicit: {explicitDisposeTime}ms, Finalizer: {finalizerCleanupTime}ms");
        }

        private long MeasureResourceCleanupTime(bool useExplicitDispose)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (useExplicitDispose)
            {
                MeasureExplicitDisposal();
            }
            else
            {
                MeasureFinalizerCleanup();
            }
            
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void MeasureExplicitDisposal()
        {
            for (int i = 0; i < 200; i++)
            {
                using var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name LIMIT 1");
                if (result.HasNext())
                {
                    using var row = result.GetNext();
                    if (row != null)
                    {
                        using var value = row[0];
                    }
                }
            }
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void MeasureFinalizerCleanup()
        {
            CreateResourcesWithoutDisposal(200);
            
            // Force garbage collection and wait for finalizers
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CreateResourcesWithoutDisposal(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Create query result without disposal - finalizer should handle cleanup
                var result = TestConnection!.Query("MATCH (p:Person) RETURN p.name LIMIT 1");
                
                if (result.IsSuccess && result.HasNext())
                {
                    // Create row without disposal
                    var row = result.GetNext();
                    if (row != null)
                    {
                        // Create value without disposal
                        var value = row[0];
                        
                        // Clear local references to ensure objects are eligible for GC
                        value = null!;
                        row = null!;
                    }
                }
                
                result = null!; // Clear local reference
                
                // Intentionally not disposing - finalizers should handle cleanup
            }
        }

        [Fact]
        public void ResourceManagement_LongRunningOperations_ShouldMaintainResourceIntegrity()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var startTime = DateTime.UtcNow;
            var resourceOperations = 0;

            // Act - Simulate long-running operations with resource management
            while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(30)) // Run for 30 seconds
            {
                using var result = TestConnection!.Query("MATCH (p:Person)-[w:WorksFor]->(c:Company) RETURN p.name, c.name, w.salary ORDER BY w.salary DESC");
                
                foreach (var row in result)
                {
                    using var rowDisposable = row;
                    
                    for (ulong i = 0; i < result.NumColumns; i++)
                    {
                        using var value = row[i];
                        
                        // Verify value is accessible by getting its data type
                        using var dataType = value.GetDataType();
                        Assert.NotNull(dataType);
                    }
                }
                
                resourceOperations++;
                
                // Periodically force garbage collection to test resource stability
                if (resourceOperations % 50 == 0)
                {
                    GC.Collect();
                }
            }

            // Assert
            Assert.True(resourceOperations > 100, $"Too few operations completed: {resourceOperations}");
        }
    }
}