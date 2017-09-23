using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class LikesResponseJSON  {

        [JsonProperty("likes")]
        public List<LikesJSON> likes { get; set; }  // IList is not sortable... List is.

    }
}