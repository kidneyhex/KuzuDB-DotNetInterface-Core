# KuzuDB-NetCore Stress Tests and Memory Leak Detection

This document describes the comprehensive stress testing and memory leak detection suite added to the KuzuDB-NetCore-Tests project.

## Overview

Three new test classes have been added to ensure the stability and proper resource management of the KuzuDB NetCore wrapper:

1. **StressTests.cs** - High-load stress testing
2. **MemoryLeakTests.cs** - Advanced memory leak detection using WeakReferences
3. **ResourceManagementTests.cs** - Resource lifecycle and disposal testing

## Test Classes

### StressTests.cs

Tests system behavior under high load conditions:

- **StressTest_ManyQueryExecutions_ShouldNotLeakMemory**: Executes 1,000 queries to detect memory leaks
- **StressTest_QueryResultDisposal_ShouldReleaseResourcesProperly**: Tests disposal patterns with 10,000 iterations
- **StressTest_ConcurrentQueries_ShouldHandleParallelLoad**: Parallel execution with 50 concurrent operations
- **StressTest_LargeResultSets_ShouldHandleEfficiently**: Tests processing of 1,000+ row result sets
- **StressTest_PreparedStatementReuse_ShouldBeEfficient**: Reuses prepared statements 1,000 times
- **StressTest_ConnectionRecreation_ShouldNotLeakResources**: Creates/destroys 100 database connections
- **StressTest_ComplexQueryExecution_ShouldHandleIntensiveOperations**: Complex queries with joins and aggregations
- **MemoryLeakTest_FinalizerCleanup_ShouldReleaseUnmanagedResources**: Tests finalizer behavior
- **StressTest_ErrorHandling_ShouldBeRobustUnderLoad**: Mixed valid/invalid query handling
- **PerformanceTest_QueryExecutionTime_ShouldBeMaintainedUnderLoad**: Performance regression detection

### MemoryLeakTests.cs

Advanced memory leak detection using WeakReferences and detailed memory profiling:

- **MemoryLeak_QueryResultLifecycle_ShouldNotLeakWithWeakReferences**: Uses WeakReferences to track object lifecycles
- **MemoryLeak_DatabaseConnectionLifecycle_ShouldReleaseResources**: Database and connection lifecycle testing
- **MemoryLeak_PreparedStatementReuse_ShouldNotAccumulateMemory**: Memory monitoring during prepared statement reuse
- **MemoryLeak_LargeResultSetIteration_ShouldStreamEfficiently**: Streaming behavior for large datasets
- **MemoryLeak_ExceptionHandling_ShouldNotLeakOnErrors**: Resource cleanup during error conditions
- **MemoryPressure_GCBehaviorUnderLoad_ShouldHandleMemoryPressure**: Garbage collection behavior analysis
- **MemoryLeak_FlatTupleProcessing_ShouldNotAccumulateMemory**: FlatTuple object lifecycle testing
- **PerformanceRegression_MemoryAllocationRate_ShouldBeMaintained**: Memory allocation rate monitoring

### ResourceManagementTests.cs

Resource management patterns and disposal testing:

- **ResourceManagement_QueryResultDisposalPattern_ShouldFollowBestPractices**: Various disposal patterns (using, try-finally, explicit)
- **ResourceManagement_DatabaseConnectionLifecycle_ShouldCleanupProperly**: Database resource lifecycle management
- **ResourceManagement_PreparedStatementLifecycle_ShouldManageResourcesCorrectly**: Prepared statement resource management
- **ResourceManagement_ValueAndFlatTupleLifecycle_ShouldDisposeCorrectly**: Value and tuple object management
- **ResourceManagement_ExceptionDuringResourceUsage_ShouldNotLeakResources**: Exception safety testing
- **ResourceManagement_ConcurrentResourceUsage_ShouldBeThreadSafe**: Thread-safe resource management
- **ResourceManagement_FinalizerBehavior_ShouldHandleUnmanagedCleanup**: Finalizer behavior testing
- **ResourceManagement_DisposeVsFinalizerBehavior_ShouldPreferExplicitDisposal**: Performance comparison
- **ResourceManagement_LongRunningOperations_ShouldMaintainResourceIntegrity**: Long-running operation stability

## Test Configuration

### Constants Used

- `STRESS_ITERATIONS = 1,000` - Standard stress test iteration count
- `MEMORY_PRESSURE_ITERATIONS = 10,000` - High memory pressure iteration count
- `CONCURRENT_OPERATIONS = 50` - Concurrent operation count
- `GC_PRESSURE_ITERATIONS = 1,000` - Garbage collection pressure iterations

### Memory Thresholds

- General memory growth limit: 10-50 MB depending on test
- Large dataset processing: < 100 MB
- Finalizer cleanup: < 30 MB growth
- Prepared statement reuse: < 5 MB growth

## How to Run

Run the tests using your preferred test runner:

```bash
# Run all stress tests
dotnet test --filter "StressTests"

# Run memory leak tests
dotnet test --filter "MemoryLeakTests"

# Run resource management tests
dotnet test --filter "ResourceManagementTests"

# Run all new tests
dotnet test --filter "StressTests|MemoryLeakTests|ResourceManagementTests"
```

## Test Features

### Memory Leak Detection

1. **WeakReference Tracking**: Uses WeakReferences to verify objects are garbage collected
2. **Memory Snapshots**: Takes periodic memory measurements to detect growth trends
3. **Finalizer Testing**: Verifies finalizers properly clean up unmanaged resources
4. **GC Pressure Testing**: Forces garbage collection to test cleanup behavior

### Stress Testing

1. **High Iteration Counts**: Tests with thousands of operations
2. **Concurrent Execution**: Multi-threaded stress testing
3. **Large Datasets**: Processing of large result sets
4. **Error Conditions**: Mixed success/failure scenarios
5. **Performance Monitoring**: Execution time tracking

### Resource Management

1. **Disposal Patterns**: Tests various disposal patterns (using, try-finally, explicit)
2. **Exception Safety**: Ensures resources are cleaned up during exceptions
3. **Thread Safety**: Concurrent resource usage testing
4. **Lifecycle Management**: Complete object lifecycle testing

## Expected Behavior

### Pass Criteria

- Memory growth should remain within defined thresholds
- WeakReferences should become non-alive after GC
- No exceptions should be thrown during disposal
- Performance should remain consistent across iterations
- Concurrent operations should complete successfully

### Failure Indicators

- Excessive memory growth suggesting leaks
- WeakReferences remaining alive after GC
- Exceptions during resource cleanup
- Performance degradation over time
- Thread safety violations

## Troubleshooting

### Common Issues

1. **Memory Growth**: If tests fail due to memory growth, check for:
   - Missing `using` statements
   - Improper disposal in finally blocks
   - Finalizer implementation issues

2. **Performance Issues**: If performance tests fail, check for:
   - Resource contention
   - Inefficient query patterns
   - Excessive garbage collection

3. **Concurrency Issues**: If thread safety tests fail, check for:
   - Shared resource access
   - Race conditions
   - Deadlocks

### Test Environment

These tests are designed to run in various environments but may be sensitive to:
- Available system memory
- CPU core count
- Garbage collector implementation
- Test runner behavior

## Contributing

When adding new stress tests:

1. Follow the established naming patterns
2. Include appropriate memory and performance thresholds
3. Use WeakReferences for leak detection where applicable
4. Include both positive and negative test cases
5. Document expected behavior and failure conditions