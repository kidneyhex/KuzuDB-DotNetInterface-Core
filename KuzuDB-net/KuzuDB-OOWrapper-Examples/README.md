# KuzuDB Object-Oriented Wrapper Examples

This project demonstrates comprehensive KuzuDB operations using the Object-Oriented .NET wrapper. It showcases graph database functionality with a clean, type-safe API that abstracts the low-level native bindings.

## Project Overview

KuzuDB-OOWrapper-Examples is a comprehensive demonstration application that showcases:
- Database initialization and connection management using OO patterns
- Schema creation for nodes and relationships
- Sample data creation and loading
- Various types of graph queries with type-safe result handling
- Data modification operations
- Advanced pattern matching and path finding
- Prepared statements with parameter binding
- Comprehensive data type handling and conversion

## Prerequisites

- .NET 9 SDK
- KuzuDB native libraries
- KuzuDB-OOWrapper project reference

## Architecture Benefits

The Object-Oriented wrapper provides several advantages over direct native bindings:

### Type Safety
- Strongly typed `Value` objects with explicit type conversion methods
- Compile-time checking for parameter types
- Exception-based error handling instead of return code checking

### Resource Management
- Automatic disposal of native resources using `IDisposable` pattern
- RAII-style resource management with `using` statements
- Reduced risk of memory leaks and native handle corruption

### Intuitive API
- Method chaining and fluent interfaces
- Enumerable query results with `foreach` support
- Column access by name or index
- Clean separation of concerns

## Data Schema

The application demonstrates a social network graph consisting of:

### Node Types
- **User**: Properties `name` (STRING, PRIMARY KEY), `age` (INT64)
- **City**: Properties `name` (STRING, PRIMARY KEY), `population` (INT64)
- **Product**: Properties `id` (INT64, PRIMARY KEY), `name` (STRING), `price` (DOUBLE), `in_stock` (BOOLEAN)
- **DataTypeDemo**: Comprehensive table showcasing various data types

### Relationship Types
- **Follows**: FROM User TO User, Properties `since` (INT64)
- **LivesIn**: FROM User TO City

## Example Categories

### 1. Schema Creation
```csharp
ExecuteQuery(connection, "CREATE NODE TABLE User(name STRING, age INT64, PRIMARY KEY (name))");
ExecuteQuery(connection, "CREATE REL TABLE Follows(FROM User TO User, since INT64)");
```
- Creating node tables with properties and primary keys
- Creating relationship tables with FROM/TO constraints
- Setting up complete graph schemas

### 2. Sample Data Loading
```csharp
ExecuteQuery(connection, "CREATE (:User {name: 'Adam', age: 30})");
ExecuteQuery(connection, "MATCH (a:User {name: 'Adam'}), (b:User {name: 'Karissa'}) CREATE (a)-[:Follows {since: 2020}]->(b)");
```
- Programmatic data creation using Cypher queries
- Creating nodes with properties
- Establishing relationships between existing nodes

### 3. Basic Pattern Matching
```csharp
using var result = connection.Query("MATCH (a:User)-[f:Follows]->(b:User) RETURN a.name, f.since, b.name ORDER BY f.since, a.name");
```
- Simple graph traversal queries
- Matching nodes and relationships
- Type-safe property extraction from query results

### 4. Aggregation Queries
```csharp
using var followersResult = connection.Query(
    "MATCH (a:User)<-[f:Follows]-(b:User) RETURN a.name, COUNT(*) AS follower_count ORDER BY follower_count DESC");
```
- Counting relationships and grouping results
- Statistical functions (COUNT, AVG, SUM)
- Result ordering and formatting

### 5. Complex Multi-Relationship Queries
```csharp
using var sameCityResult = connection.Query(
    @"MATCH (u1:User)-[:Follows]->(u2:User), 
            (u1)-[:LivesIn]->(c:City)<-[:LivesIn]-(u2) 
      RETURN u1.name, u2.name, c.name");
```
- Multiple relationship traversals in single queries
- Complex pattern matching
- Cross-referencing different entity types

### 6. Data Modification Operations
```csharp
ExecuteQuery(connection, "CREATE (u:User {name: 'Emma', age: 28})");
ExecuteQuery(connection, "MATCH (u:User {name: 'Adam'}) SET u.age = 31");
```
- Creating new nodes dynamically
- Updating existing node properties
- Real-time data manipulation

### 7. Path Finding Queries
```csharp
using var followChainsResult = connection.Query(
    @"MATCH (u1:User)-[:Follows]->(u2:User)-[:Follows]->(u3:User) 
      RETURN u1.name, u2.name, u3.name");
```
- Finding connection paths between nodes
- Multi-hop relationship traversals
- Graph analysis patterns

