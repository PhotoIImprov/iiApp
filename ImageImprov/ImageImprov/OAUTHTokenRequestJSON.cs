using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    public class OAUTHTokenRequestJSON {
        [JsonProperty("method")]
        public string method { get; set; }

        [JsonProperty("token")]
        public string token { get; set; }
    }
}
