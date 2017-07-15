using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using System.Net.Http;
using System.Diagnostics;  // for debug assertions.
using Newtonsoft.Json;
using System.IO;

using Xamarin.Forms;
using ExifLib;

namespace ImageImprov {
    public class LeaderboardPage : ContentView {
        public const int MAX_LEADERBOARDS = 5;
        public const int MAX_IMAGES = 9;

        public readonly static string LOAD_FAILURE = "No open voting category currently available.";
        readonly static string LEADERBOARD = "leaderboard";
        readonly static string CATEGORY = "?category_id=";
        readonly static TimeSpan MIN_TIME_BETWEEN_RELOADS = new TimeSpan(0, 0, 180);

        static object uiLock = new object();

        KeyPageNavigator defaultNavigationButtonsP;

        public event EventHandler RequestLeaderboard;
        EventArgs eDummy = null;

        Grid portraitView = null;

        StackLayout selectBoardButtonsStackP = new StackLayout();

        ScrollView selectBoardScrollP = new ScrollView();

        Label leaderboardLabelP = new Label
        {
            Text = "BEST OF: ",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };

        // tracks what category I'm showing
        //long activeCategory;

        //IList<LeaderboardJSON> leaders;
        // originally categoryJSON, but this means there's no match in ContainsKey when checking dicts.
        IDictionary<CategoryJSON, IList<LeaderboardJSON>> listOfLeaderboards = new Dictionary<CategoryJSON, IList<LeaderboardJSON>>();
        IDictionary<CategoryJSON, DateTime> leaderboardUpdateTimestamps = new Dictionary<CategoryJSON, DateTime>();

        IDictionary<CategoryJSON, IList<Image>> leaderImgsP = null;
        StackLayout leaderStackP = new StackLayout()
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2.0,
        };

        ScrollView leadersScrollP = new ScrollView();

        //
        //   BEGIN Variables related/needed for images to place background on screen.
        //
        AbsoluteLayout layoutP;  // this lets us place a background image on the screen.
        Assembly assembly = null;
        Image backgroundImgP = null;
        string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place background on screen.
        // 

        public LeaderboardPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;
            // This class is hooked up to JudgingContentPage to tell me when the categories for leaderboards are available.
            // That happens in MainPageSwipeUI.
            leaderImgsP = new Dictionary<CategoryJSON, IList<Image>>();

            // and to listen for leaderboard requests; done as event so easy to process async.
            this.RequestLeaderboard += new EventHandler(OnRequestLeaderboard);

            // fire a loadChallengeNameEvent.
            eDummy = new EventArgs();

