using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        KeyPageNavigator defaultNavigationButtons;

        public event EventHandler RequestLeaderboard;
        EventArgs eDummy = null;

        Grid portraitView = null;

        Label leaderboardLabel = new Label {
            Text = "Leaderboard category: ",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
        };

        // tracks what category I'm showing
        long activeCategory;

        IList<LeaderboardJSON> leaders;
        IList<Image> leaderImgs = null;

        public LeaderboardPage() {
            // This class is hooked up to JudgingContentPage to tell me when the categories for leaderboards are available.
            // That happens in MainPageSwipeUI.
            leaderImgs = new List<Image>();

            // and to listen for leaderboard requests; done as event so easy to process async.
            this.RequestLeaderboard += new EventHandler(OnRequestLeaderboard);

            // fire a loadChallengeNameEvent.
            eDummy = new EventArgs();

            // place holder.
            Content = leaderboardLabel;
        }

        public virtual void OnCategoryLoad(object sender, EventArgs e) {
            //categoryLabel.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
            if (RequestLeaderboard != null) {
                RequestLeaderboard(sender, e);
            }
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
            if (defaultNavigationButtons == null) {
                defaultNavigationButtons = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
            }
            StackLayout leaderStack = new StackLayout();
            int j = 0;
            foreach (LeaderboardJSON leader in leaders) {
                string uname = leader.username;
                if (uname == null) {
                    uname = "empty name";
                }
                StackLayout leaderRow = new StackLayout {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        leaderImgs[j],
                        new Label { Text = System.Convert.ToString(leader.rank) },
                        new Label { Text = uname },
                        new Label { Text = System.Convert.ToString(leader.score) }
                    }
                };
                leaderStack.Children.Add(leaderRow);
                j++;
            }
            ScrollView leadersScroll = new ScrollView { Content = leaderStack };

            portraitView.Children.Add(leaderboardLabel, 0, 0);
            portraitView.Children.Add(leadersScroll, 0, 1);
            Grid.SetRowSpan(leadersScroll, 18);
            portraitView.Children.Add(defaultNavigationButtons, 0, 19);

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
                leaderboardLabel.Text = "Category: " + GlobalStatusSingleton.votingCategoryDescription + "; voting still active";
            } else {
                leaderboardLabel.Text = "Category: " + GlobalStatusSingleton.mostRecentClosedCategoryDescription + "; voting closed";
            }
            string result = await requestLeaderboardAsync(activeCategory);

            if (result.Equals(LOAD_FAILURE)) {
                // @todo This fail case is untested code. Does the UI come back?
                leaderboardLabel.Text = "Connection failed. Please try again later";
            } else {
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
                    Image image = new Image();
                    image.Source = ImageSource.FromStream(() => new MemoryStream(leader.imgStr));
                    //image.Aspect = Aspect.AspectFill;
                    image.Aspect = Aspect.AspectFit;

                    // did not implement click recognition on the images.

                    leaderImgs.Add(image);
                }
                buildPortraitView();
                //buildLandscapeView();
                // new images, content needs to be updated.
                Content = portraitView;
                
            }
        }


        static async Task<string> requestLeaderboardAsync(long category_id) {
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
                    bool anotherfakepause = false;
                }
            } catch (System.Net.WebException err) {
                bool anotherfakepause = false;
            }
            return result;
        }
    }
}
