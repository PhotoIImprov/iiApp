using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public MainPageSwipeUI()
        {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            //var padding = new Thickness(0, Device.OnPlatform(40, 40, 0), 0, 0);
            playerPage = new PlayerContentPage();  // player page must be built before judging page sets up listeners.
            judgingPage = new JudgingContentPage();
            cameraPage = new CameraContentPage();

            // TokenReceived is my successful login event.
            playerPage.TokenReceived += new TokenReceivedEventHandler(this.TokenReceived);
            playerPage.TokenReceived += new TokenReceivedEventHandler(judgingPage.TokenReceived);
            playerPage.LogoutClicked += new LogoutClickedEventHandler(this.OnLogoutClicked);

            // both judgingPage and cameraPage are guaranteed to exist at this point.
            // is categoryLoad?
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(cameraPage.OnCategoryLoad);
            //judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(leaderboardPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(playerPage.CenterConsole.LeaderboardPage.OnCategoryLoad);

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
        public void gotoCameraPage() {
            this.CurrentPage = cameraPage;
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            // ok, we're in. add pages.
            this.Children.Insert(0, judgingPage);
            this.Children.Add(cameraPage);
            gotoJudgingPage();
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
    }
}
