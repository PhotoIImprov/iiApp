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

namespace ImageImprov {
    class LeaderboardPage : ContentView {
        public readonly static string LOAD_FAILURE = "No open voting category currently available.";
        readonly static string LEADERBOARD = "leaderboard";
        readonly static string CATEGORY = "?category_id=";

        static object uiLock = new object();

        KeyPageNavigator defaultNavigationButtonsP;
        KeyPageNavigator defaultNavigationButtonsL;

        public event EventHandler RequestLeaderboard;
        EventArgs eDummy = null;

        Grid portraitView = null;
        Grid landscapeView = null;

        Label leaderboardLabelP = new Label {
            Text = "BEST OF: ",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
            BackgroundColor = Color.FromRgb(252, 213, 21),
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };
        Label leaderboardLabelL = new Label
        {
            Text = "BEST OF: ",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
            BackgroundColor = Color.FromRgb(252, 213, 21),
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };

        // tracks what category I'm showing
        long activeCategory;

        IList<LeaderboardJSON> leaders;
        IList<Image> leaderImgsP = null;
        IList<Image> leaderImgsL = null;

        //
        //   BEGIN Variables related/needed for images to place background on screen.
        //
        AbsoluteLayout layoutP;  // this lets us place a background image on the screen.
        AbsoluteLayout layoutL;  // this lets us place a background image on the screen.
        Assembly assembly = null;
        Image backgroundImgP = null;
        Image backgroundImgL = null;
        string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place background on screen.
        // 

        public LeaderboardPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;
            // This class is hooked up to JudgingContentPage to tell me when the categories for leaderboards are available.
            // That happens in MainPageSwipeUI.
            leaderImgsP = new List<Image>();
            leaderImgsL = new List<Image>();

            // and to listen for leaderboard requests; done as event so easy to process async.
            this.RequestLeaderboard += new EventHandler(OnRequestLeaderboard);

            // fire a loadChallengeNameEvent.
            eDummy = new EventArgs();

            // place holder.
            Content = leaderboardLabelP;
        }

