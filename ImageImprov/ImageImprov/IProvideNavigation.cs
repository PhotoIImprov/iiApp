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
        void gotoJudgingPageHome();
        void gotoLeaderboardPage();
        void gotoCameraPage();
        void gotoProfilePage();

        //void gotoZoomPage();

        /* These are now part of IProvideProfileNavigation
        void gotoHomePage();  // this is now a pseudonumn for gotoProfilePage();
        void gotoHamburgerPage();  // this is a pseudonymn for gotoProfilePage();
        void gotoInstructionsPage();   // this page no longer exists..., wait what?
        void gotoSettingsPage();
        //void gotoMedalsPage();
        void gotoLikesPage();
        void gotoMySubmissionsPage();
        //void gotoPurchasePage();
        */
    }
}

