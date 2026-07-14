using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        var connectionString = "Data Source=apps/backend-dotnet/vehicle_vision.db";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT RawExtractedText FROM VehicleScans ORDER BY CreatedAt DESC LIMIT 1;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine("--- RAW OCR TEXT ---");
            Console.WriteLine(reader.GetString(0));
            Console.WriteLine("--------------------");
        }
    }
}
