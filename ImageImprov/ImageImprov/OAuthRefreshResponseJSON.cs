using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    public class OAuthRefreshResponseJSON {
        [JsonProperty("access_token")]
        public string accessToken = "";

        [JsonProperty("refresh_token")]
        public string refreshToken = "";
    }
}
