# KuzuDB Object-Oriented Wrapper

This library provides an object-oriented wrapper around the KuzuDB .NET Core API, making it easier to work with KuzuDB from C# applications.

## Features

- **Object-Oriented Design**: Clean, intuitive API following .NET conventions
- **Automatic Resource Management**: Proper IDisposable implementation for all resources
- **Type Safety**: Strongly-typed operations with proper exception handling
- **LINQ-style Enumeration**: Support for foreach loops and LINQ operations on query results
- **Comprehensive Coverage**: Wraps all major KuzuDB operations including:
  - Database creation and management
  - Connection management
  - Query execution
  - Prepared statements with parameter binding
  - Result set iteration and data access

## Quick Start

### Basic Usage

```csharp
using KuzuDB.OOWrapper;

// Create or open a database
using var database = new Database("path/to/database");

// Create a connection
using var connection = database.CreateConnection();

// Execute a simple query
using var result = connection.Query("MATCH (n) RETURN n LIMIT 10");

// Iterate through results
foreach (var row in result)
{
    Console.WriteLine(row.ToString());
    
    // Access values by index or column name
    var value = row[0];  // or row["columnName"]
    
    if (!value.IsNull)
    {
        Console.WriteLine(value.ToString());
    }
}
```

### Using Prepared Statements

```csharp
using var database = new Database("path/to/database");
using var connection = database.CreateConnection();

// Prepare a statement with parameters
using var stmt = connection.Prepare("MATCH (n:Person) WHERE n.age > $age RETURN n.name");

// Bind parameters
stmt.BindInt32("age", 25);

// Execute the prepared statement
using var result = stmt.Execute();

// Process results
foreach (var row in result)
{
    foreach (var value in row)
    {
        if (!value.IsNull)
        {
            Console.WriteLine($"Name: {value.GetString()}");
        }
    }
}
```

### Configuration Options

```csharp
// Create custom system configuration
using var config = KuzuUtilities.CreateDefaultSystemConfig();
using var database = new Database("path/to/database", config);

// Configure connection settings
using var connection = database.CreateConnection();
connection.SetMaxNumThreadsForExecution(4);
connection.SetQueryTimeout(30000); // 30 seconds
```

### Error Handling

```csharp
try
{
    using var database = new Database("path/to/database");
    using var connection = database.CreateConnection();
    using var result = connection.Query("INVALID QUERY");
}
catch (KuzuException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
```

## API Reference

### Core Classes

- **Database**: Represents a KuzuDB database instance
- **Connection**: Provides a connection to a database for executing queries
- **QueryResult**: Contains the results of a query execution
- **PreparedStatement**: Represents a prepared statement that can be executed multiple times
- **Row**: Represents a single row of data from a query result
- **Value**: Represents a value with type information
- **DataType**: Represents a KuzuDB data type
- **SystemConfig**: Contains system configuration options

### Exception Handling

- **KuzuException**: Base exception class for all KuzuDB-related errors

### Utilities

- **KuzuUtilities**: Static class providing utility methods like version information

## Requirements

- .NET 9.0 or later
- KuzuDB-NetCore library

## Installation

Add a reference to the KuzuDB-OOWrapper project in your solution:

```xml
<ProjectReference Include="path/to/KuzuDB-OOWrapper/KuzuDB-OOWrapper.csproj" />
```

## Performance Considerations

- Always use `using` statements or manually dispose of Database, Connection, QueryResult, and other IDisposable objects
- Reuse connections when possible; creating connections is more expensive than executing queries
- Use prepared statements for queries that will be executed multiple times
- Consider using connection pooling for multi-threaded applications

## Thread Safety

- Database instances are thread-safe for creating multiple connections
- Connection instances are NOT thread-safe and should not be shared between threads
- Each thread should have its own Connection instance

## License

This wrapper follows the same license as the underlying KuzuDB library.