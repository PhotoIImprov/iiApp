using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// This replaces MainPageUISwipe as the parent page for the UI. (That's now a carousel view subset of this).
    /// </summary>
    class MasterPage : ContentPage, IExposeCamera, IProvideNavigation, ILifecycleManager {
        // Q: Do I need IExposeCamera?  Yes. As that is reached through App.
        // Q: Do I need IProvideNavigation???  Yes. Drilling down from root is how the system is setup.
        Grid portraitView = new Grid();
        PageHeader header = new PageHeader();
        MainPageSwipeUI thePages = new MainPageSwipeUI();
        KeyPageNavigator defaultNavigation = new KeyPageNavigator();

        public MasterPage() {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;
            buildUI();
            // for now, keep the construction of the pages in the carousel...
            Debug.WriteLine("DHB:MasterPage ctor complete");
        }

        public void buildUI() {
            portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, };
            portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16, GridUnitType.Star) });
            portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });

            portraitView.Children.Add(header, 0, 0);
            portraitView.Children.Add(thePages, 0, 1);
            portraitView.Children.Add(defaultNavigation, 0, 2);

            Content = portraitView;
        }

        public ICamera getCamera() {
            return thePages.getCamera();
        }

        public BallotJSON GetActiveBallot() {
            return thePages.GetActiveBallot();
        }
        public Queue<string> GetBallotQueue() {
            return thePages.GetBallotQueue();
        }
        public IDictionary<CategoryJSON, IList<LeaderboardJSON>> GetLeaderboardList() {
            return thePages.GetLeaderboardList();
        }
        public IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps() {
            return thePages.GetLeaderboardTimestamps();
        }
        public void FireLoadChallengeName() {
            thePages.FireLoadChallengeName();
        }

        /// IProvideNavigation
        public void gotoJudgingPage() {
            thePages.gotoJudgingPage();
        }
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            thePages.gotoHomePage();
        }
        public void gotoCameraPage() {
            thePages.gotoCameraPage();
        }
        public void gotoHamburgerPage() {
            thePages.gotoHamburgerPage();
        }

        public void gotoInstructionsPage() {
            thePages.gotoInstructionsPage();
        }
        public void gotoLeaderboardPage() {
            thePages.gotoLeaderboardPage();
        }
        public void gotoSettingsPage() {
            thePages.gotoSettingsPage();
        }

        //public void gotoMedalsPage();
        //public void gotoMyFavoritesPage();
        public void gotoMySubmissionsPage() {
            thePages.gotoMySubmissionsPage();
        }
        //public void gotoPurchasePage();

    }
}
