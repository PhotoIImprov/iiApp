using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// This is the ballot returned on a photo update.
    /// Extended version of BallotJSON so we can cleanly catch the pid.
    /// </summary>
    [JsonObject]
    public class BallotJSONExtended : BallotJSON {
        [JsonProperty("pid")]
        public long pid;
    }
}
