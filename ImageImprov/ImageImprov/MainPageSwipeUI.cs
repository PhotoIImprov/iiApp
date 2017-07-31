using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;  // for debug assertions.

using Xamarin.Forms;

namespace ImageImprov
{
    // this is the main page when we are using the carousel to manage the ui.
    public class MainPageSwipeUI : CarouselView, IExposeCamera, IProvideNavigation
    {
        List<View> _children = new List<View> { };
        public List<View> Children {
            get { return _children; }
            set {
                _children = value;
                OnPropertyChanged();
                //Debug.WriteLine("DHB:MainPageSwipeUI:Children:Set called");
            }
        }

        JudgingContentPage judgingPage;
        PlayerContentPage playerPage;
        CameraContentPage cameraPage;
        //LeaderboardPage leaderboardPage;  handled through playerPage.

        //ContentPage lastPage = null;
        int lastPage = -1;
        // needed if the lastPage was a PlayerPage view.
        View lastView = null;

        public MainPageSwipeUI()
        {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            //var padding = new Thickness(0, Device.OnPlatform(40, 40, 0), 0, 0);
            playerPage = new PlayerContentPage();  // player page must be built before judging page sets up listeners.
            judgingPage = new JudgingContentPage();
            cameraPage = new CameraContentPage();
            //hamburgerPage = new HamburgerPage();

            // TokenReceived is my successful login event.
            playerPage.TokenReceived += new TokenReceivedEventHandler(this.TokenReceived);
            playerPage.TokenReceived += new TokenReceivedEventHandler(judgingPage.TokenReceived);
            playerPage.TokenReceived += new TokenReceivedEventHandler(playerPage.CenterConsole.HamburgerPage.TokenReceived);
            playerPage.LogoutClicked += new LogoutClickedEventHandler(this.OnLogoutClicked);

            // These lines enable another page to process a first page's events.
            // both judgingPage and cameraPage are guaranteed to exist at this point.
            // is categoryLoad?
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(cameraPage.OnCategoryLoad);
            //judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(leaderboardPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(playerPage.CenterConsole.LeaderboardPage.OnCategoryLoad);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(judgingPage.OnLoadBallotFromSubmission);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(playerPage.CenterConsole.MySubmissionsPage.OnPhotoSubmit);

            // Change behavior. Don't want to be able to do stuff prior to successful login...
            // Easiest to add these pages POST login.
            //Children.Add(judgingPage);
            Children.Add(playerPage);
            //Children.Add(cameraPage);

            Debug.WriteLine("DHB:MainPageSwipeUI:MainPageSwipeUI Children count is:" + Children.Count);

            // leaderboard is not part of the carousel.  It's reached from the player page only.
            //this.CurrentPage = playerPage;
            this.ItemTemplate = new CarouselTemplateSelector();
            this.ItemsSource = Children;
            this.SetBinding(CarouselView.ItemsSourceProperty, "Children");
            BindingContext = this;
            Position = 0; // this is player page, but since it is currently the only page, it is at position 0.
            //this.IsEnabled = false;  // does this turn everything off, or just the carousel? does fuck all as far as i can tell.
        }

        public ICamera getCamera()
        {
            return cameraPage;
        }

