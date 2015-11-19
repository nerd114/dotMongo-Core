using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Driver;

using dotMongo.Core.Attributes;
using MongoDB.Bson.Serialization;
using System.Linq.Expressions;

namespace dotMongo.Core
{
    public sealed class DotMongoCollection<T> : IDisposable, IDotMongoCollection<T>
    {
        public IMongoCollection<BsonDocument> MongoCollection { get; set; }

        public DotMongoCollection(IMongoDatabase db)
        {
            var collectionName = typeof(T).GetCustomAttributes(typeof(CollectionName), true).FirstOrDefault() as CollectionName;
            if (collectionName != null)
            {
                MongoCollection = db.GetCollection<BsonDocument>(collectionName.Name);
            } else
            {
                MongoCollection = null;
            }
        }

        //public async Task<IEnumerable<T>> Where(Expression<Func<T, bool>> filter)
        public async Task<IEnumerable<T>> Where(FilterDefinition<BsonDocument> filter)
        {
            var items = new List<T>();

            //var filter = new FilterDefinitionBuilder<T>().Where(filter);
            using (var cursor = await MongoCollection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var item in batch)
                    {
                        items.Add(BsonSerializer.Deserialize<T>(item));
                    }
                }
            }

            return items;
        }

        public async Task<T> FirstOrDefault(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).FirstOrDefaultAsync());
        }

        // parsing expression tree
        public async Task<T> FirstOrDefault(Expression<Func<T, bool>> expression)
        {
            // Decompose the expression tree. (works for single expression)
            var body = (BinaryExpression)expression.Body;
            var left = (MemberExpression)body.Left;
            var right = (ConstantExpression)body.Right;

            // NOTE: working on multiple expression
            // Check if NodeType is Relational Operator (==, >, <, >=, <=, !=, x==y==z (all equal), x!=y!=z (all unequal), x>y>z> (strictly decreasing))

            // Check if NodeType is Logical Operator (&&, ||)

            switch (body.NodeType)
            {
                case ExpressionType.Equal:
                    var filter = Builders<BsonDocument>.Filter.Eq(left.Member.Name, right.Value);
                    return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).FirstOrDefaultAsync());
            }

            return (T)Activator.CreateInstance(typeof(T));
        }


        public async Task<T> First(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).FirstAsync());
        }

        public async Task<T> SingleOrDefault(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).SingleOrDefaultAsync());
        }

        public async Task<T> Single(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).SingleAsync());
        }

        public async Task Insert(T item)
        {
            await MongoCollection.InsertOneAsync(item.ToBsonDocument());
        }

        public async Task InsertMany(IEnumerable<T> items)
        {
            await MongoCollection.InsertManyAsync(items.Select(s => s.ToBsonDocument()));
        }

        public async Task Delete(FilterDefinition<BsonDocument> filter)
        {
            await MongoCollection.DeleteOneAsync(filter);
        }

        public async Task DeleteMany(FilterDefinition<BsonDocument> filter)
        {
            await MongoCollection.DeleteManyAsync(filter);
        }

        public async Task<long> Count()
        {
            return await MongoCollection.CountAsync(new BsonDocument());
        }

        public async Task<long> Count(FilterDefinition<BsonDocument> filter)
        {
            return await MongoCollection.CountAsync(filter);
        }

        public async Task<IEnumerable<T>> WhereDynamic(FilterDefinition<BsonDocument> filter)
        {
            var items = new List<T>();

            //var filter = new FilterDefinitionBuilder<T>().Where(filter);
            using (var cursor = await MongoCollection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var item in batch)
                    {
                        //items.Add(BsonSerializer.Deserialize<T>(item));
                        object n = Activator.CreateInstance(typeof(T));

                        // create a private function, need to support sub class (one-to-one, one-to-many)
                        var props = n.GetType().GetProperties();
                        if (props.Length > 0)
                        {
                            foreach (var prop in props)
                            {
                                if (prop.CanWrite)
                                {
                                    // object id
                                    if (prop.Name.EndsWith("Id") && prop.PropertyType == typeof(ObjectId))
                                    {
                                        prop.SetValue(n, item["_id"].AsObjectId);
                                        continue;
                                    }

                                    // string value
                                    if (prop.PropertyType == typeof(string) && item[prop.Name].IsString)
                                    {
                                        prop.SetValue(n, item[prop.Name].AsString);
                                    }
                                }
                            }
                        }

                        items.Add((T)n);
                    }
                }
            }

            return items;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (MongoCollection != null)
                {
                    MongoCollection = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
