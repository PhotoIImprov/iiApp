using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;  // for debug assertions.
using Newtonsoft.Json;

using Xamarin.Forms;
using ExifLib;

namespace ImageImprov {
    public delegate void LoadChallengeNameEventHandler(object sender, EventArgs e);
    public delegate void LoadBallotPicsEventHandler(object sender, EventArgs e);
    public delegate void CategoryLoadSuccessEventHandler(object sender, EventArgs e);

    // This is the first roog page of the judging.
    class JudgingContentPage : ContentPage {
        public static string LOAD_FAILURE = "No open voting category currently available.";

        Grid portraitView = null;
        Grid landscapeView = null;
        KeyPageNavigator defaultNavigationButtons;

        // Yesterday's challenge 
        //< challengeLabel
        readonly Label challengeLabel = new Label
        {
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
        };
        //> challengeLabel
        // This is the request to load.
        public event LoadChallengeNameEventHandler LoadChallengeName;
        // This is the I'm loaded, who else wants me.
        public event CategoryLoadSuccessEventHandler CategoryLoadSuccess;


        IList<BallotJSON> ballots;
        // @todo ideally, the images would be built in BallotJSON. Get this working, then think about that.
        // @todo imgs currently only respond to taps.  Would like to be able to longtap a fast vote.
        IList<Image> ballotImgs = null;

        public event LoadBallotPicsEventHandler LoadBallotPics;
        public event EventHandler Vote;
        EventArgs eDummy = null;

        // the http command name to request category information to determine what we can vote on.
        const string CATEGORY = "category";
        // the command name to request a ballot when voting.
        const string BALLOT = "ballot";

        // interesting. doing this somehow sets up a double tap (and therefore an error due to data clearing)
        //TapGestureRecognizer tapGesture;

        // tracks the number of pictures in each orientation so we know how to display.
        int orientationCount;

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
            this.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(LoadBallotPics);

            // and to listen for vote sends; done as event so easy to process async.
            this.Vote += new EventHandler(OnVote);

            // fire a loadChallengeNameEvent.
            eDummy = new EventArgs();
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            // right now, do nothing.
            if (LoadChallengeName != null) {
                LoadChallengeName(this, eDummy);
            }
            // can't do this without the challenge info!
            // now done as a listener off CategoryLoad
            /*
            if (LoadBallotPics != null) {
                LoadBallotPics(this, eDummy);
            }
            */
        }

        private void AdjustContentToRotation() {
            if (GlobalStatusSingleton.IsPortrait(this)) {
                //if (portraitView == null) {
                    buildPortraitView();
                //}
                Content = portraitView;
            } else {
                //if (landscapeView == null) {
                    buildLandscapeView();
                //}
                Content = landscapeView;
            }
        }

        private void ClearContent() {
            ballots.Clear();
            ballotImgs.Clear();  // does Content update? No
            Content = new StackLayout() { Children = { challengeLabel, } };
        }

        private void ClearContent(int index) {
            portraitView.IsEnabled = false;
            landscapeView.IsEnabled = false;
            ballots.Clear();
            //ballotImgs.Clear();  // does Content update? No
            for (int i=0; i<4; i++) {
                if (i != index) {
                    ballotImgs[i].IsEnabled = false;
                    ballotImgs[i].IsVisible = false;
                } else {

                    // @todo setting isenabled to false is insufficient.
                    //      clicking definitely causes an exception.
                    //      removing the gesture recognizer causes an exception down stream with no tap...
                    ballotImgs[i].IsEnabled = false;
                    //ballotImgs[i].GestureRecognizers.Remove(ballotImgs[i].GestureRecognizers[0]);
                }
            }
            //Content = new StackLayout() { Children = { challengeLabel, } };
        }

