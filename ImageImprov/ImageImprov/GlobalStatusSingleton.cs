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

        // set to true on a successful login.
        // deactivated on a logout.
        public static bool loggedIn = false;

        public static LoginResponseJSON loginCredentials;
        // The category id currently open for voting.
        // A -1 indicates the category id has not been received from the server yet.
        public static long votingCategoryId = NO_CATEGORY_INFO;

        public static AuthenticationToken authToken;

        //public static string activeURL = "https://imageimprov.com/";
        public static string activeURL = "http://104.198.176.198:8080/";

        // returns true if we are in vertical mode, or false for landscape.
        public static bool IsPortrait(Page p) { return p.Width < p.Height; }
    }
}

