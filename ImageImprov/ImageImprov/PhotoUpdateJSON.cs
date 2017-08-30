using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class PhotoUpdateJSON {
        [JsonProperty("flag")]
        public bool flag { get; set; } = false;
        [JsonProperty("like")]
        public bool like { get; set; } = false;

        [JsonProperty("tags")]
        public string tags { get; set; }
    }
}
