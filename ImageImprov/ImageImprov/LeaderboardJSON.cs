﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    public class LeaderboardJSON {
        [JsonProperty("image")]
        public Byte[] imgStr { get; set; }

        [JsonProperty("isfriend")]
        public string isFriend { get; set; }

        [JsonProperty("rank")]
        public int rank { get; set; }

        [JsonProperty("score")]
        public int score { get; set; }

        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("you")]
        public string isYou { get; set; }

        // @todo add thumbnail here.
    }
}


