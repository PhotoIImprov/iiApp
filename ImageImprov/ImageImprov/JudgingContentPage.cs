using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

using Xamarin.Forms;

namespace ImageImprov
{
    public delegate void LoadChallengeNameEventHandler(object sender, EventArgs e);
    public delegate void LoadBallotPicsEventHandler(object sender, EventArgs e);

    // This is the first roog page of the judging.
    class JudgingContentPage : ContentPage {
        Grid portraitView = null;
        Grid landscapeView = null;

        // Yesterday's challenge 
        //< challengeLabel
        readonly Label challengeLabel = new Label
        {
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
        };
        //> challengeLabel
        public event LoadChallengeNameEventHandler LoadChallengeName;

        // @todo ideally, the images would be built in BallotJSON. Get this working, then think about that.
        // @todo imgs currently only respond to taps.  Would like to be able to longtap a fast vote.
        IList<Image> ballotImgs = null;
        // start with a simple stack layout...
        StackLayout picStack;

        public event LoadBallotPicsEventHandler LoadBallotPics;

        EventArgs eDummy = null;

        // the http command name to request category information to determine what we can vote on.
        const string CATEGORY = "category";
        // the command name to request a ballot when voting.
        const string BALLOT = "ballot";

        
        public JudgingContentPage() {
            ballotImgs = new List<Image>();
            buildPortraitView();
            buildLandscapeView();
            //Content = challengeLabel;

            // listen for orientation changes, which are currently handled through size change.
            SizeChanged += (sender, e) => AdjustContentToRotation();
            //GlobalStatusSingleton.IsPortrait(this) ? { buildPortraitView(); portraitView; } : landscapeView;

            // set myself up to listen for the loading events...
            this.LoadChallengeName += new LoadChallengeNameEventHandler(OnLoadChallengeName);
            this.LoadBallotPics += new LoadBallotPicsEventHandler(OnLoadBallotPics);
            // fire a loadChallengeNameEvent.
            eDummy = new EventArgs();
            if (LoadChallengeName != null) {
                LoadChallengeName(this, eDummy);
            }
            if (LoadBallotPics != null) {
                LoadBallotPics(this, eDummy);
            }
        }

        private void AdjustContentToRotation() {
            if (GlobalStatusSingleton.IsPortrait(this)) {
                buildPortraitView();
                Content = portraitView;
            } else {
                buildLandscapeView();
                Content = landscapeView;
            }
        }

        /// <summary>
        /// Builds/updates the portrait view
        /// </summary>
        /// <returns>1 on success, -1 if there are the wrong number of ballot imgs.</returns>
        public int buildPortraitView() {
            int result = 1;
            // all my elements are already members...
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                for (int i = 0; i < 25; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
            }
            if (ballotImgs.Count == 4) {
                portraitView.Children.Add(ballotImgs[0], 0, 0);
                Grid.SetRowSpan(ballotImgs[0], 6);

                portraitView.Children.Add(ballotImgs[1], 0, 6);  // col, row format
                Grid.SetRowSpan(ballotImgs[1], 6);

                portraitView.Children.Add(ballotImgs[2], 0, 13);  // col, row format
                Grid.SetRowSpan(ballotImgs[2], 6);

                portraitView.Children.Add(ballotImgs[3], 0, 19);  // col, row format
                Grid.SetRowSpan(ballotImgs[3], 6);
            } else {
                result = -1;
            }
            portraitView.Children.Add(challengeLabel, 0, 12);
            return result;
        }

