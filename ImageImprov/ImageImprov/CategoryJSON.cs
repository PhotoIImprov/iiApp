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
        public readonly static string UNKNOWN = "UNKNOWN";   // i shouldn't recv this one.
        public readonly static string UPLOAD = "UPLOAD";
        public readonly static string VOTING = "VOTING";
        public readonly static string COUNTING = "COUNTING";  // i shouldn't recv this one.
        public readonly static string CLOSED = "CLOSED";
        // @todo confirm all state strings are accounted for.

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
