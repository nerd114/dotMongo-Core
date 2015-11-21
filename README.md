# dotMongo Core Project (v1.0.0.0)
dotMongo (core) is a C#.NET open source project that tries to extend and simplify MongoDB database function calls for .NET developers. 
The project wraps around the MongoDB .NET Driver (v2.1.1) and handles;

1. Creating of MongoDB client and connecting to the database by parsing an XML configuration Connection String.
2. Serialization of BsonDocument to Generic C# object.
3. Converting basic Lambda Expressions to Filter Definition.
 
You might ask me **Why another layer of service? MongoDB .NET Driver already supports the features mentioned**? 

My answer is **less reading, faster coding**!

If you're a seasoned .NET developer who's familiar with LINQ, you can start working immediately with MongoDB!

This code:
```
var builder = Builders<Widget>.Filter;
var filter = builder.Eq(widget => widget.X, 10) & builder.Lt(widget => widget.Y, 20);
```

is equal to this code using dotMongo: 
```
widget.Where(x => x.X == 10 && x.Y < 20);
```

Now even though it's this simple to create filters, I still recommend reading and learning MongoDB .NET Driver on your spare time! dotMongo is intended as an add-on to the MongoDB .NET Driver. Learning the basics is still the best practice! 
