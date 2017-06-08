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
    class JudgingContentPage : ContentPage {
        public static string LOAD_FAILURE = "No open voting category currently available.";

        // A dummy object for controlling lock on ui resources
        static readonly object uiLock = new object();

        Grid portraitView = null;
        Grid landscapeView = null;
        KeyPageNavigator defaultNavigationButtonsP;
        KeyPageNavigator defaultNavigationButtonsL;

        // Yesterday's challenge 
        //< challengeLabel
        Label challengeLabelP = new Label
        {
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            BackgroundColor = Color.FromRgb(252, 213, 21),
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };
        Label challengeLabelL = new Label
        {
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            BackgroundColor = Color.FromRgb(252, 213, 21),
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
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
        IList<Image> ballotImgsP = null;
        IList<Image> ballotImgsL = null;

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
        EventArgs eDummy = null;

        // the http command name to request category information to determine what we can vote on.
        const string CATEGORY = "category";
        // the command name to request a ballot when voting.
        const string BALLOT = "ballot";
        
        // When third is selected, there is enough info to rank all 4 images.
        const int PENULTIMATE_BALLOT_SELECTED = 3;

        // interesting. doing this somehow sets up a double tap (and therefore an error due to data clearing)
        //TapGestureRecognizer tapGesture;

        // tracks the number of pictures in each orientation so we know how to display.
        int orientationCount;

        // tracks the votes that have been made until the final vote.
        VotesJSON votes = null;
        int firstSelectedIndex;
        // tracks which ballots have not yet received a vote in the multi-img vote voting scenario.
        List<BallotCandidateJSON> unvotedImgs;

        //
        //   BEGIN Variables related/needed for images to place image rankings and backgrounds on screen.
        //
        AbsoluteLayout layoutP;  // this lets us place a background image on the screen.
        AbsoluteLayout layoutL;  // this lets us place a background image on the screen.
        List<SKBitmap> rankImages = new List<SKBitmap>();
        Assembly assembly = null;
        Image backgroundImgP = null;
        Image backgroundImgL = null;
        string[] rankFilenames = new string[] { "ImageImprov.IconImages.first.png", "ImageImprov.IconImages.second.png",
                "ImageImprov.IconImages.third.png", "ImageImprov.IconImages.fourth.png"};
        string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place image rankings and backgrounds on screen.
        // 

        public JudgingContentPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;
            ballot = new BallotJSON();

            preloadedBallots = new Queue<string>();

            ballotImgsP = new List<Image>();
            ballotImgsL = new List<Image>();
            //buildPortraitView();
            //buildLandscapeView();

            // set myself up to listen for the loading events...
            this.LoadChallengeName += new LoadChallengeNameEventHandler(OnLoadChallengeName);
            this.LoadBallotPics += new LoadBallotPicsEventHandler(OnLoadBallotPics);
            this.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(LoadBallotPics);
            this.DequeueBallotRequest += new DequeueBallotRequestEventHandler(OnDequeueBallotRequest);

            // and to listen for vote sends; done as event so easy to process async.
            this.Vote += new EventHandler(OnVote);

            eDummy = new EventArgs();

            // used to merge with the base image to show the ranking number.
            buildRankImages();

            // Do I have a persisted ballot ready and waiting for me?
            managePersistedBallot(this, eDummy);
            // Do I have a persisted ballot ready and waiting for me?
            buildUI();
        }

        protected void buildRankImages() {
            foreach (string filename in rankFilenames) {
                rankImages.Add(GlobalSingletonHelpers.loadSKBitmapFromResourceName(filename, assembly));
            }
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            // right now, do nothing.
            if (LoadChallengeName != null) {
                LoadChallengeName(this, eDummy);
            }
        }

        double widthCheck = 0;
        double heightCheck = 0;

        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height);
            // ensure we have a change before doing anything...
            if ((widthCheck != width) || (heightCheck != height)) {
                widthCheck = width;
                heightCheck = height;
                try {
                    Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated start");
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated inside lock");
                        if ((width > height) && (height > 0) && (landscapeView != null)) {
                            GlobalStatusSingleton.inPortraitMode = false;
                            if (backgroundImgL == null) {
                                backgroundImgL = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Width, (int)Height);
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
                            GlobalStatusSingleton.inPortraitMode = true;
                            if ((backgroundImgP == null) && (width > 0) && (portraitView != null)) {
                                backgroundImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Width, (int)Height);
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
                            }
                        }
                        Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated lock released");
                    });
                    Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated end");
                } catch (Exception e) {
                    Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated:Exception");
                    Debug.WriteLine(e.ToString());
                }
            }
        }

        /*
        protected override void OnSizeAllocated(double width, double height) {
            Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated start");
            base.OnSizeAllocated(width, height);
            if (width > height) {
                GlobalStatusSingleton.inPortraitMode = false;
                Content = landscapeView;
            } else {
                GlobalStatusSingleton.inPortraitMode = true;
                Content = portraitView;
            }
            Debug.WriteLine("DHB:JudgingContentPage:OnSizeAllocated complete");
        }
        */

        /* I have better luck with onsizeallocated.
        void OnPageSizeChanged(Object sender, EventArgs args) {
            if ((backgroundImgP == null) && (Width>0.0) && (Height>0.0)) {
                if (Height > Width) {
                    backgroundImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Width, (int)Height);
                    backgroundImgL = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Height, (int)Width);
                } else {
                    backgroundImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Height, (int)Width);
                    backgroundImgL = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Width, (int)Height);
                }
                layoutP = new AbsoluteLayout
                {
                    Children = {
                        { backgroundImgP, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All },
                        { portraitView, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All }
                    }
                };
                layoutL = new AbsoluteLayout
                {
                    Children = {
                        { backgroundImgL, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All },
                        { landscapeView, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All }
                    }
                };
                //Content = layout;
                OnSizeAllocated(Width, Height);
            }
        }
        */

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
            ballotImgsL.Clear();  // does Content update? No
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
        private void highlightCorrectImg(IList<Image> ballotImgs, int index) {
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
            landscapeView.IsEnabled = false;
            ballot.Clear();

            highlightCorrectImg(ballotImgsP, index);
            highlightCorrectImg(ballotImgsL, index);
        }

        public int buildUI() {
            int res = 0;
            int res2 = 0;
            //Device.BeginInvokeOnMainThread(() =>
            //{
                res = buildPortraitView();
                res2 = buildLandscapeView();
                //OnSizeAllocated(Width, Height);
            //});
            return ((res<res2)?res:res2);
        }

        /// <summary>
        /// Builds/updates the portrait view
        /// </summary>
        /// <returns>1 on success, -1 if there are the wrong number of ballot imgs.</returns>
        private int buildPortraitView() {
            Debug.WriteLine("DHB:JudgingContentPage:buildPortraitView begin");
            // ignoring orientation count for now.
            // the current implemented case is orientationCount == 0. (displays as a stack)
            // There are two other options... 
            //    orientationCount == 2 - displays as 2xstack or stackx2 per first img orientation
            //    orientationCount == 4 - display as a 2x2 grid
            int result = 1;
            try {
                // all my elements are already members...
                if (portraitView == null) {
                    portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = Color.Transparent };
                } else {
                    // flush the old children.
                    portraitView.Children.Clear();
                    portraitView.IsEnabled = true;
                }
                if (defaultNavigationButtonsP == null) {
                    defaultNavigationButtonsP = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
                }

                // ok. Everything has been initialized. So now I just need to decide where to put it.
                if (ballotImgsP.Count > 0) {
                    if (orientationCount < 2) {
                        result = buildFourLandscapeImgPortraitView();
                    } else if (orientationCount > 2) {
                        result = buildFourPortraitImgPortraitView();
                    } else {
                        result = buildTwoXTwoImgPortraitView();
                    }
                } else {
                    // Note: This is reached on ctor call, so don't put an assert here.
                    result = -1;
                    portraitView.RowDefinitions.Clear();
                    portraitView.ColumnDefinitions.Clear();
                    for (int i = 0; i < 20; i++) {
                        portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    }

                    // want to show the instruction!
                    loadInstructions();
                    if (ballotImgsP.Count > 0) {
                        portraitView.Children.Add(ballotImgsP[0], 0, 0);
                        Grid.SetRowSpan(ballotImgsP[0], 4);
                    }

                    if (ballotImgsP.Count > 1) {
                        portraitView.Children.Add(ballotImgsP[1], 0, 4);  // col, row format
                        Grid.SetRowSpan(ballotImgsP[1], 4);
                    }

                    if (ballotImgsP.Count > 2) {
                        portraitView.Children.Add(ballotImgsP[2], 0, 10);  // col, row format
                        Grid.SetRowSpan(ballotImgsP[2], 4);
                    }

                    if (ballotImgsP.Count > 3) {
                        portraitView.Children.Add(ballotImgsP[3], 0, 14);  // col, row format
                        Grid.SetRowSpan(ballotImgsP[3], 4);
                    }
                    
#if DEBUG
                    challengeLabelP.Text += " no image case";
#endif

                    portraitView.Children.Add(challengeLabelP, 0, 8);
                    Grid.SetColumnSpan(challengeLabelP, 1);
                    Grid.SetRowSpan(challengeLabelP, 2);
                    portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
                    Grid.SetColumnSpan(defaultNavigationButtonsP, 1);
                    Grid.SetRowSpan(defaultNavigationButtonsP, 2);
                }
            } catch (Exception e) {
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
            for (int i = 0; i < 20; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // I can add none, but if i add one, then i just have 1. So here's 2. :)
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            portraitView.Children.Add(ballotImgsP[0], 0, 0);
            Grid.SetRowSpan(ballotImgsP[0], 8);

            portraitView.Children.Add(ballotImgsP[1], 1, 0);  // col, row format
            Grid.SetRowSpan(ballotImgsP[1], 8);

            portraitView.Children.Add(ballotImgsP[2], 0, 10);  // col, row format
            Grid.SetRowSpan(ballotImgsP[2], 8);

            portraitView.Children.Add(ballotImgsP[3], 1, 10);  // col, row format
            Grid.SetRowSpan(ballotImgsP[3], 8);

#if DEBUG
            challengeLabelP.Text += " 4P_P case";
#endif

            portraitView.Children.Add(challengeLabelP, 0, 8);
            Grid.SetColumnSpan(challengeLabelP, 2);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 2);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);
            return 1;
        }

        /// <summary>
        /// Helper function for buildPortraitView that constructs the layout when the current ballot has 4
        /// images exif defines as landscape images.
        /// </summary>
        /// <returns></returns>
        public int buildFourLandscapeImgPortraitView() {
            // setup rows.
            portraitView.RowDefinitions.Clear();
            portraitView.ColumnDefinitions.Clear();
            for (int i = 0; i < 20; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            portraitView.Children.Add(ballotImgsP[0], 0, 0);
            Grid.SetRowSpan(ballotImgsP[0], 4);

            portraitView.Children.Add(ballotImgsP[1], 0, 4);  // col, row format
            Grid.SetRowSpan(ballotImgsP[1], 4);

            portraitView.Children.Add(ballotImgsP[2], 0, 10);  // col, row format
            Grid.SetRowSpan(ballotImgsP[2], 4);

            portraitView.Children.Add(ballotImgsP[3], 0, 14);  // col, row format
            Grid.SetRowSpan(ballotImgsP[3], 4);

#if DEBUG
            challengeLabelP.Text += " 4L_P case";
#endif

            portraitView.Children.Add(challengeLabelP, 0, 8);
            Grid.SetRowSpan(challengeLabelP, 2);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 1);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);

            return 1;
        }

        /// <summary>
        /// Helper function for buildPortraitView that constructs the layout when the current ballot has 2
        /// images exif defines as portrait images and 2 it defines as landscape.
        /// </summary>
        /// NOTE UNTESTED STILL. OUT OF IMGS and NO AUTO-ROUND SWITCH
        /// <returns></returns>
        public int buildTwoXTwoImgPortraitView() {
            // setup rows and cols.
            // regardless, 26 rows, 2 cols
            portraitView.RowDefinitions.Clear();
            portraitView.ColumnDefinitions.Clear();
            for (int i = 0; i < 20; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // I can add none, but if i add one, then i just have 1. So here's 2. :)
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // am I in two landscape on top or two portrait on top?
            if (ballot.ballots[0].isPortrait == BallotCandidateJSON.PORTRAIT) {
                // portrait on top
                portraitView.Children.Add(ballotImgsP[0], 0, 0);
                Grid.SetRowSpan(ballotImgsP[0], 8);

                portraitView.Children.Add(ballotImgsP[1], 1, 0);  // col, row format
                Grid.SetRowSpan(ballotImgsP[1], 8);

                portraitView.Children.Add(ballotImgsP[2], 0, 10);  // col, row format
                Grid.SetRowSpan(ballotImgsP[2], 4);
                Grid.SetColumnSpan(ballotImgsP[2], 2);

                portraitView.Children.Add(ballotImgsP[3], 0, 14);  // col, row format
                Grid.SetRowSpan(ballotImgsP[3], 4);
                Grid.SetColumnSpan(ballotImgsP[3], 2);
            } else {
                // landscape on top
                portraitView.Children.Add(ballotImgsP[0], 0, 0);
                Grid.SetRowSpan(ballotImgsP[0], 4);
                Grid.SetColumnSpan(ballotImgsP[0], 2);

                portraitView.Children.Add(ballotImgsP[1], 0, 4);  // col, row format
                Grid.SetRowSpan(ballotImgsP[1], 4);
                Grid.SetColumnSpan(ballotImgsP[1], 2);

                portraitView.Children.Add(ballotImgsP[2], 0, 10);  // col, row format
                Grid.SetRowSpan(ballotImgsP[2], 8);

                portraitView.Children.Add(ballotImgsP[3], 1, 10);  // col, row format
                Grid.SetRowSpan(ballotImgsP[3], 8);
            }
#if DEBUG
            challengeLabelP.Text += " 2x2P case";
#endif

            portraitView.Children.Add(challengeLabelP, 0, 8);
            Grid.SetColumnSpan(challengeLabelP, 2);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 2);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);


            return 1;
        }

        public int buildLandscapeView() {
            Debug.WriteLine("DHB:JudgingContentPage:buildLandscapeView begin");
            int result = 1;
            try {
                // the current implemented case is orientationCount == 0. (displays as a 2x2)
                // There are two other options... 
                //    orientationCount == 2 - displays as 2xstack or stackx2 per first img orientation
                //    orientationCount == 4 - display as a 4x1 horizontally aligned portraits.
                if (defaultNavigationButtonsL == null) {
                    defaultNavigationButtonsL = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
                }

                // all my elements are already members...
                if (landscapeView == null) {
                    landscapeView = new Grid { ColumnSpacing = 0, RowSpacing = 0, BackgroundColor = Color.Transparent, };
                } else {
                    // flush the old children.
                    landscapeView.Children.Clear();
                    landscapeView.IsEnabled = true;
                }
                // should I be flushing the children each time???
                if (ballotImgsL.Count > 0) {
                    if (orientationCount < 2) {
                        // landscape
                        result = buildFourLandscapeImgLandscapeView();
                    } else if (orientationCount > 2) {
                        // portrait
                        result = buildFourPortraitImgLandscapeView();
                    } else {
                        result = buildTwoXTwoImgLandscapeView();
                    }
                } else {
                    // Note: This is reached on ctor call, so don't put an assert here.
                    result = -1;
                    landscapeView.RowDefinitions.Clear();
                    landscapeView.ColumnDefinitions.Clear();
                    for (int i = 0; i < 10; i++) {
                        landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    }
                    for (int i = 0; i < 2; i++) {
                        landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    }
                    /*
                    if (ballotImgsL.Count > 0) {
                        landscapeView.Children.Add(ballotImgsL[0], 0, 0);
                        Grid.SetRowSpan(ballotImgsL[0], 12);
                    }

                    if (ballotImgsL.Count > 1) {
                        landscapeView.Children.Add(ballotImgsL[1], 1, 0);  // col, row format
                        Grid.SetRowSpan(ballotImgsL[1], 12);
                    }
                    if (ballotImgsL.Count > 2) {
                        landscapeView.Children.Add(ballotImgsL[2], 0, 14);  // col, row format
                        Grid.SetRowSpan(ballotImgsL[2], 12);
                    }
                    if (ballotImgsL.Count > 3) {
                        landscapeView.Children.Add(ballotImgsL[3], 1, 14);  // col, row format
                        Grid.SetRowSpan(ballotImgsL[3], 12);
                    }
                    */
#if DEBUG
                    challengeLabelL.Text += " no image L case";
#endif

                    landscapeView.Children.Add(challengeLabelL, 0, 0);
                    Grid.SetColumnSpan(challengeLabelL, 2);

                    landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);  // going to wrong position for some reason...
                    Grid.SetColumnSpan(defaultNavigationButtonsL, 2);

                }
            } catch (Exception e) {
                Debug.WriteLine("DHB:JudgingContentPage:buildLandscapeView:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:JudgingContentPage:buildLandscapeView end");
            return result;
        }

        public int buildFourPortraitImgLandscapeView() {
            landscapeView.RowDefinitions.Clear();
            landscapeView.ColumnDefinitions.Clear();

            for (int i = 0; i < 10; i++) {
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            // 4 columns, 25% each
            for (int i = 0; i < 4; i++) {
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            landscapeView.Children.Add(ballotImgsL[0], 0, 1);
            Grid.SetRowSpan(ballotImgsL[0], 8);

            landscapeView.Children.Add(ballotImgsL[1], 1, 1);  // col, row format
            Grid.SetRowSpan(ballotImgsL[1], 8);

            landscapeView.Children.Add(ballotImgsL[2], 2, 1);  // col, row format
            Grid.SetRowSpan(ballotImgsL[2], 8);

            landscapeView.Children.Add(ballotImgsL[3], 3, 1);  // col, row format
            Grid.SetRowSpan(ballotImgsL[3], 8);

#if DEBUG
            challengeLabelL.Text += " 4P L Case";
#endif

            landscapeView.Children.Add(challengeLabelL, 0, 0);
            Grid.SetColumnSpan(challengeLabelL, 4);

            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);  // going to wrong position for some reason...
            Grid.SetColumnSpan(defaultNavigationButtonsL, 4);

            return 1;
        }
        public int buildFourLandscapeImgLandscapeView() {
            landscapeView.RowDefinitions.Clear();
            landscapeView.ColumnDefinitions.Clear();
            // topleft img1 48%H 50%w; topright img2 48%H 50%W
            // middle challenge label 4%H 100% W
            // bot left img3 48%H 50%W; bot right img4 48%H 50%W
            // 25 rows of 4% each.
            // No, go with an extra row so the full text shows.  Went to 28 rows for nav buttons.
            for (int i = 0; i < 10; i++) {
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            // 2 columns, 50% each
            landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // I can add none, but if i add one, then i just have 1. So here's 2. :)
            landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            landscapeView.Children.Add(ballotImgsL[0], 0, 0);
            Grid.SetRowSpan(ballotImgsL[0], 4);

            landscapeView.Children.Add(ballotImgsL[1], 1, 0);  // col, row format
            Grid.SetRowSpan(ballotImgsL[1], 4);

            landscapeView.Children.Add(ballotImgsL[2], 0, 5);  // col, row format
            Grid.SetRowSpan(ballotImgsL[2], 4);

            landscapeView.Children.Add(ballotImgsL[3], 1, 5);  // col, row format
            Grid.SetRowSpan(ballotImgsL[3], 4);

#if DEBUG
            challengeLabelL.Text += " 4L L case";
#endif

            landscapeView.Children.Add(challengeLabelL, 0, 4);
            Grid.SetColumnSpan(challengeLabelL, 2);
            //Grid.SetRowSpan(challengeLabel, 2);

            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);  // going to wrong position for some reason...
            Grid.SetColumnSpan(defaultNavigationButtonsL, 2);
            //Grid.SetRowSpan(defaultNavigationButtons, 2); no, this generates a nightmare

            return 1;
        }

        public int buildTwoXTwoImgLandscapeView() {
            landscapeView.RowDefinitions.Clear();
            landscapeView.ColumnDefinitions.Clear();

            for (int i = 0; i < 10; i++) {
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            // 4 columns, 25% each
            for (int i = 0; i < 4; i++) {
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            if (ballot.ballots[0].isPortrait == BallotCandidateJSON.PORTRAIT) {
                // 2 portraits, then 2 landscape
                landscapeView.Children.Add(ballotImgsL[0], 0, 1);
                Grid.SetRowSpan(ballotImgsL[0], 8);

                landscapeView.Children.Add(ballotImgsL[1], 1, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[1], 8);

                landscapeView.Children.Add(ballotImgsL[2], 2, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[2], 4);
                Grid.SetColumnSpan(ballotImgsL[2], 2);

                landscapeView.Children.Add(ballotImgsL[3], 2, 5);  // col, row format
                Grid.SetRowSpan(ballotImgsL[3], 4);
                Grid.SetColumnSpan(ballotImgsL[3], 2);
            } else {
                // 2 landscape, then 2 portrait
                landscapeView.Children.Add(ballotImgsL[0], 0, 1);
                Grid.SetRowSpan(ballotImgsL[0], 4);
                Grid.SetColumnSpan(ballotImgsL[0], 2);

                landscapeView.Children.Add(ballotImgsL[1], 0, 5);  // col, row format
                Grid.SetRowSpan(ballotImgsL[1], 4);
                Grid.SetColumnSpan(ballotImgsL[1], 2);

                landscapeView.Children.Add(ballotImgsL[2], 2, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[2], 8);

                landscapeView.Children.Add(ballotImgsL[3], 3, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[3], 8);
            }

#if DEBUG
            challengeLabelL.Text += " 2x2 L case";
#endif

            landscapeView.Children.Add(challengeLabelL, 0, 0);
            Grid.SetColumnSpan(challengeLabelL, 4);

            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);  // going to wrong position for some reason...
            Grid.SetColumnSpan(defaultNavigationButtonsL, 4);

            return 1;
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
            string ignorableResult = await requestChallengeNameAsync();
            //challengeLabelP.Text = await requestChallengeNameAsync();
            //challengeLabelL.Text = challengeLabelP.Text;
            if (CategoryLoadSuccess != null) {
                CategoryLoadSuccess(sender, e);
            }
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
                lock (ballot) {
                    ClearContent();
                    try {
                        processBallotString(preloadedBallots.Dequeue());
                    } catch (Exception ex) {
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest wtf. dequeing from empty queue. how???");
                        Debug.WriteLine(ex.ToString());
                    }
                }
            });
            // should be ok at this point. however, sometimes an invalid status gets saved.
            // this is how I pick that up and prevent it.
            if (ballot.ballots == null) {
                // apparently can't fire again from here. just hangs here continually calling this.
                //DequeueBallotRequest(this, e);
                if (preloadedBallots.Count > 0) {
                    //processBallotString(preloadedBallots.Dequeue());
                    Debug.WriteLine("DHB:JudgingContentPage:OnDequeueBallotRequest bad read. do I autofix with OnLoadBallotPics?");
                }
            }

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
                            GlobalStatusSingleton.votingCategories.Add(cat);
                            result = cat.description;
                        } else if (cat.state.Equals(CategoryJSON.UPLOAD)) {
                            //GlobalStatusSingleton.uploadingCategoryId = cat.categoryId;
                            //GlobalStatusSingleton.uploadCategoryDescription = cat.description;
                            GlobalStatusSingleton.uploadingCategories.Add(cat);
                        } else if (cat.state.Equals(CategoryJSON.CLOSED)) {
                            //GlobalStatusSingleton.mostRecentClosedCategoryId = cat.categoryId;
                            //GlobalStatusSingleton.mostRecentClosedCategoryDescription = cat.description;
                            GlobalStatusSingleton.closedCategories.Add(cat);
                        }
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

         static int counter = 1;
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
                //challengeLabelP.Text = "Currently unable to load ballots.  Attempts: " + counter;
                //challengeLabelL.Text = "Currently unable to load ballots.  Attempts: " + counter;
                counter++;
                if (counter > 2) {
                    Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics " + counter + " load fails");
                }
                await Task.Delay(5000);
                if (LoadBallotPics != null) {
                    LoadBallotPics(this, eDummy);
                }

            }
            Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics pre while");
            // keep this thread going constantly, making sure we never run out.
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
                    await Task.Delay(500);
                }
                // this happens when i run out of ballots
                // but why??
                // unhappy with this solution as it means I don't understand what's happening.                
                if ((ballot.ballots == null) && (preloadedBallots.Count > 1)) {
                    Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics out of ballots, sending dqueue");
                    //processBallotString(preloadedBallots.Dequeue());
                    DequeueBallotRequest(this, eDummy);
                }
                //}
            }
            // This line gets tagged as unreachable, which it is... so just comment out.
            //Debug.WriteLine("DHB:JudgingContentPage:OnLoadBallotPics end");
        }

        /// <summary>
        /// Helper for managePersistedBallot that loads instructions on startup if
        /// there is no ballot.
        /// </summary>
        protected virtual void loadInstructions() {
            ballot.ballots = new List<BallotCandidateJSON>();
            challengeLabelP.Text = "Instructions";
            challengeLabelL.Text = "Instructions";
            // now handle ballot
            Debug.WriteLine("DHB:JudgingContentPage:processBallotString generating images");
            for (int i=1;i<5; i++) { 
                Image imgP = new Image {
                    Source = ImageSource.FromResource("ImageImprov.IconImages.Instructions.Instructions_" + i + ".png"),
                };
                ballotImgsP.Add(imgP);

                Image imgL = new Image
                {
                    Source = ImageSource.FromResource("ImageImprov.IconImages.Instructions.Instructions_" + i + ".png"),
                };
                ballotImgsL.Add(imgL);
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
                Image lImg = ballotImgsL[swap];
                ballotImgsL.RemoveAt(swap);
                ballotImgsL.Insert(1, lImg);
                Image pImg = ballotImgsP[swap];
                ballotImgsP.RemoveAt(swap);
                ballotImgsP.Insert(1, pImg);
            }


        }

        /// <summary>
        /// Currently, this also manages orientation Count.
        /// This is what is called during the initial image setup.
        /// DEPREACTED.  Use setupImgsFromBallotCandidate.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        protected Image setupImgFromBallotCandidate(BallotCandidateJSON candidate) {
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
            Image image = GlobalSingletonHelpers.buildFixedRotationImage(candidate);
            //image.Aspect = Aspect.AspectFill;
            //image.Aspect = Aspect.AspectFit;
            image.Aspect = GlobalStatusSingleton.aspectOrFillImgs;

            // This works. looks like a long press will be a pain in the ass.
            TapGestureRecognizer tapGesture = new TapGestureRecognizer();
            if (tapGesture == null) {
                tapGesture = new TapGestureRecognizer();
            }
            tapGesture.Tapped += OnClicked;
            image.GestureRecognizers.Add(tapGesture);

            // orientation info is based on the relative w/h of the image.
            // square images are all considered "landscape"
            //candidate.orientation = isPortraitOrientation(candidate.imgStr);
            orientationCount += candidate.isPortrait;
            return image;
        }

        protected IList<Image> setupImgsFromBallotCandidate(BallotCandidateJSON candidate) {
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
            IList<Image> images = GlobalSingletonHelpers.buildTwoFixedRotationImageFromCandidate(candidate);
            images[0].Aspect = GlobalStatusSingleton.aspectOrFillImgs;
            images[1].Aspect = GlobalStatusSingleton.aspectOrFillImgs;

            // This works. looks like a long press will be a pain in the ass.
            TapGestureRecognizer tapGesture = new TapGestureRecognizer();
            if (tapGesture == null) {
                tapGesture = new TapGestureRecognizer();
            }
            tapGesture.Tapped += OnClicked;  
            images[0].GestureRecognizers.Add(tapGesture);
            images[1].GestureRecognizers.Add(tapGesture);

            // orientation info is based on the relative w/h of the image.
            // square images are all considered "landscape"
            //candidate.orientation = isPortraitOrientation(candidate.imgStr);
            orientationCount += candidate.isPortrait;
            return images;
        }
        protected Image setupImgFromStream(MemoryStream imgStream) {
            Image image = new Image();
            //
            //
            // The line below is the line of death.
            // The line below is the line of death.
            // The line below is the line of death.
            //
            //  What happens is the imgStream will go out of scope, be collected, then unhandled excpetion ensues.
            image.Source = ImageSource.FromStream(() => imgStream);
            //image.Aspect = Aspect.AspectFill;
            //image.Aspect = Aspect.AspectFit;
            image.Aspect = GlobalStatusSingleton.aspectOrFillImgs;

            // This works. looks like a long press will be a pain in the ass.
            TapGestureRecognizer tapGesture = new TapGestureRecognizer();
            if (tapGesture == null) {
                tapGesture = new TapGestureRecognizer();
            }
            tapGesture.Tapped += OnClicked;
            image.GestureRecognizers.Add(tapGesture);
            return image;
        }

        /// <summary>
        /// Adds the passed in rank image to the raw ballot, and then produces an image.
        /// This is what is called when we add our ranking to the image.
        /// 
        /// NOT EVEN CLOSED TO FINISHED OR USABLE FUNCTION!!!
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        protected Image setupImgWithRanking(byte[] rawImg, SKImage rankingImg) {
            // build and merge rawImg and rankingImg.
            // get stream
            var res = new MemoryStream(rawImg);
            // not impacting orientation count at this point in time.
            Image image = setupImgFromStream(res);
            //Image image = new Image();
            //image.Source = ImageSource.FromStream(() => new MemoryStream(candidate.imgStr));

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
                challengeLabelP.Text = "Current category: " + ballot.category.description;
                challengeLabelL.Text = "Current category: " + ballot.category.description;
                // now handle ballot
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString generating images");
                foreach (BallotCandidateJSON candidate in ballot.ballots) {
                    /*
                    // this method does a double load. loading is one of the slowest things, so change.
                    // also double counted orientation!
                    Image imgP = setupImgFromBallotCandidate(candidate);
                    ballotImgsP.Add(imgP);

                    Image imgL = setupImgFromBallotCandidate(candidate);
                    ballotImgsL.Add(imgL);
                    */
                    IList<Image> img = setupImgsFromBallotCandidate(candidate);
                    ballotImgsP.Add(img[0]);
                    ballotImgsL.Add(img[1]);
                }
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString image generation done");
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString orientationCount: " +orientationCount);
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString orientationCount: " + orientationCount);
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString orientationCount: " + orientationCount);
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString orientationCount: " + orientationCount);
                Debug.WriteLine("DHB:JudgingContentPage:processBallotString orientationCount: " + orientationCount);
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
                Debug.WriteLine(e.ToString());
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

        protected int findUnallocatedBid(List<VoteJSON> votes) {
            return 1;
        }

        protected void UpdateUIForFinalVote(List<VoteJSON> votes, int penultimateSelectedIndex, int ultimateSelectedIndex) {
            // not sure how I do indexing...
            //ClearContent(firstSelectedIndex);
            //Device.BeginInvokeOnMainThread(() => {
            SKBitmap baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                    ballot.ballots[penultimateSelectedIndex].imgStr, (ExifOrientation)ballot.ballots[penultimateSelectedIndex].orientation);
            SKImage mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[rankImages.Count - 2]);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[penultimateSelectedIndex], mergedImage);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsL[penultimateSelectedIndex], mergedImage);
            // see if a new object eliminates flicker.
            // also needs a buildUI at the end to update the layout objects with the new info...
            //ballotImgsP[penultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
            //ballotImgsL[penultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);

            baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                ballot.ballots[ultimateSelectedIndex].imgStr, (ExifOrientation)ballot.ballots[ultimateSelectedIndex].orientation);
            mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[rankImages.Count - 1]);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[ultimateSelectedIndex], mergedImage);
            GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsL[ultimateSelectedIndex], mergedImage);

            //ballotImgsP[ultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
            //ballotImgsL[ultimateSelectedIndex] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);

            foreach (Image img in ballotImgsP) {
                    img.IsEnabled = false;
                // clearing these prevents ui from updating for some reason.
                // so instead, I catch a tap and ignore it.
                    //img.GestureRecognizers.Clear();
            }
            foreach (Image img in ballotImgsL) {
                img.IsEnabled = false;
                //img.GestureRecognizers.Clear();
            }
                // this is very expensive and may be cause of flicker...
                // what happens if i don't call this?
                // much faster, and images are updating (this is updating the images via the updatexamarinfromskimage function case)
                //AdjustContentToRotation();
            //});
            Debug.WriteLine("DHB:JudgingContentPage:UpdateUIForFinalVote done");
        }

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
            var searchImgs = ballotImgsL;
            if (GlobalStatusSingleton.inPortraitMode == true) {
                searchImgs = ballotImgsP;
            }
            foreach (Image img in searchImgs) {
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
                throw new Exception("A button clicked on an image not in my ballots.");
            }