        /// <summary>
        /// Builds/updates the portrait view
        /// </summary>
        /// <returns>1 on success, -1 if there are the wrong number of ballot imgs.</returns>
        public int buildPortraitView() {
            // ignoring orientation count for now.
            // the current implemented case is orientationCount == 0. (displays as a stack)
            // There are two other options... 
            //    orientationCount == 2 - displays as 2xstack or stackx2 per first img orientation
            //    orientationCount == 4 - display as a 2x2 grid
            int result = 1;
            // all my elements are already members...
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                for (int i = 0; i < 26; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            if (defaultNavigationButtons==null) {
                defaultNavigationButtons = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
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
                // Note: This is reached on ctor call, so don't put an assert here.
                result = -1;
                if (ballotImgs.Count > 0) {
                    portraitView.Children.Add(ballotImgs[0], 0, 0);
                    Grid.SetRowSpan(ballotImgs[0], 6);
                }

                if (ballotImgs.Count > 1) {
                    portraitView.Children.Add(ballotImgs[1], 0, 6);  // col, row format
                    Grid.SetRowSpan(ballotImgs[1], 6);
                }

                if (ballotImgs.Count > 2) {
                    portraitView.Children.Add(ballotImgs[2], 0, 13);  // col, row format
                    Grid.SetRowSpan(ballotImgs[2], 6);
                }

                if (ballotImgs.Count > 3) {
                    portraitView.Children.Add(ballotImgs[3], 0, 19);  // col, row format
                    Grid.SetRowSpan(ballotImgs[3], 6);
                }
            }
            portraitView.Children.Add(challengeLabel, 0, 12);
            portraitView.Children.Add(defaultNavigationButtons, 0, 25);

            return result;
        }

        public int buildLandscapeView() {
            // ignoring orientation count for now.
            // the current implemented case is orientationCount == 0. (displays as a 2x2)
            // There are two other options... 
            //    orientationCount == 2 - displays as 2xstack or stackx2 per first img orientation
            //    orientationCount == 4 - display as a 4x1 horizontally aligned portraits.

            int result = 1;
            //landscapeView = new Grid();
            // make sure landscape creation is not messing with portrait...

            if (defaultNavigationButtons == null) {
                defaultNavigationButtons = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
            }

            // all my elements are already members...
            if (landscapeView == null) {
                landscapeView = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
                // topleft img1 48%H 50%w; topright img2 48%H 50%W
                // middle challenge label 4%H 100% W
                // bot left img3 48%H 50%W; bot right img4 48%H 50%W
                // 25 rows of 4% each.
                // No, go with an extra row so the full text shows.  Went to 28 rows for nav buttons.
                for (int i = 0; i < 28; i++) {
                    landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                // 2 columns, 50% each
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                // flush the old children.
                landscapeView.Children.Clear();
                landscapeView.IsEnabled = true;
            }
            // should I be flushing the children each time???
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
                if (ballotImgs.Count > 0) {
                    landscapeView.Children.Add(ballotImgs[0], 0, 0);
                    Grid.SetRowSpan(ballotImgs[0], 12);
                }

                if (ballotImgs.Count > 1) {
                    landscapeView.Children.Add(ballotImgs[1], 1, 0);  // col, row format
                    Grid.SetRowSpan(ballotImgs[1], 12);
                }
                if (ballotImgs.Count > 2) {
                    landscapeView.Children.Add(ballotImgs[2], 0, 14);  // col, row format
                    Grid.SetRowSpan(ballotImgs[2], 12);
                }
                if (ballotImgs.Count > 3) {
                    landscapeView.Children.Add(ballotImgs[3], 1, 14);  // col, row format
                    Grid.SetRowSpan(ballotImgs[3], 12);
                }
            }
            landscapeView.Children.Add(challengeLabel, 0, 12);
            Grid.SetColumnSpan(challengeLabel, 2);
            Grid.SetRowSpan(challengeLabel, 2);

            landscapeView.Children.Add(defaultNavigationButtons, 0, 26);  // going to wrong position for some reason...
            Grid.SetColumnSpan(defaultNavigationButtons, 2);
            Grid.SetRowSpan(defaultNavigationButtons, 2);
            return result;
        }

        // image clicks
        // @todo adjust so that voting occurs on a long click
        // @todo adjust so that a tap generates the selection confirm/report buttons.
        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (Vote != null) {
                Vote(sender, e);
            }
        }

