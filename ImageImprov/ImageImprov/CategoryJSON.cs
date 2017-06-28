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
    public class CategoryJSON
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

        public override bool Equals(System.Object obj) {
            if (obj==null) { return false; }
            CategoryJSON y = obj as CategoryJSON;
            if ((System.Object)y == null) {
                // failed to cast as a CategoryJSON. --> Not equal!
                return false;
            }
            // description is a string, and therefore could be null!!
            if ((description==null) || (y.description==null)) {
                return ((description == null) && (y.description == null));
            }
            return description.Equals(y.description);
        }

        public bool Equals(CategoryJSON y) {
            if ((object)y == null) { return false; }
            // description is a string, and therefore could be null!!
            // Note: Will return true if description null in both instances.
            if ((description == null) || (y.description == null)) {
                return ((description == null) && (y.description == null));
            }
            return description.Equals(y);
        }
        public override int GetHashCode() {
            int res = 0;
            if (description != null) {
                res = description.GetHashCode();
            }
            return res;
        }
    }
}