#endif
            VoteJSON vote = new ImageImprov.VoteJSON();
            vote.bid = bid;
            vote.vote = votes.votes.Count + 1;
            vote.like = true;
            votes.votes.Add(vote);

            string jsonQuery = JsonConvert.SerializeObject(votes);
            string origText = challengeLabelP.Text;
            challengeLabelP.Text = "Vote submitted, loading new ballot";
            challengeLabelL.Text = "Vote submitted, loading new ballot";
            ClearContent(selectionId);
            string result = await requestVoteAsync(jsonQuery);
            if (result.Equals("fail")) {
                // @todo This fail case is untested code. Does the UI come back?
                challengeLabelP.Text = "Connection failed. Please revote";
                challengeLabelL.Text = "Connection failed. Please revote";
                AdjustContentToRotation();
                //} else ("no ballot created") { 
            } else {
                // only clear on success
                ClearContent();
                challengeLabelP.Text = origText;
                challengeLabelL.Text = origText;
                processBallotString(result);
            }
        }

        /// <summary>
        /// Helper function for MultiVoteGeneratesBallot
        /// Given the tapped image, find the index in the ballot.ballots array of that image and sets it to the selectionId variable.
        /// And grabs the bid at the same time for easy use.
        /// bleh, setting so many things this turned out to be more trouble than worth...
        /// </summary>
        /// <returns></returns>
        private bool findSelectionIndexAndBid(object sender, ref int selectionId, ref long bid, ref BallotCandidateJSON votedOnCandidate) {
            bool found = false;
            int index = 0;
            // ballotImgsP and L have same meta info, so only need this once.
            // but I do need to search the correct one to id the sender.
            var searchImgs = ballotImgsL;
            if (GlobalStatusSingleton.inPortraitMode == true) {
                searchImgs = ballotImgsP;
            }
            foreach (Image img in searchImgs) {
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
                int ballotIndex = 0;
                int voteNum = -1;
                foreach (BallotCandidateJSON candidate in ballot.ballots) {
                    if (votedOn(candidate.bidId, ref voteNum)) {
                        SKBitmap baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(candidate.imgStr, (ExifOrientation)candidate.orientation);
                        // note: votedOn sets voteNum to the zero based index, not the votes.votes.vote num.
                        SKImage mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[voteNum]);
                        GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[ballotIndex], mergedImage);
                        GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsL[ballotIndex], mergedImage);
                    } else {
                        SKImage img = SKImage.FromBitmap(GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(candidate.imgStr, (ExifOrientation)candidate.orientation));
                        GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[ballotIndex], img);
                        GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsL[ballotIndex], img);
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

            // ballots may have been cleared and this can be a dbl tap registration.
            // in which case, ignore.
            if (ballot.ballots.Count == 0) { return; }
            int selectionId = 0;
            long bid = -1;
            BallotCandidateJSON votedOnCandidate = null;
            bool found = findSelectionIndexAndBid(sender, ref selectionId, ref bid, ref votedOnCandidate);

