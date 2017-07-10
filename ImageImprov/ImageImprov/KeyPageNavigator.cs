using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// This is a UI object that I've created because I need the same functionality and behavior across
    /// all of our pages.
    /// This UI object provides the functionality to go between the pages of the carousel via buttons at the bottom of the page.
    /// </summary>
    class KeyPageNavigator : Grid {
        TapGestureRecognizer tapGesture;
        Image gotoVotingImgButton;
        //Image goHomeImgButton;
        Image gotoLeaderboardImgButton;
        Image gotoCameraImgButton;
        Image gotoHamburgerImgButton;
        Label categoryLabel;

        //StackLayout defaultNavigationButtons;
        //Grid defaultNavigationButtons;
        //Color navigatorColor = Color.FromRgb(100, 100, 100);
        BoxView horizLine;

        public KeyPageNavigator(string categoryDesc = "") {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;
            //Padding = 10;

            gotoVotingImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.vote.png")
            };
            /*
            goHomeImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.home.png")
            };   */
            gotoLeaderboardImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.leaderboard.png")
            };
            gotoCameraImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.camera.png"),
            };
            gotoHamburgerImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.Hamburger.png"),
            };
            categoryLabel = new Label
            {
                Text = categoryDesc,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.Black,
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                LineBreakMode = LineBreakMode.WordWrap,
                FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
            };

            tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnClicked;
            gotoVotingImgButton.GestureRecognizers.Add(tapGesture);
            //goHomeImgButton.GestureRecognizers.Add(tapGesture);
            gotoLeaderboardImgButton.GestureRecognizers.Add(tapGesture);
            gotoCameraImgButton.GestureRecognizers.Add(tapGesture);
            gotoHamburgerImgButton.GestureRecognizers.Add(tapGesture);

            horizLine = new BoxView { HeightRequest = 1.0, BackgroundColor = GlobalStatusSingleton.highlightColor, HorizontalOptions = LayoutOptions.FillAndExpand, };
            // ColumnSpacing = 1; RowSpacing = 1;
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(14, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Star) });
            Children.Add(horizLine, 0, 0);
            Grid.SetColumnSpan(horizLine, 4);
            Children.Add(gotoVotingImgButton, 0, 2);
            //Children.Add(goHomeImgButton, 1, 1);
            Children.Add(gotoLeaderboardImgButton, 1, 2);
            Children.Add(gotoCameraImgButton, 2, 2);
            Children.Add(categoryLabel, 2, 3);
            Children.Add(gotoHamburgerImgButton, 3, 2);

            // This object should always be created AFTER the judging page, so this should exist...
            if (GlobalStatusSingleton.ptrToJudgingPageLoadCategory != null) {
                GlobalStatusSingleton.ptrToJudgingPageLoadCategory += new CategoryLoadSuccessEventHandler(OnCategoryLoad);
            }
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == gotoVotingImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            } else if (sender == gotoCameraImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
                // want to go instantly to this...
                //((MainPageSwipeUI)Xamarin.Forms.Application.Current.MainPage).getCamera().takePictureP.Clicked;
            } else if (sender == gotoHamburgerImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHamburgerPage();
            } else if (sender == gotoLeaderboardImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoLeaderboardPage();
            } else {
                // go home for default.
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHomePage();
            }
        }

        public virtual void OnCategoryLoad(object sender, EventArgs e) {
            categoryLabel.Text = GlobalSingletonHelpers.getUploadingCategoryDesc();
        }
    }
}
