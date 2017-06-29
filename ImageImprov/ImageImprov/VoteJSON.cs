using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class VoteJSON {
        /// <summary>
        /// The bid id that was passed with the original ballot.
        /// </summary>
        [JsonProperty("bid")]
        public long bid { get; set; }

        /// <summary>
        /// The position ranking of this bid.  Typically 1 through 4.
        /// </summary>
        [JsonProperty("vote")]
        public int vote{ get; set; }

        [JsonProperty("like")]
        public string like { get; set; }

        [JsonProperty("offensive")]
        public string offensive { get; set; }
    }
}
