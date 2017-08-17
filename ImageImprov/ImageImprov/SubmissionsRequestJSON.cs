using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class SubmissionsRequestJSON {
        [JsonProperty("dir")]
        public long direction { get; set; }

        [JsonProperty("cid")]
        public long categoryId { get; set; }
    }
}
