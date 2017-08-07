using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    public class URLRequestJSON {
        [JsonProperty("baseurl")]
        public string baseurl;
    }
}