#if DEBUG
            if (found == false) {
                throw new Exception("A button clicked on an image not in my ballots.");
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
                vote.like = true;
                votes.votes.Add(vote);
                if (votedOnCandidate != null) {
                    unvotedImgs.Remove(votedOnCandidate);
                }

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
                    vote.like = true;
                    votes.votes.Add(vote);

                    string jsonQuery = JsonConvert.SerializeObject(votes);
                    string origText = challengeLabelP.Text;
                    challengeLabelP.Text = "Vote submitted, loading new ballot";
                    challengeLabelL.Text = "Vote submitted, loading new ballot";

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
                        result = await requestVoteAsync(jsonQuery);
                        if (result.Equals("fail")) {
                            // @todo This fail case is untested code. Does the UI come back?
                            if (preloadedBallots.Count == 0) {
                                challengeLabelP.Text = "No connection. Awaiting connection for more ballots.";
                                challengeLabelL.Text = "No connection. Awaiting connection for more ballots.";
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
                    // turn this image off and wait till all are selected.
                    //ballotImgsP[selectionId].IsEnabled = false;
                    //ballotImgsP[selectionId].IsVisible = false;
                    //ballotImgsL[selectionId].IsEnabled = false;
                    //ballotImgsL[selectionId].IsVisible = false;
                    // New behavior: Leave the images on, but put ranking numbers on them.
                    // @todo Leave enabled so I can uncheck.
                    // bleh. do I have the imgStr still? Yes, it lives in Ballot.
                    // hmm... 
                    // vote.vote is indexed from 1. rankimages from 0.
                    //SKBitmap baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(
                        //ballot.ballots[selectionId].imgStr, (ExifOrientation)ballot.ballots[selectionId].orientation);
                    SKBitmap baseImg = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(ballot.ballots[selectionId].imgStr);

                    SKImage mergedImage = GlobalSingletonHelpers.MergeImages(baseImg, rankImages[vote.vote - 1]);

                    // this method triggers the UI change.
                    GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsP[selectionId], mergedImage);
                    GlobalSingletonHelpers.UpdateXamarinImageFromSKImage(ballotImgsL[selectionId], mergedImage);

                    // this, on it's own, does not...
                    //ballotImgsP[selectionId] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
                    //ballotImgsL[selectionId] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
                    // writing it in the UI thread does not solve the problem... need to tell content there's a change.
                    //Device.BeginInvokeOnMainThread(() => {
                    //    ballotImgsP[selectionId] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
                    //    ballotImgsL[selectionId] = GlobalSingletonHelpers.SKImageToXamarinImage(mergedImage);
                    //});

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
    } // class
} // namespace

