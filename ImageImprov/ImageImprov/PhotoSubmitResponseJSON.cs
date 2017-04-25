using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    // The JSON response from the server on successful photo upload.
    [JsonObject]
    class PhotoSubmitResponseJSON {
        public static readonly string SUCCESS_MSG = "photo uploaded";

        //[JsonProperty("message")]
        [JsonProperty("msg")]
        public string message { get; set; }

        [JsonProperty("filename")]
        public string filename { get; set; }

    }
}
