using System;
using Npgsql;

class Program
{
    static int Main(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("DB_BASELINE_CONN")
            ?? "Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password";

        var mode = args.Length > 0 ? args[0] : "baseline";
        Console.WriteLine($"Connecting to: {connStr} (mode={mode})");
        try
        {
            using var conn = new NpgsqlConnection(connStr);
            conn.Open();

            if (mode.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"select table_name from information_schema.tables where table_schema='public' order by table_name";
                using var reader = cmd.ExecuteReader();
                Console.WriteLine("Public tables:");
                while (reader.Read())
                {
                    Console.WriteLine(" - " + reader.GetString(0));
                }
                return 0;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
    ""MigrationId"" character varying(150) NOT NULL,
    ""ProductVersion"" character varying(32) NOT NULL,
    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('20250909165144_EnhancedSessionManagement','9.0.9')
ON CONFLICT (""MigrationId"") DO NOTHING;";
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("Baseline inserted for migration 20250909165144_EnhancedSessionManagement.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Operation failed: " + ex);
            return 1;
        }
    }
}