        // I think this is handled through and abstract event handler in the cstr.
        //private void OnSizeChanged() {        }

        /////
        /////
        ///// BEGIN Loading section
        /////
        /////

        protected async virtual void OnLoadChallengeName(object sender, EventArgs e) {
            challengeLabel.Text = await requestChallengeNameAsync();
            if (CategoryLoadSuccess != null) {
                CategoryLoadSuccess(sender, e);
            }

        }

        //< requestChallengeNameAsync
        static async Task<string> requestChallengeNameAsync() {
            string result = LOAD_FAILURE;

            /* on token received event.
            // @todo. What/when should this function be called?  Right now I'm calling on instantiation.
            while (GlobalStatusSingleton.loggedIn == false) {
                // should sleep me for 100 millisecs
                await Task.Delay(100);
            }
            */
            try {
                HttpClient client = new HttpClient();
                //client.DefaultRequestHeaders.Authorization 
                //= new System.Net.Http.Headers.AuthenticationHeaderValue(GlobalSingletonHelpers.getAuthToken());
                string categoryURL = GlobalStatusSingleton.activeURL + CATEGORY
                    //+ "?user_id=" + GlobalStatusSingleton.loginCredentials.userId;
                    ;
                HttpRequestMessage categoryRequest = new HttpRequestMessage(HttpMethod.Get, categoryURL);
                // Authentication currently turned off on Harry's end.
                //categoryRequest.Headers.Add("Authorization", "JWT "+GlobalStatusSingleton.authToken.accessToken);
                categoryRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage catResult = await client.SendAsync(categoryRequest);
                if (catResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    string incoming = await catResult.Content.ReadAsStringAsync();

                    IList<CategoryJSON> categories = JsonHelper.DeserializeToList<CategoryJSON>(incoming);
#if DEBUG
                    // will be null if everything went ok.
                    if (JsonHelper.InvalidJsonElements != null) {
                        // ignore this error. do we have a debug compiler directive??
                        throw new Exception("Invalid category parse from server in Category get.");
                    }
#endif // DEBUG
                    // iterate through the categories till i reach one that 
                    foreach (CategoryJSON cat in categories) {
                        if (cat.state.Equals(CategoryJSON.VOTING)) {
                            GlobalStatusSingleton.votingCategoryId = cat.categoryId;
                            GlobalStatusSingleton.votingCategoryDescription = cat.description;
                            result = cat.description;
                        } else if (cat.state.Equals(CategoryJSON.UPLOAD)) {
                            GlobalStatusSingleton.uploadingCategoryId = cat.categoryId;
                            GlobalStatusSingleton.uploadCategoryDescription = cat.description;
                        } else if (cat.state.Equals(CategoryJSON.CLOSED)) {
                            GlobalStatusSingleton.mostRecentClosedCategoryId = cat.categoryId;
                            GlobalStatusSingleton.mostRecentClosedCategoryDescription = cat.description;
                        }
                    }
                } else {
                    // no ok back from the server! gahh.
                    bool anotherfakepause = false;
                }
            } catch (System.Net.WebException err) {

            }



            return result;
        }
        //> RequestTimeAsync

        //<Ballot Loading
        protected async virtual void OnLoadBallotPics(object sender, EventArgs e) {
            /* waiting for event now.
            while ((GlobalStatusSingleton.loggedIn == false)
                   || (GlobalStatusSingleton.votingCategoryId == GlobalStatusSingleton.NO_CATEGORY_INFO)) {
                // should sleep me for 100 millisecs
                await Task.Delay(100);
            }
            */
            string result = await requestBallotPicsAsync();
            if (!result.Equals("fail")) {
                processBallotString(result);
            } else {
                challengeLabel.Text = "Currently unable to load ballots";
            }
        }

