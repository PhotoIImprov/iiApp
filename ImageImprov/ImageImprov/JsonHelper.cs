using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ImageImprov {
    // provides helper functions around JSON.
    static class JsonHelper {
        public static List<string> InvalidJsonElements;

        public static IList<T> DeserializeToList<T>(string jsonString) {
            InvalidJsonElements = null;
            var array = JArray.Parse(jsonString);
            IList<T> objectsList = new List<T>();

            foreach (var item in array) {
                try {
                    // the good
                    objectsList.Add(item.ToObject<T>());
                } catch (Exception ex) {
                    InvalidJsonElements = InvalidJsonElements ?? new List<string>();
                    InvalidJsonElements.Add(item.ToString());
                }
            }
            return objectsList;
        }
    }
}
