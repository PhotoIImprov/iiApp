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
        KeyPageNavigator defaultNavigationButtonsP;
        KeyPageNavigator defaultNavigationButtonsL;

        // Yesterday's challenge 
        //< challengeLabel
        Label challengeLabelP = new Label
        {
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
        };
        Label challengeLabelL = new Label
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


        // The BallotJSON now holds a list, rather than a single instance.
        // The single instance has been refactored to BallotCandidateJSON
        //IList<BallotJSON> ballots;
        BallotJSON ballot;

        // @todo ideally, the images would be built in BallotJSON. Get this working, then think about that.
        // @todo imgs currently only respond to taps.  Would like to be able to longtap a fast vote.
        // Need to instances to accomodate the way GridLayout handles spans.
        IList<Image> ballotImgsP = null;
        IList<Image> ballotImgsL = null;

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


        public JudgingContentPage() {
            ballotImgsP = new List<Image>();
            ballotImgsL = new List<Image>();
            buildPortraitView();
            buildLandscapeView();
            //Content = challengeLabel;

            // TURNED OFF TO TEST OVERRIDE OnSizeAllocated
            // TURNED OFF TO TEST OVERRIDE OnSizeAllocated
            // TURNED OFF TO TEST OVERRIDE OnSizeAllocated
            // listen for orientation changes, which are currently handled through size change.
            //SizeChanged += (sender, e) => AdjustContentToRotation();
            // TURNED OFF TO TEST OVERRIDE OnSizeAllocated
            // TURNED OFF TO TEST OVERRIDE OnSizeAllocated
            // TURNED OFF TO TEST OVERRIDE OnSizeAllocated


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

        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height);
            if (width > height) {
                GlobalStatusSingleton.inPortraitMode = false;
                Content = landscapeView;
            } else {
                GlobalStatusSingleton.inPortraitMode = true;
                Content = portraitView;
            }
        }

        /// <summary>
        /// This is called whenever my underlying data for my layout has changed (e.g. for a vote)
        /// Forcing a new layout to be drawn.
        /// </summary>
        private void AdjustContentToRotation() {
            buildPortraitView();
            buildLandscapeView();
            if (GlobalStatusSingleton.IsPortrait(this)) {
                Content = portraitView;
            } else {
                Content = landscapeView;
            }
        }

        private void ClearContent() {
            ballot.Clear();
            ballotImgsP.Clear();  // does Content update? No
            ballotImgsL.Clear();  // does Content update? No
            unvotedImgs.Clear();
            votes.votes.Clear();
            Content = new StackLayout() { Children = { challengeLabelP, } };
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

        private void RemoveVotedOnImage(int index) {
            portraitView.IsEnabled = false;
            landscapeView.IsEnabled = false;
            ballot.Clear();

            highlightCorrectImg(ballotImgsP, index);
            highlightCorrectImg(ballotImgsL, index);
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
            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            if (defaultNavigationButtonsP==null) {
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
                /*
                portraitView.RowDefinitions.Clear();
                portraitView.ColumnDefinitions.Clear();
                for (int i = 0; i < 26; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }

                if (ballotImgsP.Count > 0) {
                    portraitView.Children.Add(ballotImgsP[0], 0, 0);
                    Grid.SetRowSpan(ballotImgsP[0], 6);
                }

                if (ballotImgsP.Count > 1) {
                    portraitView.Children.Add(ballotImgsP[1], 0, 6);  // col, row format
                    Grid.SetRowSpan(ballotImgsP[1], 6);
                }

                if (ballotImgsP.Count > 2) {
                    portraitView.Children.Add(ballotImgsP[2], 0, 13);  // col, row format
                    Grid.SetRowSpan(ballotImgsP[2], 6);
                }

                if (ballotImgsP.Count > 3) {
                    portraitView.Children.Add(ballotImgsP[3], 0, 19);  // col, row format
                    Grid.SetRowSpan(ballotImgsP[3], 6);
                }
                */
#if DEBUG
                challengeLabelP.Text += " no image case";
#endif

                portraitView.Children.Add(challengeLabelP, 0, 12);
                Grid.SetColumnSpan(challengeLabelP, 1);
                portraitView.Children.Add(defaultNavigationButtonsP, 0, 25);
                Grid.SetColumnSpan(defaultNavigationButtonsP, 1);
            }

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
            for (int i = 0; i < 26; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // I can add none, but if i add one, then i just have 1. So here's 2. :)
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            portraitView.Children.Add(ballotImgsP[0], 0, 0);
            Grid.SetRowSpan(ballotImgsP[0], 12);

            portraitView.Children.Add(ballotImgsP[1], 1, 0);  // col, row format
            Grid.SetRowSpan(ballotImgsP[1], 12);

            portraitView.Children.Add(ballotImgsP[2], 0, 13);  // col, row format
            Grid.SetRowSpan(ballotImgsP[2], 12);

            portraitView.Children.Add(ballotImgsP[3], 1, 13);  // col, row format
            Grid.SetRowSpan(ballotImgsP[3], 12);

#if DEBUG
            challengeLabelP.Text += " 4P_P case";
#endif

            portraitView.Children.Add(challengeLabelP, 0, 12);
            Grid.SetColumnSpan(challengeLabelP, 2);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 25);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 2);
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
            for (int i = 0; i < 26; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            portraitView.Children.Add(ballotImgsP[0], 0, 0);
            Grid.SetRowSpan(ballotImgsP[0], 6);

            portraitView.Children.Add(ballotImgsP[1], 0, 6);  // col, row format
            Grid.SetRowSpan(ballotImgsP[1], 6);

            portraitView.Children.Add(ballotImgsP[2], 0, 13);  // col, row format
            Grid.SetRowSpan(ballotImgsP[2], 6);

            portraitView.Children.Add(ballotImgsP[3], 0, 19);  // col, row format
            Grid.SetRowSpan(ballotImgsP[3], 6);

#if DEBUG
            challengeLabelP.Text += " 4L_P case";
#endif

            portraitView.Children.Add(challengeLabelP, 0, 12);
            Grid.SetColumnSpan(challengeLabelP, 1);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 25);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 1);

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
            for (int i = 0; i < 26; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // I can add none, but if i add one, then i just have 1. So here's 2. :)
            portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // am I in two landscape on top or two portrait on top?
            if (ballot.ballots[0].isPortrait() == BallotCandidateJSON.PORTRAIT) {
                // portrait on top
                portraitView.Children.Add(ballotImgsP[0], 0, 0);
                Grid.SetRowSpan(ballotImgsP[0], 12);

                portraitView.Children.Add(ballotImgsP[1], 1, 0);  // col, row format
                Grid.SetRowSpan(ballotImgsP[1], 12);

                portraitView.Children.Add(ballotImgsP[2], 0, 13);  // col, row format
                Grid.SetRowSpan(ballotImgsP[2], 6);
                Grid.SetColumnSpan(ballotImgsP[2], 2);

                portraitView.Children.Add(ballotImgsP[3], 0, 19);  // col, row format
                Grid.SetRowSpan(ballotImgsP[3], 6);
                Grid.SetColumnSpan(ballotImgsP[3], 2);
            } else {
                // landscape on top
                portraitView.Children.Add(ballotImgsP[0], 0, 0);
                Grid.SetRowSpan(ballotImgsP[0], 6);
                Grid.SetColumnSpan(ballotImgsP[0], 2);

                portraitView.Children.Add(ballotImgsP[1], 0, 6);  // col, row format
                Grid.SetRowSpan(ballotImgsP[1], 6);
                Grid.SetColumnSpan(ballotImgsP[1], 2);

                portraitView.Children.Add(ballotImgsP[2], 0, 13);  // col, row format
                Grid.SetRowSpan(ballotImgsP[2], 12);

                portraitView.Children.Add(ballotImgsP[3], 1, 13);  // col, row format
                Grid.SetRowSpan(ballotImgsP[3], 12);
            }
#if DEBUG
            challengeLabelP.Text += " 2x2P case";
#endif

            portraitView.Children.Add(challengeLabelP, 0, 12);
            Grid.SetColumnSpan(challengeLabelP, 2);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 25);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 2);


            return 1;
        }

        public int buildLandscapeView() {
            // the current implemented case is orientationCount == 0. (displays as a 2x2)
            // There are two other options... 
            //    orientationCount == 2 - displays as 2xstack or stackx2 per first img orientation
            //    orientationCount == 4 - display as a 4x1 horizontally aligned portraits.

            int result = 1;

            if (defaultNavigationButtonsL == null) {
                defaultNavigationButtonsL = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
            }

            // all my elements are already members...
            if (landscapeView == null) {
                landscapeView = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
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
                /*
                for (int i = 0; i < 26; i++) {
                    landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                for (int i = 0; i < 4; i++) {
                    landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
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

                landscapeView.Children.Add(defaultNavigationButtonsL, 0, 25);  // going to wrong position for some reason...
                Grid.SetColumnSpan(defaultNavigationButtonsL, 2);

            }
            return result;
        }

        public int buildFourPortraitImgLandscapeView() {
            landscapeView.RowDefinitions.Clear();
            landscapeView.ColumnDefinitions.Clear();

            for (int i = 0; i < 20; i++) {
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            // 4 columns, 25% each
            for (int i = 0; i < 4; i++) {
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            landscapeView.Children.Add(ballotImgsL[0], 0, 1);
            Grid.SetRowSpan(ballotImgsL[0], 18);

            landscapeView.Children.Add(ballotImgsL[1], 1, 1);  // col, row format
            Grid.SetRowSpan(ballotImgsL[1], 18);

            landscapeView.Children.Add(ballotImgsL[2], 2, 1);  // col, row format
            Grid.SetRowSpan(ballotImgsL[2], 18);

            landscapeView.Children.Add(ballotImgsL[3], 3, 1);  // col, row format
            Grid.SetRowSpan(ballotImgsL[3], 18);

#if DEBUG
            challengeLabelL.Text += " 4P L Case";
#endif

            landscapeView.Children.Add(challengeLabelL, 0, 0);
            Grid.SetColumnSpan(challengeLabelL, 4);

            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 19);  // going to wrong position for some reason...
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
            for (int i = 0; i < 14; i++) {
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            // 2 columns, 50% each
            landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // I can add none, but if i add one, then i just have 1. So here's 2. :)
            landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            landscapeView.Children.Add(ballotImgsL[0], 0, 0);
            Grid.SetRowSpan(ballotImgsL[0], 6);

            landscapeView.Children.Add(ballotImgsL[1], 1, 0);  // col, row format
            Grid.SetRowSpan(ballotImgsL[1], 6);

            landscapeView.Children.Add(ballotImgsL[2], 0, 7);  // col, row format
            Grid.SetRowSpan(ballotImgsL[2], 6);

            landscapeView.Children.Add(ballotImgsL[3], 1, 7);  // col, row format
            Grid.SetRowSpan(ballotImgsL[3], 6);

#if DEBUG
            challengeLabelL.Text += " 4L L case";
#endif

            landscapeView.Children.Add(challengeLabelL, 0, 6);
            Grid.SetColumnSpan(challengeLabelL, 2);
            //Grid.SetRowSpan(challengeLabel, 2);

            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 13);  // going to wrong position for some reason...
            Grid.SetColumnSpan(defaultNavigationButtonsL, 2);
            //Grid.SetRowSpan(defaultNavigationButtons, 2); no, this generates a nightmare

            return 1;
        }

        public int buildTwoXTwoImgLandscapeView() {
            landscapeView.RowDefinitions.Clear();
            landscapeView.ColumnDefinitions.Clear();

            for (int i = 0; i < 20; i++) {
                landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            // 4 columns, 25% each
            for (int i = 0; i < 4; i++) {
                landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            if (ballot.ballots[0].isPortrait() == BallotCandidateJSON.PORTRAIT) {
                // 2 portraits, then 2 landscape
                landscapeView.Children.Add(ballotImgsL[0], 0, 1);
                Grid.SetRowSpan(ballotImgsL[0], 18);

                landscapeView.Children.Add(ballotImgsL[1], 1, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[1], 18);

                landscapeView.Children.Add(ballotImgsL[2], 2, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[2], 9);
                Grid.SetColumnSpan(ballotImgsL[2], 2);

                landscapeView.Children.Add(ballotImgsL[3], 2, 10);  // col, row format
                Grid.SetRowSpan(ballotImgsL[3], 9);
                Grid.SetColumnSpan(ballotImgsL[3], 2);
            } else {
                // 2 landscape, then 2 portrait
                landscapeView.Children.Add(ballotImgsL[0], 0, 1);
                Grid.SetRowSpan(ballotImgsL[0], 9);
                Grid.SetColumnSpan(ballotImgsL[0], 2);

                landscapeView.Children.Add(ballotImgsL[1], 0, 10);  // col, row format
                Grid.SetRowSpan(ballotImgsL[1], 9);
                Grid.SetColumnSpan(ballotImgsL[1], 2);

                landscapeView.Children.Add(ballotImgsL[2], 2, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[2], 18);

                landscapeView.Children.Add(ballotImgsL[3], 3, 1);  // col, row format
                Grid.SetRowSpan(ballotImgsL[3], 18);
            }

#if DEBUG
            challengeLabelL.Text += " 2x2 L case";
#endif

            landscapeView.Children.Add(challengeLabelL, 0, 0);
            Grid.SetColumnSpan(challengeLabelL, 4);

            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 19);  // going to wrong position for some reason...
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

        // I think this is handled through and abstract event handler in the cstr.
        //private void OnSizeChanged() {        }

        /////
        /////
        ///// BEGIN Loading section
        /////
        /////

        protected async virtual void OnLoadChallengeName(object sender, EventArgs e) {
            challengeLabelP.Text = await requestChallengeNameAsync();
            challengeLabelL.Text = challengeLabelP.Text;
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

         static int counter = 0;
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
                challengeLabelP.Text = "Currently unable to load ballots";
                challengeLabelL.Text = "Currently unable to load ballots";
                counter++;
                if (counter > 2) {
                    bool falsebreak = true;
                }
                await Task.Delay(5000);
                if (LoadBallotPics != null) {
                    LoadBallotPics(this, eDummy);
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

        }

        protected Image setupImgFromBallotCandidate(BallotCandidateJSON candidate) {
            Image image = new Image();
            image.Source = ImageSource.FromStream(() => new MemoryStream(candidate.imgStr));
            //image.Aspect = Aspect.AspectFill;
            //image.Aspect = Aspect.AspectFit;
            image.Aspect = GlobalStatusSingleton.aspectOrFillImgs;

            // orientation info now sent from the server.
            //candidate.orientation = isPortraitOrientation(candidate.imgStr);
            orientationCount += candidate.isPortrait();

            // This works. looks like a long press will be a pain in the ass.
            TapGestureRecognizer tapGesture = new TapGestureRecognizer();
            if (tapGesture == null) {
                tapGesture = new TapGestureRecognizer();
            }
            tapGesture.Tapped += OnClicked;
            image.GestureRecognizers.Add(tapGesture);
            return image;
        }

        // implmented as a function so it can be reused by the vote message response.
        protected virtual void processBallotString(string result) {
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
                foreach (BallotCandidateJSON candidate in ballot.ballots) {
                    Image imgP = setupImgFromBallotCandidate(candidate);
                    ballotImgsP.Add(imgP);

                    Image imgL = setupImgFromBallotCandidate(candidate);
                    ballotImgsL.Add(imgL);
                }
                if (orientationCount == 2) {
                    checkImgOrderAndReorderAsNeeded();
                }
#if DEBUG
                int checkFull = ballotImgsP.Count;
                // not guaranteed 4 back anymore.
                //Debug.Assert(ballotImgsP.Count == 4, "less than 4 ballots sent");
#endif // Debug            
            } catch (Exception e) {
                // probably thrown by Deserialize.
                bool falseBreak = false;
            }

            // These are both built in AdjustContentToRotation
            //buildPortraitView();
            //buildLandscapeView();

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
                string ballotURL = GlobalStatusSingleton.activeURL + BALLOT;
                    //+ "?category_id=" + GlobalStatusSingleton.votingCategoryId;  // no longer needed? return a random category.
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
            //SingleVoteGeneratesBallot(sender, e);
            MultiVoteGeneratesBallot(sender, e);
        }

        protected int findUnallocatedBid(List<VoteJSON> votes) {
            return 1;
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

        protected async virtual void MultiVoteGeneratesBallot(object sender, EventArgs e) {
            if (votes == null) {
                votes = new VotesJSON();
                votes.votes = new List<VoteJSON>();
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
            BallotCandidateJSON votedOnCandidate = null;
            foreach (Image img in searchImgs) {
                if (img == sender) {
                    found = true;
                    votedOnCandidate = ballot.ballots[index];
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
            vote.vote = votes.votes.Count+1;
            if (vote.vote == 1) {
                // first selected save the selectionId;
                firstSelectedIndex = selectionId;
            }
            vote.like = true;
            votes.votes.Add(vote);
            if (votedOnCandidate != null) {
                unvotedImgs.Remove(votedOnCandidate);
            }
            // No. This fails for cases where num ballots != 4, which is allowed.
            //if (vote.vote == PENULTIMATE_BALLOT_SELECTED) {
            if (vote.vote == (ballot.ballots.Count-1)) {
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
                ClearContent(firstSelectedIndex);
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
            } else {
                // turn this image off and wait till all are selected.
                ballotImgsP[selectionId].IsEnabled = false;
                ballotImgsP[selectionId].IsVisible = false;
                ballotImgsL[selectionId].IsEnabled = false;
                ballotImgsL[selectionId].IsVisible = false;
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

