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
    class LeaderboardPage : ContentView {
        public const int MAX_LEADERBOARDS = 5;

        public readonly static string LOAD_FAILURE = "No open voting category currently available.";
        readonly static string LEADERBOARD = "leaderboard";
        readonly static string CATEGORY = "?category_id=";
        readonly static TimeSpan MIN_TIME_BETWEEN_RELOADS = new TimeSpan(0,0,180);

        static object uiLock = new object();

        KeyPageNavigator defaultNavigationButtonsP;

        public event EventHandler RequestLeaderboard;
        EventArgs eDummy = null;

        Grid portraitView = null;

        StackLayout selectBoardButtonsStackP = new StackLayout();

        ScrollView selectBoardScrollP = new ScrollView();

        Label leaderboardLabelP = new Label {
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

        // hmm... need to be careful is we delete from listOfLeaderboards.
        //int activeLeaderboardIndex;

        // activeLeaderboard MUST be the categoryJSON, or we have issues!
        CategoryJSON activeLeaderboard = null;
        //string activeLeaderboard;

        // First button is P, Second button is L
        //Tuple<Button, Button> activeButton = null;
        Button activeButton = null;
        IDictionary<long, Button> selectLeaderboardDictP = new Dictionary<long, Button>();

        IList<Image> leaderImgsP = null;
        StackLayout leaderStackP = new StackLayout() {
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
            leaderImgsP = new List<Image>();

            // and to listen for leaderboard requests; done as event so easy to process async.
            this.RequestLeaderboard += new EventHandler(OnRequestLeaderboard);

            // fire a loadChallengeNameEvent.
            eDummy = new EventArgs();

            // place holder.
            //Content = leaderboardLabelP;
            managePersistedLeaderboard();
            buildUI();
        }

        //private void createSelectButton(CategoryJSON newCategory, StackLayout container, IDictionary<long, Button> buttonDict, bool isLoading = false) {
        // only have to add to a single container, but have to change both buttons... how does that happen?
        private Button createSelectButton(CategoryJSON newCategory, StackLayout container, IDictionary<long, Button> buttonDict) {
            /* just update this when making call to the server
            string buttonText = newCategory.description;
            if (isLoading) {
                buttonText += " Loading...";
            }
            */
            Button newButton = new Button {
                //Text = buttonText,
                Text = newCategory.description,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            };
            newButton.Clicked += (sender, args) => {
                // when the button is clicked... 
                //    prev activeButton decolors as active category.
                //    this button colors as the active category
                //    this category becomes active. on leaerboard
                // also...
                //   if leaderboard age > 5 mins reload
                //   if < 5 mins, play the clavicle game...
                startTime = DateTime.Now;
                // Leaderboard loading is REALLY expensive.  Only do it if it's changed!
                if (activeLeaderboard.Equals(newCategory.description)) {
                    // no load case!
                } else {
                    if (activeButton != null) {
                        activeButton.BackgroundColor = GlobalStatusSingleton.ButtonColor;
                    }
                    //activeButton = newButton;
                    //currentButton.BackgroundColor = GlobalStatusSingleton.ActiveButtonColor;
                    activeLeaderboard = newCategory; // I'll believe this line when I see it! Wow, it works.
                    activeButton = selectLeaderboardDictP[newCategory.categoryId];
                    activeButton.BackgroundColor = GlobalStatusSingleton.ActiveButtonColor;
                    DateTime preDraw = DateTime.Now;
                    Debug.WriteLine("DHB:LeaderboardPage:AnonButtonClicked time to pre image draw:" + (preDraw - startTime));
                    // insufficient: Task(() => { drawLeaderImages(); });
                    //var t = Task.Run(() => { drawLeaderImages(); });
                    //t.Wait();
                    //Task t = new Task(async () => { drawLeaderImages(); });
                    //await Task.WhenAll(t);
                    drawLeaderImages();
                    Debug.WriteLine("DHB:LeaderboardPage:AnonButtonClicked time to post image draw:" + (DateTime.Now - preDraw));
                    buildUI();
                }
                // if reloading, we'll trigger another drawLeaderImages asynchronously.
                // as reload will take time, it comes below the buildUI call
                // even if this is the currently showing category, we still want it to test for the reload...
                reloadAnalysis(newCategory);

            };
            container.Children.Add(newButton);
            try {
                buttonDict.Add(newCategory.categoryId, newButton);
            } catch (Exception e) {
                Debug.WriteLine(e.ToString());
            }
            return newButton;
        }

        /// <summary>
        /// Helper for Button.OnClicked that makes the decision whether to request an update from the server
        /// for this leaderboard.
        /// Also a Helper for managePersisedLeaderboard to determine on startup if updates are needed...
        /// If updating, triggers a leaderboardRequest.
        /// </summary>
        private void reloadAnalysis(CategoryJSON category) {
            DateTime timeOfRequest = DateTime.Now;
            
            if (leaderboardUpdateTimestamps.ContainsKey(category)) {
                if ((timeOfRequest - leaderboardUpdateTimestamps[category]) > MIN_TIME_BETWEEN_RELOADS) {
                    // before firing request, check that this leaderboard is not already in the closed state.
                    if (category.end > timeOfRequest) {
                        // ending is still in the future.
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
                } else {
                    activeButton.Text = category.description; // + " to soon to reload";
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
            IList<Image> pImgs = new List<Image>();
            //IList<Image> lImgs = new List<Image>();
            var t = Task.Run(() => {
                //drawLeaderImagesActual(ref pImgs, ref lImgs);
                drawLeaderImagesActual(ref pImgs);
            });
            t.Wait();

            Device.BeginInvokeOnMainThread(() =>
            {
                if (leaderImgsP == null) {
                    leaderImgsP = new List<Image>(listOfLeaderboards[activeLeaderboard].Count);
                } else {
                    leaderImgsP.Clear();
                    // need to clear the stack as well or the image is held onto...
                    leaderStackP.Children.Clear();
                }
                /*
                if (leaderImgsL == null) {
                    leaderImgsL = new List<Image>(listOfLeaderboards[activeLeaderboard].Count);
                } else {
                    leaderImgsL.Clear();
                    leaderStackL.Children.Clear();
                }
                */
                foreach (Image i in pImgs) {
                    leaderImgsP.Add(i);
                }
                /*
                foreach (Image j in lImgs) {
                    leaderImgsL.Add(j);
                }
                */
            });
        }

        //private void drawLeaderImagesActual(ref IList<Image> pImgs, ref IList<Image> lImgs) {
        private void drawLeaderImagesActual(ref IList<Image> pImgs) {
            Debug.WriteLine("DHB:LeaderboardPage:drawLeaderImagesActual begin");

            if ((listOfLeaderboards != null) && (listOfLeaderboards.Count > 0) && listOfLeaderboards.ContainsKey(activeLeaderboard)) {
                foreach (LeaderboardJSON leader in listOfLeaderboards[activeLeaderboard]) {
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

        private void drawLeaderImagesOrig() {
            Debug.WriteLine("DHB:LeaderboardPage:drawLeaderImagesActual begin");
            if (leaderImgsP == null) {
                leaderImgsP = new List<Image>(listOfLeaderboards[activeLeaderboard].Count);
            } else {
                leaderImgsP.Clear();
                // need to clear the stack as well or the image is held onto...
                leaderStackP.Children.Clear();
            }
            /*
            if (leaderImgsL == null) {
                leaderImgsL = new List<Image>(listOfLeaderboards[activeLeaderboard.description].Count);
            } else {
                leaderImgsL.Clear();
                leaderStackL.Children.Clear();
            }
            */
            foreach (LeaderboardJSON leader in listOfLeaderboards[activeLeaderboard]) {
                /*
                Image image = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(leader.imgStr, ExifLib.ExifOrientation.TopLeft);
                // did not implement click recognition on the images.
                leaderImgsP.Add(image);

                Image imageL = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(leader.imgStr, ExifLib.ExifOrientation.TopLeft);
                leaderImgsL.Add(imageL);
                */
                IList<Image> images = GlobalSingletonHelpers.buildTwoFixedRotationImageFromBytes(leader.imgStr, (ExifOrientation)leader.orientation);
                leaderImgsP.Add(images[0]);
                //leaderImgsL.Add(images[1]);
            }
            Debug.WriteLine("DHB:LeaderboardPage:drawLeaderImagesActual end");
        }

        public void managePersistedLeaderboard() {
            // timestamps and leaderboards are separated in the data.
            // load timestamps first as it has no draw function to suck down time...
            Debug.WriteLine("DHB:LeaderboardPage:managePersistedLeaderboard timestamps:");
            if (GlobalStatusSingleton.persistedLeaderboardTimestamps.Count>0) {
                foreach (KeyValuePair<CategoryJSON,DateTime> stamp in GlobalStatusSingleton.persistedLeaderboardTimestamps) {
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
                selectBoardButtonsStackP.Children.Clear();
                //selectBoardButtonsStackL.Children.Clear();
                selectLeaderboardDictP.Clear();
                //selectLeaderboardDictL.Clear();
                activeButton = null;
                foreach (KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> leaderboard in GlobalStatusSingleton.persistedLeaderboards) {
                    // only load if there are images in the leaderboard!
                    if (leaderboard.Value.Count > 0) {
                        listOfLeaderboards.Add(leaderboard);
                        Button pButton = createSelectButton(leaderboard.Key, selectBoardButtonsStackP, selectLeaderboardDictP);
                        //Button lButton = createSelectButton(leaderboard.Key, selectBoardButtonsStackL, selectLeaderboardDictL);
                    }
                }

                while (listOfLeaderboards.Count > MAX_LEADERBOARDS) {
                    removeOldestLeaderboard();
                }

                activeLeaderboard = listOfLeaderboards.ElementAt(0).Key;
                if (activeButton == null) {
                    long catId = activeLeaderboard.categoryId;
                    activeButton = selectLeaderboardDictP[catId];
                    activeButton.BackgroundColor = GlobalStatusSingleton.ActiveButtonColor;
                }
                foreach(KeyValuePair<CategoryJSON, IList<LeaderboardJSON>> board in listOfLeaderboards) {
                    reloadAnalysis(board.Key);
                }
                drawLeaderImages();
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
                    if (activeLeaderboard == null) {
                        // nothing in memory. make this one the active.
                        activeLeaderboard = category;
                    }
                    // category not found. 
                    // create a button for it!
                    // then load it up!
                    Button pButton = createSelectButton(category, selectBoardButtonsStackP, selectLeaderboardDictP);
                    if (activeButton == null) {
                        activeButton = pButton;
                    }

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

        double widthCheck = 0;
        double heightCheck = 0;

        /*
        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height);
            if ((widthCheck != width) || (heightCheck != height)) {
                widthCheck = width;
                heightCheck = height;

                if ((Width > Height) && (landscapeView != null)) {
                    //GlobalStatusSingleton.inPortraitMode = false;
                    if (backgroundImgL == null) {
                        backgroundImgL = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)width, (int)height,
                            GlobalStatusSingleton.PATTERN_FULL_COVERAGE, GlobalStatusSingleton.PATTERN_PCT);
                        layoutL = new AbsoluteLayout
                        {
                            Children = {
                                    { backgroundImgL, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All },
                                    { landscapeView, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All }
                                }
                        };
                    }
                    if (layoutL != null) {
                        Content = layoutL;
                    } else if (landscapeView != null) {
                        Content = landscapeView;
                    }
                } else {
                    //GlobalStatusSingleton.inPortraitMode = true;
                    if ((backgroundImgP == null) && (width > 0) && (portraitView != null)) {
                        backgroundImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)width, (int)height);
                        layoutP = new AbsoluteLayout
                        {
                            Children = {
                                    { backgroundImgP, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All },
                                    { portraitView, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All }
                                }
                        };
                    }
                    if (layoutP != null) {
                        Content = layoutP;
                    } else if (portraitView != null) {
                        Content = portraitView;
                    } // otherwise don't change content.
                }
            }
        }
        */

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
            Device.BeginInvokeOnMainThread(() => {
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

            // selectBoardButtonsStackP build by clicks.
            selectBoardScrollP.Content = selectBoardButtonsStackP;

            leaderStackP.Children.Clear();
            if (activeLeaderboard != null) {
                // only do this if activeLeaderboard has been set; and then check it has been loaded!!
                if (listOfLeaderboards.ContainsKey(activeLeaderboard)) {
                    for (int j = 0; j < listOfLeaderboards[activeLeaderboard].Count; j += 2) {
                        // does not contrain to screen width.
                        // of course not.  It's a STACK!!  Solution: set the suggested width of the images!
                        StackLayout leaderRow = new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            
                        };
                        leaderImgsP[j].WidthRequest = (Width / 2.05);
                        leaderRow.Children.Add(leaderImgsP[j]);
                        if ((j + 1) < listOfLeaderboards[activeLeaderboard].Count) {
                            leaderImgsP[j+1].WidthRequest = (Width / 2.05);
                            leaderRow.Children.Add(leaderImgsP[j + 1]);
                        }
                        leaderStackP.Children.Add(leaderRow);
                        /*
                        Grid leaderRowGrid = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                        leaderRowGrid.ColumnDefinitions.Add(new ColumnDefinition());
                        leaderRowGrid.ColumnDefinitions.Add(new ColumnDefinition());
                        leaderRowGrid.Children.Add(leaderImgsP[j],0,0);
                        if ((j + 1) < listOfLeaderboards[activeLeaderboard].Count) {
                            leaderRowGrid.Children.Add(leaderImgsP[j + 1],1,0);
                        }
                        leaderStackP.Children.Add(leaderRowGrid);
                        */
                    }

                    /*
                    int j=0;
                    foreach (LeaderboardJSON leader in listOfLeaderboards[activeLeaderboard]) {
                        // may need to put a lock here. as leaderImgsP can change in an external thread.
                        if (j < leaderImgsP.Count) { leaderStackP.Children.Add(leaderImgsP[j]); }
                        j++;
                    }
                    */
                }
            }
            leadersScrollP.Content = leaderStackP;

            portraitView.Children.Add(leaderboardLabelP, 0, 0);
            portraitView.Children.Add(selectBoardScrollP, 0, 1);
            Grid.SetRowSpan(selectBoardScrollP, 3);
            portraitView.Children.Add(leadersScrollP, 0, 4);
            Grid.SetRowSpan(leadersScrollP, 14);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);

            return result;
        }


        protected async virtual void OnRequestLeaderboard(object sender, EventArgs e) {
            // the current category command does not send back a category in leaderboard state.
            // for now, the system is only going to support Yesterday's and Today's leaderboard.

            /*
            // @todo Add ui to switch between yesterday and today
            activeCategory = GlobalStatusSingleton.mostRecentClosedCategoryId;
            // @todo remove this if constraint once I know harry has setup closed category results.
            if (activeCategory == -1) {
                activeCategory = GlobalStatusSingleton.votingCategoryId;
                leaderboardLabelP.Text = "BEST OF: " + GlobalStatusSingleton.votingCategoryDescription + "; voting still active";
                leaderboardLabelL.Text = "BEST OF: " + GlobalStatusSingleton.votingCategoryDescription + "; voting still active";
            } else {
                leaderboardLabelP.Text = "BEST OF: " + GlobalStatusSingleton.mostRecentClosedCategoryDescription + "; voting closed";
                leaderboardLabelL.Text = "BEST OF: " + GlobalStatusSingleton.mostRecentClosedCategoryDescription + "; voting closed";
            }
            */
            Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard start");
            long loadCategory = ((RequestLeaderboardEventArgs)e).Category.categoryId;
            string categoryName = ((RequestLeaderboardEventArgs)e).Category.description;
            // change button to say loading...
            selectLeaderboardDictP[loadCategory].Text = categoryName + "  Loading...";
            string result = await requestLeaderboardAsync(loadCategory);

            if (result.Equals(LOAD_FAILURE)) {
                // @todo This fail case is untested code. Does the UI come back?
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

            // no need to add a button here.  That is created in the request to load the leaderboard.
            // I do, however, need to update the button to remove the loading message.
            selectLeaderboardDictP[loadCategory].Text = categoryName;

            // I do need to update the ui if this is the active leaderboard.
            if (activeLeaderboard.Equals( ((RequestLeaderboardEventArgs)e).Category)  ) {
                drawLeaderImages();
                if (activeButton == null) {
                    activeButton = selectLeaderboardDictP[activeLeaderboard.categoryId];
                }
                activeButton.BackgroundColor = GlobalStatusSingleton.ActiveButtonColor;
                buildUI();
            }
#if DEBUG
            // will be null if everything went ok.
            if (JsonHelper.InvalidJsonElements != null) {
                // ignore this error. do we have a debug compiler directive??
                throw new Exception("Invalid leaderboard parse from server in Leaderboard get.");
            }
#endif // DEBUG
            /* No longer build the images when a leaderboard load is completed. Do that when switching to a leaderboard.
            // iterate through the leaders and build the ui output.
            foreach (LeaderboardJSON leader in leaders) {
                Image image = GlobalSingletonHelpers.buildFixedRotationImageFromStr(leader.imgStr);
                // did not implement click recognition on the images.
                leaderImgsP.Add(image);

                Image imageL = GlobalSingletonHelpers.buildFixedRotationImageFromStr(leader.imgStr);
                leaderImgsL.Add(imageL);
            }
            try {
                buildUI();
                //buildPortraitView();
                //buildLandscapeView();
                // new images, content needs to be updated.
                //Content = portraitView;
            } catch (Exception err) {
                Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard:Exception");
                Debug.WriteLine(err.ToString());
            }
            */
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
