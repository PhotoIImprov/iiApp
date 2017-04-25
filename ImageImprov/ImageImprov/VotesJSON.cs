using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class VotesJSON {
        //[JsonProperty("user_id")]
        //public long userId { get; set; }

        [JsonProperty("votes")]
        public List<VoteJSON> votes { get; set; }
    }
}
