using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    // all the info needed to upload a photo.
    [JsonObject]
    class PhotoSubmitJSON {
        [JsonProperty("image")]
        public Byte[] imgStr { get; set; }

        // should generally be JPG.  I will have to do special code if the user pulls a tiff, png, or bmp from camera roll
        [JsonProperty("extension")]
        public string extension { get; set; }

        [JsonProperty("category_id")]
        public long categoryId { get; set; }

        //[JsonProperty("user_id")]
        //public long userId { get; set; }

    }
}
