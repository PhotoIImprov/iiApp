using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;  // for debug assertions.

using Xamarin.Forms;
using CarouselView.FormsPlugin.Abstractions;

namespace ImageImprov
{
    // this is the main page when we are using the carousel to manage the ui.
    public class MainPageSwipeUI : CarouselViewControl, IExposeCamera, IProvideNavigation
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

        public JudgingContentPage judgingPage;
        // public so that i can implement zoom callback.
        public LeaderboardPage leaderboardPage;
        public CameraContentPage cameraPage;
        public PlayerContentPage playerPage;

        // Listed as a reference to highlight I no longer own the lifecycle of this page.
        LoginPage refToLoginPage;

        //ContentPage lastPage = null;
        int lastPage = -1;
        // needed if the lastPage was a PlayerPage view.
        View lastView = null;

        public MainPageSwipeUI(LoginPage loginPage)
        {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            refToLoginPage = loginPage;

            //var padding = new Thickness(0, Device.OnPlatform(40, 40, 0), 0, 0);
            leaderboardPage = new LeaderboardPage();  
            judgingPage = new JudgingContentPage();
            cameraPage = new CameraContentPage();
            Debug.WriteLine("DHB:MainPageSwipeUI:ctor cameraPage created.");

            //hamburgerPage = new HamburgerPage(); // owned by player page; subordinate to playerPage.
            playerPage = new PlayerContentPage();  // now on the carousel stack.
            

            // TokenReceived is my successful login event.
            loginPage.TokenReceived += new TokenReceivedEventHandler(this.TokenReceived);
            loginPage.TokenReceived += new TokenReceivedEventHandler(judgingPage.TokenReceived);
            loginPage.TokenReceived += new TokenReceivedEventHandler(playerPage.CenterConsole.HamburgerPage.TokenReceived);
            loginPage.LogoutClicked += new LogoutClickedEventHandler(this.OnLogoutClicked);

            // These lines enable another page to process a first page's events.
            // both judgingPage and cameraPage are guaranteed to exist at this point.
            // is categoryLoad?
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(cameraPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(leaderboardPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(playerPage.CenterConsole.MySubmissionsPage.OnCategoryLoad);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(judgingPage.OnLoadBallotFromSubmission);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(playerPage.CenterConsole.MySubmissionsPage.OnPhotoSubmit);

            // Change behavior. Don't want to be able to do stuff prior to successful login...
            // Easiest to add these pages POST login.
            // Delayed add doesn't seem to work.  Adding now, and switching from login page to this.
            Children.Add(judgingPage);
            Children.Add(leaderboardPage);
            Children.Add(cameraPage);
            Children.Add(playerPage);

            Debug.WriteLine("DHB:MainPageSwipeUI:MainPageSwipeUI Children count is:" + Children.Count);

            this.ItemTemplate = new CarouselTemplateSelector();
            this.ItemsSource = Children;
            //this.SetBinding(CarouselView.ItemsSourceProperty, "Children");
            BindingContext = this;
            Position = 0; // should be the voting page.
        }

        public ICamera getCamera() {
            return cameraPage;
        }

        public void gotoJudgingPage() {
            //this.CurrentPage = judgingPage;
            this.Position = 0;
            //Debug.WriteLine("DHB:MainPageSwipeUI:gotoJudgingPage: Position:" + Position);
            //Debug.WriteLine("DHB:MainPageSwipeUI:gotoJudgingPage: Content:" + this.Item.ToString());
        }
        public void gotoJudgingPageHome() {
            judgingPage.goHome();
        }
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            playerPage.goHome();
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }
        public void gotoInstructionsPage() {
            playerPage.Content = playerPage.CenterConsole.InstructionsPage;
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }
        public void gotoLeaderboardPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoLeaderboardPage");
            //playerPage.Content = playerPage.CenterConsole.LeaderboardPage;
            //this.CurrentPage = playerPage;
            leaderboardPage.returnToCaller();
            this.Position = 1;
        }
        public void gotoSettingsPage() {
            playerPage.Content = playerPage.CenterConsole.SettingsPage;
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }

        public void gotoMySubmissionsPage() {
            playerPage.Content = playerPage.CenterConsole.MySubmissionsPage;
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }

        public void gotoCameraPage() {
            //this.CurrentPage = cameraPage;
            this.Position = 2;
            // no switching of current scene occurs.
            // this moves into the click observer for category selection
            //cameraPage.startCamera();
        }

        public void gotoHamburgerPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage");
            // debugging code
            foreach (View cp in Children) {
                Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage child:" +cp.ToString());
            }
            // end debugging.

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
                Position = 3;
                Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage not hamburger. becoming");
            }
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburderPage finished TWEAK4");
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            // ok, we're in. add pages.
            //this.Children.Insert(0, judgingPage);
            //Debug.WriteLine("DHB:MainPageSwipeUI:TokenReceived post insert posn:" + this.Position);
            //this.Children.Add(cameraPage);
            //Debug.WriteLine("DHB:MainPageSwipeUI:TokenReceived post add posn:" + this.Position);
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

        // this needs to be handled above
        public virtual void OnLogoutClicked(object sender, EventArgs e) {
            //this.CurrentPage = playerPage;
            Position = 1;
            //this.Children.Remove(judgingPage);
            //this.Children.Remove(cameraPage);
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
            return leaderboardPage.GetLeaderboardList();
        }
        public IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps() {
            return leaderboardPage.GetLeaderboardTimestamps();
        }

        // Kinda serialize/deserialize. This is part of the OnResume process.
        public void FireLoadChallengeName() {
            judgingPage.FireLoadChallengeName();
        }
    }
}
