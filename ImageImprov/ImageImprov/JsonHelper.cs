using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;  // for debug assertions.

namespace ImageImprov {
    // provides helper functions around JSON.
    static class JsonHelper {
        public static List<string> InvalidJsonElements;

        public static IList<T> DeserializeToList<T>(string jsonString) {
            InvalidJsonElements = null;
            IList<T> objectsList = new List<T>();
            try {
                // this line will crash if json is null.
                var array = JArray.Parse(jsonString);

                foreach (var item in array) {
                    try {
                        // the good
                        objectsList.Add(item.ToObject<T>());
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.ToString());
                        InvalidJsonElements = InvalidJsonElements ?? new List<string>();
                        InvalidJsonElements.Add(item.ToString());
                    }
                }
            } catch (Exception e) {
                // ignore. we received a null list.  users of the fcn will be responsible when nothing comes back.
                Debug.WriteLine(e.ToString());
            }
            return objectsList;
        }
    }
}
