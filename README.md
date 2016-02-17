# [dotMongo Project (v0.0.2)](http://dotmongo.com/)
dotMongo is a C#.NET open source project that (tries to) extend and simplify MongoDB database function calls for .NET developers. 
The project wraps around the MongoDB .NET Driver (v2.2.3) and handles;

1. Creating MongoDB client and connecting to the database by parsing an XML configuration Connection String.
2. Serialization of BsonDocument to Generic C# object.
3. Converting basic Lambda Expressions to Filter Definition.
 
**But why another layer of service? MongoDB .NET Driver already supports connecting to MongoDB and Lamda expressions?** 

**Well how about giving you a helping hand on coding with MongoDB .NET Driver so you can finish your project faster!**

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

We still recommend that you read and learn MongoDB .NET Driver. dotMongo is intended as an add-on to the MongoDB .NET Driver. Nothing beats learning the basics! 

Visit [dotMongo's web page](http://dotmongo.com/) for more info!