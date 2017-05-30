using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    public class BallotCandidateJSON {

        // the bid id
        [JsonProperty("bid")]
        public long bidId { get; set; }

        // a byte representation of a jpg
        [JsonProperty("image")]
        public Byte[] imgStr { get; set; }

        // this is exif orientation, not portrait/landscape
        [JsonProperty("orientation")]
        public int orientation { get; set; }

        static int ORIENTATION_UNKNOWN = -1;
        public static int LANDSCAPE = 0;
        public static int PORTRAIT = 1;
        public int isPortrait = ORIENTATION_UNKNOWN;

        public BallotCandidateJSON() {
            this.orientation = ORIENTATION_UNKNOWN;
        } 
    }
}
