using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Controllers;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    class TestModel
    {
        [WebApi(FilterLevel.PutDisabled)]
        public int A { get; set; }

        [WebApi(FilterLevel.PatchDisabled | FilterLevel.GetNeedRoot)]
        public string B { get; set; }

        [WebApi(FilterLevel.GetSingleDisabled | FilterLevel.GetListDisabled)]
        public double C { get; set; }
    }



    class Program
    {
        public static IEnumerable<string> PatchEntity<T>(T entity, string json)
        {
            var type = entity.GetType();
            var properties = type.GetProperties();
            var jsonToObject = JsonConvert.DeserializeObject<T>(json);
            var jsonToDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var ret = new List<string>();
            foreach (var x in jsonToDictionary.Keys)
            {
                var property = properties.SingleOrDefault(y => y.Name.ToLower() == x.ToLower());

                if (property == null)
                    continue;

                var webapiAttribute = property.GetCustomAttribute<WebApiAttribute>();
                if (webapiAttribute != null && webapiAttribute.Level.HasFlag(FilterLevel.PatchDisabled))
                    continue;
                
                property.SetValue(entity, property.GetValue(jsonToObject));
                ret.Add(property.Name);
            }

            return ret;
        }

        static void Main(string[] args)
        {
            var baseController = new BaseController();
            var entity = new TestModel
            {
                A = 7,
                B = "Hello World",
                C = 123.456
            };


            PatchEntity(entity, "{ \"A\": 5, \"B\": \"123\", \"C\": 1.23 }");
            Console.Read();
        }
    }
}
