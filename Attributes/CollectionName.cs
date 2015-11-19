using System;

namespace dotMongo.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct )]
    public sealed class CollectionName : Attribute
    {
        private string name { get; set; }

        public CollectionName(string name)
        {
            this.name = name;
        }

        public string Name { get { return name; } }
    }
}
