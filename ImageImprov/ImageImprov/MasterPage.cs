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
    class MasterPage : ContentPage, IExposeCamera, IProvideNavigation, ILifecycleManager, IOverlayable {
        // Q: Do I need IExposeCamera?  Yes. As that is reached through App.
        // Q: Do I need IProvideNavigation???  Yes. Drilling down from root is how the system is setup.
        Grid portraitView = new Grid();
        PageHeader header = new PageHeader();
        // thePages is public so that modal pages I push/pop can bind 
        public MainPageSwipeUI thePages;
        KeyPageNavigator defaultNavigation; // = new KeyPageNavigator { HighlightedButtonIndex = 0, }; must be created after thepages
        LoginPage loginPage = new LoginPage();
        public ZoomPage zoomPage { get; set; }

        public MasterPage() {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            thePages = new MainPageSwipeUI(loginPage);
            zoomPage = new ZoomPage { IsVisible = false, };
            defaultNavigation = new KeyPageNavigator { HighlightedButtonIndex = 0, };

            buildUI();  // so it is reader to go!
                        // for now, keep the construction of the pages in the carousel...

            // bind the footer to the current position on the list view
            Binding binding = new Binding { Source = thePages, Path = "Position" };
            defaultNavigation.SetBinding(KeyPageNavigator.HighlightedButton, binding);
            Binding headerBinding = new Binding { Source = thePages, Path = "Position" };
            header.SetBinding(PageHeader.HighlightedButton, headerBinding);

            Binding settingsOnOffBinding = new Binding { Source = thePages.profilePage.navRow, Path = "NavHighlightIndex" };
            header.SetBinding(PageHeader.ProfileNav, settingsOnOffBinding);

            Content = loginPage;
            Debug.WriteLine("DHB:MasterPage ctor complete");
        }

        public void buildUI() {
            portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, };
            portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16, GridUnitType.Star) });
            portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });

            portraitView.Children.Add(header, 0, 0);
            portraitView.Children.Add(thePages, 0, 1);
            portraitView.Children.Add(zoomPage, 0, 1);
            portraitView.Children.Add(defaultNavigation, 0, 2);

            //Content = portraitView;
        }

        public void leaveLogin() {
            Content = portraitView;

            /* grrr.... what a mess.  how am i navigating to, from, pages that aren't on the carousel?
            if (GlobalSingletonHelpers.isEmailAddress(GlobalStatusSingleton.username)) {
                // already configured for voting page.
            } else {
                // anonymous user
                if (GlobalStatusSingleton.firstTimePlaying == true) {
                    Content = CenterConsole.InstructionsPage;
                    GlobalStatusSingleton.firstTimePlaying = false;
                } else if (Content == CenterConsole.InstructionsPage) {
                    // do nothing - this means that I logged in and the token setting occurred already.
                    // it's a time issue between multiple async event handlers.
                } else {
                    Content = createAnonLoggedInLayout();
                }
            }
            */
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

        private void zoomVis() {
            zoomPage.IsVisible = true;
            thePages.IsVisible = false;
        }
        private void pagesVis() {
            zoomPage.IsVisible = false;
            thePages.IsVisible = true;
        }
        public void returnFromZoom() {
            pagesVis();
        }
        /// IProvideNavigation
        public void gotoJudgingPage() {
            pagesVis();
            thePages.gotoJudgingPage();
        }
        public void gotoJudgingPageHome() {
            pagesVis();
            thePages.gotoJudgingPageHome();
        }
        public void gotoLeaderboardPage() {
            pagesVis();
            thePages.gotoLeaderboardPage();
        }
        public void gotoCameraPage() {
            pagesVis();
            thePages.gotoCameraPage();
        }
        public void gotoCreateCategoryPage() {
            pagesVis();
            thePages.cameraPage.switchToCreateCategoryView();
        }
        public void gotoProfilePage() {
            pagesVis();
            thePages.gotoProfilePage();
        }

        public void gotoZoomPage() {
            zoomVis();
        }
        
        /*
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            thePages.gotoHomePage();
        }
        public void gotoHamburgerPage() {
            thePages.gotoHamburgerPage();
        }

        public void gotoInstructionsPage() {
            thePages.gotoInstructionsPage();
        }
        public void gotoSettingsPage() {
            thePages.gotoSettingsPage();
        }

        //public void gotoMedalsPage();
        public void gotoMySubmissionsPage() {
            thePages.gotoMySubmissionsPage();
        }
        public void gotoLikesPage() {
            thePages.gotoLikesPage();
        }
        //public void gotoPurchasePage();
        */

        ContentView overlay = null;
        public void pushOverlay(ContentView overlay) {
            this.overlay = overlay;
            portraitView.Children.Add(overlay, 0, 0);
            Grid.SetRowSpan(overlay, 3);  // it's units of 20, but only 3 rows.
            Grid.SetColumnSpan(overlay, 1);
            overlay.IsVisible = true;
        }
        public void popOverlay() {
            overlay.IsVisible = false;
            portraitView.Children.Remove(overlay);
        }
    }
}
