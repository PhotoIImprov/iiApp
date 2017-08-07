using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;  // for debug assertions.
using System.Reflection;


using Newtonsoft.Json;
using Org.BouncyCastle;


using Xamarin.Forms;

namespace ImageImprov
{

    /// <summary>
    /// Original login and player data page.
    /// Will become a player profile page eventually.
    /// Currently refactoring out the login stuff.
    /// </summary>
    public class PlayerContentPage : ContentView {

        //KeyPageNavigator defaultNavigationButtons;


        PlayerPageCenterConsole playerPageCenterConsole;
        public PlayerPageCenterConsole CenterConsole {
            get { return playerPageCenterConsole; }
        }


        // I should look into reusing background img across pages...
        const string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        Image backgroundImg = null;

        public PlayerContentPage() {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            playerPageCenterConsole = new PlayerPageCenterConsole(this);
        }

        protected void buildBackground(double verticalExtent = GlobalStatusSingleton.PATTERN_FULL_COVERAGE) {
            if (backgroundImg == null) {
                int w = (int)Width;
                int h = (int)Height;
                // don't switch w or h here.  we are building the correct image for the current w,h setting.
                backgroundImg = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, this.GetType().GetTypeInfo().Assembly, w, h, verticalExtent);
            }
        }

        // resets my ui as it may have been changed to a subpage.
        public void goHome() {
            Debug.Assert(GlobalStatusSingleton.loggedIn, "Not logged and creating a loggedin page");
            if (GlobalStatusSingleton.loggedIn == false) {
                Debug.WriteLine("DHB:PlayerContentPage:goHome fyi - not logged in");
            }

            /*
            if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                // anonymous user
                Content = createAnonLoggedInLayout();
            } else {
                Content = createAutoLoginLayout();
            }
            */

            /* this was live pre refactor.
            if (GlobalStausSingleton.isEmailAddress(GlobalStatusSingleton.username)) {
                Content = createAutoLoginLayout();
            } else {
                // anonymous user
                Content = createAnonLoggedInLayout();
            }
            */
        }



        //< requestLoginAsync
        // @todo bad password/account fail case
        // @todo no network connection fail case
        // I'm deserializing and instantly reserializing an object. consider fixing.


        /* lives and works in teh KeyPageNavigator
        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == gotoVotingImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            } else if (sender == gotoCameraImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
            } // else ignore goHomeImgButton
        }
        */


        /*
        public IDictionary<CategoryJSON, IList<LeaderboardJSON>> GetLeaderboardList() {
            return CenterConsole.LeaderboardPage.GetLeaderboardList();
        }
        public IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps() {
            return CenterConsole.LeaderboardPage.GetLeaderboardTimestamps();
        }
        */

    }  // class

}  // namespace
