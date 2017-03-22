using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov
{
    // Represents a category users compete on, along with associated meta data.
    [JsonObject]
    class CategoryJSON
    {
        [JsonProperty("id")]
        public long categoryId { get; set; }

        // e.g. Cute Puppies
        [JsonProperty("description")]
        public string description { get; set; }

        [JsonProperty("start")]
        public DateTime start { get; set; }

        [JsonProperty("end")]
        public DateTime end { get; set; }

        // @todo Create a state enum with all the valid states.
        // Current valid states are uploading, voting, closed
        [JsonProperty("state")]
        public string state { get; set; }
    }
}
