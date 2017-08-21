using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;  // for debug assertions.
using Newtonsoft.Json;

using Xamarin.Forms;
using System.Reflection;
using ExifLib;
using SkiaSharp;

namespace ImageImprov {
    public delegate void LoadChallengeNameEventHandler(object sender, EventArgs e);
    public delegate void LoadBallotPicsEventHandler(object sender, EventArgs e);
    public delegate void CategoryLoadSuccessEventHandler(object sender, EventArgs e);
    public delegate void DequeueBallotRequestEventHandler(object sender, EventArgs e);

    // This is the first roog page of the judging.
    public class JudgingContentPage : ContentView, ILeaveZoomCallback {
        public static string LOAD_FAILURE = "No open voting category currently available.";

        // A dummy object for controlling lock on ui resources
        static readonly object uiLock = new object();

        Grid portraitView = null;

        // Yesterday's challenge 
        //< challengeLabel
        Label challengeLabelP = new Label
        {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
            //WidthRequest = Width,
            //MinimumHeightRequest = Height / 15.0,
            //FontSize = 30, // program later

        };
        
        //> challengeLabel
        // This is the request to load.
        public event LoadChallengeNameEventHandler LoadChallengeName;
        // This is the I'm loaded, who else wants me.
        public event CategoryLoadSuccessEventHandler CategoryLoadSuccess;
        // We just voted. Time to grab another ballot from the queue.
        public event DequeueBallotRequestEventHandler DequeueBallotRequest;


        // The BallotJSON now holds a list, rather than a single instance.
        // The single instance has been refactored to BallotCandidateJSON
        //IList<BallotJSON> ballots;
        BallotJSON ballot = null;

        // @todo ideally, the images would be built in BallotJSON. Get this working, then think about that.
        // @todo imgs currently only respond to taps.  Would like to be able to longtap a fast vote.
        // Need to instances to accomodate the way GridLayout handles spans.
        IList<iiBitmapView> ballotImgsP = null;
        IList<iiBitmapView> voteBoxes = null;
        public const string VOTE_BOX_FILENAME = "ImageImprov.IconImages.votebox.png";

        /// <summary>
        /// ballot, ballotImgsP, and ballotImgsL hold the active Ballot.
        /// This queue holds the ballots I have pre-downloaded for faster response times.
        /// Stores each ballot as the incoming json string, rather than adding a whole bunch of objects.
        /// An incoming ballot is ALWAYS added to the queue rather than processed. This way I avoid logic
        /// difficulties and potential race conditions in assessing where the ballot should go.
        /// From there, the queue is consumed whenever I need a ballot.
        /// </summary>
        Queue<string> preloadedBallots = null;

        public event LoadBallotPicsEventHandler LoadBallotPics;
        public event EventHandler Vote;
        /// <summary>
        /// Needed because iOS fires both clicked and double clicked events when a double click occurs.
        /// </summary>
        private iiBitmapView lastClicked = null;
        /// <summary>
        /// If the image had been unchecked, nothing doing.
        /// However, if the image was checked, we need to make sure it is reselected in the correct order.
        /// Just reselecting could push an image that had in place1 to place 2.
        /// </summary>
        private int checkPosition = -1;

        EventArgs eDummy = null;

        // the http command name to request category information to determine what we can vote on.
        const string CATEGORY = "category";
        // the command name to request a ballot when voting.
        const string BALLOT = "ballot";
        
        // When third is selected, there is enough info to rank all 4 images.
        const int PENULTIMATE_BALLOT_SELECTED = 3;

        // tracks the number of pictures in each orientation so we know how to display.
        int orientationCount;

        // tracks the votes that have been made until the final vote.
        VotesJSON votes = null;
        int firstSelectedIndex;
        // tracks which ballots have not yet received a vote in the multi-img vote voting scenario.
        List<BallotCandidateJSON> unvotedImgs;

        iiBitmapView helpButton;
        private const string HELP_BUTTON_FILENAME = "ImageImprov.IconImages.Help.png";
        private const string LOADING_FILENAME = "ImageImprov.IconImages.ii_loading.png";

        //
        //   BEGIN Variables related/needed for images to place image rankings and backgrounds on screen.
        //
        AbsoluteLayout layoutP;  // this lets us place a background image on the screen.
        List<iiBitmapView> rankImages = new List<iiBitmapView>();
        Assembly assembly = null;
        Image backgroundImgP = null;
        public static string[] rankFilenames = new string[] { "ImageImprov.IconImages.first.png", "ImageImprov.IconImages.second.png",
                "ImageImprov.IconImages.third.png", "ImageImprov.IconImages.fourth.png"};
        string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place image rankings and backgrounds on screen.
        // 
        //
        //   BEGIN Variables related/needed for double clicking an image
        //

        // Going with a single zoomPage owned by MasterPage
        //ZoomPage zoomPage = new ZoomPage();

        // lightbulb stars for voting!
        LightbulbTracker lightbulbRow; // = new LightbulbTracker();

        VotingInstructionsOverlay helpPage;

        public JudgingContentPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;
            ballot = new BallotJSON();

            preloadedBallots = new Queue<string>();

            ballotImgsP = new List<iiBitmapView>();
            //buildPortraitView();
            //buildLandscapeView();

            // set myself up to listen for the loading events...
            this.LoadChallengeName += new LoadChallengeNameEventHandler(OnLoadChallengeName);
            this.LoadBallotPics += new LoadBallotPicsEventHandler(OnLoadBallotPics);
            this.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(LoadBallotPics);
            this.DequeueBallotRequest += new DequeueBallotRequestEventHandler(OnDequeueBallotRequest);

            GlobalStatusSingleton.ptrToJudgingPageLoadCategory = CategoryLoadSuccess;
            

            // and to listen for vote sends; done as event so easy to process async.
            this.Vote += new EventHandler(OnVote);

            // This gets called too often to be of value (impacts performance dramatically)
            //challengeLabelP.PropertyChanged += OnPortraitViewSizeChanged;

            eDummy = new EventArgs();

            // used to merge with the base image to show the ranking number.
            buildRankImages();

            // Do I have a persisted ballot ready and waiting for me?
            buildUI();
            // Do I have a persisted ballot ready and waiting for me?
            managePersistedBallot(this, eDummy);
        }

        /// <summary>
        /// This has actualy become a generic helper function to load most persistent button images.
        /// </summary>
        protected void buildRankImages() {
            voteBoxes = new List<iiBitmapView>();
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += OnClicked;
            for (int i = 0; i < 4; i++) {
                iiBitmapView vBox = new iiBitmapView {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(VOTE_BOX_FILENAME, assembly),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MinimumWidthRequest = 60,
                    Margin = 2,
                };
                vBox.GestureRecognizers.Add(tap);
                voteBoxes.Add(vBox);
            }

            foreach (string filename in rankFilenames) {
                iiBitmapView img = new iiBitmapView {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(filename, assembly),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MinimumWidthRequest = 60,
                    Margin = 2,
                };
                //img.GestureRecognizers.Add(tap);
                rankImages.Add(img);
            }

            TapGestureRecognizer helpTap = new TapGestureRecognizer();
            helpTap.Tapped += OnHelpTapped;
            helpButton = new iiBitmapView {
                Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(HELP_BUTTON_FILENAME, assembly),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = 2,
            };
            helpButton.GestureRecognizers.Add(helpTap);
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            if (LoadChallengeName != null) {
                LoadChallengeName(this, eDummy);
            }
        }

        /// <summary>
        /// Used by App:OnResume.
        /// </summary>
        public void FireLoadChallengeName() {
            if (LoadChallengeName != null) {
                LoadChallengeName(this, eDummy);
            }
        }

