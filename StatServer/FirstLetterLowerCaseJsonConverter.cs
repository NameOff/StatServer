using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace StatServer
{
    public class FirstLetterLowerCaseJsonConverter : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return char.ToLower(propertyName[0]) + propertyName.Substring(1);
        }
    }
}
