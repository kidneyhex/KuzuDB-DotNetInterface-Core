# OOWrapper Test Suite Extensions

This directory contains advanced test suites for the KuzuDB OOWrapper that mirror the comprehensive testing provided for the NetCore wrapper.

## New Test Files Added

### ResourceManagementTests.cs
Tests focused on resource management, disposal patterns, and resource leak detection. These tests ensure proper cleanup of unmanaged resources in the KuzuDB OOWrapper.

**Key Test Areas:**
- **QueryResult Disposal Patterns**: Tests various disposal approaches (using statements, explicit disposal, try-finally patterns)
- **Database/Connection Lifecycle**: Verifies proper cleanup of database and connection resources
- **PreparedStatement Lifecycle**: Tests resource management for prepared statements
- **Row and Value Lifecycle**: Ensures proper disposal of row and value objects
- **Exception Handling**: Tests resource cleanup during exception scenarios
- **Concurrent Resource Usage**: Tests thread-safe resource management
- **Finalizer Behavior**: Tests that finalizers properly clean up unmanaged resources
- **Explicit vs Finalizer Disposal**: Compares performance of explicit disposal vs finalizer cleanup
- **Long-running Operations**: Tests resource integrity during extended operations

### StressTests.cs
Comprehensive stress tests and memory leak detection tests for the KuzuDB OOWrapper. These tests verify system stability under high load and proper resource cleanup.

**Key Test Areas:**
- **Many Query Executions**: Tests memory behavior under high query volume (1000+ iterations)
- **Query Result Disposal**: Tests resource cleanup under memory pressure (10,000+ iterations)
- **Concurrent Queries**: Tests parallel query execution with multiple threads
- **Large Result Sets**: Tests handling of large datasets (1000+ rows)
- **Prepared Statement Reuse**: Tests efficiency of prepared statement reuse
- **Connection Recreation**: Tests resource management during repeated connection creation
- **Complex Query Execution**: Tests performance with complex queries involving joins and aggregations
- **Finalizer Cleanup**: Tests finalizer-based resource cleanup under stress
- **Error Handling**: Tests robustness under mixed success/failure scenarios
- **Performance Monitoring**: Tests execution time consistency under load
- **Multiple Connections**: Tests concurrent access patterns

### MemoryLeakTests.cs
Advanced memory leak detection tests using WeakReferences and detailed memory profiling. These tests help identify specific memory leak scenarios in the KuzuDB OOWrapper.

**Key Test Areas:**
- **QueryResult Lifecycle**: Uses WeakReferences to detect query result memory leaks
- **Database/Connection Lifecycle**: Tests memory cleanup for database and connection objects
- **PreparedStatement Reuse**: Monitors memory growth during prepared statement reuse
- **Failed Query Handling**: Tests memory behavior with invalid queries
- **Memory Pressure**: Tests garbage collection behavior under memory pressure
- **Row Processing**: Tests memory usage during row iteration
- **Performance Regression**: Monitors memory allocation rates and execution times

## Test Design Principles

### Adapted for OOWrapper API
All tests have been carefully adapted to use the OOWrapper's higher-level API:
- Uses `QueryResult.Query()` instead of native kuzu_connection_query
- Uses `Row[index]` indexer instead of kuzu_flat_tuple_get_value
- Uses `PreparedStatement.BindInt64()` instead of kuzu_prepared_statement_bind_int64
- Properly handles OOWrapper's IDisposable pattern

### Resource Management Testing
- Tests both explicit disposal and finalizer-based cleanup
- Uses WeakReferences to detect memory leaks
- Monitors garbage collection behavior
- Tests resource cleanup during exceptions

### Concurrency Testing
- Creates separate database instances for each thread to avoid concurrency issues
- Tests both read and write operations under concurrent access
- Validates data consistency across multiple connections

### Performance Testing
- Measures query execution times under load
- Monitors memory allocation rates
- Tests performance degradation detection
- Validates resource cleanup performance

## Test Configuration

### Constants Used
- `STRESS_ITERATIONS = 1000`: Standard stress test iteration count
- `MEMORY_PRESSURE_ITERATIONS = 10000`: High memory pressure test count
- `CONCURRENT_OPERATIONS = 50`: Number of concurrent operations
- `LEAK_TEST_ITERATIONS = 5000`: Memory leak detection iteration count
- `GC_PRESSURE_ITERATIONS = 1000`: Garbage collection pressure test count

### Memory Thresholds
- Memory growth limits: 20-100MB depending on test type
- Finalizer cleanup threshold: <30% objects remaining alive
- Performance thresholds: <100ms per iteration, <50KB allocations per iteration

## Usage

These tests are designed to be run as part of the regular test suite. They will:
1. Automatically create temporary databases for testing
2. Clean up all resources after completion
3. Report memory usage and performance metrics
4. Detect and report potential memory leaks
5. Validate proper resource disposal patterns

The tests complement the existing OOWrapper test suite by providing comprehensive resource management and performance validation.