        public virtual void OnCategoryLoad(object sender, EventArgs e) {
            //categoryLabel.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
            if (RequestLeaderboard != null) {
                RequestLeaderboard(sender, e);
            }
        }
        //protected override void OnSizeAllocated(double width, double height) {
        //protected void OnPageSizeChanged(object sender, EventArgs e) {
        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height);
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
                } else {
                    Content = landscapeView;
                }
            } else {
                //GlobalStatusSingleton.inPortraitMode = true;
                if ((backgroundImgP == null) && (width>0) && (portraitView != null)) {
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

        public int buildUI() {
            int res = 0;
            int res2 = 0;
            Device.BeginInvokeOnMainThread(() =>
            {
                res = buildPortraitView();
                res2 = buildLandscapeView();
                OnSizeAllocated(Width, Height);
            });
            return ((res < res2) ? res : res2);
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
                defaultNavigationButtonsP = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
            }
            StackLayout leaderStack = new StackLayout { HorizontalOptions = LayoutOptions.FillAndExpand, };
            int j = 0;
            foreach (LeaderboardJSON leader in leaders) {
                string uname = leader.username;
                if (uname == null) {
                    uname = "empty name";
                }
                /*
                StackLayout leaderRow = new StackLayout {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        leaderImgsP[j],
                        new Label { Text = System.Convert.ToString(leader.rank),TextColor = Color.Black, },
                        new Label { Text = uname,TextColor = Color.Black, },
                        new Label { Text = System.Convert.ToString(leader.score),TextColor = Color.Black, }
                    }
                };
                leaderStack.Children.Add(leaderRow);
                */
                leaderStack.Children.Add(leaderImgsP[j]);
                j++;
            }
            ScrollView leadersScroll = new ScrollView { Content = leaderStack };

            portraitView.Children.Add(leaderboardLabelP, 0, 0);
            portraitView.Children.Add(leadersScroll, 0, 1);
            Grid.SetRowSpan(leadersScroll, 17);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);

            return result;
        }

        public int buildLandscapeView() {
            int result = 1;
            // all my elements are already members...
            if (landscapeView == null) {
                landscapeView = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                //for (int i = 0; i < 20; i++) {
                //    landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                //}
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(9, GridUnitType.Star) });
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(9, GridUnitType.Star) });
            }
            if (defaultNavigationButtonsL == null) {
                defaultNavigationButtonsL = new KeyPageNavigator(false) { ColumnSpacing = 1, RowSpacing = 1 };
            }
            //StackLayout leaderStack = new StackLayout { Orientation = StackOrientation.Horizontal, VerticalOptions = LayoutOptions.FillAndExpand, };
            // Has to be vertical because of the carousel 
            StackLayout leaderStack = new StackLayout { Orientation = StackOrientation.Vertical, VerticalOptions = LayoutOptions.FillAndExpand, };
            //int j = 0;
            //foreach (LeaderboardJSON leader in leaders) {
            for (int j=0; j<leaders.Count; j=j+2) { 
                string uname = leaders[j].username;
                if (uname == null) {
                    uname = "empty name";
                }
                /*
                StackLayout leaderRow = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        leaderImgsL[j],
                        new Label { Text = System.Convert.ToString(leader.rank),TextColor = Color.Black, },
                        new Label { Text = uname,TextColor = Color.Black, },
                        new Label { Text = System.Convert.ToString(leader.score),TextColor = Color.Black, }
                    }
                };
                leaderStack.Children.Add(leaderRow);
                */
                //leaderStack.Children.Add(leaderImgsL[j]);
                StackLayout leaderRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    Children = {
                        leaderImgsL[j], leaderImgsL[j+1],
                    }
                };
                leaderStack.Children.Add(leaderRow);
                //j++;
            }
            ScrollView leadersScroll = new ScrollView { HorizontalOptions=LayoutOptions.Center, Content = leaderStack };

            landscapeView.Children.Add(leaderboardLabelL, 0, 0);
            //Grid.SetColumnSpan(leaderboardLabelL, 17);
            landscapeView.Children.Add(leadersScroll, 0, 1);
            //Grid.SetColumnSpan(leadersScroll, 17);
            landscapeView.Children.Add(defaultNavigationButtonsL, 1, 0);
            Grid.SetRowSpan(defaultNavigationButtonsL, 2);
            //Grid.SetColumnSpan(defaultNavigationButtonsL, 2);

            return result;

        }

        protected async virtual void OnRequestLeaderboard(object sender, EventArgs e) {
            // the current category command does not send back a category in leaderboard state.
            // for now, the system is only going to support Yesterday's and Today's leaderboard.

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
            string result = await requestLeaderboardAsync(activeCategory);

            if (result.Equals(LOAD_FAILURE)) {
                // @todo This fail case is untested code. Does the UI come back?
                leaderboardLabelP.Text = "Connection failed. Please check connection";
                leaderboardLabelL.Text = "Connection failed. Please check connection";
                while (result.Equals(LOAD_FAILURE)) {
                    await Task.Delay(3000);
                    Debug.WriteLine("DHB:LeaderboardPage:OnRequestLeaderboard sending re-request.");
                    result = await requestLeaderboardAsync(activeCategory);
                }
            } 
            // nolonder an else case, as I stay in fail till i can reach here.
            // process successful leaderboard result string
            leaders = JsonHelper.DeserializeToList<LeaderboardJSON>(result);
#if DEBUG
            // will be null if everything went ok.
            if (JsonHelper.InvalidJsonElements != null) {
                // ignore this error. do we have a debug compiler directive??
                throw new Exception("Invalid leaderboard parse from server in Leaderboard get.");
            }
#endif // DEBUG
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
    }
}
