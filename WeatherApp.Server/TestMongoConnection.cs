using MongoDB.Driver;
using MongoDB.Bson;

namespace WeatherApp.Server
{
    public static class MongoConnectionTest
    {
        public static async Task TestConnection(string connectionString)
        {
            try
            {
                Console.WriteLine("Testing MongoDB Atlas connection...");
                Console.WriteLine(
    $"Connection String: {connectionString[..Math.Min(50, connectionString.Length)]}..."
);


                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);

                var client = new MongoClient(settings);
                var database = client.GetDatabase("WeatherAppDB");

                // Ping the database
                var result = await database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1)
                );

                Console.WriteLine("✅ Successfully connected to MongoDB Atlas!");
                Console.WriteLine("Database: WeatherAppDB");
                Console.WriteLine($"Response: {result}");

                // List collections
                var collections = await database.ListCollectionNamesAsync();
                var collectionList = await collections.ToListAsync();
                Console.WriteLine($"Collections: {string.Join(", ", collectionList)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ MongoDB Atlas connection failed!");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
