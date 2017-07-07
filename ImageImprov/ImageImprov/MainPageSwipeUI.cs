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
    public class MainPageSwipeUI : CarouselPage, IExposeCamera, IProvideNavigation
    {
        JudgingContentPage judgingPage;
        PlayerContentPage playerPage;
        CameraContentPage cameraPage;
        //LeaderboardPage leaderboardPage;  handled through playerPage.

        //HamburgerPage hamburgerPage;  // hamburger is never on the carousel. trying moving to centerconsole.
        ContentPage lastPage = null;
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
            playerPage.LogoutClicked += new LogoutClickedEventHandler(this.OnLogoutClicked);

            // These lines enable another page to process a first page's events.
            // both judgingPage and cameraPage are guaranteed to exist at this point.
            // is categoryLoad?
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(cameraPage.OnCategoryLoad);
            //judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(leaderboardPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(playerPage.CenterConsole.LeaderboardPage.OnCategoryLoad);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(judgingPage.OnLoadBallotFromSubmission);

            // Change behavior. Don't want to be able to do stuff prior to successful login...
            // Easiest to add these pages POST login.
            //Children.Add(judgingPage);
            Children.Add(playerPage);
            //Children.Add(cameraPage);
            
            // leaderboard is not part of the carousel.  It's reached from the player page only.
            this.CurrentPage = playerPage;
            //this.IsEnabled = false;  // does this turn everything off, or just the carousel? does fuck all as far as i can tell.
        }

        public ICamera getCamera()
        {
            return cameraPage;
        }

        public void gotoJudgingPage(){
            this.CurrentPage = judgingPage;
        }
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            playerPage.goHome();
            this.CurrentPage = playerPage;
        }
        public void gotoInstructionsPage() {
            playerPage.Content = playerPage.CenterConsole.InstructionsPage;
            this.CurrentPage = playerPage;
        }
        public void gotoLeaderboardPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoLeaderboardPage");
            playerPage.Content = playerPage.CenterConsole.LeaderboardPage;
            this.CurrentPage = playerPage;
        }
        public void gotoSettingsPage() {
            playerPage.Content = playerPage.CenterConsole.SettingsPage;
            this.CurrentPage = playerPage;
        }

        public void gotoCameraPage() {
            this.CurrentPage = cameraPage;
            cameraPage.startCamera();
        }

        public void gotoHamburgerPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage");
            foreach(ContentPage cp in Children) {
                Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage child:" +cp.ToString());
            }
            if ((this.CurrentPage == playerPage) && (playerPage.Content == playerPage.CenterConsole.HamburgerPage)) {
                if (lastPage == playerPage) {
                    playerPage.Content = lastView;
                }
                this.CurrentPage = lastPage;
            } else {
                lastPage = this.CurrentPage;
                if (lastPage == playerPage) { lastView = playerPage.Content; }
                playerPage.Content = playerPage.CenterConsole.HamburgerPage;
                this.CurrentPage = playerPage;
                Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage not hamburger. becoming");
            }
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburderPage finished TWEAK4");
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            // ok, we're in. add pages.
            this.Children.Insert(0, judgingPage);
            this.Children.Add(cameraPage);
            if (GlobalStatusSingleton.firstTimePlaying == true) {
                // need to goto instructions page!
                playerPage.Content = playerPage.CenterConsole.InstructionsPage;
                GlobalStatusSingleton.firstTimePlaying = false;
            } else {
                gotoJudgingPage();
            }
        }

        public virtual void OnLogoutClicked(object sender, EventArgs e) {
            this.CurrentPage = playerPage;
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
