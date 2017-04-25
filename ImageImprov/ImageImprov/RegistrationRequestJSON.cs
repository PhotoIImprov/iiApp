using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    class RegistrationRequestJSON : LoginRequestJSON {
        /* now inherited.
        // should be an email address if this is not a guid.
        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("password")]
        public string password { get; set; }
        */

        // will be blank if this is an anonymous registration
        [JsonProperty("guid")]
        public string guid { get; set; }

        // will be blank if this is an anonymous registration
        //[JsonProperty("email")]
        //public string email { get; set; }
    }
}
