using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov
{
    // captures the response from the server.
    // not converting to an image (yet)
    [JsonObject]
    public class BallotJSON
    {
        // the category we're now voting on
        [JsonProperty("category")]
        public CategoryJSON category;

        [JsonProperty("ballots")]
        public List<BallotCandidateJSON> ballots { get; set; }

        public void Clear() {
            // tbd. Do I need to clear category as well?
            if (ballots != null) {
                ballots.Clear();
            }
        }
    }
}
