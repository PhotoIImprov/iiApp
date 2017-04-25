using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// This interface exposes within the master ui page controls for jumping from one page to another.
    /// </summary>
    public interface IProvideNavigation {
        void gotoJudgingPage();
        // This takes the user to the PlayerContentPage.
        void gotoHomePage();
        void gotoCameraPage();

        //void gotoLeaderboardPage();
        //void gotoPurchasePage();
        //void gotoSettingsPage();
    }
}