        public void gotoJudgingPage(){
            //this.CurrentPage = judgingPage;
            this.Position = 0;
            //Debug.WriteLine("DHB:MainPageSwipeUI:gotoJudgingPage: Position:" + Position);
            //Debug.WriteLine("DHB:MainPageSwipeUI:gotoJudgingPage: Content:" + this.Item.ToString());
        }
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            playerPage.goHome();
            //this.CurrentPage = playerPage;
            this.Position = 1;
        }
        public void gotoInstructionsPage() {
            playerPage.Content = playerPage.CenterConsole.InstructionsPage;
            //this.CurrentPage = playerPage;
            this.Position = 1;
        }
        public void gotoLeaderboardPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoLeaderboardPage");
            playerPage.Content = playerPage.CenterConsole.LeaderboardPage;
            //this.CurrentPage = playerPage;
            this.Position = 1;
        }
        public void gotoSettingsPage() {
            playerPage.Content = playerPage.CenterConsole.SettingsPage;
            //this.CurrentPage = playerPage;
            this.Position = 1;
        }

        public void gotoMySubmissionsPage() {
            playerPage.Content = playerPage.CenterConsole.MySubmissionsPage;
            //this.CurrentPage = playerPage;
            this.Position = 1;
        }

        public void gotoCameraPage() {
            //this.CurrentPage = cameraPage;
            this.Position = 2;
            cameraPage.startCamera();
        }

        public void gotoHamburgerPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage");
            foreach(View cp in Children) {
                Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage child:" +cp.ToString());
            }
            if ((Children[Position] == playerPage) && (playerPage.Content == playerPage.CenterConsole.HamburgerPage)) {
                // always set the last view, as hamburger changes it.
                playerPage.Content = lastView;
                //this.CurrentPage = lastPage;
                Position = lastPage;
            } else {
                lastPage = Position;
                // always set the last view, as hamburger changes it. (see following line!)
                lastView = playerPage.Content;
                // I'm crashing here alot.
                //playerPage.Content = playerPage.CenterConsole.HamburgerPage;
                // test invoking on ui thread.  Nope.
                try { 
                    playerPage.Content = playerPage.CenterConsole.HamburgerPage;
                } catch (NullReferenceException e) {
                    Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage wtf err: " + e.ToString());
                }
                Position = 1;
                Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage not hamburger. becoming");
            }
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburderPage finished TWEAK4");
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            // ok, we're in. add pages.
            this.Children.Insert(0, judgingPage);
            Debug.WriteLine("DHB:MainPageSwipeUI:TokenReceived post insert posn:" + this.Position);
            this.Children.Add(cameraPage);
            Debug.WriteLine("DHB:MainPageSwipeUI:TokenReceived post add posn:" + this.Position);
            // hmm... did the above lines adequately update _children?
            //printChildren(); this looks good. so why am i selecting the wrong template?
            if (GlobalStatusSingleton.firstTimePlaying == true) {
                // need to goto instructions page!
                playerPage.Content = playerPage.CenterConsole.InstructionsPage;
                GlobalStatusSingleton.firstTimePlaying = false;
            } else {
                gotoJudgingPage();
            }
        }

        protected void printChildren() {
            foreach (View v in _children) {
                Debug.WriteLine("DHB:MainPageSwipeUI:printChildren __" + v.ToString());
            }

            foreach (View v in Children) {
                Debug.WriteLine("DHB:MainPageSwipeUI:printChildren " + v.ToString());
            }

            foreach (View v in ItemsSource) {
                Debug.WriteLine("DHB:MainPageSwipeUI:printChildren s:" + v.ToString());
            }
        }

        public virtual void OnLogoutClicked(object sender, EventArgs e) {
            //this.CurrentPage = playerPage;
            Position = 1;
            this.Children.Remove(judgingPage);
            this.Children.Remove(cameraPage);
        }

        // SPECIAL SERIALIZE/DESERIALIZE functions
        public BallotJSON GetActiveBallot() {
            return judgingPage.GetBallot();
        }
        public void SetActiveBallot(string ballotAsStr) {
            judgingPage.SetBallot(ballotAsStr);
        }
        public Queue<string> GetBallotQueue() {
            return judgingPage.GetBallotQueue();
        }
        public void SetBallotQueue(Queue<string> ballotQueue) {
            judgingPage.SetPreloadedBallots(ballotQueue);
        }

        public IDictionary<CategoryJSON, IList<LeaderboardJSON>> GetLeaderboardList() {
            return playerPage.GetLeaderboardList();
        }
        public IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps() {
            return playerPage.GetLeaderboardTimestamps();
        }

        // Kinda serialize/deserialize. This is part of the OnResume process.
        public void FireLoadChallengeName() {
            judgingPage.FireLoadChallengeName();
        }
    }
}
