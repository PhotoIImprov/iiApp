using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class VoteJSON {
        [JsonProperty("bid")]
        public long bid { get; set; }

        [JsonProperty("vote")]
        public int vote{ get; set; }

        [JsonProperty("like")]
        public bool like { get; set; }
    }
}
