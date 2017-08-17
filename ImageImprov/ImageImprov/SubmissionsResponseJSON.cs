using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class SubmissionsResponseJSON {
        [JsonProperty("created_date")]
        public DateTime createdDate { get; set; }

        [JsonProperty("id")]
        public long id { get; set; }

        [JsonProperty("submissions")]
        //public IDictionary<CategoryJSON, IList<PhotoMetaJSON>> submissions { get; set; }
        public List<SubmissionJSON> submissions { get; set; }

    }
}
