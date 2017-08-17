using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    // all the non-image data info about a photo
    [JsonObject]
    public class PhotoMetaJSON {
        [JsonProperty("likes")]
        public long likes { get; set; }

        [JsonProperty("pid")]
        public long pid { get; set; }

        [JsonProperty("score")]
        public long score { get; set; }

        [JsonProperty("tags")]
        public IList<string> tags { get; set; }

        /// <summary>
        /// This is the stuff AFTER base. So the www is NOT included.
        /// </summary>
        [JsonProperty("url")]
        public string url { get; set; }

        [JsonProperty("votes")]
        public long votes { get; set; }

        /// <summary>
        /// regularly not included.
        /// </summary>
        [JsonProperty("category_id")]
        public long categoryId { get; set; }

        /// <summary>
        /// regularly not included, e.g. for retrieving my own images.
        /// </summary>
        [JsonProperty("photographer")]
        public string photographer { get; set; }
    }
}
