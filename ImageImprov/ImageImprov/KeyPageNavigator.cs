using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using System.Reflection;

namespace ImageImprov {
    /// <summary>
    /// This is a UI object that I've created because I need the same functionality and behavior across
    /// all of our pages.
    /// This UI object provides the functionality to go between the pages of the carousel via buttons at the bottom of the page.
    /// </summary>
    public class KeyPageNavigator : Grid {
        TapGestureRecognizer tapGesture;
        iiBitmapView gotoVotingImgButtonOff;
        iiBitmapView gotoVotingImgButtonOn;
        iiBitmapView gotoLeaderboardImgButtonOff;
        iiBitmapView gotoLeaderboardImgButtonOn;
        iiBitmapView gotoCameraImgButtonOff;
        iiBitmapView gotoCameraImgButtonOn;
        iiBitmapView gotoHamburgerImgButtonOff;
        iiBitmapView gotoHamburgerImgButtonOn;
        Label categoryLabel;

        //private int _highlightedButtonIndex = 0;
        public int HighlightedButtonIndex {
            get { return (int)GetValue(HighlightedButton); }
            set {
                SetValue(HighlightedButton, value);
            }
        }

        private static void OnHighlightedButtonChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as KeyPageNavigator;
            Device.BeginInvokeOnMainThread(() =>
            {
                if (myObj.HighlightedButtonIndex == 0) {
                    myObj.gotoVotingImgButtonOff.IsVisible = false;
                    myObj.gotoVotingImgButtonOn.IsVisible = true;
                    myObj.gotoLeaderboardImgButtonOff.IsVisible = true;
                    myObj.gotoLeaderboardImgButtonOn.IsVisible = false;
                    myObj.gotoCameraImgButtonOff.IsVisible = true;
                    myObj.gotoCameraImgButtonOn.IsVisible = false;
                    myObj.gotoHamburgerImgButtonOff.IsVisible = true;
                    myObj.gotoHamburgerImgButtonOn.IsVisible = false;
                } else if (myObj.HighlightedButtonIndex == 1) {
                    myObj.gotoVotingImgButtonOff.IsVisible = true;
                    myObj.gotoVotingImgButtonOn.IsVisible = false;
                    myObj.gotoLeaderboardImgButtonOff.IsVisible = false;
                    myObj.gotoLeaderboardImgButtonOn.IsVisible = true;
                    myObj.gotoCameraImgButtonOff.IsVisible = true;
                    myObj.gotoCameraImgButtonOn.IsVisible = false;
                    myObj.gotoHamburgerImgButtonOff.IsVisible = true;
                    myObj.gotoHamburgerImgButtonOn.IsVisible = false;
                } else if (myObj.HighlightedButtonIndex == 2) {
                    myObj.gotoVotingImgButtonOff.IsVisible = true;
                    myObj.gotoVotingImgButtonOn.IsVisible = false;
                    myObj.gotoLeaderboardImgButtonOff.IsVisible = true;
                    myObj.gotoLeaderboardImgButtonOn.IsVisible = false;
                    myObj.gotoCameraImgButtonOff.IsVisible = false;
                    myObj.gotoCameraImgButtonOn.IsVisible = true;
                    myObj.gotoHamburgerImgButtonOff.IsVisible = true;
                    myObj.gotoHamburgerImgButtonOn.IsVisible = false;
                } else if (myObj.HighlightedButtonIndex == 3) {
                    myObj.gotoVotingImgButtonOff.IsVisible = true;
                    myObj.gotoVotingImgButtonOn.IsVisible = false;
                    myObj.gotoLeaderboardImgButtonOff.IsVisible = true;
                    myObj.gotoLeaderboardImgButtonOn.IsVisible = false;
                    myObj.gotoCameraImgButtonOff.IsVisible = true;
                    myObj.gotoCameraImgButtonOn.IsVisible = false;
                    myObj.gotoHamburgerImgButtonOff.IsVisible = false;
                    myObj.gotoHamburgerImgButtonOn.IsVisible = true;
                } else {
                    // invalid range!!!
                    Debug.WriteLine("DHB:KeyPageNavigator:OnHighlightedButtonChanged invalid setting!");
                }
            });
        }

        public static readonly BindableProperty HighlightedButton
            = BindableProperty.Create("HighlightedButtonIndex", typeof(int), typeof(KeyPageNavigator), 0, BindingMode.Default, null, OnHighlightedButtonChanged);


