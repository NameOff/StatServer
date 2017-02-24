using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StatServer
{
    public class SerializerContractResolver : DefaultContractResolver
    {
        protected readonly HashSet<string> Allows;

        public SerializerContractResolver()
        {
            Allows = new HashSet<string>();
        }
        
        public void Allow(params string[] propertyName)
        {
            foreach (var prop in propertyName)
                Allows.Add(prop);
        }

        public bool IsAllow(string propertyName) => Allows.Contains(propertyName);

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = instance => IsAllow(property.PropertyName);
            return property;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            return char.ToLower(propertyName[0]) + propertyName.Substring(1);
        }

        public void IgnoreAll()
        {
            Allows.Clear();
        }
    }
}
