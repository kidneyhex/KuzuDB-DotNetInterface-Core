# KuzuDB Console Core Examples

This project demonstrates various KuzuDB operations using the .NET 9 console application. It showcases graph database functionality including schema creation, data loading, querying, and manipulation using the KuzuDB .NET bindings.

## Project Overview

KuzuDB-ConsoleCore is a comprehensive example application that demonstrates:
- Database initialization and connection management
- Schema creation for nodes and relationships
- CSV data loading
- Various types of graph queries
- Data modification operations
- Advanced pattern matching and path finding

## Prerequisites

- .NET 9 SDK
- KuzuDB native libraries
- CSV data files (included in the `csv/` directory)

## Data Schema

The application works with a simple social network graph consisting of:

### Node Types
- **User**: Properties `name` (STRING, PRIMARY KEY), `age` (INT64)
- **City**: Properties `name` (STRING, PRIMARY KEY), `population` (INT64)

### Relationship Types
- **Follows**: FROM User TO User, Properties `since` (INT64)
- **LivesIn**: FROM User TO City

## Sample Data

The application includes sample CSV files:

### users.csv
```
Adam,30
Karissa,40
Zhang,50
Noura,25
```

### cities.csv
```
Waterloo,150000
Kitchener,200000
Guelph,75000
```

### follows.csv
```
Adam,Karissa,2020
Adam,Zhang,2020
Karissa,Zhang,2021
Zhang,Noura,2022
```

### lives-in.csv
```
Adam,Waterloo
Karissa,Waterloo
Zhang,Kitchener
Noura,Guelph
```

## Features Demonstrated

### 1. Schema Creation
- Creating node tables with properties and primary keys
- Creating relationship tables with FROM/TO constraints
- Setting up a complete graph schema

### 2. Data Loading
- Loading data from CSV files using the COPY command
- Bulk data import for nodes and relationships

### 3. Basic Pattern Matching
- Simple graph traversal queries
- Matching nodes and relationships
- Extracting properties from query results

### 4. Aggregation Queries
- Counting followers per user
- Calculating average age by city
- Computing total population across cities
- Using ORDER BY for result sorting

### 5. Complex Multi-Relationship Queries
- Finding users who follow others in the same city
- Identifying cities with popular users
- Cross-referencing multiple relationship types

### 6. Data Modification
- Creating new nodes dynamically
- Adding new relationships
- Updating node properties
- Real-time data manipulation

### 7. Path Finding
- Finding follow chains (paths of length 2)
- Discovering mutual follow relationships
- Graph traversal patterns

### 8. Property Filtering
- Filtering nodes by property values
- Date/time range queries on relationships
- Conditional result filtering

## Key Code Patterns

### Database Initialization
```csharp
using kuzu_database db = new();
using kuzu_connection conn = new();
using kuzu_system_config config = kuzu_default_system_config();

var state = kuzu_database_init("", config, db);
if (state == kuzu_state.KuzuError) {
    // Handle error
}
```

### Query Execution
```csharp
using kuzu_query_result result = new();
var state = kuzu_connection_query(conn, query, result);

while (kuzu_query_result_has_next(result)) {
    using kuzu_flat_tuple tuple = new();
    kuzu_query_result_get_next(result, tuple);
    
    // Extract values from tuple
    using (kuzu_value value = new()) {
        kuzu_flat_tuple_get_value(tuple, 0, value);
        kuzu_value_get_string(value, out string stringVal);
    }
}
```

### Error Handling
The application demonstrates proper error handling patterns:
- Checking return states for all operations
- Using proper disposal patterns with `using` statements
- Graceful error reporting and recovery

## Running the Application

1. Ensure all dependencies are installed
2. Place CSV files in the `csv/` directory
3. Run the application:
   ```bash
   dotnet run
   ```

## Expected Output

The application will display:
1. Schema creation confirmation
2. Data loading confirmation
3. Results from various query examples
4. Data modification confirmations
5. Complex query results with formatted output

## Error Handling

The application includes comprehensive error handling:
- Database initialization errors
- Connection errors
- Query execution errors
- Data type conversion handling
- Resource disposal management

## Performance Considerations

- Uses proper resource disposal with `using` statements
- Implements efficient query patterns
- Demonstrates bulk data loading techniques
- Shows proper connection management

## Extension Points

This example can be extended with:
- More complex graph algorithms
- Additional node and relationship types
- Custom data validation
- Performance benchmarking
- Integration with other data sources

## Troubleshooting

Common issues and solutions:

1. **Database creation fails**: Ensure write permissions in the application directory
2. **CSV loading fails**: Verify CSV files exist and have correct format
3. **Query errors**: Check Cypher query syntax and node/relationship names
4. **Connection issues**: Verify KuzuDB native libraries are properly installed

## Related Projects

- **KuzuDB-NetCore**: Core .NET bindings for KuzuDB
- **KuzuDB-OOWrapper**: Object-oriented wrapper for easier development
- **KuzuDB-OOWrapper-Examples**: Examples using the OO wrapper

## License

This project follows the same license as the parent KuzuDB .NET Interface project.