            leaderStackP.SizeChanged += redrawImages;
            // place holder.
            //Content = leaderboardLabelP;
            managePersistedLeaderboard();
            buildUI();
        }

        /// <summary>
        /// Helper for Button.OnClicked that makes the decision whether to request an update from the server
        /// for this leaderboard.
        /// Also a Helper for managePersisedLeaderboard to determine on startup if updates are needed...
        /// No longer have button.clicked.  This is now tested against whenever the user enters the leaderboard page
        /// and fires the reload for any category that is still active and has last reload > MIN_TIME_BETWEEN_RELOADS away
        /// If updating, triggers a leaderboardRequest.
        /// </summary>
        private void reloadAnalysis(CategoryJSON category) {
            DateTime timeOfRequest = DateTime.Now;
            
            if (leaderboardUpdateTimestamps.ContainsKey(category)) {
                if ((timeOfRequest - leaderboardUpdateTimestamps[category]) > MIN_TIME_BETWEEN_RELOADS) {
                    // before firing request, check that this leaderboard is not already in the closed state.
                    if ((category.end > timeOfRequest) && (leaderboardUpdateTimestamps[category]<category.end)) {
                        // ending is still in the future and last load was before the category ended.
                        // hmm. if I"m called by managePersistedLeaderboard, this can occur before I have an authToken...
                        // so... check for auth token. if I don't have one, the OnCategoryLoad process catches calls here.
                        if (GlobalStatusSingleton.authToken != null) {
                            RequestLeaderboardEventArgs evtArgs = new RequestLeaderboardEventArgs { Category = category, };
                            if (RequestLeaderboard != null) {
                                RequestLeaderboard(this, evtArgs);
                            }
                        }
                    }
#if DEBUG
                    else {
                        Debug.WriteLine("DHB:LeaderboardPage:reloadAnalysis Hit the closed category no reload case");
                    }
#endif
                }
            } else {
                // no timestamp info. fire a load.  Request takes care of setting the timestamp.
                RequestLeaderboardEventArgs evtArgs = new RequestLeaderboardEventArgs { Category = category, };
                if (RequestLeaderboard != null) {
                    RequestLeaderboard(this, evtArgs);
                }
            }
        }

        // cant update the ui objects in a separate thread.
        private void drawLeaderImages() {
            foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> category in listOfLeaderboards) {
                drawLeaderImages(category.Key);
            }
        }

        private void drawLeaderImages(CategoryJSON category) {
            var t = Task.Run(() =>
            {
                leaderImgsP[category] = buildLeaderboardImagesForCategory(category);
            });
            t.Wait();
        }

        private IList<Image> buildLeaderboardImagesForCategory(CategoryJSON category) {
            Debug.WriteLine("DHB:LeaderboardPage:buildLeaderboardImagesForCategory begin");
            IList<Image> images = new List<Image>();
            if ((listOfLeaderboards != null) && (listOfLeaderboards.Count > 0) && listOfLeaderboards.ContainsKey(category)) {
                foreach (LeaderboardJSON leader in listOfLeaderboards[category]) {
                    Image img = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(leader.imgStr);
                    if (img != null) {
                        images.Add(img);
                    }
                }
            }
            Debug.WriteLine("DHB:LeaderboardPage:buildLeaderboardImagesForCategory end");
            return images;
        }


        //private void drawLeaderImagesActual(ref IList<Image> pImgs, ref IList<Image> lImgs) {
        private void drawLeaderImagesActual(ref IList<Image> pImgs, CategoryJSON category) {
            Debug.WriteLine("DHB:LeaderboardPage:drawLeaderImagesActual begin");

            if ((listOfLeaderboards != null) && (listOfLeaderboards.Count > 0) && listOfLeaderboards.ContainsKey(category)) {
                foreach (LeaderboardJSON leader in listOfLeaderboards[category]) {
                    IList<Image> images = GlobalSingletonHelpers.buildTwoFixedRotationImageFromBytes(leader.imgStr, (ExifOrientation)leader.orientation);
                    pImgs.Add(images[0]);
                    //lImgs.Add(images[1]);
                }
            }
            Debug.WriteLine("DHB:LeaderboardPage:drawLeaderImagesActual end");
        }

        /// <summary>
        /// Removes by oldest leaderboard by enddate, not last load timestamp.
        /// </summary>
        private void removeOldestLeaderboard() {
            if (listOfLeaderboards.Count > 0) {
                CategoryJSON oldestKey = listOfLeaderboards.ElementAt(0).Key;
                foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> leaderboard in listOfLeaderboards) {
                    if (leaderboard.Key.end < oldestKey.end) {
                        oldestKey = leaderboard.Key;
                    }
                }
                listOfLeaderboards.Remove(oldestKey);
            }
        }

        public void managePersistedLeaderboard() {
            // timestamps and leaderboards are separated in the data.
            // load timestamps first as it has no draw function to suck down time...
            Debug.WriteLine("DHB:LeaderboardPage:managePersistedLeaderboard timestamps:");
            if (GlobalStatusSingleton.persistedLeaderboardTimestamps.Count > 0) {
                foreach (KeyValuePair<CategoryJSON, DateTime> stamp in GlobalStatusSingleton.persistedLeaderboardTimestamps) {
                    leaderboardUpdateTimestamps[stamp.Key] = stamp.Value;
                    Debug.WriteLine("   " + stamp.Value);
                }
            }
            // move every leaderboard into listOfLeaderboards
            // create selection buttons for every leaderboard
            // create images for the element at dict[0]
            if (GlobalStatusSingleton.persistedLeaderboards.Count > 0) {
                // clear everything in case this was a sleep...
                listOfLeaderboards.Clear();
                foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> leaderboard in GlobalStatusSingleton.persistedLeaderboards) {
                    // only load if there are images in the leaderboard!
                    if (leaderboard.Value.Count > 0) {
                        listOfLeaderboards.Add(leaderboard);
                    }
                }

                while (listOfLeaderboards.Count > MAX_LEADERBOARDS) {
                    removeOldestLeaderboard();
                }
                GlobalStatusSingleton.persistedLeaderboards = null;  // should not need this anymore.

                foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> board in listOfLeaderboards) {
                    // check for reloads across the board
                    reloadAnalysis(board.Key);
                }
                // this is the first time through, so none of this is ready.
                drawLeaderImages();
                buildUI();
            }
        }

        private bool ListOfLeaderboardsContains(long categoryId) {
            bool found = false;
            foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> kvp in listOfLeaderboards) {
                if (kvp.Key.categoryId == categoryId) {
                    found = true;
                    break;
                }
            }
            return found;
        }

        private void CheckCategoryListLoaded(IList<CategoryJSON> categoryList) {
            foreach (CategoryJSON category in categoryList) {
                if (ListOfLeaderboardsContains(category.categoryId) == false) {
                    // new category. load it!
                    RequestLeaderboardEventArgs evtArgs = new RequestLeaderboardEventArgs { Category = category, };
                    if (RequestLeaderboard != null) {
                        RequestLeaderboard(this, evtArgs);
                    }
                } else {
                    // check to see if I should reload this category anyway.
                    reloadAnalysis(category);
                }
            }
        }

        public virtual void OnCategoryLoad(object sender, EventArgs e) {
            //categoryLabel.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
            // check for categories I don't already have the leaderboard for, then send load requests.
            CheckCategoryListLoaded(GlobalStatusSingleton.votingCategories);
            CheckCategoryListLoaded(GlobalStatusSingleton.closedCategories);
        }

        DateTime startTime;
        DateTime buildUIstartTime;

        public int buildUI() {
            /*
            buildUIstartTime = DateTime.Now;
            int res = 0;
            int res2 = 0;
            Device.BeginInvokeOnMainThread(() => {
                DateTime timeForThread = DateTime.Now;
                res = buildPortraitView();
                Debug.WriteLine("DHB:LeaderboardPage:buildUI portrait time:" + (DateTime.Now - buildUIstartTime));
                res2 = buildLandscapeView();
                // why OnSizeAllocated?
                //OnSizeAllocated(Width, Height);
                Debug.WriteLine("DHB:LeaderboardPage:buildUI thread catch time:" + (timeForThread - buildUIstartTime));
            });
            Debug.WriteLine("DHB:LeaderboardPage:buildUI total elapsedTime:" + (DateTime.Now - buildUIstartTime));
            return ((res < res2) ? res : res2);
            */
            int res = 0;
            Device.BeginInvokeOnMainThread(() =>
            {
                DateTime timeForThread = DateTime.Now;
                res = buildPortraitView();
                if (res == 1) {
                    Content = portraitView;
                }
            });
            return res;
        }

        public int buildPortraitView() {
            int result = 1;
            // all my elements are already members...
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                for (int i = 0; i < 20; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
            }
            if (defaultNavigationButtonsP == null) {
                defaultNavigationButtonsP = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            }
            // no button scroll
            leaderStackP.Children.Clear();

            if (listOfLeaderboards.Count != leaderImgsP.Count) {
                drawLeaderImages();   // will want a fast version that just loads the missings...
            }
            bool notFirstPass = false;
            foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> kvpCatBoard in listOfLeaderboards) {
                Label catLabel = new Label {
                    Text = kvpCatBoard.Key.description,
                    BackgroundColor = GlobalStatusSingleton.ButtonColor,
                    TextColor = Color.White,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    WidthRequest = Width,
                    MinimumHeightRequest = Height/15.0,
                };
                if ((Width>-1)&&(Height>-1)) {
                    GlobalSingletonHelpers.fixLabelHeight(catLabel, Width, Height / 5.0, 30);
                    //Debug.WriteLine("DHB:LeaderboardPage:buildPortraitView labelHeight=" + catLabel.Height);
                } else {
                    catLabel.FontSize = 30;
                }
                if (notFirstPass) {
                    leaderStackP.Children.Add(new Label { Text = "", BackgroundColor = GlobalStatusSingleton.backgroundColor, });
                }
                leaderStackP.Children.Add(catLabel);
                if ((Width > -1) && (Height > -1)) {
                    // get nothing without out a positive W!
                    build3Across(kvpCatBoard.Key, MAX_IMAGES);
                }
                notFirstPass = true;
            }
            leadersScrollP.Content = leaderStackP;

            portraitView.Children.Add(leadersScrollP, 0, 2);
            Grid.SetRowSpan(leadersScrollP, 16);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);

            Debug.WriteLine("DHB:LeaderboardPage:buildPortraitView final mem status:" + PlatformSpecificCalls.GetMemoryStatus());
            return result;
        }

        /// <summary>
        /// Builds the images to display
        /// </summary>
        /// <param name="numImages">-1 signifies build all images returned, o/w build the number passed in.</param>
        private void build3Across(CategoryJSON category, int numImages = -1) {
            int numToDraw = listOfLeaderboards[category].Count;
            if ((numImages > -1) && (numImages < listOfLeaderboards[category].Count)) {
                numToDraw = numImages;
            }
            for (int j = 0; j < numToDraw; j += 3) {
                // does not contrain to screen width.
                // of course not.  It's a STACK!!  Solution: set the suggested width of the images!
                StackLayout leaderRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,

                };
                leaderImgsP[category][j].WidthRequest = (Width / 3.01);
                leaderRow.Children.Add(leaderImgsP[category][j]);
                if ((j + 1) < listOfLeaderboards[category].Count) {
                    leaderImgsP[category][j + 1].WidthRequest = (Width / 3.01);
                    leaderRow.Children.Add(leaderImgsP[category][j + 1]);

                    if ((j + 2) < listOfLeaderboards[category].Count) {
                        leaderImgsP[category][j + 2].WidthRequest = (Width / 3.01);
                        leaderRow.Children.Add(leaderImgsP[category][j + 2]);
                    }
                }
                leaderStackP.Children.Add(leaderRow);
            }
        }

        double lastDrawnWidth = -1.0;
        protected virtual void redrawImages(object sender, EventArgs e) {
            // only need this to trigger once.
            if (Width != lastDrawnWidth) {
                lastDrawnWidth = Width;
                buildUI();
            }
        }

        protected async virtual void OnRequestLeaderboard(object sender, EventArgs e) {
            // the current category command does not send back a category in leaderboard state.
            // for now, the system is only going to support Yesterday's and Today's leaderboard.

            Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard start");
            long loadCategory = ((RequestLeaderboardEventArgs)e).Category.categoryId;
            string categoryName = ((RequestLeaderboardEventArgs)e).Category.description;

            string result = await requestLeaderboardAsync(loadCategory);

            if (result.Equals(LOAD_FAILURE)) {
                leaderboardLabelP.Text = "Connection failed. Please check connection";
                while (result.Equals(LOAD_FAILURE)) {
                    await Task.Delay(3000);
                    Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard sending re-request.");
                    result = await requestLeaderboardAsync(loadCategory);
                }
            }
            // fix labels, connection is back...
            leaderboardLabelP.Text = "BEST OF: ";

            // process successful leaderboard result string
            IList<LeaderboardJSON> newLeaderBoard = JsonHelper.DeserializeToList<LeaderboardJSON>(result);

            // may have been added already. this approach throws an exception if key exists
            //listOfLeaderboards.Add(((RequestLeaderboardEventArgs)e).Category, newLeaderBoard);
            // this one resets the value:
            listOfLeaderboards[(((RequestLeaderboardEventArgs)e).Category)] = newLeaderBoard;

            leaderboardUpdateTimestamps[((RequestLeaderboardEventArgs)e).Category] = DateTime.Now;
            drawLeaderImages(((RequestLeaderboardEventArgs)e).Category);
            // I do need to update the ui now...
            buildUI();
            Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard UI Change");
#if DEBUG
            // will be null if everything went ok.
            if (JsonHelper.InvalidJsonElements != null) {
                // ignore this error. do we have a debug compiler directive??
                throw new Exception("Invalid leaderboard parse from server in Leaderboard get.");
            }
#endif // DEBUG

            Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard end");
        }
        


        static async Task<string> requestLeaderboardAsync(long category_id) {
            Debug.WriteLine("DHB:LeaderboardPage:requestLeaderboardAsync start");
            string result = LOAD_FAILURE;

            try {
                HttpClient client = new HttpClient();
                string categoryURL = GlobalStatusSingleton.activeURL + LEADERBOARD + CATEGORY + System.Convert.ToString(category_id);
                HttpRequestMessage leaderboardRequest = new HttpRequestMessage(HttpMethod.Get, categoryURL);
                leaderboardRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage catResult = await client.SendAsync(leaderboardRequest);
                if (catResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = await catResult.Content.ReadAsStringAsync();
                } else {
                    // no ok back from the server! gahh.
                    Debug.WriteLine("DHB:LeaderboardPage:requestLeaderboardAsync invalid result code: " + catResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine("DHB:LeaderboardPage:requestLeaderboardAsync:WebException");
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:LeaderboardPage:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:LeaderboardPage:requestLeaderboardAsync end");
            return result;
        }

        public IDictionary<CategoryJSON, IList<LeaderboardJSON>> GetLeaderboardList() {
            return listOfLeaderboards;
        }
        public IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps() {
            return leaderboardUpdateTimestamps;
        }

    }
}
