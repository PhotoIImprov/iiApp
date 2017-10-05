using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ImageImprov {
    [JsonObject]
    public class BadgesResponseJSON {
        [JsonProperty("HighestRatedPhotoURL")]
        public string highestRatedPhoto;

        public long pid {
            get {
                //preview/
                long _pid = -1;
                if (highestRatedPhoto != null) {
                    string pidStr = highestRatedPhoto.Substring(8);
                    long.TryParse(pidStr, out _pid);
                }
                return _pid;
            }
        }

        [JsonProperty("mostBulbsInADay")]
        public int maxDailyBulbs { get; set; }

        [JsonProperty("tags")]
        public IList<string> tags { get; set; }

        [JsonProperty("totalLightbulbs")]
        public int totalBulbs { get; set; }

        [JsonProperty("unspentBulbs")]
        public int unspentBulbs { get; set; }

        [JsonProperty("firstphoto")]
        public bool firstphoto { get; set; }

        [JsonProperty("upload100")]
        public bool upload100 { get; set; }

        [JsonProperty("upload30")]
        public bool upload30 { get; set; }

        [JsonProperty("upload7")]
        public bool upload7 { get; set; }

        [JsonProperty("vote100")]
        public bool vote100 { get; set; }

        [JsonProperty("vote30")]
        public bool vote30 { get; set; }
    }
}
