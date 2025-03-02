// this simple console app demonstrates the usage of Kuzu v.0.3.2

using System;
using static kuzunet;

namespace ConsoleAppExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (kuzu_database db = kuzu_database_init("test", kuzu_default_system_config()))
            using (kuzu_connection conn = kuzu_connection_init(db))
            {
                PerformNonQuery(conn, "CREATE NODE TABLE User(name STRING, age INT64, PRIMARY KEY (name))");
                PerformNonQuery(conn, "CREATE NODE TABLE City(name STRING, population INT64, PRIMARY KEY (name))");
                PerformNonQuery(conn, "CREATE REL TABLE Follows(FROM User TO User, since INT64)");
                PerformNonQuery(conn, "CREATE REL TABLE LivesIn(FROM User TO City)");

                PerformNonQuery(conn, "COPY User FROM \"csv/users.csv\"");
                PerformNonQuery(conn, "COPY City FROM \"csv/cities.csv\"");
                PerformNonQuery(conn, "COPY Follows FROM \"csv/follows.csv\"");
                PerformNonQuery(conn, "COPY LivesIn FROM \"csv/lives.csv\"");

                var result = kuzu_connection_query(conn, "MATCH (a:User)-[f:Follows]->(b:User) RETURN a.name, f.since, b.name;");

                while (kuzu_query_result_has_next(result))
                {
                    var tuple = kuzu_query_result_get_next(result);

                    var value = kuzu_flat_tuple_get_value(tuple, 0);

                    String name = kuzu_value_get_string(value);
                    kuzu_value_destroy(value);

                    value = kuzu_flat_tuple_get_value(tuple, 1);
                    long since = kuzu_value_get_int64(value);
                    kuzu_value_destroy(value);

                    value = kuzu_flat_tuple_get_value(tuple, 2);
                    String name2 = kuzu_value_get_string(value);
                    kuzu_value_destroy(value);

                    Console.WriteLine(String.Format("{0} follows {1} since {2}", name, name2, since));
                    kuzu_flat_tuple_destroy(tuple);
                }

                kuzu_query_result_destroy(result);
                kuzu_connection_destroy(conn);
                kuzu_database_destroy(db);
            }

            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        private static void PerformNonQuery(kuzu_connection conn, string query)
        {
            kuzu_query_result result = kuzu_connection_query(conn, query);
            kuzu_query_result_destroy(result);
        }
    }
}
