using System;
using System.Configuration;
using MongoDB.Driver;

namespace dotMongo.Core
{
    public sealed class DotMongoDb : IDotMongoDb
    {
        public string ConnectionStringName { get; set; }

        private static IMongoDatabase _db { get; set; }

        private static readonly Lazy<DotMongoDb> lazy = new Lazy<DotMongoDb>(() => new DotMongoDb());

        public static DotMongoDb Instance { get { return lazy.Value; } }

        public DotMongoDb()
        {
            Initialize(ConnectionStringName);
        }

        public DotMongoDb(string connectionStringName)
        {
            ConnectionStringName = connectionStringName;
            Initialize(connectionStringName);
        }

        private void Initialize(string connectionStringName)
        {
            // parse the connectionstring "Server=mongodb://192.168.2.10:27000; Database=dotMongo"
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                var connections = connectionString.Split(';');
                if (connections.Length > 0)
                {
                    var dataSource = "";
                    var catalog = "";
                    foreach (var connection in connections)
                    {
                        if (connection.Trim().ToUpper().StartsWith("SERVER="))
                        {
                            var items = connection.Split('=');
                            dataSource = items[1];
                        }

                        if (connection.Trim().ToUpper().StartsWith("DATABASE="))
                        {
                            var items = connection.Split('=');
                            catalog = items[1];
                        }
                    }

                    var client = new MongoClient(dataSource);
                    _db = client.GetDatabase(catalog);
                }
            }
        }

        public IDotMongoCollection<T> GetCollection<T>() where T : class
        {
            return new DotMongoCollection<T>(_db);
        }
    }
}
