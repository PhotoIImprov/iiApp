using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;  // for debug assertions.

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// Provides the interface for going to the hamburger(navigation) page.
    /// </summary>
    class HamburgerPage : ContentView {
        // List of buttons on the hamburger page:
        // Instructions page
        // Voting
        // Home
        // Camera
        // Leaderboard
        // Medals
        // My submissions
        // My favorites
        // Settings
        Image instructionsButton;
        Label instructionsLabel;
        Image votingButton;
        Label votingLabel;
        Image homeButton;
        Label homeLabel;
        Image cameraButton;
        Label cameraLabel;
        Image leaderboardButton;
        Label leaderboardLabel;
        //Image medalsButton;
        Label medalsLabel;
        //Image myEntriesButton;
        Label myEntriesLabel;
        //Image myFavoritesButton;
        Label myFavsLabel;
        Image storeButton;
        Label storeLabel;
        Image settingsButton;
        Label settingsLabel;

        KeyPageNavigator defaultNavigationButtons;

        Grid portraitView;

        public HamburgerPage() {
            // this is a bunch of buttons and text that click through to other pages.
            // clicking hamburger a second time needs to take the user back to previous page. (Handled in MainPageNavigation).
            buildUI();
        }

        private int buildUI() {
            TapGestureRecognizer instructionsClick = new TapGestureRecognizer();
            instructionsButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.Help.png"), BackgroundColor = GlobalStatusSingleton.backgroundColor, };
            instructionsLabel = new Label { Text = "Show me how to play!", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };
            StackLayout instructionsRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { instructionsButton, instructionsLabel, }
            };
            instructionsButton.GestureRecognizers.Add(instructionsClick);
            instructionsLabel.GestureRecognizers.Add(instructionsClick);
            instructionsClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoInstructionsPage();
                // need to actually goto instructions...
            };

            TapGestureRecognizer voteClick = new TapGestureRecognizer();
            votingButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.vote.png"), BackgroundColor = GlobalStatusSingleton.highlightColor, };
            votingLabel = new Label {
                Text = "Vote for your favorites!",
                //HorizontalOptions = LayoutOptions.StartAndExpand,
                BackgroundColor = GlobalStatusSingleton.highlightColor,
                TextColor = Color.Black, };
            StackLayout votingRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { votingButton, votingLabel, },
                BackgroundColor= GlobalStatusSingleton.highlightColor,
            };
            votingButton.GestureRecognizers.Add(voteClick);
            votingLabel.GestureRecognizers.Add(voteClick);
            voteClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            };

            TapGestureRecognizer homeClick = new TapGestureRecognizer();
            homeButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.home.png"), BackgroundColor = GlobalStatusSingleton.backgroundColor, };
            homeLabel = new Label { Text = "Show me the home feed!", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor=Color.Black,};
            StackLayout homeRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { homeButton, homeLabel, }
            };
            homeButton.GestureRecognizers.Add(homeClick);
            homeLabel.GestureRecognizers.Add(homeClick);
            homeClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHomePage();
            };


            TapGestureRecognizer cameraClick = new TapGestureRecognizer();
            cameraButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.camera.png"), BackgroundColor = GlobalStatusSingleton.backgroundColor, };
            cameraLabel = new Label { Text = "Your pictures shall rule!", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };
            StackLayout cameraRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { cameraButton, cameraLabel, }
            };
            cameraButton.GestureRecognizers.Add(cameraClick);
            cameraLabel.GestureRecognizers.Add(cameraClick);
            cameraClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
            };


            TapGestureRecognizer leaderboardClick = new TapGestureRecognizer();
            leaderboardButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.leaderboard.png"), BackgroundColor = GlobalStatusSingleton.backgroundColor, };
            leaderboardLabel= new Label { Text = "See the best images!", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };
            StackLayout leaderboardRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { leaderboardButton, leaderboardLabel, }
            };
            leaderboardButton.GestureRecognizers.Add(leaderboardClick);
            leaderboardLabel.GestureRecognizers.Add(leaderboardClick);
            leaderboardClick.Tapped += (sender, args) => {
                Debug.WriteLine("DHB:HamburgerPage:buildUI leaderboardClick. pre move");
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoLeaderboardPage();
                Debug.WriteLine("DHB:HamburgerPage:buildUI leaderboardClick. post move");
            };

            TapGestureRecognizer settingsClick = new TapGestureRecognizer();
            settingsButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.settings.png"), BackgroundColor = GlobalStatusSingleton.backgroundColor, };
            settingsLabel = new Label { Text = "The settings", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };
            StackLayout settingsRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { settingsButton, settingsLabel, }
            };
            settingsButton.GestureRecognizers.Add(settingsClick);
            settingsLabel.GestureRecognizers.Add(settingsClick);
            settingsClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoSettingsPage();
            };

            StackLayout hamburger = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Spacing = 6,
                Children = { instructionsRow, votingRow, homeRow, cameraRow, leaderboardRow, settingsRow, }
            };
            ScrollView scroller = new ScrollView { Padding = new Thickness(10) };
            scroller.Content = hamburger;

            if (defaultNavigationButtons == null) {
                defaultNavigationButtons = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            }

            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            }
            portraitView.Children.Add(scroller, 0, 0);
            portraitView.Children.Add(defaultNavigationButtons, 0, 1);

            Content = portraitView;
            return 1;
        }
    }
}
