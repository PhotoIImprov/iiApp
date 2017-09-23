using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class LikesJSON : IComparable<LikesJSON> {
        [JsonProperty("category")]
        public CategoryJSON category { get; set; }

        [JsonProperty("photos")]
        public IList<PhotoMetaJSON> photos { get; set; }

        public int CompareTo(LikesJSON b) {
            return category.CompareTo(b.category);
        }
    }
}