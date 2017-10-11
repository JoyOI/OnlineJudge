using System.Collections.Generic;
using Newtonsoft.Json;

namespace System
{
    public static class ObjectToDictionaryExtension
    {
        public static Dictionary<string, object> InternalToDictionary(this object self)
        {
            var json = JsonConvert.SerializeObject(self);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
    }
}
