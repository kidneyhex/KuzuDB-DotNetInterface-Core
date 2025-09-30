# KuzuDB NetCore Tests

This directory contains comprehensive test cases for the KuzuDB-NetCore project, which provides SWIG-generated .NET wrapper classes for the native KuzuDB C++ library.

## Test Structure

The test suite is organized into the following test classes:

### Base Test Infrastructure
- **`BaseKuzuNetCoreTest.cs`** - Abstract base class providing common test utilities, database setup, and cleanup functionality

### Core Functionality Tests
- **`BasicFunctionalityTests.cs`** - Smoke tests to verify basic KuzuDB functionality and wrapper instantiation
- **`DatabaseTests.cs`** - Tests for `kuzu_database` wrapper class and database initialization
- **`ConnectionTests.cs`** - Tests for `kuzu_connection` wrapper class and connection management
- **`QueryResultTests.cs`** - Tests for `kuzu_query_result` wrapper class and query execution results
- **`PreparedStatementTests.cs`** - Tests for `kuzu_prepared_statement` wrapper class and parameterized queries
- **`ValueTests.cs`** - Tests for `kuzu_value` wrapper class and value creation/manipulation
- **`FlatTupleTests.cs`** - Tests for `kuzu_flat_tuple` wrapper class and row data access
- **`DataTypeTests.cs`** - Tests for `kuzu_logical_type` and data type functionality

### Integration Tests
- **`IntegrationTests.cs`** - End-to-end tests that verify multiple wrapper classes working together

## Test Coverage

The test suite covers:

### Database Operations
- Database initialization with file paths and in-memory mode
- System configuration management
- Multiple database instance isolation
- Database version and storage version retrieval

### Connection Management
- Connection initialization and cleanup  
- Thread configuration for query execution
- Query timeout settings
- Connection interruption
- Multiple independent connections

### Query Execution
- Basic query execution (DDL, DML, SELECT)
- Complex queries with joins and aggregations
- Query result iteration and data access
- Error handling for invalid queries
- Query performance metrics via query summary

### Prepared Statements
- Statement preparation and validation
- Parameter binding for all supported data types:
  - Boolean, Integer types (8/16/32/64-bit signed/unsigned)
  - Float, Double, String
  - Date, Timestamp, Interval types
- Parameterized query execution
- Statement reuse with different parameter sets

### Value and Data Type System
- Value creation for all supported data types
- Null value handling
- Value cloning and copying
- Data type introspection and comparison
- Type conversion and string representations

### Advanced Features
- Flat tuple iteration and column access
- Complex data type handling (arrays, lists, structs)
- Node and relationship value access
- Memory management and resource cleanup

## Running Tests

### Individual Test Classes
Due to native library resource management issues, it's recommended to run test classes individually:

```bash
# Run basic functionality tests
dotnet test --filter "FullyQualifiedName~BasicFunctionalityTests"

# Run database tests  
dotnet test --filter "FullyQualifiedName~DatabaseTests"

# Run connection tests
dotnet test --filter "FullyQualifiedName~ConnectionTests"

# Run value tests
dotnet test --filter "FullyQualifiedName~ValueTests"
```

### All Tests
You can attempt to run all tests, but the test runner may crash due to native library resource conflicts:

```bash
dotnet test
```

### Test Configuration
- **Target Framework**: .NET 9.0
- **Test Framework**: xUnit 2.9.2  
- **Test Runner**: Microsoft.NET.Test.Sdk 17.12.0

## Known Issues

- **Test Runner Crashes**: When running the full test suite, the test host process may crash due to native library resource management issues. This is a limitation of the underlying native KuzuDB library, not the test code itself.
- **Individual Tests Work**: All individual test classes and methods work correctly when run in isolation.

## Test Design Principles

1. **Isolation**: Each test method is isolated with its own database instance
2. **Cleanup**: Proper resource disposal and database cleanup after each test
3. **Realistic Data**: Tests use realistic schemas and data patterns
4. **Error Scenarios**: Tests cover both success and failure cases
5. **Type Coverage**: Tests exercise all supported KuzuDB data types
6. **Native API Coverage**: Tests directly use the SWIG-generated native wrapper methods

The tests provide comprehensive validation of the KuzuDB NetCore wrapper functionality and serve as examples of how to use the native API effectively.