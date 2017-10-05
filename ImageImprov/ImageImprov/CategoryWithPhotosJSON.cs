using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    public class CategoryWithPhotosJSON {
        [JsonProperty("category")]
        public CategoryJSON category { get; set; }

        [JsonProperty("photos")]
        public List<PhotoMetaJSON> photos { get; set; }
    }
}
