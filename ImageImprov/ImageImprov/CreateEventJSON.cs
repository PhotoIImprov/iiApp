using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// Used to create an event.
    /// </summary>
    [JsonObject]
    public class CreateEventJSON {
        [JsonProperty("categories")]
        public IList<string> categories { get; set; }

        [JsonProperty("event_name")]
        public string eventName { get; set; }

        [JsonProperty("games_excluded")]
        public IList<string> gamesExcluded { get; set; }

        // we should have called this max players.
        [JsonProperty("num_players")]
        public int numPlayers { get; set; }

        [JsonProperty("start_time")]
        // going with string rather than DateTime so I dont have to write a custom converter.
        public string startTime { get; set; }

        /// <summary>
        /// Time in minutes
        /// </summary>
        [JsonProperty("upload_duration")]
        public int uploadDuration { get; set; }

        /// <summary>
        /// Time in minutes
        /// </summary>
        [JsonProperty("voting_duration")]
        public int votingDuration { get; set; }
    }
}
