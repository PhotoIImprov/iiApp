using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;  // for debug assertions.
using System.Reflection;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// Provides the interface for going to the hamburger(navigation) page.
    /// </summary>
    public class HamburgerPage : ContentView {
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

        //Image leaderboardButton;
        //Label leaderboardLabel;
        //Image medalsButton;
        Label medalsLabel;
        Image mySubmissionsButton;
        Label mySubmissionsLabel;
        //Image myFavoritesButton;
        Label myFavsLabel;
        //Image storeButton;
        //Label storeLabel;
        Image settingsButton;
        Label settingsLabel;

        Label termsOfServiceLabel;
        iiWebPage tosPage;
        Label privacyPolicyLabel;
        iiWebPage privacyPolicyPage;

        Label loggedInLabel = new Label
        {
            Text = "Connecting...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };
        //> loggedInLabel
        Label versionLabel = new Label
        {
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };

        //KeyPageNavigator defaultNavigationButtons;

        Grid portraitView;

        public HamburgerPage() {
            // this is a bunch of buttons and text that click through to other pages.
            // clicking hamburger a second time needs to take the user back to previous page. (Handled in MainPageNavigation).
            Content = new Label { Text = "Loading...", TextColor = Color.Black };
            buildUI();
        }

        private void setVersionLabelText() {
            // see answer at this forum page for more details on what's in full name.
            //https://forums.xamarin.com/discussion/26522/how-to-get-application-runtime-version-build-version-using-xamarin-forms
            string version = this.GetType().GetTypeInfo().Assembly.FullName;
            string[] splitString = version.Split(',');
            versionLabel.Text = splitString[1];
#if DEBUG
            versionLabel.Text = "Debug " + versionLabel.Text;
#endif
        }

        private async void buildUI() {
            await Task.Run(() => buildPortraitView());
            Device.BeginInvokeOnMainThread(() => {
                Content = portraitView;
            });
        }

        private int buildPortraitView() {
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

            setVersionLabelText();
            if (GlobalSingletonHelpers.isEmailAddress(GlobalStatusSingleton.username)) {
                loggedInLabel.Text = "Logged in as " + GlobalStatusSingleton.username;
            } else {
                loggedInLabel.Text = "Logged in anonymously";
            }

            TapGestureRecognizer mySubmissionsClick = new TapGestureRecognizer();
            mySubmissionsButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.contests_inactive.png"), BackgroundColor = GlobalStatusSingleton.backgroundColor, };
            mySubmissionsLabel = new Label { Text = "   My entries", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };
            StackLayout submissionsRow= new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { mySubmissionsButton, mySubmissionsLabel, }
            };
            mySubmissionsButton.GestureRecognizers.Add(mySubmissionsClick);
            mySubmissionsLabel.GestureRecognizers.Add(mySubmissionsClick);
            mySubmissionsClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoMySubmissionsPage();
            };

            TapGestureRecognizer myLikesClick = new TapGestureRecognizer();
            myFavsLabel = new Label {
                Text = "   My favorites/likes/bookmarks",
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                TextColor = Color.Black, };
            myFavsLabel.GestureRecognizers.Add(myLikesClick);
            myLikesClick.Tapped += (sender, args) => {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoLikesPage();
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

            Label comingSoon = new Label { Text = "Coming soon:", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };
            Label medals = new Label { Text = "   My medals", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };

            //Label purchases = new Label { Text = "Coming soon:", BackgroundColor = GlobalStatusSingleton.backgroundColor, TextColor = Color.Black, };

            if (termsOfServiceLabel == null) {
                createWebButtons();
            }

            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < 16; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            portraitView.Children.Add(instructionsRow, 1, 2);
            portraitView.Children.Add(submissionsRow, 1, 4);
            portraitView.Children.Add(myFavsLabel, 1, 6);
            portraitView.Children.Add(settingsRow, 1, 8);
            portraitView.Children.Add(comingSoon, 1, 11);
            portraitView.Children.Add(medals, 1, 12);
            portraitView.Children.Add(versionLabel, 1, 13);
            portraitView.Children.Add(loggedInLabel, 1, 14);
            portraitView.Children.Add(termsOfServiceLabel, 1, 15);
            portraitView.Children.Add(privacyPolicyLabel, 1, 16);

            //portraitView.Children.Add(scroller, 1, 1);
            //portraitView.Children.Add(hamburger, 1, 1);
            //portraitView.Children.Add(defaultNavigationButtons, 0, 2);
            //Grid.SetColumnSpan(defaultNavigationButtons, 3);
            return 1;
        }

        /// <summary>
        /// We listen for token received so we can update login info with correct username.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void TokenReceived(object sender, EventArgs e) {
            loggedInLabel.Text = GlobalStatusSingleton.username;
        }

        View returnLayout;
        protected void createWebButtons() {
            tosPage = iiWebPage.getInstance(GlobalStatusSingleton.TERMS_OF_SERVICE_URL, this, Content);
            //tosPage.setReturnPoint(this.Content);
            termsOfServiceLabel = new Label {
                //Text = "Tap here to read our Terms of Service",
                Text = "Terms of Service",
                TextColor = Color.Blue,
                FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.End,
            };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            termsOfServiceLabel.GestureRecognizers.Add(tap);
            tap.Tapped += (sender, args) => {
                //boom.
                returnLayout = Content;
                iiWebPage newPage = iiWebPage.getInstance(GlobalStatusSingleton.TERMS_OF_SERVICE_URL, this, Content);
                Content = newPage;
            };

            privacyPolicyPage = iiWebPage.getInstance(GlobalStatusSingleton.PRIVACY_POLICY_URL, this, Content);
            privacyPolicyLabel = new Label {
                //Text = "And here for our Privacy Policy",
                Text = "Privacy Policy",
                TextColor = Color.Blue,
                FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.End,
            };
            tap = new TapGestureRecognizer();
            privacyPolicyLabel.GestureRecognizers.Add(tap);
            tap.Tapped += (sender, args) => {
                returnLayout = Content;
                iiWebPage newPage = iiWebPage.getInstance(GlobalStatusSingleton.PRIVACY_POLICY_URL, this, Content);
                Content = newPage;
            };
        }

    }
}
