using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov
{
    // Stores the authorization token.
    // Done as a class rather than a variable for easy JSON deserialization
    [JsonObject]
    public class AuthenticationToken
    {
        [JsonProperty("access_token")]
        public string accessToken { get; set; }
    }
}
