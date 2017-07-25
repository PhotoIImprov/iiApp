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
        public static string imgPath = "";
        public const string IMAGE_NAME_PREFIX = "ImageImprov_";

        public static Color backgroundColor = Color.FromRgb(242, 242, 242);
        public static Color highlightColor = Color.FromRgb(100, 100, 100);
        public static Color ButtonColor = Color.FromRgb(252, 213, 21);
        public static Color ActiveButtonColor = Color.FromRgb(252, 21, 21);
        /////////////////////////////////// 
        /// END PREFERENCES SECTION
        /// END PREFERENCES SECTION
        /////////////////////////////////// 

        // holdover from when we used "login"
        //public static LoginResponseJSON loginCredentials;

        /// <summary>
        /// Checked on wakeup to see if categories should be reloaded.
        /// </summary>
        public static DateTime lastCategoryLoadTime;

        // The category id currently open for voting.
        // A -1 indicates the category id has not been received from the server yet.
        //public static long votingCategoryId = NO_CATEGORY_INFO;
        //public static string votingCategoryDescription = "";
        public static IList<CategoryJSON> votingCategories = new List<CategoryJSON>();

        // The category id currently open for uploading.
        // A -1 indicates the category id has not been received from the server yet.
        //public static long uploadingCategoryId = NO_CATEGORY_INFO;
        //public static string uploadCategoryDescription = "";
        // @see getUploadingCategoryDesc
        public static IList<CategoryJSON> uploadingCategories = new List<CategoryJSON>();
        /// <summary>
        /// Used for access from things like the KeyPageNavigator.
        /// </summary>
        public static CategoryLoadSuccessEventHandler ptrToJudgingPageLoadCategory = null;
    
        //public static long mostRecentClosedCategoryId = NO_CATEGORY_INFO;
        //public static string mostRecentClosedCategoryDescription = "";
        public static IList<CategoryJSON> closedCategories = new List<CategoryJSON>();

        /// <summary>
        /// Used for setting notifications.
        /// </summary>
        public static IList<CategoryJSON> pendingCategories = new List<CategoryJSON>();

        // Key is CategoryJson, value is the associated leaderboard.
        public static IDictionary<CategoryJSON, IList<LeaderboardJSON>> persistedLeaderboards = new Dictionary<CategoryJSON, IList<LeaderboardJSON>>();
        public static IDictionary<CategoryJSON, DateTime> persistedLeaderboardTimestamps = new Dictionary<CategoryJSON, DateTime>();

        public static AuthenticationToken authToken;

        public static string persistedBallotAsString;
        public static Queue<string> persistedPreloadedBallots;
        // static ip completely off right now.
//#if DEBUG
//        public static string activeURL = "http://35.190.162.96:8080/";
//#else
        public static string activeURL = "https://api.imageimprov.com/";
        //#endif
        public static string TERMS_OF_SERVICE_URL = "https://www.imageimprov.com/en-US/landing/terms-of-service.html";
        public static string PRIVACY_POLICY_URL = "https://www.imageimprov.com/en-US/landing/privacy-policy.html";

        // returns true if we are in vertical mode, or false for landscape.
        public static bool IsPortrait(Page p) { return p.Width < p.Height; }
        // single place to track if we are in portrait or landscape.
        public static bool inPortraitMode;

        // Pct of screen covered by pattern. (remainder reserved for ui stuff)
        public const double PATTERN_PCT = 0.9;
        public const double PATTERN_FULL_COVERAGE = 1.0;

        /// <summary>
        /// Default minimum font size when dynamically scaling the font.
        /// </summary>
        public const int MIN_FONT_SIZE = 10;
        /// <summary>
        /// Default maximum font size when dynamically scaling the font.
        /// </summary>
        public const int MAX_FONT_SIZE = 100;

        // will be set to false by android or iOS if the device has no camera.
        // need to check this in cameraContentPage still. (is that created before or after ios/android contexts?)
        public static bool hasCamera = true;

        /// <summary>
        /// Set to true on play anon click or register click from the starting page.
        /// </summary>
        public static bool firstTimePlaying = false;
    }
}

