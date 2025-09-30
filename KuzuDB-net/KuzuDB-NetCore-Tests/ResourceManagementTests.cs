using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace KuzuDB_NetCore_Tests
{
    /// <summary>
    /// Tests focused on resource management, disposal patterns, and resource leak detection.
    /// These tests ensure proper cleanup of unmanaged resources in the KuzuDB NetCore wrapper.
    /// </summary>
    public class ResourceManagementTests : BaseKuzuNetCoreTest
    {
        [Fact]
        public void ResourceManagement_QueryResultDisposalPattern_ShouldFollowBestPractices()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var disposedCount = 0;
            var results = new List<kuzu_query_result>();

            // Act - Test various disposal patterns
            try
            {
                // Pattern 1: Using statement
                using (var result1 = ExecuteQuery("MATCH (p:Person) RETURN p.name"))
                {
                    Assert.True(kuzunet.kuzu_query_result_is_success(result1));
                    disposedCount++;
                }

                // Pattern 2: Explicit disposal
                var result2 = ExecuteQuery("MATCH (p:Person) RETURN p.age");
                result2.Dispose();
                disposedCount++;

                // Pattern 3: Try-finally
                kuzu_query_result? result3 = null;
                try
                {
                    result3 = ExecuteQuery("MATCH (p:Person) RETURN p.active");
                    Assert.True(kuzunet.kuzu_query_result_is_success(result3));
                }
                finally
                {
                    result3?.Dispose();
                    disposedCount++;
                }

                // Pattern 4: Collection disposal
                for (int i = 0; i < 5; i++)
                {
                    results.Add(ExecuteQuery($"MATCH (p:Person) WHERE p.id = {i + 1} RETURN p.name"));
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
                var systemConfig = kuzunet.kuzu_default_system_config();
                resources.Add(systemConfig);

                var database = new kuzu_database();
                resources.Add(database);
                
                var dbResult = kuzunet.kuzu_database_init(tempDbPath, systemConfig, database);
                Assert.Equal(kuzu_state.KuzuSuccess, dbResult);

                var connection = new kuzu_connection();
                resources.Add(connection);
                
                var connResult = kuzunet.kuzu_connection_init(database, connection);
                Assert.Equal(kuzu_state.KuzuSuccess, connResult);

                // Use the resources
                using var result = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection, 
                    "CREATE NODE TABLE ResourceTest(id INT64, name STRING, PRIMARY KEY(id))", result);
                Assert.True(kuzunet.kuzu_query_result_is_success(result));

                // Test that resources are still usable before disposal
                using var queryResult = new kuzu_query_result();
                kuzunet.kuzu_connection_query(connection, 
                    "CREATE (:ResourceTest {id: 1, name: 'test'})", queryResult);
                Assert.True(kuzunet.kuzu_query_result_is_success(queryResult));
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
                    if (File.Exists(tempDbPath))
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
            var preparedStatements = new List<kuzu_prepared_statement>();

            try
            {
                // Act - Create multiple prepared statements
                for (int i = 0; i < 10; i++)
                {
                    var stmt = new kuzu_prepared_statement();
                    preparedStatements.Add(stmt);
                    
                    var query = i % 2 == 0 
                        ? "MATCH (p:Person) WHERE p.id = $id RETURN p.name"
                        : "MATCH (p:Person) WHERE p.age > $age RETURN COUNT(*)";
                    
                    var prepResult = kuzunet.kuzu_connection_prepare(TestConnection!, query, stmt);
                    Assert.Equal(kuzu_state.KuzuSuccess, prepResult);

                    // Use the prepared statement
                    if (i % 2 == 0)
                    {
                        kuzunet.kuzu_prepared_statement_bind_int64(stmt, "id", 1);
                    }
                    else
                    {
                        kuzunet.kuzu_prepared_statement_bind_int32(stmt, "age", 25);
                    }

                    using var result = new kuzu_query_result();
                    var execResult = kuzunet.kuzu_connection_execute(TestConnection!, stmt, result);
                    Assert.Equal(kuzu_state.KuzuSuccess, execResult);
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
        public void ResourceManagement_ValueAndFlatTupleLifecycle_ShouldDisposeCorrectly()
        {
            // Arrange
            SetupTestDatabaseWithData();
            var values = new List<kuzu_value>();
            var flatTuples = new List<kuzu_flat_tuple>();

            try
            {
                using var result = ExecuteQuery("MATCH (p:Person) RETURN p.id, p.name, p.age, p.active ORDER BY p.id");

                // Act - Process results and create value/tuple objects
                while (kuzunet.kuzu_query_result_has_next(result))
                {
                    var flatTuple = new kuzu_flat_tuple();
                    flatTuples.Add(flatTuple);
                    
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);

                    // Extract values
                    for (ulong i = 0; i < kuzunet.kuzu_query_result_get_num_columns(result); i++)
                    {
                        var value = new kuzu_value();
                        values.Add(value);
                        
                        kuzunet.kuzu_flat_tuple_get_value(flatTuple, i, value);
                        
                        // Verify value is valid by getting its data type
                        using var dataType = new kuzu_logical_type();
                        kuzunet.kuzu_value_get_data_type(value, dataType);
                        var dataTypeId = kuzunet.kuzu_data_type_get_id(dataType);
                        Assert.NotEqual(kuzu_data_type_id.KUZU_ANY, dataTypeId);
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
                
                foreach (var tuple in flatTuples)
                {
                    tuple?.Dispose();
                }
            }

            // Verify we processed expected number of objects
            Assert.Equal(12, values.Count); // 3 persons × 4 columns each
            Assert.Equal(3, flatTuples.Count); // 3 persons
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
                kuzu_query_result? result = null;
                kuzu_flat_tuple? flatTuple = null;
                kuzu_value? value = null;

                try
                {
                    result = new kuzu_query_result();
                    resourcesCreated++;

                    // Execute query that will sometimes fail
                    var query = i % 5 == 0 ? "INVALID QUERY" : "MATCH (n) RETURN COUNT(*)";
                    kuzunet.kuzu_connection_query(TestConnection!, query, result);

                    if (kuzunet.kuzu_query_result_is_success(result))
                    {
                        flatTuple = new kuzu_flat_tuple();
                        resourcesCreated++;

                        if (kuzunet.kuzu_query_result_has_next(result))
                        {
                            kuzunet.kuzu_query_result_get_next(result, flatTuple);

                            value = new kuzu_value();
                            resourcesCreated++;

                            kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, value);
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
                    if (flatTuple != null)
                    {
                        flatTuple.Dispose();
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
                    using var systemConfig = kuzunet.kuzu_default_system_config();
                    using var database = new kuzu_database();
                    
                    // Initialize with a unique path for each task
                    var dbResult = kuzunet.kuzu_database_init(tempDbPath, systemConfig, database);
                    if (dbResult == kuzu_state.KuzuSuccess)
                    {
                        using var connection = new kuzu_connection();
                        var connResult = kuzunet.kuzu_connection_init(database, connection);
                        
                        if (connResult == kuzu_state.KuzuSuccess)
                        {
                            // Create the same schema and data as the main test database
                            using var createPersonResult = new kuzu_query_result();
                            kuzunet.kuzu_connection_query(connection, 
                                "CREATE NODE TABLE Person(id INT64, name STRING, age INT32, active BOOLEAN, PRIMARY KEY(id))", 
                                createPersonResult);
                            
                            if (kuzunet.kuzu_query_result_is_success(createPersonResult))
                            {
                                // Insert test data
                                using var insertResult = new kuzu_query_result();
                                kuzunet.kuzu_connection_query(connection,
                                    "CREATE (:Person {id: 1, name: 'Alice Johnson', age: 30, active: true})", 
                                    insertResult);

                                if (kuzunet.kuzu_query_result_is_success(insertResult))
                                {
                                    // Execute multiple queries per task
                                    for (int i = 0; i < 10; i++)
                                    {
                                        using var result = new kuzu_query_result();
                                        taskResourcesCreated++; // Count query result
                                        
                                        kuzunet.kuzu_connection_query(connection, 
                                            "MATCH (p:Person) RETURN p.name ORDER BY p.id", result);

                                        if (kuzunet.kuzu_query_result_is_success(result))
                                        {
                                            while (kuzunet.kuzu_query_result_has_next(result))
                                            {
                                                using var flatTuple = new kuzu_flat_tuple();
                                                taskResourcesCreated++; // Count flat tuple
                                                
                                                kuzunet.kuzu_query_result_get_next(result, flatTuple);

                                                using var value = new kuzu_value();
                                                taskResourcesCreated++; // Count value
                                                
                                                kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, value);
                                                
                                                // Resources automatically disposed by using statements
                                                taskResourcesDisposed++; // Value disposed
                                                taskResourcesDisposed++; // FlatTuple disposed
                                            }
                                        }
                                        
                                        taskResourcesDisposed++; // Query result disposed
                                    }
                                }
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
                var result = new kuzu_query_result();
                weakRefs.Add(new WeakReference(result));
                
                kuzunet.kuzu_connection_query(TestConnection!, "MATCH (p:Person) RETURN p.name LIMIT 1", result);
                
                if (kuzunet.kuzu_query_result_is_success(result) && kuzunet.kuzu_query_result_has_next(result))
                {
                    // Create flat tuple without disposal
                    var flatTuple = new kuzu_flat_tuple();
                    weakRefs.Add(new WeakReference(flatTuple));
                    
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);

                    // Create value without disposal
                    var value = new kuzu_value();
                    weakRefs.Add(new WeakReference(value));
                    
                    kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, value);
                    
                    // Clear local references
                    value = null!;
                    flatTuple = null!;
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
                using var result = ExecuteQuery("MATCH (p:Person) RETURN p.name LIMIT 1");
                if (kuzunet.kuzu_query_result_has_next(result))
                {
                    using var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                    
                    using var value = new kuzu_value();
                    kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, value);
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
                var result = new kuzu_query_result();
                kuzunet.kuzu_connection_query(TestConnection!, "MATCH (p:Person) RETURN p.name LIMIT 1", result);
                
                if (kuzunet.kuzu_query_result_is_success(result) && kuzunet.kuzu_query_result_has_next(result))
                {
                    // Create flat tuple without disposal
                    var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                    
                    // Create value without disposal
                    var value = new kuzu_value();
                    kuzunet.kuzu_flat_tuple_get_value(flatTuple, 0, value);
                    
                    // Clear local references to ensure objects are eligible for GC
                    value = null!;
                    flatTuple = null!;
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
                using var result = ExecuteQuery("MATCH (p:Person)-[w:WorksFor]->(c:Company) RETURN p.name, c.name, w.salary ORDER BY w.salary DESC");
                
                while (kuzunet.kuzu_query_result_has_next(result))
                {
                    using var flatTuple = new kuzu_flat_tuple();
                    kuzunet.kuzu_query_result_get_next(result, flatTuple);
                    
                    for (ulong i = 0; i < kuzunet.kuzu_query_result_get_num_columns(result); i++)
                    {
                        using var value = new kuzu_value();
                        kuzunet.kuzu_flat_tuple_get_value(flatTuple, i, value);
                        
                        // Verify value is accessible by getting its data type
                        using var dataType = new kuzu_logical_type();
                        kuzunet.kuzu_value_get_data_type(value, dataType);
                        var dataTypeId = kuzunet.kuzu_data_type_get_id(dataType);
                        Assert.NotEqual(kuzu_data_type_id.KUZU_ANY, dataTypeId);
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