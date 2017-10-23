using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public const int JUDGING_PAGE = 0;
        public const int LEADERS_PAGE = 1;
        public const int CAMERA_PAGE = 2;
        public const int PROFILE_PAGE = 3;
        public const int ZOOM_PAGE = 4;  // need to make this unreachable.

        /*List<View> _children = new List<View> { };
        public List<View> Children {
            get { return _children; }
            set {
                _children = value;
                OnPropertyChanged();
                //Debug.WriteLine("DHB:MainPageSwipeUI:Children:Set called");
            }
        }*/
        ObservableCollection<View> Children = new ObservableCollection<View>();

        public JudgingContentPage judgingPage;
        // public so that i can implement zoom callback.
        public LeaderboardPage leaderboardPage;
        public CameraContentPage cameraPage;
        public ProfilePage profilePage;

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
            profilePage = new ProfilePage();

            Debug.WriteLine("DHB:MainPageSwipeUI:ctor cameraPage created.");

            

            // TokenReceived is my successful login event.
            loginPage.TokenReceived += new TokenReceivedEventHandler(this.TokenReceived);
            loginPage.TokenReceived += new TokenReceivedEventHandler(judgingPage.TokenReceived);
            loginPage.TokenReceived += new TokenReceivedEventHandler(profilePage.TokenReceived);
            loginPage.LogoutClicked += new LogoutClickedEventHandler(this.OnLogoutClicked);

            // These lines enable another page to process a first page's events.
            // both judgingPage and cameraPage are guaranteed to exist at this point.
            // is categoryLoad?
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(cameraPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(leaderboardPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(profilePage.MySubmissionsPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(profilePage.LikesPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(profilePage.EventsPage.OnCategoryLoad);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(judgingPage.OnLoadBallotFromSubmission);
            cameraPage.LoadBallotFromPhotoSubmission += new LoadBallotFromPhotoSubmissionEventHandler(profilePage.MySubmissionsPage.OnPhotoSubmit);

            // Change behavior. Don't want to be able to do stuff prior to successful login...
            // Easiest to add these pages POST login.
            // Delayed add doesn't seem to work.  Adding now, and switching from login page to this.
            Children.Add(judgingPage);
            Children.Add(leaderboardPage);
            Children.Add(cameraPage);
            Children.Add(profilePage);

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
            this.Position = JUDGING_PAGE;
            //Debug.WriteLine("DHB:MainPageSwipeUI:gotoJudgingPage: Position:" + Position);
            //Debug.WriteLine("DHB:MainPageSwipeUI:gotoJudgingPage: Content:" + this.Item.ToString());
        }
        public void gotoJudgingPageHome() {
            judgingPage.goHome();
        }
        public void gotoLeaderboardPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoLeaderboardPage");
            leaderboardPage.returnToCaller();
            this.Position = LEADERS_PAGE;
        }
        public void gotoCameraPage() {
            //this.CurrentPage = cameraPage;
            this.Position = CAMERA_PAGE;
            // no switching of current scene occurs.
            // this moves into the click observer for category selection
            //cameraPage.startCamera();
        }

        /*
        public void gotoZoomPage() {
            this.Position = ZOOM_PAGE;
        }
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            //playerPage.goHome();
            //this.CurrentPage = playerPage;
            // do anything more here?
            this.Position = 3;
        }
        public void gotoInstructionsPage() {
            profilePage.gotoInstructionsPage();
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }
        public void gotoSettingsPage() {
            playerPage.Content = playerPage.CenterConsole.SettingsPage;
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }
        public void gotoLikesPage() {
            playerPage.Content = playerPage.CenterConsole.LikesPage;
            this.Position = 3;
        }
        public void gotoMySubmissionsPage() {
            playerPage.Content = playerPage.CenterConsole.MySubmissionsPage;
            //this.CurrentPage = playerPage;
            this.Position = 3;
        }


        public void gotoHamburgerPage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoHamburgerPage");
            
            gotoProfilePage();
        }
        */

        public void gotoProfilePage() {
            Debug.WriteLine("DHB:MainPageSwipeUI:gotoProfilePage");
            profilePage.gotoSubmissionsPage();
            //profilePage.gotoEventsHistoryPage();
            Position = PROFILE_PAGE;
            
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
                profilePage.gotoInstructionsPage();
                // dont set to false here. still need to pop the help page on category load.
                //GlobalStatusSingleton.firstTimePlaying = false;
            } else {
                gotoJudgingPage();
            }
        }

        protected void printChildren() {
            //foreach (View v in _children) {
            //    Debug.WriteLine("DHB:MainPageSwipeUI:printChildren __" + v.ToString());
            //}

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
