using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// all the non-image data info about a photo that we share with users.
    /// @see PhotoUpdateJSON for the meta data a user can share with us about a photo.
    /// </summary>
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

        //
        //
        // END SUBMISSIONS SECTION. Supplements below
        //
        //

        /// <summary>
        /// regularly not included.
        /// </summary>
        //[JsonProperty("category_id")]
        //public long categoryId { get; set; }

        /// <summary>
        /// Not included in submissions. Used by likes.
        /// </summary>
        [JsonProperty("username")]
        public string user { get; set; }

        /// <summary>
        /// Not included in submissions. Used by likes.
        /// </summary>
        [JsonProperty("isfriend")]
        public bool isFriend { get; set; }
    }
}
