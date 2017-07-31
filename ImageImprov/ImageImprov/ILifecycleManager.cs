using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// There's several functions I expose to make lifecycle management easier.
    /// These are really about data access through a layer of objects.
    /// Technically, I don't need this.  It's just nice to have this so I'm aware of them.
    /// Actually, having just updated the code, this reduces maintenance/update load and increases code clarity. Score!
    /// </summary>
    interface ILifecycleManager {
        BallotJSON GetActiveBallot();
        Queue<string> GetBallotQueue();
        IDictionary<CategoryJSON, IList<LeaderboardJSON>> GetLeaderboardList();
        IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps();

        void FireLoadChallengeName();
    }
}
