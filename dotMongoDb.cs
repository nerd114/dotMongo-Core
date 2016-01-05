using System;
using System.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;

namespace dotMongo.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DotMongoDb : IDotMongoDb
    {
        /// <summary>
        /// 
        /// </summary>
        public string ConnectionStringName { get; set; }

        private static IMongoDatabase _db { get; set; }

        private static IMongoClient _client { get; set; }

        private static string host { get; set; }
        private static string port { get; set; }
        private static string catalog { get; set; }
        private static string username { get; set; }
        private static string password { get; set; }

        private static readonly Lazy<DotMongoDb> lazy = new Lazy<DotMongoDb>(() => new DotMongoDb());

        /// <summary>
        /// 
        /// </summary>
        public static DotMongoDb Instance { get { return lazy.Value; } }

        /// <summary>
        /// 
        /// </summary>
        public DotMongoDb()
        {
            Initialize(ConnectionStringName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionStringName"></param>
        public DotMongoDb(string connectionStringName)
        {
            ConnectionStringName = connectionStringName;
            Initialize(connectionStringName);
        }

        private void Initialize(string connectionStringName)
        {
            // parse the connectionstring e.g. "Server=mongodb://192.168.2.10; Port=27000; Database=dotMongo; Username=user; Password=password"
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                var connections = connectionString.Split(';');
                if (connections.Length > 0)
                {
                    host = "";
                    port = "";
                    catalog = "";
                    username = "";
                    password = "";
                    foreach (var connection in connections)
                    {
                        if (connection.Trim().ToUpper().StartsWith("SERVER="))
                        {
                            var items = connection.Split('=');
                            host = items[1];
                        }

                        if (connection.Trim().ToUpper().StartsWith("PORT="))
                        {
                            var items = connection.Split('=');
                            port = items[1];
                        }

                        if (connection.Trim().ToUpper().StartsWith("DATABASE="))
                        {
                            var items = connection.Split('=');
                            catalog = items[1];
                        }

                        if (connection.Trim().ToUpper().StartsWith("USERNAME="))
                        {
                            var items = connection.Split('=');
                            username = items[1];
                        }

                        if (connection.Trim().ToUpper().StartsWith("PASSWORD="))
                        {
                            var items = connection.Split('=');
                            password = items[1];
                        }
                    }

                    _client = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)
                        ? new MongoClient(new MongoClientSettings()
                        {
                            Credentials = new[]
                            {
                                MongoCredential.CreateCredential(catalog, username, password)
                            },
                            Server = new MongoServerAddress(host.Replace("mongodb://", ""), Convert.ToInt32(port))
                        })
                        : new MongoClient($"{host}:{port}");

                    _db = _client.GetDatabase(catalog);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Ping()
        {
            var ret = "Server connection failed.";
            var ret2 = "";

            try
            {
                var client1 = new MongoClient($"{host}:{port}");
                var db1 = client1.GetDatabase(catalog);

                // will throw an exception (timeout) if connection is unsuccessful
                db1.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();

                if (db1.Client.Cluster.Description.State.ToString() == "Connected")
                {
                    ret = "Server connection success!";

                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        ret2 = $"Credential authentication for database {catalog} failed. ";

                        var client2 = new MongoClient(new MongoClientSettings()
                        {
                            Credentials = new[]
                            {
                            MongoCredential.CreateCredential(catalog, username, password)
                        },
                            Server = new MongoServerAddress(host.Replace("mongodb://", ""), Convert.ToInt32(port))
                        });
                        var db2 = client2.GetDatabase(catalog);

                        db2.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();

                        if (db2.Client.Cluster.Description.State.ToString() == "Connected")
                        {
                            ret2 = $"Credential authentication for database {catalog} success!";
                        }
                    }
                }

                return $"{ret} {ret2}";
            }
            catch (Exception ex)
            {
                return $"{ret} {ret2}Error: {ex.InnerException.Message}";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDotMongoCollection<T> GetCollection<T>() where T : class
        {
            return new DotMongoCollection<T>(_db);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public IDotMongoCollection<T> GetCollection<T>(string collectionName) where T : class
        {
            return new DotMongoCollection<T>(_db, collectionName);
        }

    }
}
