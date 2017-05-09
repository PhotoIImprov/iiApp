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

        static int ORIENTATION_UNKNOWN = -1;
        public static int LANDSCAPE = 0;
        public static int PORTRAIT = 1;
        [JsonProperty("orientation")]
        public int orientation { get; set; }       
        
        public int isPortrait() {
            int res = LANDSCAPE;
            if ((orientation==1) || (orientation==3)) {
                res = PORTRAIT;
            }
            return res;
        }

        public BallotCandidateJSON() {
            this.orientation = ORIENTATION_UNKNOWN;
        } 
    }
}
