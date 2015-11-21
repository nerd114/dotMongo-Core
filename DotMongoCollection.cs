using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Driver;

using dotMongo.Core.Attributes;
using MongoDB.Bson.Serialization;
using System.Linq.Expressions;
using System.Reflection;

namespace dotMongo.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class DotMongoCollection<T> : IDisposable, IDotMongoCollection<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public IMongoCollection<BsonDocument> MongoCollection { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
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

        public DotMongoCollection(IMongoDatabase db, string collectionName)
        {
            MongoCollection = db.GetCollection<BsonDocument>(collectionName);
        }

        public async Task<IEnumerable<T>> Where(FilterDefinition<BsonDocument> filter)
        {
            var items = new List<T>();

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

        public async Task<IEnumerable<T>> Where(Expression<Func<T, bool>> expression)
        {
            var items = new List<T>();

            if (IsBinaryExpression(expression))
            {
                using (var cursor = await MongoCollection.FindAsync(ParseExpression((BinaryExpression)expression.Body)))
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
            }

            return items;
        }

        /// <summary>
        /// Get the first result or null
        /// </summary>
        /// <param name="filter">MongoDB driver filter definition</param>
        /// <returns>T in DotMongoCollection</returns>
        public async Task<T> FirstOrDefault(FilterDefinition<BsonDocument> filter)
        {
            var item = await MongoCollection.Find(filter).FirstOrDefaultAsync();
            if (item != null)
            {
                return BsonSerializer.Deserialize<T>(item);
            }

            return default(T);
        }

        /// <summary>
        /// Get the first result or null 
        /// </summary>
        /// <param name="expression">Lambda expression</param>
        /// <returns>T in DotMongoCollection</returns>
        public async Task<T> FirstOrDefault(Expression<Func<T, bool>> expression)
        {
            if (IsBinaryExpression(expression))
            {
                var item = await MongoCollection.Find(ParseExpression((BinaryExpression)expression.Body)).FirstOrDefaultAsync();
                if (item != null)
                {
                    return BsonSerializer.Deserialize<T>(item);
                }
            }

            return default(T); // return null;
        }

        public async Task<T> First(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).FirstAsync());
        }

        public async Task<T> First(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
            {
                return BsonSerializer.Deserialize<T>(await MongoCollection.Find(ParseExpression((BinaryExpression)expression.Body)).FirstAsync());
            } else
            {
                throw new ArgumentException($"Binary expression expected in '{expression.ToString()}'.");
            }
        }

        public async Task<T> SingleOrDefault(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).SingleOrDefaultAsync());
        }

        public async Task<T> SingleOrDefault(Expression<Func<T, bool>> expression)
        {
            if (IsBinaryExpression(expression))
            {
                var item = await MongoCollection.Find(ParseExpression((BinaryExpression)expression.Body)).SingleOrDefaultAsync();
                if (item != null)
                {
                    return BsonSerializer.Deserialize<T>(item);
                }
            }

            return default(T);
        }

        public async Task<T> Single(FilterDefinition<BsonDocument> filter)
        {
            return BsonSerializer.Deserialize<T>(await MongoCollection.Find(filter).SingleAsync());
        }

        public async Task<T> Single(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
            {
                return BsonSerializer.Deserialize<T>(await MongoCollection.Find(ParseExpression((BinaryExpression)expression.Body)).SingleAsync());
            }
            else
            {
                throw new ArgumentException($"Binary expression expected in '{expression.ToString()}'.");
            }
        }

        public async Task Delete(FilterDefinition<BsonDocument> filter)
        {
            await MongoCollection.DeleteOneAsync(filter);
        }

        public async Task Delete(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
            {
                await MongoCollection.DeleteOneAsync(ParseExpression((BinaryExpression)expression.Body));
            }
            else
            {
                throw new ArgumentException($"Binary expression expected in '{expression.ToString()}'.");
            }
        }

        public async Task DeleteMany(FilterDefinition<BsonDocument> filter)
        {
            await MongoCollection.DeleteManyAsync(filter);
        }

        public async Task DeleteMany(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
            {
                await MongoCollection.DeleteManyAsync(ParseExpression((BinaryExpression)expression.Body));
            }
            else
            {
                throw new ArgumentException($"Binary expression expected in '{expression.ToString()}'.");
            }
        }

        public async Task<long> Count(FilterDefinition<BsonDocument> filter)
        {
            return await MongoCollection.CountAsync(filter);
        }

        public async Task<long> Count(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
            {
                return await MongoCollection.CountAsync(ParseExpression((BinaryExpression)expression.Body));
            }
            else
            {
                throw new ArgumentException($"Binary expression expected in '{expression.ToString()}'.");
            }
        }

        public async Task<long> Count()
        {
            return await MongoCollection.CountAsync(new BsonDocument());
        }

        public async Task Insert(T item)
        {
            await MongoCollection.InsertOneAsync(item.ToBsonDocument());
        }

        public async Task InsertMany(IEnumerable<T> items)
        {
            await MongoCollection.InsertManyAsync(items.Select(s => s.ToBsonDocument()));
        }

        private FilterDefinition<BsonDocument> ParseExpression(BinaryExpression binary)
        {
            if (IsRelationalNode(binary.NodeType)) // evaluating Member.Name = Value (e.g. x.User == "James")
            {
                ValidateParts(binary);

                return BuildFilterDefinition(binary.NodeType, (MemberExpression)binary.Left, (ConstantExpression)binary.Right);

            } else if (IsLogicalNode(binary.NodeType))
            {
                ValidateOperands(binary);

                return BranchFilterDefinition(binary.NodeType, (BinaryExpression)binary.Left, (BinaryExpression)binary.Right);
            } else 
            {
                throw new ArgumentException($"Failed to evaluate expression of node type '{binary.NodeType.ToString()}'.");
            }
        }

        private FilterDefinition<BsonDocument> BranchFilterDefinition(ExpressionType node, BinaryExpression binaryleft, BinaryExpression binaryright)
        {
            if (IsRelationalNode(binaryleft.NodeType) && IsRelationalNode(binaryright.NodeType))
            {
                ValidateParts(binaryleft);

                ValidateParts(binaryright);

                switch (node)
                {
                    case ExpressionType.AndAlso:
                        return BuildFilterDefinition(binaryleft.NodeType, (MemberExpression)binaryleft.Left, (ConstantExpression)binaryleft.Right) &
                               BuildFilterDefinition(binaryright.NodeType, (MemberExpression)binaryright.Left, (ConstantExpression)binaryright.Right);
                    case ExpressionType.OrElse:
                        return BuildFilterDefinition(binaryleft.NodeType, (MemberExpression)binaryleft.Left, (ConstantExpression)binaryleft.Right) |
                               BuildFilterDefinition(binaryright.NodeType, (MemberExpression)binaryright.Left, (ConstantExpression)binaryright.Right);
                    default:
                        throw new ArgumentException($"'{node.ToString()}' operation not supported in expression.");
                }
            } else if (IsLogicalNode(binaryleft.NodeType) && IsRelationalNode(binaryright.NodeType))
            {
                ValidateOperands(binaryleft);

                ValidateParts(binaryright);

                switch (node)
                {
                    case ExpressionType.AndAlso:
                        return BranchFilterDefinition(binaryleft.NodeType, (BinaryExpression)binaryleft.Left, (BinaryExpression)binaryleft.Right) &
                               BuildFilterDefinition(binaryright.NodeType, (MemberExpression)binaryright.Left, (ConstantExpression)binaryright.Right);
                    case ExpressionType.OrElse:
                        return BranchFilterDefinition(binaryleft.NodeType, (BinaryExpression)binaryleft.Left, (BinaryExpression)binaryleft.Right) |
                               BuildFilterDefinition(binaryright.NodeType, (MemberExpression)binaryright.Left, (ConstantExpression)binaryright.Right);
                    default:
                        throw new ArgumentException($"'{node.ToString()}' operation not supported in expression.");
                }
            } else if (IsRelationalNode(binaryleft.NodeType) && IsLogicalNode(binaryright.NodeType))
            {
                ValidateParts(binaryleft);

                ValidateOperands(binaryright);

                switch (node)
                {
                    case ExpressionType.AndAlso:
                        return BuildFilterDefinition(binaryleft.NodeType, (MemberExpression)binaryleft.Left, (ConstantExpression)binaryleft.Right) &
                               BranchFilterDefinition(binaryright.NodeType, (BinaryExpression)binaryright.Left, (BinaryExpression)binaryright.Right);
                    case ExpressionType.OrElse:
                        return BuildFilterDefinition(binaryleft.NodeType, (MemberExpression)binaryleft.Left, (ConstantExpression)binaryleft.Right) |
                               BranchFilterDefinition(binaryright.NodeType, (BinaryExpression)binaryright.Left, (BinaryExpression)binaryright.Right);
                    default:
                        throw new ArgumentException($"'{node.ToString()}' operation not supported in expression.");
                }
            } else if (IsLogicalNode(binaryleft.NodeType) && IsLogicalNode(binaryright.NodeType))
            {
                ValidateOperands(binaryleft);

                ValidateOperands(binaryright);

                switch (node)
                {
                    case ExpressionType.AndAlso:
                        return BranchFilterDefinition(binaryleft.NodeType, (BinaryExpression)binaryleft.Left, (BinaryExpression)binaryleft.Right) &
                               BranchFilterDefinition(binaryright.NodeType, (BinaryExpression)binaryright.Left, (BinaryExpression)binaryright.Right);
                    case ExpressionType.OrElse:
                        return BranchFilterDefinition(binaryleft.NodeType, (BinaryExpression)binaryleft.Left, (BinaryExpression)binaryleft.Right) |
                               BranchFilterDefinition(binaryright.NodeType, (BinaryExpression)binaryright.Left, (BinaryExpression)binaryright.Right);
                    default:
                        throw new ArgumentException($"'{node.ToString()}' operation not supported in expression.");
                }
            } else
            {
                throw new ArgumentException($"Failed to evaluate expression of node type '{node.ToString()}'.");
            }
        }

        private FilterDefinition<BsonDocument> BuildFilterDefinition(ExpressionType node, MemberExpression left, ConstantExpression right)
        {
            switch (node)
            {
                case ExpressionType.Equal:
                    return Builders<BsonDocument>.Filter.Eq(left.Member.Name, right.Value);
                case ExpressionType.GreaterThan:
                    return Builders<BsonDocument>.Filter.Gt(left.Member.Name, right.Value);
                case ExpressionType.GreaterThanOrEqual:
                    return Builders<BsonDocument>.Filter.Gte(left.Member.Name, right.Value);
                case ExpressionType.LessThan:
                    return Builders<BsonDocument>.Filter.Lt(left.Member.Name, right.Value);
                case ExpressionType.LessThanOrEqual:
                    return Builders<BsonDocument>.Filter.Lte(left.Member.Name, right.Value);
                case ExpressionType.NotEqual:
                    return Builders<BsonDocument>.Filter.Ne(left.Member.Name, right.Value);
                default:
                    throw new ArgumentException($"'{node.ToString()}' comparison not supported in expression.");
            }
        }

        // 
        /// <summary>
        /// Check if node type (NodeType) is Logical Operator
        /// Currently supports:
        ///     AndAlso, OrElse
        /// </summary>
        /// <param name="node">Expression Type</param>
        /// <returns>Boolean (True/False)</returns>
        private bool IsLogicalNode(ExpressionType node)
        {
            // Inculde logical expression here..
            return (node == ExpressionType.AndAlso || node == ExpressionType.OrElse) ? true : false;
        }

        /// <summary>
        /// Check if node type (NodeType) is Relational Operator. 
        /// Currently supports:  
        ///     Equal, Greater Than, Greater Than Equal, Less Than, Less Than Equal, Not Equal 
        /// </summary>
        /// <param name="node">Expression Type</param>
        /// <returns>Boolean (True/False)</returns>
        private bool IsRelationalNode(ExpressionType node)
        {
            // Include relational expression here..
            return (node == ExpressionType.Equal || node == ExpressionType.GreaterThan || node == ExpressionType.GreaterThanOrEqual ||
                    node == ExpressionType.LessThan || node == ExpressionType.LessThanOrEqual || node == ExpressionType.NotEqual) ? true : false;
        }

        private bool IsBinaryExpression(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
            {
                return true;
            } else
            {
                throw new ArgumentException($"Binary expression expected in '{expression.ToString()}'.");
            }
        }

        private void ValidateParts(BinaryExpression binary)
        {
            if (!(binary.Left is MemberExpression))
                throw new ArgumentException($"Member expected in expression '{binary.ToString()}'.");

            if (!(binary.Right is ConstantExpression))
                throw new ArgumentException($"Constant expected in expression '{binary.ToString()}'.");
        }

        private void ValidateOperands(BinaryExpression binary)
        {
            if (!(binary.Left is BinaryExpression))
                throw new ArgumentException($"Expression expected on left operand in '{binary.ToString()}'.");

            if (!(binary.Right is BinaryExpression))
                throw new ArgumentException($"Expression expected on right operand in '{binary.ToString()}'.");
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
