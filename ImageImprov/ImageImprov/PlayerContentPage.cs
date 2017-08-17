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
    public class PlayerContentPage : ContentView, ILeaveZoomCallback {
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
        }

        // the zoom callback.
        public void returnToCaller() {
            // This will be a problem that I have to refactor out when we get to PlayerProfile Page.
            // Right now, this is the only page that has a zoom in the center console, so skirting by.
            Content = CenterConsole.MySubmissionsPage;
        }
    }  // class

}  // namespace
