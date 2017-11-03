using System;
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

        [JsonProperty("likes")]
        public long likes { get; set; }

        // this is exif orientation, not portrait/landscape
        [JsonProperty("orientation")]
        public int orientation { get; set; }

        [JsonProperty("pid")]
        public long pid { get; set; }

        [JsonProperty("rank")]
        public long rank { get; set; }

        [JsonProperty("score")]
        public long score { get; set; }

        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("votes")]
        public long votes { get; set; }

        [JsonProperty("you")]
        public string isYou { get; set; }

        public static PhotoMetaJSON Convert(LeaderboardJSON orig) {
            if (orig == null) { return null; }
            return new PhotoMetaJSON {
                likes = orig.likes,
                pid = orig.pid,
                score = orig.score,
                //tags,
                //url,
                votes = orig.votes,
                user = orig.username,
                //isFriend,
                //active,
                offensive = false,
            };
        }
        // unlike ballot, I don't need portrait vs landscape orientation info as it doesn't impact layout
        // @todo add thumbnail here.
    }
}


