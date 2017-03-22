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
        // the bid id
        [JsonProperty("bid")]
        public long bidId { get; set; }

        // a byte representation of a jpg
        [JsonProperty("image")]
        public Byte[] imgStr { get; set; }
    }
}
