using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov
{
    // Stores the login response data so I can go get a token.
    // Done as a class rather than a variable for easy JSON deserialization
    [JsonObject]
    public class LoginResponseJSON
    {
        [JsonProperty("user_id")]
        public string userId { get; set; }

        // The uploading category_id is the id for the competition currently open for people to 
        // submit entries (ie, upload).
        // The voting category is not returned in the login info, so it's not included here.
        // @see GlobalStatusSingleton for that info.
        [JsonProperty("uploading_category_id")]
        public string uploadingCategoryId { get; set; }
    }
}