### 8. Property Filtering
```csharp
using var olderUsersResult = connection.Query(
    "MATCH (u:User) WHERE u.age > 30 RETURN u.name, u.age ORDER BY u.age");
```
- Conditional filtering on node properties
- Range queries and comparisons
- Complex WHERE clause combinations

### 9. Advanced Prepared Statements
```csharp
using var insertStmt = connection.Prepare("CREATE (:Product {id: $id, name: $name, price: $price, in_stock: $in_stock})");
insertStmt.BindInt64("id", 1);
insertStmt.BindString("name", "Laptop");
insertStmt.BindDouble("price", 999.99);
insertStmt.BindBool("in_stock", true);
using var result = insertStmt.Execute();
```
- Parameterized queries for security and performance
- Type-safe parameter binding
- Bulk operations with prepared statements
- Query reuse and optimization

### 10. Comprehensive Data Types
```csharp
var value = row[i];
var dataType = value.GetDataType();
string displayValue = dataType.ToString().ToLower() switch
{
    var dt when dt.Contains("bool") => value.GetBool().ToString(),
    var dt when dt.Contains("int64") => value.GetInt64().ToString(),
    var dt when dt.Contains("double") => value.GetDouble().ToString("F2"),
    _ => value.ToString()
};
```
- Working with all KuzuDB data types
- Type detection and conversion
- Proper formatting for different data types

## Key Code Patterns

### Database Initialization
```csharp
using var database = new Database(dbPath);
using var connection = database.CreateConnection();
```

### Safe Query Execution
```csharp
private static void ExecuteQuery(Connection connection, string query)
{
    using var result = connection.Query(query);
    if (!result.IsSuccess)
    {
        throw new Exception($"Query failed: {result.ErrorMessage}");
    }
}
```

### Result Processing
```csharp
foreach (var row in result)
{
    for (ulong i = 0; i < row.ColumnCount; i++)
    {
        var value = row[i];
        if (value.IsNull)
        {
            Console.WriteLine("NULL");
        }
        else
        {
            Console.WriteLine(value.ToString());
        }
    }
}
```

### Column Access by Name
```csharp
var name = row["e.name"].GetString();
var salary = row["e.salary"].GetDouble();
```

## Error Handling

The OO wrapper provides structured exception handling:

```csharp
try
{
    using var result = connection.Query(complexQuery);
    // Process results
}
catch (KuzuException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
    // Handle database-specific errors
}
catch (Exception ex)
{
    Console.WriteLine($"General error: {ex.Message}");
    // Handle other errors
}
```

## Performance Features

- **Prepared Statements**: Compile queries once, execute multiple times
- **Resource Management**: Automatic cleanup prevents memory leaks
- **Type Safety**: Compile-time checking reduces runtime errors
- **Efficient Iteration**: Direct enumeration over query results

## Running the Application

1. Ensure all dependencies are installed
2. Build the solution including KuzuDB-OOWrapper
3. Run the application:
   ```bash
   dotnet run
   ```

## Expected Output

The application demonstrates:
1. Version information display
2. Schema creation with detailed logging
3. Sample data loading with statistics
4. Query results with formatted output
5. Data modification confirmations
6. Advanced prepared statement usage
7. Comprehensive data type handling

Each example includes:
- Clear section headers
- Detailed operation descriptions  
- Formatted result display
- Error handling and validation

## Comparison with Native Bindings

| Feature | Native Bindings | OO Wrapper |
|---------|----------------|------------|
| Type Safety | Manual type checking | Compile-time type safety |
| Error Handling | Return code checking | Exception-based |
| Resource Management | Manual disposal | Automatic with `using` |
| API Complexity | Low-level C-style | High-level .NET-style |
| Memory Safety | Manual management | RAII patterns |
| Code Readability | Verbose | Clean and intuitive |

## Extension Points

This example can be extended with:
- Custom data validation and business logic
- Complex graph algorithms implementation
- Integration with .NET data access patterns
- Performance monitoring and profiling
- Custom data type converters
- Advanced query builders

## Troubleshooting

Common issues and solutions:

1. **Connection failures**: Verify database path and permissions
2. **Query errors**: Check Cypher syntax and node/relationship names
3. **Type conversion errors**: Ensure proper data type handling
4. **Memory issues**: Verify proper disposal of resources with `using` statements

## Related Projects

- **KuzuDB-NetCore**: Low-level .NET bindings for KuzuDB
- **KuzuDB-ConsoleCore**: Examples using direct native bindings
- **KuzuDB-OOWrapper**: The object-oriented wrapper library

## Performance Considerations

- Use prepared statements for repeated queries
- Dispose resources properly with `using` statements
- Consider connection pooling for multi-threaded applications
- Monitor memory usage with native resource cleanup

## License

This project follows the same license as the parent KuzuDB .NET Interface project.