using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// Passed when a leaderboard load request is sent.
    /// </summary>
    class RequestLeaderboardEventArgs : EventArgs {
        //public long CategoryId { get; set; }
        //public string CategoryName { get; set; }
        public CategoryJSON Category { get; set; }
    }
}