        /// <summary>
        /// Tests the orientation of the passed in image.
        /// </summary>
        /// <param name="imgStr"></param>
        /// <returns>1 if the orientation is portrait, 0 otherwise.</returns>
        protected int isPortraitOrientation(byte[] imgStr) {
            int result = 0;
            var jpegInfo = new JpegInfo();
            using (var myFStream = new MemoryStream(imgStr)) {
                try {
                    jpegInfo = ExifReader.ReadJpeg(myFStream);
                    // portrait. upright. ExifLib.ExifOrientation.TopRight;
                    // portrait. upside down. ExifLib.ExifOrientation.BottomLeft;
                    // landscape. top to the right. ExifLib.ExifOrientation.BottomRight;
                    // Landscape. Top (where the samsung is) rotated to the left. ExifLib.ExifOrientation.TopLeft;
                    if ((jpegInfo.Orientation == ExifOrientation.TopRight) || (jpegInfo.Orientation == ExifOrientation.BottomLeft)) {
                        result = 1;
                    } else if ((jpegInfo.Orientation == 0) && (jpegInfo.Height > jpegInfo.Width)) {
                        // if there's no orientation info, go with portrait if H > W.
                        result = 1;
                    }
                } catch (Exception e) {
                    // will barf if there's an exif issue. so just accept result==0 and move on.
                }
            }
            return result;
        }

