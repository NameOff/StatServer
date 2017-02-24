using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StatServer
{
    public class Serializable
    {
        private readonly SerializerContractResolver serializer;

        public static JsonSerializerSettings Settings =
            new JsonSerializerSettings { ContractResolver = new FirstLetterLowerCaseJsonConverter() };

        public Serializable()
        {
            serializer = new SerializerContractResolver();
        }

        protected string Serialize<T>(T obj, string[] fields)
        {
            serializer.Allow(fields);
            var jsonSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = serializer };
            var json = JsonConvert.SerializeObject(obj, jsonSettings);
            serializer.IgnoreAll();
            return json;
        }
    }
}
