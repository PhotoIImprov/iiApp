using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class PhotosResponseJSON {
        [JsonProperty()]
        public List<PhotoMetaJSON> photos { get; set; }  // IList is not sortable... List is.
    }
}