        // implmented as a function so it can be reused by the vote message response.
        protected virtual void processBallotString(string result) {
#if DEBUG
            int checkEmpty = ballotImgs.Count;
#endif // Debug            
            orientationCount = 0;
            try {
                ballots = JsonHelper.DeserializeToList<BallotJSON>(result);
                foreach (BallotJSON ballot in ballots) {
                    Image image = new Image();
                    image.Source = ImageSource.FromStream(() => new MemoryStream(ballot.imgStr));
                    image.Aspect = Aspect.AspectFill;
                    orientationCount += isPortraitOrientation(ballot.imgStr);
                    // This works. looks like a long press will be a pain in the ass.
                    TapGestureRecognizer tapGesture = new TapGestureRecognizer();
                    if (tapGesture == null) {
                        tapGesture = new TapGestureRecognizer();
                    }
                    tapGesture.Tapped += OnClicked;
                    image.GestureRecognizers.Add(tapGesture);
                    ballotImgs.Add(image);
                }
#if DEBUG
                int checkFull = ballotImgs.Count;
                Debug.Assert(ballotImgs.Count == 4, "less than 4 ballots sent");
#endif // Debug            
            } catch (Exception e) {
                // probably thrown by Deserialize.
                bool falseBreak = false;
                try {
                    ballots = JsonHelper.DeserializeToList<BallotJSON>(result);
                    foreach (BallotJSON ballot in ballots) {
                        Image image = new Image();
                        image.Source = ImageSource.FromStream(() => new MemoryStream(ballot.imgStr));
                        image.Aspect = Aspect.AspectFill;
                        orientationCount += isPortraitOrientation(ballot.imgStr);
                        // This works. looks like a long press will be a pain in the ass.
                        TapGestureRecognizer tapGesture = new TapGestureRecognizer();
                        if (tapGesture == null) {
                            tapGesture = new TapGestureRecognizer();
                        }
                        tapGesture.Tapped += OnClicked;
                        image.GestureRecognizers.Add(tapGesture);
                        ballotImgs.Add(image);
                    }
                } catch (Exception e2) {
                    // did i rethrow
                    bool falseBreak2 = false;
                }
            }
            buildPortraitView();
            buildLandscapeView();
            // new images, content needs to be updated.
            AdjustContentToRotation();
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
        static async Task<string> requestBallotPicsAsync() {
            string result = "fail";
            try {
                HttpClient client = new HttpClient();
                string ballotURL = GlobalStatusSingleton.activeURL + BALLOT
                    //+ "?user_id=" + GlobalStatusSingleton.loginCredentials.userId
                    //+ "&category_id=" + GlobalStatusSingleton.votingCategoryId;
                    + "?category_id=" + GlobalStatusSingleton.votingCategoryId;
                HttpRequestMessage ballotRequest = new HttpRequestMessage(HttpMethod.Get, ballotURL);
                // Authentication currently turned off on Harry's end.
                //ballotRequest.Headers.Add("Authorization", "JWT "+GlobalStatusSingleton.authToken.accessToken);
                ballotRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage ballotResult = await client.SendAsync(ballotRequest);
                if (ballotResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = await ballotResult.Content.ReadAsStringAsync();
                } else {
                    // no ok back from the server! gahh.
                    // @todo handle what happens when I request with an old/non-voting category_id
                    // @todo test this condition (old/non-voting category err).
                    bool anotherfakepause = false;
                }
            } catch (System.Net.WebException err) {

            }

            return result;
        }
        /////
        /////
        ///// End Loading section
        /////
        /////

        // Voting.
        protected async virtual void OnVote(object sender, EventArgs e) {
            // I can't vote without having ballots, so there should be no need to check login status
            // that said, this should be a quick check, and who knows what race conditions could sneak in
            // once I'm storing state.

            // @todo. do i need anything here?
            /*
            while ((GlobalStatusSingleton.loggedIn == false)
                   || (GlobalStatusSingleton.votingCategoryId == GlobalStatusSingleton.NO_CATEGORY_INFO)) {
                // should sleep me for 100 millisecs
                await Task.Delay(100);
            }
            */
            // ballots may have been cleared and this can be a dbl tap registration.
            // in which case, ignore.
            if (ballots.Count == 0) { return; }
            bool found = false;
            int index = 0;
            int selectionId = 0;
            long bid = -1;
            foreach (Image img in ballotImgs) {
                if (img == sender) {
                    found = true;
                    bid = ballots[index].bidId;
                    selectionId = index;
                } else {
                    index++;
                }
            }
            VoteJSON vote = new ImageImprov.VoteJSON();
            vote.bid = bid;
            vote.vote = 1;
            vote.like = true;
            VotesJSON votes = new VotesJSON();
            //votes.userId = GlobalStatusSingleton.loginCredentials.userId;
            votes.votes = new List<VoteJSON>();
            votes.votes.Add(vote);
#if DEBUG
            if (found == false) {
                throw new Exception("A button clicked on an image not in my ballots.");
            }
#endif
            string jsonQuery = JsonConvert.SerializeObject(votes);
            string origText = challengeLabel.Text;
            challengeLabel.Text = "Vote submitting, loading new ballot";
            ClearContent(selectionId);  
            string result = await requestVoteAsync(jsonQuery);
            if (result.Equals("fail")) {
                // @todo This fail case is untested code. Does the UI come back?
                challengeLabel.Text = "Connection failed. Please revote";
                AdjustContentToRotation();
            } else {
                // only clear on success
                ClearContent();
                challengeLabel.Text = origText;
                processBallotString(result);
            }
        }

        // sends the vote in and waits for a new ballot.
        static async Task<string> requestVoteAsync(string jsonQuery) {
            string result = "fail";
            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "vote");
                // build vote!
                request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage voteResult = await client.SendAsync(request);
                if (voteResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    // do I need these?
                    result = await voteResult.Content.ReadAsStringAsync();
                } else {
                    // pooh. what do i do here?
                    //result = "internal fail; why?";
                    // server failure. keep the msg as a fail for correct onVote processing
                    // do we get back json?
                    result = await voteResult.Content.ReadAsStringAsync();
                    bool fakePause = false;
                }
            } catch (System.Net.WebException err) {
                //result = "exception";
                // web failure. keep the msg as a simple fail for correct onVote processing

            }
            return result;
        }

    } // class
} // namespace

