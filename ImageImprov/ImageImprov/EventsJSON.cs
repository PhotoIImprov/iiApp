using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// Poor reuse here. Not sure how to jigger it so that I can leverage the EventJSON object.
    /// </summary>
    [JsonObject]
    public class EventsJSON : IComparable<EventsJSON> {
        [JsonProperty("accesskey")]
        public string accessKey { get; set; }

        [JsonProperty("categories")]
        //public IList<CategoryWithPhotosJSON> categories { get; set; }
        public IList<CategoryJSON> categories { get; set; }

        [JsonProperty("created")]
        public DateTime created { get; set; }

        [JsonProperty("created_by")]
        public string createdBy { get; set; }

        [JsonProperty("id")]
        public long eventId { get; set; }

        [JsonProperty("max_players")]
        public string maxPlayers { get; set; }

        [JsonProperty("name")]
        public string eventName { get; set; }

        // Currently compare on create date.
        public int CompareTo(EventsJSON b) {
            if (this.created > b.created) return 1;
            else if (this.created < b.created) return -1;
            else return 0;
        }
    }
}
