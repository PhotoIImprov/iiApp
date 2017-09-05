using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// Class used to encapsulate event objects returned by the event call.
    /// </summary>
    [JsonObject]
    public class EventJSON {
        // listing in swagger order.

        [JsonProperty("accesskey")]
        public string accessKey { get; set; }

        [JsonProperty("categories")]
        public IList<CategoryJSON> categories { get; set; }

        [JsonProperty("created")]
        public DateTime created{ get; set; }

        [JsonProperty("created_by")]
        public string createdBy { get; set; }

        [JsonProperty("id")]
        public long eventId { get; set; }

        [JsonProperty("max_players")]
        public string maxPlayers { get; set; }

        [JsonProperty("name")]
        public string eventName { get; set; }
    }
}
