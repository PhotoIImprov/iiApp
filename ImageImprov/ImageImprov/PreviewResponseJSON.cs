using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageImprov {
    class PreviewResponseJSON {
        // a byte representation of a jpg
        [JsonProperty("image")]
        public Byte[] imgStr { get; set; }
    }
}
