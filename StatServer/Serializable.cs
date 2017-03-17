using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StatServer
{
    public class Serializable
    {
        private readonly SerializerContractResolver serializer;

        public static JsonSerializerSettings Settings =
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Error};

        public Serializable()
        {
            serializer = new SerializerContractResolver();
            
        }

        protected string Serialize<T>(T obj, string[] fields)
        {
            serializer.Allow(fields);
            var jsonSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = serializer, Formatting = Formatting.Indented };
            var json = JsonConvert.SerializeObject(obj, jsonSettings);
            serializer.IgnoreAll();
            return json;
        }
    }
}