        public int buildLandscapeView() {
            int result = 1;
            //landscapeView = new Grid();
            // make sure landscape creation is not messing with portrait...
            // bleh, seems like it is...

            // all my elements are already members...
            if (landscapeView == null) {
                landscapeView = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
                // topleft img1 48%H 50%w; topright img2 48%H 50%W
                // middle challenge label 4%H 100% W
                // bot left img3 48%H 50%W; bot right img4 48%H 50%W
                // 25 rows of 4% each.
                // No, go with an extra row so the full text shows.
                for (int i = 0; i < 26; i++) {
                    landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                // 2 columns, 50% each
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            if (ballotImgs.Count == 4) {
                landscapeView.Children.Add(ballotImgs[0], 0, 0);
                Grid.SetRowSpan(ballotImgs[0], 12);

                landscapeView.Children.Add(ballotImgs[1], 1, 0);  // col, row format
                Grid.SetRowSpan(ballotImgs[1], 12);

                landscapeView.Children.Add(ballotImgs[2], 0, 14);  // col, row format
                Grid.SetRowSpan(ballotImgs[2], 12);

                landscapeView.Children.Add(ballotImgs[3], 1, 14);  // col, row format
                Grid.SetRowSpan(ballotImgs[3], 12);
            } else {
                result = -1;
            }
            landscapeView.Children.Add(challengeLabel, 0, 12);
            Grid.SetColumnSpan(challengeLabel, 2);
            Grid.SetRowSpan(challengeLabel, 2);

            return result;
        }

        // image clicks
        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            bool dummy = false;
        }

        // I think this is handled through and abstract event handler in the cstr.
        //private void OnSizeChanged() {        }

        /////
        /////
        ///// BEGIN Loading section
        /////
        /////

        protected async virtual void OnLoadChallengeName(object sender, EventArgs e)
        {
            challengeLabel.Text = await requestChallengeNameAsync();
        }

        //< requestChallengeNameAsync
        static async Task<string> requestChallengeNameAsync()
        {
            string result = "No open voting category currently available.";

            while (GlobalStatusSingleton.loggedIn == false) {
                // should sleep me for 100 millisecs
                await Task.Delay(100);
            }
            try {
                HttpClient client = new HttpClient();
                string categoryURL = GlobalStatusSingleton.activeURL + CATEGORY
                    + "?user_id=" + GlobalStatusSingleton.loginCredentials.userId;
                HttpRequestMessage categoryRequest = new HttpRequestMessage(HttpMethod.Get, categoryURL);
                // Authentication currently turned off on Harry's end.
                //ballotRequest.Headers.Add("Authorization", "JWT "+GlobalStatusSingleton.authToken.accessToken);

                HttpResponseMessage catResult = await client.SendAsync(categoryRequest);
                if (catResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    string incoming = await catResult.Content.ReadAsStringAsync();

                    IList<CategoryJSON> categories = JsonHelper.DeserializeToList<CategoryJSON>(incoming);
                    //IList<CategoryJSON> categories = JsonConvert.DeserializeObject<List<CategoryJSON>>(incoming);
#if DEBUG
                    // will be null if everything went ok.
                    if (JsonHelper.InvalidJsonElements != null) {
                        // ignore this error. do we have a debug compiler directive??
                        throw new Exception("Invalid category parse from server in Category get.");
                    }
#endif // DEBUG
                    // iterate through the categories till i reach one that 
                    bool foundFirstVotingCategory = false;
                    bool moved = true;
                    IEnumerator<CategoryJSON> iter = categories.GetEnumerator();
                    // enumerator Current starts before the start of the list, so have to move onto it.
                    moved = iter.MoveNext();
                    while ((foundFirstVotingCategory == false) && (moved)) {
                        if (String.Equals(iter.Current.state, "VOTING")) {
                            GlobalStatusSingleton.votingCategoryId = iter.Current.categoryId;
                            foundFirstVotingCategory = true;
                            result = iter.Current.description;
                        } else {
                            moved = iter.MoveNext();
                        }
                    }
                    
                } else {
                    // no ok back from the server! gahh.
                    bool anotherfakepause = false;
                }
            }
            catch (System.Net.WebException err) {

            }



            return result;
        }
        //> RequestTimeAsync

        //<Ballot Loading
        protected async virtual void OnLoadBallotPics(object sender, EventArgs e)
        {
            while ((GlobalStatusSingleton.loggedIn == false) 
                   || (GlobalStatusSingleton.votingCategoryId == GlobalStatusSingleton.NO_CATEGORY_INFO)) {
                // should sleep me for 100 millisecs
                await Task.Delay(100);
            }

            string result = await requestBallotPicsAsync();
            IList<BallotJSON> ballots = JsonHelper.DeserializeToList<BallotJSON>(result);
            foreach (BallotJSON ballot in ballots) {
                Image image = new Image();
                image.Source = ImageSource.FromStream(() => new MemoryStream(ballot.imgStr));
                image.Aspect = Aspect.AspectFill;
                // This works. looks like a long press will be a pain in the ass.
                TapGestureRecognizer tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnClicked;
                image.GestureRecognizers.Add(tapGesture);
                ballotImgs.Add(image);
            }
            /*
            picStack = new StackLayout();
            picStack.Children.Add(challengeLabel);
            foreach (Image img in ballotImgs) {
                picStack.Children.Add(img);
            }
            */
            buildPortraitView();
            buildLandscapeView();
            // new images, content needs to be updated.
            AdjustContentToRotation();
            //this.Content = picStack;
        }
        // this gets the image streams from the server. It does NOT builds them as this is a statis async fcn.
        // try to do this by staying away from Bitmap (android) and UIImage (iOS).
        // may not be possible as it seems the abstraction layer Image does not have a way to build the image 
        // from bytes.
        //    my guess is I should focus on streams...
        // @return The stream as a string. It will still need to be processed into images.
        // @todo handle errors from ballots
        // @todo handle request on fails and network reactivation
        // @todo turn on authentication
        static async Task<string> requestBallotPicsAsync()
        {
            string result = "fail";
            try {
                HttpClient client = new HttpClient();
                string ballotURL = GlobalStatusSingleton.activeURL + BALLOT 
                    + "?user_id="+GlobalStatusSingleton.loginCredentials.userId
                    + "&category_id="+GlobalStatusSingleton.votingCategoryId;
                HttpRequestMessage ballotRequest = new HttpRequestMessage(HttpMethod.Get, ballotURL);
                // Authentication currently turned off on Harry's end.
                //ballotRequest.Headers.Add("Authorization", "JWT "+GlobalStatusSingleton.authToken.accessToken);

                HttpResponseMessage ballotResult = await client.SendAsync(ballotRequest);
                if (ballotResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = await ballotResult.Content.ReadAsStringAsync();
                }
                else {
                    // no ok back from the server! gahh.
                    bool anotherfakepause = false;
                }
            } catch (System.Net.WebException err) {

            }
            

            /*
            byte[] decodedString = System.BitConverter.GetBytes(   (ballotResult);
            Image i = new Image(decodedString);
            ImageSource.
            */
            return result;
        }
        /////
        /////
        ///// End Loading section
        /////
        /////
    }
}