        void OnPortraitViewSizeChanged(Object sender, EventArgs args) {
            View view = (View)sender;
            if ((view.Width <=0)||(view.Height<=0)) { return; }
            // This assumes label can be whole width.
            
            GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, view.Width, view.Height/10.0);
        }

        /// <summary>
        /// This is called whenever my underlying data for my layout has changed (e.g. for a vote)
        /// This is the only function that updates this.
        /// Unfortunately, we can have multiple threads reaching this point.
        /// So we need to enforce atomicity of the action.
        /// Forcing a new layout to be drawn.
        /// </summary>
        private void AdjustContentToRotation() {
            Debug.WriteLine("DHB:JudgingContentPage:AdjustContentToRotation start");
            //lock (uiLock) {
            //  buildPortraitView();
            //  buildLandscapeView();
            //}
            buildUI();
            // how does this work with background?
            // what purpose does this fcn serve?
            /*
            if (GlobalStatusSingleton.IsPortrait(this)) {
                Content = portraitView;
            } else {
                Content = landscapeView;
            }
            */
            Debug.WriteLine("DHB:JudgingContentPage:AdjustContentToRotation end");
        }

        private void ClearContent() {
            Debug.WriteLine("DHB:JudgingContentPage:ClearContent() start");
            if (ballot != null) {
                ballot.Clear();
            }
            ballotImgsP.Clear();  // does Content update? No
            if (unvotedImgs != null) {
                unvotedImgs.Clear();
            }
            if (votes != null) {
                votes.votes.Clear();
            }
            // This next line causes blow ups.
            // Shift to a strategy of only ever changing Content in a setContent fcn.
            //Content = new StackLayout() { Children = { challengeLabelP, } };
            Debug.WriteLine("DHB:JudgingContentPage:ClearContent() end");
        }

        // Turn of all images. voting is done.  Leave the selected image visible.
        // Hide all the others.
        private void highlightCorrectImg(IList<iiBitmapView> ballotImgs, int index) {
            for (int i = 0; i < ballotImgs.Count; i++) {
                if (i != index) {
                    ballotImgs[i].IsEnabled = false;
                    ballotImgs[i].IsVisible = false;
                } else {

                    // @todo setting isenabled to false is insufficient.
                    //      clicking definitely causes an exception.
                    //      removing the gesture recognizer causes an exception down stream with no tap...
                    ballotImgs[i].IsEnabled = false;
                    ballotImgs[i].IsVisible = true;
                    //ballotImgs[i].GestureRecognizers.Remove(ballotImgs[i].GestureRecognizers[0]);
                }
            }
        }

        /// <summary>
        /// This is for the old way, where we only select a single image for a ballot 
        /// </summary>
        /// <param name="index"></param>
        private void ClearContent(int index) {
            portraitView.IsEnabled = false;
            ballot.Clear();

            highlightCorrectImg(ballotImgsP, index);
        }

        public int buildUI() {
            int res = 0;
            Device.BeginInvokeOnMainThread(() => {
                res = buildPortraitView();
                if (res==1) {
                    Content = portraitView;
                }
            });
            return res;
        }

        /// <summary>
        /// Builds/updates the portrait view
        /// </summary>
        /// <returns>1 on success, -1 if there are the wrong number of ballot imgs.</returns>
        private int buildPortraitView() {
            Debug.WriteLine("DHB:JudgingContentPage:buildPortraitView begin");
            int result = 1;
            try {
                // all my elements are already members...
                if (portraitView == null) {
                    // yes, these are unbalanced for a reason.
                    portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                    portraitView.SizeChanged += OnPortraitViewSizeChanged;
                } else {
                    // flush the old children.
                    portraitView.Children.Clear();
                    portraitView.IsEnabled = true;
                }

                // ok. Everything has been initialized. So now I just need to decide where to put it.
                if (ballotImgsP.Count > 0) {
                    Debug.WriteLine("DHB:JudgingContentPage:buildPortraitView drawing a ballot");
                    // new design is just 4 squares and that's the only orientation
                    result = buildFourPortraitImgPortraitView();
                } else {
                    Debug.WriteLine("DHB:JudgingContentPage:buildPortraitView no ballot; building instruction");
                    // Note: This is reached on ctor call, so don't put an assert here.
                    //result = -1;  This is a valid exit case.  Result should be 1!
                    portraitView.RowDefinitions.Clear();
                    portraitView.ColumnDefinitions.Clear();
                    for (int i = 0; i < 16; i++) {
                        portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    }
                    for (int j = 0; j < 6; j++) {
                        portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        //portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    }

                    // want to show the instruction!
                    loadInstructions();

                    if (ballotImgsP.Count > 0) {
                        portraitView.Children.Add(ballotImgsP[0], 0, 2);
                        Grid.SetRowSpan(ballotImgsP[0], 6);
                        Grid.SetColumnSpan(ballotImgsP[0], 3);
                    }

                    if (ballotImgsP.Count > 1) {
                        portraitView.Children.Add(ballotImgsP[1], 3, 2);  // col, row format
                        Grid.SetRowSpan(ballotImgsP[1], 6);
                        Grid.SetColumnSpan(ballotImgsP[1], 3);
                    }

                    if (ballotImgsP.Count > 2) {
                        portraitView.Children.Add(ballotImgsP[2], 0, 8);  // col, row format
                        Grid.SetRowSpan(ballotImgsP[2], 6);
                        Grid.SetColumnSpan(ballotImgsP[2], 3);
                    }

                    if (ballotImgsP.Count > 3) {
                        portraitView.Children.Add(ballotImgsP[3], 3, 8);  // col, row format
                        Grid.SetRowSpan(ballotImgsP[3], 6);
                        Grid.SetColumnSpan(ballotImgsP[3], 3);
                    }

                    challengeLabelP.Text = "Loading images...";
#if DEBUG
                    challengeLabelP.Text += " no image case";
#endif


                    //#if DEBUG
                    //challengeLabelP.Text += " 4P_P case";
                    //@todo Periodically check to see if my xamarin forums query solved this.
                    //GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView, portraitView.Width);
                    // Calling this here is calling from an invalid height state that seems to fubar everything...
                    //GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
                    //#endif
                    portraitView.Children.Add(challengeLabelP, 0, 0);  // orig 15, now moved to top
                    Grid.SetColumnSpan(challengeLabelP, 6);
                    Grid.SetRowSpan(challengeLabelP, 2);
                }
            } catch (Exception e) {
                Debug.WriteLine("DHB:JudgingContentPage:buildPortraitView exception");
                Debug.WriteLine(e.ToString());
                result = -1;
            }
            Debug.WriteLine("DHB:JudgingContentPage:buildPortraitView end");
            return result;
        }

        /// <summary>
        /// Helper function for buildPortraitView that constructs the layout when the current ballot has 4
        /// images exif defines as portrait images.
        /// </summary>
        /// <returns></returns>
        public int buildFourPortraitImgPortraitView() {
            portraitView.RowDefinitions.Clear();
            portraitView.ColumnDefinitions.Clear();
            for (int i = 0; i < 16; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            for (int j = 0; j < 6; j++) {
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                // I can add none, but if i add one, then i just have 1. So here's 2. :)
                //portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            portraitView.Children.Add(ballotImgsP[0], 0, 2);
            Grid.SetRowSpan(ballotImgsP[0], 6);
            Grid.SetColumnSpan(ballotImgsP[0], 3);

            portraitView.Children.Add(ballotImgsP[1], 3, 2);  // col, row format
            Grid.SetRowSpan(ballotImgsP[1], 6);
            Grid.SetColumnSpan(ballotImgsP[1], 3);

            portraitView.Children.Add(ballotImgsP[2], 0, 8);  // col, row format
            Grid.SetRowSpan(ballotImgsP[2], 6);
            Grid.SetColumnSpan(ballotImgsP[2], 3);

            portraitView.Children.Add(ballotImgsP[3], 3, 8);  // col, row format
            Grid.SetRowSpan(ballotImgsP[3], 6);
            Grid.SetColumnSpan(ballotImgsP[3], 3);

            portraitView.Children.Add(voteBoxes[0], 2, 6);
            portraitView.Children.Add(voteBoxes[1], 5, 6);
            portraitView.Children.Add(voteBoxes[2], 2, 12);
            portraitView.Children.Add(voteBoxes[3], 5, 12);
            for (int i=0;i<4;i++) {
                Grid.SetRowSpan(voteBoxes[i], 2);
            }

            //#if DEBUG
            //challengeLabelP.Text += " 4P_P case";
            //@todo Periodically check to see if my xamarin forums query solved this.
            //GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView, portraitView.Width);
            // Calling this here is calling from an invalid height state that seems to fubar everything...
            //GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
            //#endif
            portraitView.Children.Add(challengeLabelP, 0, 0);
            Grid.SetColumnSpan(challengeLabelP, 6);
            Grid.SetRowSpan(challengeLabelP, 2);
            portraitView.Children.Add(helpButton, 5, 0);
            Grid.SetRowSpan(helpButton, 2);

            if (lightbulbRow == null) {
                lightbulbRow = new LightbulbTracker { HorizontalOptions = LayoutOptions.FillAndExpand, };
                lightbulbRow.buildUI();
            }

            //Grid.LayoutChildIntoBoundingRegion(lightbulbRow, new Xamarin.Forms.Rectangle(0.0, 17.0, 2.0, 1.0) );
            //Grid.SetColumnSpan(lightbulbRow, 2);  // Wanted this to hold the width stable, which it does, but sadly at half width.
            portraitView.Children.Add(lightbulbRow, 0, 15);
            Grid.SetColumnSpan(lightbulbRow, 6);  // this this line has to be after adding.
            Grid.SetRowSpan(lightbulbRow, 2);

            return 1;
        }

        // image clicks
        // @todo adjust so that voting occurs on a long click
        // @todo adjust so that a tap generates the selection confirm/report buttons.
        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            Debug.WriteLine("DHB:JudgingContentPage:OnClicked start");
            // The 2 tap recognizer is first. So clearly it's being ignored.
            // On first tap - dbl tap doesn't trigger. 2nd tap does.
            // on second tap - dbl tap triggers, and consumes trigger, so second does not.
            /*
            foreach (TapGestureRecognizer g in ((Image)sender).GestureRecognizers) {
                Debug.WriteLine(g.ToString()+" taps="+g.NumberOfTapsRequired);
            }
            */
            /*
            if (Content==zoomView) {
                // I'm on iOS trying to process both the click and double click. No! Click always COMPLETELY processed first.
                Debug.WriteLine("DHB:JudgingContentPage:OnClicked Content==zoomView");
                return;
            }
            */
            lastClicked = null;
            checkPosition = -1;
            if (Vote != null) {
                Vote(sender, e);
            }
            Debug.WriteLine("DHB:JudgingContentPage:OnClicked end");
        }

        public async void OnDoubleClick(object sender, EventArgs e) {
            // want to switch to a UI where just the sending image is present.
            // with a like button
            //    a flag button
            //    an entry for custom tags
            Debug.WriteLine("DHB:JudgingContentPage:OnDoubleClick start");
            iiBitmapView taggedImg = sender as iiBitmapView;
            if (taggedImg == null) { return; }

            // sadly I can't use taggedImg in zoomView as it causes a crash. 
            // probably for same reason I had to create two of everything for portrait vs landscape.
            int selectionId = 0;
            long bid = -1;
            BallotCandidateJSON votedOnCandidate = null;
            bool found = findSelectionIndexAndBid(sender, ref selectionId, ref bid, ref votedOnCandidate, ballotImgsP);
            if (found) {
                //taggedImg = GlobalSingletonHelpers.buildFixedRotationImage(votedOnCandidate);
                taggedImg = new iiBitmapView {
                    Bitmap = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(votedOnCandidate.imgStr, (ExifOrientation)votedOnCandidate.orientation)
                };
            }

            
            //buildZoomView(taggedImg, votedOnCandidate);
            Device.BeginInvokeOnMainThread(() => {
                MasterPage mp = ((MasterPage)Application.Current.MainPage);
                mp.zoomPage.MainImage = taggedImg;
                mp.zoomPage.ActiveMetaBallot = votedOnCandidate;
                mp.zoomPage.buildZoomView();
                mp.zoomPage.PreviousContent = this;
                Content = mp.zoomPage.Content;
                Debug.WriteLine("DHB:JudgingContentPage:OnDoubleClick Content==zoomView");
            });
            /*
            MasterPage mp = ((MasterPage)Application.Current.MainPage);
            mp.zoomPage.MainImage = taggedImg;
            mp.zoomPage.ActiveMetaBallot = votedOnCandidate;
            mp.zoomPage.buildZoomView();
            await mp.Navigation.PushModalAsync(mp.zoomPage);
            */

            /*
            if (Device.OS == TargetPlatform.iOS) {
                Debug.WriteLine("DHB:JudgingContentPage:OnDoubleClick on iOS do single click cleanup");
                Debug.WriteLine("DHB:JudgingContentPage:OnDoubleClick checkposition == " + checkPosition);
                if (checkPosition>-1) {
                    // was checked. deal with it.
                    if (checkPosition == (votes.votes.Count)) {  // no -1 here as the instance has been removed.
                        // was just on the end. simple fix.
                        Vote(sender, e);
                    } else {
                        // this is the pain in the butt case...
                        for (int i = checkPosition; i < votes.votes.Count; i++) {
                            votes.votes[i].vote++;
                        }
                        VoteJSON vote = new ImageImprov.VoteJSON();
                        // This has to be the only one left.
                        vote.bid = votedOnCandidate.bidId;
                        vote.vote = checkPosition;
                        vote.like = votedOnCandidate.isLiked ? "1" : "0";
                        vote.offensive = votedOnCandidate.isFlagged ? "1" : "0";
                        votes.votes.Insert(checkPosition, vote);
                        unvotedImgs.Remove(votedOnCandidate);
                        // brute force the images and do all four...
                        rebuildAllImagesWithVotes();
                    }
                } else {
                    // was not checked before. just uncheck.
                    Vote(sender, e);
                }
                // reset at start of process, as there are no process steps that can set these flags.
                //lastClicked = null;
                //checkPosition = -1;
            } */

            // event args has nothing I can set.
            // use a class member to show this has been processed and prevent click processing.
            // hmm... will this mess up android, who doesn't have this issue?
            // because how will android undo the mess...
            // let's try check to see what Content is set to! Nope. click is always processed first.
            Debug.WriteLine("DHB:JudgingContentPage:OnDoubleClick end");

        }

        public void OnHelpTapped(object sender, EventArgs e) {
            IOverlayable uiMaster = (IOverlayable)Application.Current.MainPage;
            IList<iiBitmapView> newImgs = new List<iiBitmapView>();
            foreach (iiBitmapView img in ballotImgsP) {
                iiBitmapView newCopy = new iiBitmapView {
                    Bitmap = img.Bitmap.Copy(),
                };
                newImgs.Add(newCopy);
            }
            if (helpPage == null) {
                helpPage = new VotingInstructionsOverlay(uiMaster, newImgs);
                helpPage.IsVisible = false;                
            } else {
                helpPage.setImages(newImgs);
            }
            //this.Navigation.PushModalAsync(helpPage);  this never works. :(
            uiMaster.pushOverlay(helpPage);
        }

        /////
        /////
        ///// BEGIN Loading section
        /////
        /////

        protected async virtual void OnLoadChallengeName(object sender, EventArgs e) {
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadChallengeName start");

            // Moved to the ctor so I don't have to wait for http requests to load images
            // i have on my machine.
            // Do I have a persisted ballot ready and waiting for me?
            //managePersistedBallot(sender, e);
            // Do I have a persisted ballot ready and waiting for me?

            // can't lock in an async fcn.
            // can't update challengeLabel text here as it generates a ui race condition.
            // This puts rubbish up anyway.  erase.
            string ignorableResult = LOAD_FAILURE;
            while (ignorableResult.Equals(LOAD_FAILURE)) {
                ignorableResult = await requestChallengeNameAsync();
            }
            //challengeLabelP.Text = await requestChallengeNameAsync();
            //challengeLabelL.Text = challengeLabelP.Text;
            GlobalStatusSingleton.lastCategoryLoadTime = DateTime.Now;
            if (CategoryLoadSuccess != null) {
                CategoryLoadSuccess(sender, e);
                GlobalStatusSingleton.ptrToJudgingPageLoadCategory(sender, e);
            }

            // Time to post the notifications
            postCategoryNotifications();
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadChallengeName posted a notification");
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadChallengeName end");
        }

        protected async virtual void OnDequeueBallotRequest(object sender, EventArgs e) {
            Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest start");
            // Note: sender maybe null! (when fired from processBallotString due to a no img ballot.

            // Fires a loadBallotPics if queue is empty.
            // Otherwise, does the ui update.
            while (preloadedBallots.Count == 0) {
                // This should only happen if downloads are slow, user is super fast, or... whatever.
                // should sleep me for 100 millisecs, then check again.
                await Task.Delay(100);
            }
            Device.BeginInvokeOnMainThread(() =>
            {
                //lock (ballot) {
                    ClearContent();
                    try {
                        if (portraitView == null) {
                            this.buildUI();
                        }
                        processBallotString(preloadedBallots.Dequeue());
                    } catch (Exception ex) {
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine(ex.ToString());
                    }
                //}
            });
            // should be ok at this point. however, sometimes an invalid status gets saved.
            // this is how I pick that up and prevent it.
            if (ballot.ballots == null) {
                // apparently can't fire again from here. just hangs here continually calling this.
                //DequeueBallotRequest(this, e);
                if (preloadedBallots.Count > 0) {
                    //processBallotString(preloadedBallots.Dequeue());
                    Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest bad read. do I autofix with OnLoadBallotPics?");
                    Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest bad read. do I autofix with OnLoadBallotPics?");
                    Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest bad read. do I autofix with OnLoadBallotPics?");
                }
            }
            Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest queue length at end:" +preloadedBallots.Count);
            Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest end");
        }

        //< requestChallengeNameAsync
        static async Task<string> requestChallengeNameAsync() {
            Debug.WriteLine("DHB:JudgingContentPage:requestChallengeNameAsync start");
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
                            //GlobalStatusSingleton.votingCategoryId = cat.categoryId;
                            //GlobalStatusSingleton.votingCategoryDescription = cat.description;
                            if (!GlobalSingletonHelpers.listContainsCategory(GlobalStatusSingleton.votingCategories, cat)) {
                                GlobalStatusSingleton.votingCategories.Add(cat);
                                // Need more than just this. This relies on there being an open voting category.
                                GlobalSingletonHelpers.removeCategoryFromList(GlobalStatusSingleton.uploadingCategories, cat);
                            }
                            result = cat.description;
                        } else if (cat.state.Equals(CategoryJSON.UPLOAD)) {
                            //GlobalStatusSingleton.uploadingCategoryId = cat.categoryId;
                            //GlobalStatusSingleton.uploadCategoryDescription = cat.description;
                            if (!GlobalSingletonHelpers.listContainsCategory(GlobalStatusSingleton.uploadingCategories, cat)) {
                                GlobalStatusSingleton.uploadingCategories.Add(cat);
                                // should not exist in voting or closed categories!
                            }
                        } else if (cat.state.Equals(CategoryJSON.COUNTING)) {
                            if (!GlobalSingletonHelpers.listContainsCategory(GlobalStatusSingleton.countingCategories, cat)) {
                                GlobalStatusSingleton.countingCategories.Add(cat);
                                GlobalSingletonHelpers.removeCategoryFromList(GlobalStatusSingleton.votingCategories, cat);
                            }
                        } else if (cat.state.Equals(CategoryJSON.CLOSED)) {
                            //GlobalStatusSingleton.mostRecentClosedCategoryId = cat.categoryId;
                            //GlobalStatusSingleton.mostRecentClosedCategoryDescription = cat.description;
                            if (!GlobalSingletonHelpers.listContainsCategory(GlobalStatusSingleton.closedCategories, cat)) {
                                GlobalStatusSingleton.closedCategories.Add(cat);
                                GlobalSingletonHelpers.removeCategoryFromList(GlobalStatusSingleton.countingCategories, cat);
                            }
                        } else if (cat.state.Equals(CategoryJSON.PENDING)) {
                            // For now I don't persist pending categories, so no further housekeeping is needed with any cateogry.
                            if (!GlobalSingletonHelpers.listContainsCategory(GlobalStatusSingleton.pendingCategories, cat)) {
                                GlobalStatusSingleton.pendingCategories.Add(cat);
                            }
                        }
                    }
                    // If below is true, we are in a no voting category open condition...
                    if (result.Equals(LOAD_FAILURE)) {
                        result = "No open voting categories";
                    }
                } else {
                    // no ok back from the server! gahh.
                    Debug.WriteLine("DHB:JudgingContentPage:requestChallengeNameAsync err invalid status code: " + catResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:JudgingContentPage:requestVoteAsync:Exception");
                Debug.WriteLine(e.ToString());
            }


            Debug.WriteLine("DHB:JudgingContentPage:requestChallengeNameAsync end");
            return result;
        }
        //> RequestTimeAsync

        /// <summary>
        /// tracks ballot load attempts
        /// </summary>
        static int ballotLoadAttemptCounter = 1;

        /// <summary>
        /// Tracks whether there is an instance of OnLoadBallotPics running or not.
        /// </summary>
        bool loadLoopRunning = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //<Ballot Loading
        protected async virtual void OnLoadBallotPics(object sender, EventArgs e) {
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics start");
            /* waiting for event now.
            while ((GlobalStatusSingleton.loggedIn == false)
                   || (GlobalStatusSingleton.votingCategoryId == GlobalStatusSingleton.NO_CATEGORY_INFO)) {
                // should sleep me for 100 millisecs
                await Task.Delay(100);
            }
            */

            // hmm... try doing this before sending the category request...
            // Do I have a persisted ballot ready and waiting for me?
            //managePersistedBallot(sender, e);
            // Do I have a persisted ballot ready and waiting for me?

            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics pre async call");
            string result = await requestBallotPicsAsync();
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics post async call");
            if (!result.Equals("fail")) {
                preloadedBallots.Enqueue(result);
                // :(  can't lock on a null
                //lock (ballot) {
                Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics pre ballot read");
                //if ((ballot == null) || (ballot.ballots==null) || (ballot.ballots.Count==0)) {
                // ballot is now instantiated on startup
                if ((ballot.ballots == null) || (ballot.ballots.Count == 0)) {
                    // No, constrain to a single point of interest for thread safety.
                    //processBallotString(preloadedBallots.Dequeue());
                    DequeueBallotRequest(this, eDummy);
                }
                Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics post ballot read");
            } else {
                // @todo need to update labels inside the build ui functions only.
                //challengeLabelP.Text = "Currently unable to load ballots.  Attempts: " + ballotLoadAttemptCounter ;
                //challengeLabelL.Text = "Currently unable to load ballots.  Attempts: " + ballotLoadAttemptCounter ;
                ballotLoadAttemptCounter++;
                if (ballotLoadAttemptCounter > 2) {
                    Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics " + ballotLoadAttemptCounter + " load fails");
                }
                await Task.Delay(5000);
                if (LoadBallotPics != null) {
                    LoadBallotPics(this, eDummy);
                }

            }
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics pre while");
            // keep this thread going constantly, making sure we never run out.
            // however, this is a response to an event that can be fired from multiple places and reasons.
            // do not want to have thousands of these building up and crushing the app.
            if (loadLoopRunning == false) {
                loadLoopRunning = true;

                while (true) {
                    if (preloadedBallots.Count < GlobalStatusSingleton.minBallotsToLoad) {
                        Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics pre async call 2");
                        result = await requestBallotPicsAsync();
                        Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics post async call 2");
                        if (!result.Equals("fail")) {
                            preloadedBallots.Enqueue(result);
                        }
                    } else {
                        // no need to go bonkers
                        await Task.Delay(5000);
                        //Thread.Sleep(500);
                    }
                    // this happens when i run out of ballots
                    // but why??
                    // unhappy with this solution as it means I don't understand what's happening.                
                    if (((ballot.ballots == null) || (ballot.ballots.Count == 0)) && (preloadedBallots.Count > 1)) {
                        Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics out of ballots, sending dqueue");
                        //processBallotString(preloadedBallots.Dequeue());
                        DequeueBallotRequest(this, eDummy);
                    }
                    Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics queue Length:" + preloadedBallots.Count);
                    //}
                }
            }
            // This line gets tagged as unreachable, which it is... so just comment out.  Not anymore, so uncommented.
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics end");
        }

        /// <summary>
        /// Helper for managePersistedBallot that loads instructions on startup if
        /// there is no ballot.
        /// </summary>
        protected virtual void loadInstructions() {
            ballot.ballots = new List<BallotCandidateJSON>();
            //challengeLabelP.Text = "Instructions";
            challengeLabelP.Text = "Loading...";
            if (portraitView != null) {
                GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height / 10.0);
            }
            // now handle ballot
            Debug.WriteLine("DHB:JudgingContentPage:processBallotString generating images");
            for (int i=1;i<5; i++) {
                iiBitmapView imgP = new iiBitmapView() {
                    //Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Instructions.Instructions_" + i + ".png", assembly)
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_FILENAME, assembly),
                };
                ballotImgsP.Add(imgP);
            }

        }

        /// <summary>
        /// Helper function for OnLoadBallotPics.
        /// This function manages the processing around the queue and the ballot pre-existing.
        /// Should not have race condition concerns as this should be called before the http request is
        /// sent to the server.
        /// Note, called from an async function!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void managePersistedBallot(object sender, EventArgs e) {
            Debug.WriteLine("DHB:JudgingContentPage:managePersistedBallot start");
            if ((((ballot.ballots == null) || (ballot.ballots.Count == 0))) 
                && (GlobalStatusSingleton.persistedBallotAsString != null) && (!GlobalStatusSingleton.persistedBallotAsString.Equals(""))) {
                // check that persisted ballot is a valid string.

                preloadedBallots.Enqueue(GlobalStatusSingleton.persistedBallotAsString);
                GlobalStatusSingleton.persistedBallotAsString = null;
                DequeueBallotRequest(this, e);
            } else {
                // draw the instructions.
                loadInstructions();
            }
            if ((GlobalStatusSingleton.persistedPreloadedBallots != null) && (GlobalStatusSingleton.persistedPreloadedBallots.Count>0)) {
                while (GlobalStatusSingleton.persistedPreloadedBallots.Count>0) {
                    preloadedBallots.Enqueue(GlobalStatusSingleton.persistedPreloadedBallots.Dequeue());
                }
            }
            Debug.WriteLine("DHB:JudgingContentPage:managePersistedBallot end; " + preloadedBallots.Count + " loaded.");
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
                    Debug.WriteLine(e.ToString());
                }
            }
            return result;
        }

        /// <summary>
        /// Should only be called if the orientationCount is 2.
        /// If orientation count is 2, the presentation will be a mix of portrait and
        /// landscape images.  This function makes sure the 2 landscape and 2 portrait
        /// images are grouped together in the datasets.
        /// </summary>
        protected void checkImgOrderAndReorderAsNeeded() {
            // assume I am only called if orientation count == 2.
            // assume ballot count == 4

            // 3 Cases: 
            // Case 1: [0].isPortrait == [1].isPortrait -> already in correct order. do nothing.
            // Case 2: ([0].isPortrait != [1].isPortrait) && ([1] == [2]) 
            // Case 3: ([0].isPortrait == [3].isPortrait)
            // -> find the match. then swap.
            int swap = 0;
            if (ballot.ballots[0].isPortrait == ballot.ballots[1].isPortrait) {
                return;
            } else if (ballot.ballots[0].isPortrait == ballot.ballots[2].isPortrait) {
                swap = 2;
            } else {
                swap = 3;
            }
            if (swap>0) {
                BallotCandidateJSON moving = ballot.ballots[swap];
                ballot.ballots.RemoveAt(swap);
                ballot.ballots.Insert(1, moving);
                iiBitmapView pImg = ballotImgsP[swap];
                ballotImgsP.RemoveAt(swap);
                ballotImgsP.Insert(1, pImg);
            }


        }

        /// <summary>
        /// Currently, this also manages orientation Count.
        /// This is what is called during the initial image setup.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        protected iiBitmapView setupImgFromBallotCandidate(BallotCandidateJSON candidate) {
            // TEST
            // TEST
            // TEST
            // Do this so I can confirm bad bids for harry
            //SKBitmap testBitmap = GlobalSingletonHelpers.SKBitmapFromString(candidate.imgStr);
            // TEST
            // TEST
            // TEST

            //Image image = new Image();
            //image.Source = ImageSource.FromStream(() => new MemoryStream(candidate.imgStr));
            //Image image = GlobalSingletonHelpers.buildFixedRotationImage(candidate);
            iiBitmapView image = new iiBitmapView {
                Bitmap = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(candidate.imgStr, (ExifOrientation)candidate.orientation)
            };
            //image.Aspect = Aspect.AspectFill;
            //image.Aspect = Aspect.AspectFit;
            //image.Aspect = GlobalStatusSingleton.aspectOrFillImgs;


            // This works. looks like a long press will be a pain in the ass.
            // bleh. iOS always responds to both the doubleTap and tap. Whereas android only responds to the double tap
            // order of addition is immaterial. iOS grabs.

            // no longer a double tap
            TapGestureRecognizer doubleTap = new TapGestureRecognizer();
            //doubleTap.NumberOfTapsRequired = 2;
            doubleTap.Tapped += OnDoubleClick;
            image.GestureRecognizers.Add(doubleTap);

            // orientation info is based on the relative w/h of the image.
            // square images are all considered "landscape"
            //candidate.orientation = isPortraitOrientation(candidate.imgStr);
            orientationCount += candidate.isPortrait;
            return image;
        }

        /// <summary>
        /// implmented as a function so it can be reused by the vote message response.
        /// Note: This function does NOT deal with the ballot queue.  It is assumed that whatever
        /// is coming into me has already undergone a preloadedBallots dequeue.
        /// </summary>
        /// <param name="result"></param>
        protected virtual void processBallotString(string result) {
            Debug.WriteLine("DHB:JudgingContentPage:processBallotString begin");
#if DEBUG
            int checkEmpty = ballotImgsP.Count;
#endif // Debug            
            orientationCount = 0;
            try {
                ballot = JsonConvert.DeserializeObject<BallotJSON>(result);
                unvotedImgs = new List<BallotCandidateJSON>(ballot.ballots);
                // handle category first.
                //challengeLabelP.Text = "Category: " + ballot.category.description;
                if (ballot.category.description == null) {
                    bool falseBreak = true;
                }
                challengeLabelP.Text = ballot.category.description;
                GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
                // now handle ballot
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString generating images");
                foreach (BallotCandidateJSON candidate in ballot.ballots) {
                    iiBitmapView img = setupImgFromBallotCandidate(candidate);
                    ballotImgsP.Add(img);
                }
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString image generation done");
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString orientationCount: " +orientationCount);
                if (orientationCount == 2) {
                    checkImgOrderAndReorderAsNeeded();
                }

                if ((ballot.ballots.Count < 4) || (ballotImgsP.Count != ballot.ballots.Count)) {
                    Debug.WriteLine("DHB:JudgingContentPage:processBallotString wtf. mismatch in drawing.");
                }
                if ((ballot==null) || (ballot.ballots.Count == 0)) {
                    // bad ballot. fire a dequeue
                    if (DequeueBallotRequest != null) {
                        DequeueBallotRequest(this, eDummy);
                    }
                }
#if DEBUG
                int checkFull = ballotImgsP.Count;
                // not guaranteed 4 back anymore.
                //Debug.Assert(ballotImgsP.Count == 4, "less than 4 ballots sent");
#endif // Debug            
            } catch (Exception e) {
                // probably thrown by Deserialize.
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString exception:"+e.ToString());
                //Image imgX = setupImgFromBallotCandidate(ballot.ballots[0]);
                //imgX = setupImgFromBallotCandidate(ballot.ballots[1]);
                //imgX = setupImgFromBallotCandidate(ballot.ballots[2]);
                //imgX = setupImgFromBallotCandidate(ballot.ballots[3]);
            }

            // These are both built in AdjustContentToRotation
            //buildPortraitView();
            //buildLandscapeView();

            // new images, content needs to be updated.
            AdjustContentToRotation();
            Debug.WriteLine("DHB:JudgingContentPage:processBallotString complete");
        }

        /// <summary>
        /// This gets the image streams from the server. It does NOT builds them as this is a statis async fcn.
        /// try to do this by staying away from Bitmap (android) and UIImage (iOS).
        /// may not be possible as it seems the abstraction layer Image does not have a way to build the image 
        /// from bytes.
        ///    my guess is I should focus on streams...
        /// Queueing and dequeuing is handled in the calling event handler.
        /// </summary>
        /// <returns></returns>
        static async Task<string> requestBallotPicsAsync() {
            Debug.WriteLine("DHB:JudgingContentPage:requestBallotPicsAsync started");
            string result = "fail";
            try {
                HttpClient client = new HttpClient();
                string ballotURL = GlobalStatusSingleton.activeURL + BALLOT;
                    //+ "?category_id=" + GlobalStatusSingleton.votingCategoryId;  // no longer needed? return a random category.
                HttpRequestMessage ballotRequest = new HttpRequestMessage(HttpMethod.Get, ballotURL);
                // Authentication currently turned off on Harry's end.
                //ballotRequest.Headers.Add("Authorization", "JWT "+GlobalStatusSingleton.authToken.accessToken);
                ballotRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage ballotResult = await client.SendAsync(ballotRequest);
                if (ballotResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = await ballotResult.Content.ReadAsStringAsync();
                    Debug.WriteLine("DHB:JudgingContentPage:requestBallotPicsAsync http read complete");
                } else {
                    // no ok back from the server! gahh.
                    // @todo handle what happens when I request with an old/non-voting category_id
                    // @todo test this condition (old/non-voting category err).
                    Debug.WriteLine("DHB:JudgingContentPage:requestBallotPicsAsync bad status code: "+ballotResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:JudgingContentPage:requestVoteAsync:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:JudgingContentPage:requestBallotPicsAsync complete");
            return result;
        }
        /////
        /////
        ///// End Loading section
        /////
        /////

        // Voting.
        protected virtual void OnVote(object sender, EventArgs e) {
            //SingleVoteGeneratesBallot(sender, e);
            MultiVoteGeneratesBallot(sender, e);
        }

        private int findUnallocatedBid(List<VoteJSON> votes) {
            return 1;
        }

        protected void determineColAndRowFromIndex(int index, ref int col, ref int row) {
            if (index == 0) {
                col = 2;
                row = 6;
            } else if (index == 1) {
                col = 5;
                row = 6;
            } else if (index == 2) {
                col = 2;
                row = 12;
            } else if (index == 3) {
                col = 5;
                row = 12;
            }
        }

        /// <summary>
        /// This function adds the 3 and 4 to the images.
        /// </summary>
        /// <param name="votes"></param>
        /// <param name="penultimateSelectedIndex"></param>
        /// <param name="ultimateSelectedIndex"></param>
        private void UpdateUIForFinalVote(List<VoteJSON> votes, int penultimateSelectedIndex, int ultimateSelectedIndex) {
            /*
            SKBitmap baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                    ballot.ballots[penultimateSelectedIndex].imgStr, (ExifOrientation)ballot.ballots[penultimateSelectedIndex].orientation);
            SKImage mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[rankImages.Count - 2]);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[penultimateSelectedIndex], mergedImage);
            // see if a new object eliminates flicker.

            baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                ballot.ballots[ultimateSelectedIndex].imgStr, (ExifOrientation)ballot.ballots[ultimateSelectedIndex].orientation);
            mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[rankImages.Count - 1]);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[ultimateSelectedIndex], mergedImage);
            */
            int col =0;
            int row =0;
            determineColAndRowFromIndex(penultimateSelectedIndex, ref col, ref row);
            portraitView.Children.Add(rankImages[rankImages.Count - 2], col, row);
            Grid.SetRowSpan(rankImages[rankImages.Count - 2], 2);
            determineColAndRowFromIndex(ultimateSelectedIndex, ref col, ref row);
            portraitView.Children.Add(rankImages[rankImages.Count - 1], col, row);
            Grid.SetRowSpan(rankImages[rankImages.Count - 1], 2);

            foreach (iiBitmapView img in ballotImgsP) {
                img.IsEnabled = false;
            }
            Debug.WriteLine("DHB:JudgingContentPage:UpdateUIForFinalVote done");
        }

        /*
        protected void UpdateUIForFinalVoteOld(List<VoteJSON> votes, int penultimateSelectedIndex, int ultimateSelectedIndex) {
            // not sure how I do indexing...
            //ClearContent(firstSelectedIndex);
            //Device.BeginInvokeOnMainThread(() => {
            SKBitmap baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                    ballot.ballots[penultimateSelectedIndex].imgStr, (ExifOrientation)ballot.ballots[penultimateSelectedIndex].orientation);
            SKImage mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[rankImages.Count - 2]);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[penultimateSelectedIndex], mergedImage);
            // see if a new object eliminates flicker.
            // also needs a buildUI at the end to update the layout objects with the new info...
            //ballotImgsP[penultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
            //ballotImgsL[penultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);

            baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                ballot.ballots[ultimateSelectedIndex].imgStr, (ExifOrientation)ballot.ballots[ultimateSelectedIndex].orientation);
            mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[rankImages.Count - 1]);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[ultimateSelectedIndex], mergedImage);

            //ballotImgsP[ultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
            //ballotImgsL[ultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);

            foreach (Image img in ballotImgsP) {
                    img.IsEnabled = false;
                // clearing these prevents ui from updating for some reason.
                // so instead, I catch a tap and ignore it.
                    //img.GestureRecognizers.Clear();
            }
                // this is very expensive and may be cause of flicker...
                // what happens if i don't call this?
                // much faster, and images are updating (this is updating the images via the updatexamarinfromskimage function case)
                //AdjustContentToRotation();
            //});
            Debug.WriteLine("DHB:JudgingContentPage:UpdateUIForFinalVote done");
        }
        */
        protected async virtual void SingleVoteGeneratesBallot(object sender, EventArgs e) {
            if (votes == null) {
                votes = new VotesJSON();
                votes.votes = new List<VoteJSON>();
            } else {
                votes.votes.Clear();
            }

            // ballots may have been cleared and this can be a dbl tap registration.
            // in which case, ignore.
            if (ballot.ballots.Count == 0) { return; }
            bool found = false;
            int index = 0;
            int selectionId = 0;
            long bid = -1;
            // ballotImgsP and L have same meta info, so only need this once.
            // but I do need to search the correct one to id the sender.
            var searchImgs = ballotImgsP;
            foreach (iiBitmapView img in searchImgs) {
                if (img == sender) {
                    found = true;
                    bid = ballot.ballots[index].bidId;
                    selectionId = index;
                } else {
                    index++;
                }
            }
#if DEBUG
            if (found == false) {
                // This can actually happen after voting has finished.
                // just throw a debugging statement.
                //throw new Exception("A button clicked on an image not in my ballots.");
                Debug.WriteLine("A button clicked on an image not in my ballots.");
            }
#endif
            VoteJSON vote = new ImageImprov.VoteJSON();
            vote.bid = bid;
            vote.vote = votes.votes.Count + 1;
            vote.like = ballot.ballots[index].isLiked ? "1" : "0";
            vote.offensive = ballot.ballots[index].isFlagged? "1" : "0";
            votes.votes.Add(vote);

            string jsonQuery = JsonConvert.SerializeObject(votes);
            string origText = challengeLabelP.Text;
            challengeLabelP.Text = "Vote submitted, loading new ballot";
            GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
            ClearContent(selectionId);
            string result = await requestVoteAsync(jsonQuery);
            if (result.Equals("fail")) {
                // @todo This fail case is untested code. Does the UI come back?
                challengeLabelP.Text = "Connection failed. Please revote";
                GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
                AdjustContentToRotation();
                //} else ("no ballot created") { 
            } else {
                // only clear on success
                ClearContent();
                challengeLabelP.Text = origText;
                GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
                processBallotString(result);
            }
        }

        /// <summary>
        /// Helper function for MultiVoteGeneratesBallot and OnDoubleClick
        /// Given the tapped image, find the index in the ballot.ballots array of that image and sets it to the selectionId variable.
        /// And grabs the bid at the same time for easy use.
        /// bleh, setting so many things this turned out to be more trouble than worth...
        /// </summary>
        /// <returns></returns>
        private bool findSelectionIndexAndBid(object sender, ref int selectionId, ref long bid, ref BallotCandidateJSON votedOnCandidate, IList<iiBitmapView> searchGroup) {
            bool found = false;
            int index = 0;
            // ballotImgsP and L have same meta info, so only need this once.
            // but I do need to search the correct one to id the sender.
            var searchImgs = ballotImgsP;
            //foreach (iiBitmapView img in searchImgs) {
            //foreach (iiBitmapView img in voteBoxes) { 
            // if i tap the hitbox, need that index, if i tap the like, need that box, if i tap the img, need that one.
            foreach (iiBitmapView img in searchGroup) {
                if (img == sender) {
                    found = true;
                    votedOnCandidate = ballot.ballots[index];
                    bid = ballot.ballots[index].bidId;
                    selectionId = index;
                    break; // should break out of the foreach.
                } else {
                    index++;
                }
            }
            return found;
        }

        private bool votedOn(long bid, ref int voteNum) {
            bool res = false;
            voteNum = 0;  // votes.votes are guaranteed to be in vote order.
            if ((votes != null) && (votes.votes != null)) {
                foreach (VoteJSON vote in votes.votes) {
                    if (vote.bid == bid) {
                        res = true;
                        break;
                    } else {
                        voteNum++;
                    }
                }
            }
            return res;
        }


        private void rebuildAllImagesWithVotes() {
            Device.BeginInvokeOnMainThread(() =>
            {
                // clear off all the old rank images
                for (int rankImgI = 0; rankImgI < rankImages.Count; rankImgI++) {
                    portraitView.Children.Remove(rankImages[rankImgI]);
                }

                int ballotIndex = 0;
                int voteNum = -1;
                int col = 0;
                int row = 0;

                // now add them back in.
                foreach (BallotCandidateJSON candidate in ballot.ballots) {
                    if (votedOn(candidate.bidId, ref voteNum)) {
                        determineColAndRowFromIndex(ballotIndex, ref col, ref row);
                        portraitView.Children.Add(rankImages[voteNum], col, row);
                        Grid.SetRowSpan(rankImages[voteNum],2);
                    } else {
                        // I should already have the image loaded, right?
                        //SKImage img = SKImage.FromBitmap(GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(candidate.imgStr, (ExifOrientation)candidate.orientation));
                        //GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[ballotIndex], img);
                    }
                    ballotIndex++;
                }
            });
        }

        /// <summary>
        /// Check for 4 votes already, as Enabled=false seems to fail... (maybe I need to turn of the gestures?)
        /// Checks to see if this image was already voted on.
        /// If yes, processes the uncheck.
        /// If no, returns a false so that MultiVoteGeneratesBallot can handle the selection.
        /// </summary>
        /// <param name="selectionId"></param>
        /// <param name="bid"></param>
        /// <param name="votedOnCandidate"></param>
        /// <returns></returns>
        private bool uncheckCase(int selectionId, long bid, BallotCandidateJSON votedOnCandidate) {
            int removePosn = 0;
            bool res = votedOn(bid, ref removePosn);
            if (res) {
                //if (Device.OS == TargetPlatform.iOS) {
                //    checkPosition = removePosn;
                //    Debug.WriteLine("DHB:JudgingContentPage:uncheckCase set checkposition to " + removePosn);
                //}

                // process the uncheck.
                //    remove this bid from the votes object.
                //    add back into the unvoted images list.
                //    update the vote_id on the remaining votes
                //    remove the numbers from the images
                //    renumber all images
                votes.votes.RemoveAt(removePosn);
                unvotedImgs.Add(votedOnCandidate);
                for (int i=removePosn;i<votes.votes.Count;i++) {
                    votes.votes[i].vote--;
                }
                // brute force the images and do all four...
                rebuildAllImagesWithVotes();
            }
            return res;
        }

        protected async virtual void MultiVoteGeneratesBallot(object sender, EventArgs e) {
            if (votes == null) {
                votes = new VotesJSON();
                votes.votes = new List<VoteJSON>();
            }
            if (votes.votes.Count == ballot.ballots.Count) {
                // everything already voted on. ignore the tap.
                return;
            }

            // only needed for iOS.
            //if (Device.OS == TargetPlatform.iOS) {
            //    lastClicked = (iiBitmapView)sender;
            //}

            // ballots may have been cleared and this can be a dbl tap registration.
            // in which case, ignore.
            if (ballot.ballots.Count == 0) { return; }
            int selectionId = 0;
            long bid = -1;
            BallotCandidateJSON votedOnCandidate = null;
            bool found = findSelectionIndexAndBid(sender, ref selectionId, ref bid, ref votedOnCandidate, voteBoxes);

#if DEBUG
            if (found == false) {
                //throw new Exception("A button clicked on an image not in my ballots.");
                Debug.WriteLine("A button clicked on an image not in my ballots.");
                return;
            }
#endif
            if (uncheckCase(selectionId, bid, votedOnCandidate)) {

            } else {
                VoteJSON vote = new ImageImprov.VoteJSON();
                vote.bid = bid;
                vote.vote = votes.votes.Count + 1;
                if (vote.vote == 1) {
                    // first selected save the selectionId;
                    firstSelectedIndex = selectionId;
                }
                if (votedOnCandidate != null) {
                    unvotedImgs.Remove(votedOnCandidate);
                    vote.like = votedOnCandidate.isLiked?"1":"0";
                    vote.offensive = votedOnCandidate.isFlagged?"1":"0";
                }
                votes.votes.Add(vote);

                // Is this the last img to be voted on (on the ui second to last as fully defined order at that point)
                // if so, send the votes in.
                if (vote.vote == (ballot.ballots.Count - 1)) {
#if DEBUG
                    Debug.Assert(unvotedImgs.Count == 1, "We dont have just one image left!!");
#endif
                    vote = new ImageImprov.VoteJSON();
                    // This has to be the only one left.
                    vote.bid = unvotedImgs[0].bidId;
                    vote.vote = votes.votes.Count + 1;
                    vote.like = unvotedImgs[0].isLiked ? "1" : "0";
                    vote.offensive = unvotedImgs[0].isFlagged?"1":"0";
                    votes.votes.Add(vote);

                    string jsonQuery = JsonConvert.SerializeObject(votes);
                    string origText = challengeLabelP.Text;
                    challengeLabelP.Text = "Vote submitted, loading new ballot";
                    GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);

                    UpdateUIForFinalVote(votes.votes, selectionId, getIndexOfBid(vote.bid));
                    /* the result of this is there is no ui update. that's because need to update what the ui elements point to
                     * EVEN when I put the key code inside a Device.InvokeMainThread block.
                    new Task(() =>
                    {
                        UpdateUIForFinalVote(votes.votes, selectionId, getIndexOfBid(vote.bid));
                    });
                    */
                    /* still flickers. not confident I'm avoiding race conditions either...
                    var t = Task.Run(() => { UpdateUIForFinalVote(votes.votes, selectionId, getIndexOfBid(vote.bid)); });
                    t.Wait();
                    buildUI();
                    */
                    lightbulbRow.incrementLightbulbCounter();
                    await Task.Delay(500);

                    // Ok. I want to be running the request right now!
                    // I want to wait a random amount of time then yank 1 from the queue right away.

                    // What I can easily do is:
                    //   yank one from the queue and fire the request.

                    // The solution is to fire of a pull from queue event while this trundles along...
                    // Or. Fire a load ballot event, while this pulls from queue.
                    // No, because load ballot doesn't send a vote.
                    // right now, do nothing.
                    if (DequeueBallotRequest != null) {
                        DequeueBallotRequest(this, eDummy);
                    }

                    // keep refiring until success.
                    string result = "fail";
                    while (result.Equals("fail")) {
                        Debug.WriteLine("DHB:JudgingContentPage:MultiVoteGeneratesBallot: vote json: " + jsonQuery);
                        result = await requestVoteAsync(jsonQuery);
                        if (result.Equals("fail")) {
                            // @todo This fail case is untested code. Does the UI come back?
                            if (preloadedBallots.Count == 0) {
                                challengeLabelP.Text = "No connection. Awaiting connection for more ballots.";
                                GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, portraitView.Width, portraitView.Height/10.0);
                                AdjustContentToRotation();
                            }
                        } else {
                            /* moved to queue processing.
                            // only clear on success
                            ClearContent();
                            challengeLabelP.Text = origText;
                            challengeLabelL.Text = origText;
                            processBallotString(result);
                            */
                            preloadedBallots.Enqueue(result);
                        }
                    }
                } else {
                    int col = 0;
                    int row = 0;
                    Debug.WriteLine("DHB:JudgingContentPage:MultiVoteGeneratesBallot updating selection:" + selectionId);
                    // removing and adding the voteBox is for apple as the topmost widget always consumes a tap, even if they do nothing with it.
                    portraitView.Children.Remove(voteBoxes[selectionId]);
                    determineColAndRowFromIndex(selectionId, ref col, ref row);
                    portraitView.Children.Add(rankImages[vote.vote - 1], col, row);
                    Grid.SetRowSpan(rankImages[vote.vote - 1], 2);
                    portraitView.Children.Add(voteBoxes[selectionId], col, row);
                    Grid.SetRowSpan(voteBoxes[selectionId], 2);
                }
            }
        }

        // sends the vote in and waits for a new ballot.
        static async Task<string> requestVoteAsync(string jsonQuery) {
            Debug.WriteLine("DHB:JudgingContentPage:requestVoteAsync start");
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
                    Debug.WriteLine("DHB:JudgingContentPage:requestVoteAsync vote result recvd");
                }
                voteResult.Dispose();
                request.Dispose();
                client.Dispose();
            } catch (System.Net.WebException err) {
                //result = "exception";
                // web failure. keep the msg as a simple fail for correct onVote processing
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:JudgingContentPage:requestVoteAsync:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:JudgingContentPage:requestVoteAsync end");
            return result;
        }

        /// <summary>
        /// Given a bidId, returns it's index in the current ballot array.
        /// </summary>
        /// <param name="bidId"></param>
        /// <returns>The 0 based index of the bid, or -1 if not found</returns>
        private int getIndexOfBid(long bidId) {
            int found = -1;
            int index = 0;
            foreach (BallotCandidateJSON candidate in ballot.ballots) {
                if (candidate.bidId == bidId) {
                    found = index;
                    break;
                } else {
                    index++;
                }
            }
            return found;
        }

        // expose some members for serialization/deserialization on startup/shutdown.
        public BallotJSON GetBallot() {
            return ballot;
        }
        public void SetBallot(string storedBallot) {
            // set the ballot here.
            lock (uiLock) {
                // dont have categories yet. maybe an invalid ballot...
                // what should the ui do?
                //
                // doing nothing till i get behavior feedback.
            }
        }

        public Queue<string> GetBallotQueue() {
            return preloadedBallots;
        }
        public void SetPreloadedBallots(Queue<string> previouslyStoredBallots) {
            // check each ballot to see if it is still a current category.
            // if it is, add to queue.

            // bleh, i'm not guaranteed to have the categories at this point.
            // so... set preloadedBallots and let it get processed once categories come in.
            lock (uiLock) {
                foreach (string s in previouslyStoredBallots) {
                    preloadedBallots.Enqueue(s);
                }
            }
        }

        //
        //
        // Start Special Handling for Ballots that come from Photo submission
        //
        //
        /// <summary>
        /// Public as the event is generated outside this class and sent to me. (the camera page).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void OnLoadBallotFromSubmission(object sender, EventArgs e) {
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotFromSubmission");
            // This should be coming from the camera page.
            // The eventargs should hold a ballot string.
            string inBallot = ((BallotFromPhotoSubmissionEventArgs)e).ballotString;
            // I'm competing with DeQueue ballot string.
            Device.BeginInvokeOnMainThread(() =>
            {
                // I want to make sure I'm firing and that dequeue is NOT in another thread.
                lock (ballot) {
                    // make sure we have a valid BallotJSON first...
                    BallotJSON testParse = JsonConvert.DeserializeObject<BallotJSON>(inBallot);
                    if ((testParse != null) && (testParse.ballots != null)) {
                        ClearContent();
                        try {
                            BallotJSON currentBallot = GetBallot();
                            Queue<string> qStart = new Queue<string>();
                            qStart.Enqueue(JsonConvert.SerializeObject(currentBallot));
                            qStart.Concat(preloadedBallots);
                            preloadedBallots = qStart;
                            processBallotString(inBallot);
                        } catch (Exception ex) {
                            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotFromSubmission exception:" + ex.ToString());
                        }
                    }
                }
            });
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotFromSubmission moving loc.");
            // let's just come here for starters.  This works! Booya
            ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
        }
        //
        //
        // End Special Handling for Ballots that come from Photo submission
        //
        //

        /// <summary>
        /// Called from OnCategoryLoad after successfully loading categories.
        /// We want to make sure the future categories all have notifications posted.
        /// </summary>
        protected void postCategoryNotifications() {
            string title = "Image Improv";
            string msg1_1 = "Time to take your best ";
            string msg1_2 = " pic!";
            string msg2_1 = "Only 5 hrs left for ";
            string msg2_2 = " pics.";

            foreach (CategoryJSON futureCat in GlobalStatusSingleton.pendingCategories) {
                string msg = msg1_1 + futureCat.description + msg1_2;
                DateTime postTime = futureCat.start.AddHours(5);
                PlatformSpecificCalls.SetupNotification(title, msg, postTime, futureCat.categoryId);
                msg = msg2_1 + futureCat.description + msg2_2;
                postTime = futureCat.start.AddHours(19);
                PlatformSpecificCalls.SetupNotification(title, msg, postTime, -futureCat.categoryId);
                Debug.WriteLine("DHB:JudgingContentPage:postCategoryNotificaiton posted for category " + futureCat.description);
            }
        }

        // used by keypagenavigator.
        // returns to the voting page.
        public void goHome() {
            Content = portraitView;
        }

        // the zoom callback.
        public void returnToCaller() {
            Content = portraitView;
        }
    } // class
} // namespace

