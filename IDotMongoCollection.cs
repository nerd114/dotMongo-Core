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

        Task<IEnumerable<T>> Where(FilterDefinition<BsonDocument> filter);
        Task<T> FirstOrDefault(FilterDefinition<BsonDocument> filter);        
        Task<T> First(FilterDefinition<BsonDocument> filter);
        Task<T> SingleOrDefault(FilterDefinition<BsonDocument> filter);
        Task<T> Single(FilterDefinition<BsonDocument> filter);
        Task Delete(FilterDefinition<BsonDocument> filter);
        Task DeleteMany(FilterDefinition<BsonDocument> filter);
        Task<long> Count(FilterDefinition<BsonDocument> filter);
        Task<long> Count();
        Task Insert(T item);
        Task InsertMany(IEnumerable<T> items);
        Task<UpdateResult> Update(T item);

        Task<IEnumerable<T>> Where(Expression<Func<T, bool>> expression);
        Task<T> FirstOrDefault(Expression<Func<T, bool>> expression);
        Task<T> First(Expression<Func<T, bool>> expression);
        Task<T> SingleOrDefault(Expression<Func<T, bool>> expression);
        Task<T> Single(Expression<Func<T, bool>> expression);
        Task Delete(Expression<Func<T, bool>> expression);
        Task DeleteMany(Expression<Func<T, bool>> expression);
        Task<long> Count(Expression<Func<T, bool>> expression);
    }
}
