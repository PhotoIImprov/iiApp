using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    public class EventsResponseJSON {
        [JsonProperty("events")]
        public List<EventsJSON> events { get; set; }
    }
}
