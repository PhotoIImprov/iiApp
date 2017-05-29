#define AUTH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov
{
    // build with static members so I can access easily, don't have to worry about 
    // creation timing.
    // also set our compiler directives in this file.
    public static class GlobalStatusSingleton
    {
        public const long NO_CATEGORY_INFO = -1;

        // Android an Apple have different device specific methods for retrieving this.
        // Set from MainActivity.cs ::OnCreate for Android.
        // Set from AppDelegate.cs ::FinishedLaunching for Apple.
        private static string uuid = string.Empty;
        public static string UUID {
            get { return uuid;  }
            set { uuid = GlobalSingletonHelpers.stripHyphens(value);  }
        }

        // set to true on a successful login.
        // deactivated on a logout.
        public static bool loggedIn = false;

        // @todo undo these defaults!
        // username
        //public static string username = "hcollins@gmail.com";
        public static string username = "";
        // user password
        //public static string password = "pa55w0rd";
        public static string password = "";

        /////////////////////////////////// 
        /// BEGIN PREFERENCES SECTION
        /// BEGIN PREFERENCES SECTION
        /////////////////////////////////// 
        // A user preference that says whether or not the user wishes to remain
        // logged in on session end.
        // default to false in the event there is no user data.
        public static bool maintainLogin = false;

        // Right now we constrain to only AspectFit or Fill formats
        // No AspectFill.
        public static Aspect aspectOrFillImgs = Aspect.AspectFit;

        /// <summary>
        /// The number of ballots to load at startup.
        /// Defaults to 3. Increases based on user behavior.
        /// </summary>
        public static int minBallotsToLoad = 3;

        /// <summary>
        /// Regardless of user behavior, we never preload more than 6 ballots.
        /// </summary>
        public const int MAX_BALLOTS_TO_LOAD = 6;

        /// <summary>
        /// Tracks how many images taken on this device for image improv so we don't overwrite previous images.
        /// </summary>
        public static int imgsTakenTracker = 0;
        /////////////////////////////////// 
        /// END PREFERENCES SECTION
        /// END PREFERENCES SECTION
        /////////////////////////////////// 

        // holdover from when we used "login"
        //public static LoginResponseJSON loginCredentials;

        // The category id currently open for voting.
        // A -1 indicates the category id has not been received from the server yet.
        public static long votingCategoryId = NO_CATEGORY_INFO;
        public static string votingCategoryDescription = "";

        // The category id currently open for uploading.
        // A -1 indicates the category id has not been received from the server yet.
        public static long uploadingCategoryId = NO_CATEGORY_INFO;
        public static string uploadCategoryDescription = "";

        public static long mostRecentClosedCategoryId = NO_CATEGORY_INFO;
        public static string mostRecentClosedCategoryDescription = "";

        public static AuthenticationToken authToken;


        public static string persistedBallotAsString;
        public static Queue<string> persistedPreloadedBallots;
        // static ip completely off right now.
        //#if DEBUG
        //        public static string activeURL = "http://104.198.176.198:8080/";
        //#else
        public static string activeURL = "https://api.imageimprov.com/";
//#endif

        // returns true if we are in vertical mode, or false for landscape.
        public static bool IsPortrait(Page p) { return p.Width < p.Height; }
        // single place to track if we are in portrait or landscape.
        public static bool inPortraitMode;

        // Pct of screen covered by pattern. (remainder reserved for ui stuff)
        public const double PATTERN_PCT = 0.9;

        // will be set to false by android or iOS if the device has no camera.
        // need to check this in cameraContentPage still. (is that created before or after ios/android contexts?)
        public static bool hasCamera = true;
    }
}