        //StackLayout defaultNavigationButtons;
        //Grid defaultNavigationButtons;
        //Color navigatorColor = Color.FromRgb(100, 100, 100);
        BoxView horizLine;

        public KeyPageNavigator(string categoryDesc = "") {
            RowSpacing = 0;
            ColumnSpacing = 0;

            BackgroundColor = GlobalStatusSingleton.backgroundColor;
            //Padding = 10;

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            gotoVotingImgButtonOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.vote_inactive.png", assembly));
            gotoVotingImgButtonOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.vote.png", assembly));
            gotoVotingImgButtonOff.IsVisible = false;

            gotoLeaderboardImgButtonOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.leaderboard_inactive.png", assembly));
            gotoLeaderboardImgButtonOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.leaderboard.png", assembly));
            gotoLeaderboardImgButtonOn.IsVisible = false;

            gotoCameraImgButtonOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.play_inactive.png", assembly));
            gotoCameraImgButtonOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.play.png", assembly));
            gotoCameraImgButtonOn.IsVisible = false;

            gotoHamburgerImgButtonOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Hamburger_inactive.png", assembly));
            gotoHamburgerImgButtonOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Hamburger.png", assembly));
            gotoHamburgerImgButtonOn.IsVisible = false;

            categoryLabel = new Label {
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
            gotoVotingImgButtonOff.GestureRecognizers.Add(tapGesture);
            //goHomeImgButton.GestureRecognizers.Add(tapGesture);
            gotoLeaderboardImgButtonOff.GestureRecognizers.Add(tapGesture);
            gotoCameraImgButtonOff.GestureRecognizers.Add(tapGesture);
            gotoHamburgerImgButtonOff.GestureRecognizers.Add(tapGesture);

            // on buttons behave differently...
            TapGestureRecognizer tapGestureOn = new TapGestureRecognizer();
            tapGestureOn.Tapped += OnClickedWhenOn;
            gotoVotingImgButtonOn.GestureRecognizers.Add(tapGestureOn);
            gotoLeaderboardImgButtonOn.GestureRecognizers.Add(tapGestureOn);
            gotoCameraImgButtonOn.GestureRecognizers.Add(tapGestureOn);
            gotoHamburgerImgButtonOn.GestureRecognizers.Add(tapGestureOn);

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
            Children.Add(gotoVotingImgButtonOff, 0, 2);
            Children.Add(gotoVotingImgButtonOn, 0, 2);
            //Children.Add(goHomeImgButton, 1, 1);
            Children.Add(gotoLeaderboardImgButtonOff, 1, 2);
            Children.Add(gotoLeaderboardImgButtonOn, 1, 2);
            Children.Add(gotoCameraImgButtonOff, 2, 2);
            Children.Add(gotoCameraImgButtonOn, 2, 2);
            Children.Add(categoryLabel, 2, 3);
            Children.Add(gotoHamburgerImgButtonOff, 3, 2);
            Children.Add(gotoHamburgerImgButtonOn, 3, 2);

            // We now have a play button.
            // This object should always be created AFTER the judging page, so this should exist...
            //if (GlobalStatusSingleton.ptrToJudgingPageLoadCategory != null) {
            //    GlobalStatusSingleton.ptrToJudgingPageLoadCategory += new CategoryLoadSuccessEventHandler(OnCategoryLoad);
            //}
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == gotoVotingImgButtonOff) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            } else if (sender == gotoCameraImgButtonOff) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
                // want to go instantly to this...
                //((MainPageSwipeUI)Xamarin.Forms.Application.Current.MainPage).getCamera().takePictureP.Clicked;
            } else if (sender == gotoHamburgerImgButtonOff) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHamburgerPage();
            } else if (sender == gotoLeaderboardImgButtonOff) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoLeaderboardPage();
            } else {
                // go home for default.
                //((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHomePage();
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            }
        }

        public void OnClickedWhenOn(object sender, EventArgs e) {
            if (sender == gotoVotingImgButtonOn) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPageHome();
            } else if (sender == gotoLeaderboardImgButtonOn) {
                    ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoLeaderboardPage();
            } else if (sender == gotoCameraImgButtonOn) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
            } else if (sender == gotoHamburgerImgButtonOn) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHamburgerPage();
            }
        }

        //public virtual void OnCategoryLoad(object sender, EventArgs e) {
        //    categoryLabel.Text = GlobalSingletonHelpers.getUploadingCategoryDesc();
        //}
    }
}
