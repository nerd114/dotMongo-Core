using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace dotMongo.Core
{
    public interface IDotMongoCollection<T>
    {
        IMongoCollection<BsonDocument> MongoCollection { get; set; }

        //Task<IEnumerable<T>> Where(Expression<Func<T, bool>> filter);
        Task<IEnumerable<T>> Where(FilterDefinition<BsonDocument> filter);
        Task<T> FirstOrDefault(FilterDefinition<BsonDocument> filter);
        Task<T> FirstOrDefault(Expression<Func<T, bool>> expression);
        Task<T> First(FilterDefinition<BsonDocument> filter);
        Task<T> SingleOrDefault(FilterDefinition<BsonDocument> filter);
        Task<T> Single(FilterDefinition<BsonDocument> filter);
        Task Insert(T item);
        Task InsertMany(IEnumerable<T> items);
        Task Delete(FilterDefinition<BsonDocument> filter);
        Task DeleteMany(FilterDefinition<BsonDocument> filter);
        Task<long> Count();
        Task<long> Count(FilterDefinition<BsonDocument> filter);

        Task<IEnumerable<T>> WhereDynamic(FilterDefinition<BsonDocument> filter);
    }
}
