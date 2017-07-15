using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// This class handles all the data I need to maintain state on between
    /// sessions in order to be able to do OAuth.
    /// </summary>
    [JsonObject]
    public class OAUTHDataJSON {
        [JsonProperty("access_token")]
        public string accessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string refreshToken { get; set; }

        /// <summary>
        /// Right now just Facebook or Google
        /// </summary>
        [JsonProperty("method")]
        public string method { get; set; }

        [JsonProperty("expiration")]
        public DateTime expiration { get; set; }

    }
}
