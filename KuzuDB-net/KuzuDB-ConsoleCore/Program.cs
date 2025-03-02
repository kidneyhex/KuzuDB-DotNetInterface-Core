using static kuzunet;

namespace KuzuDB_ConsoleCore
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Wipe out old test DB if it exists
            if (System.IO.Directory.Exists("test"))
                System.IO.Directory.Delete("test", true);

            var state = kuzu_database_init("test", kuzu_default_system_config(), out var db);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Could not create DB");
                return;
            }

            state = kuzu_connection_init(db, out var conn);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Could not connect to DB");
                return;
            }

            PerformNonQuery(conn, "CREATE NODE TABLE User(name STRING, age INT64, PRIMARY KEY (name))");
            PerformNonQuery(conn, "CREATE NODE TABLE City(name STRING, population INT64, PRIMARY KEY (name))");
            PerformNonQuery(conn, "CREATE REL TABLE Follows(FROM User TO User, since INT64)");
            PerformNonQuery(conn, "CREATE REL TABLE LivesIn(FROM User TO City)");

            PerformNonQuery(conn, "COPY User FROM \"csv/users.csv\"");
            PerformNonQuery(conn, "COPY City FROM \"csv/cities.csv\"");
            PerformNonQuery(conn, "COPY Follows FROM \"csv/follows.csv\"");
            PerformNonQuery(conn, "COPY LivesIn FROM \"csv/lives-in.csv\"");

            state = PerformQuery(conn, "MATCH (a:User)-[f:Follows]->(b:User) RETURN a.name, f.since, b.name;", out var result);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Error performing MATCH");
                return;
            }


            while (kuzu_query_result_has_next(result))
            {
                kuzu_query_result_get_next(result, out var tuple);

                kuzu_flat_tuple_get_value(tuple, 0, out var value);
                kuzu_value_get_string(value, out string name);
                value.Destroy();

                kuzu_flat_tuple_get_value(tuple, 1, out value);
                kuzu_value_get_int64(value, out long since);
                value.Destroy();

                kuzu_flat_tuple_get_value(tuple, 2, out value);
                kuzu_value_get_string(value, out string name2);
                value.Destroy();

                Console.WriteLine(String.Format("{0} follows {1} since {2}", name, name2, since));
                tuple.Destroy();
            }

            result.Destroy();
            conn.Destroy();
            db.Destroy();
        }

        private static void PerformNonQuery(kuzu_connection conn, string query)
        {
            var state = kuzu_connection_query(conn, query, out kuzu_query_result result);
            if (state == kuzu_state.KuzuError)
            {
                Console.WriteLine("Could not perform: " + query);
                return;
            }
            result.Destroy();
        }

        private static kuzu_state PerformQuery(kuzu_connection conn, string query, out kuzu_query_result result)
        {
            return kuzu_connection_query(conn, query, out result);
        }
    }
}
