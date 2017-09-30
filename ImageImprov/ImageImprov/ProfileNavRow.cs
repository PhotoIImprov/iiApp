using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    class ProfileNavRow : Grid {  // as a grid to enable binding.
        public const int SETTINGS_INDEX = 3;
        //
        // Nav Portion Widgets
        // Nav Portion Widgets
        // Nav Portion Widgets
        //
        iiBitmapView mySubmissionsOff;
        iiBitmapView mySubmissionsOn;
        iiBitmapView myLikesOff;
        iiBitmapView myLikesOn;
        iiBitmapView myMedalsOff;
        iiBitmapView myMedalsOn;

        public static readonly BindableProperty HighlightedButton
            = BindableProperty.Create("HighlightedButtonIndex", typeof(int), typeof(ProfileNavRow), 0, BindingMode.Default, null, OnHighlightedButtonChanged);
        public int HighlightedButtonIndex {
            get { return (int)GetValue(HighlightedButton); }
            set {
                SetValue(HighlightedButton, value);
            }
        }
        
        /// <summary>
        /// The up caret hides the non-nav portion of the upper profile.
        /// </summary>
        iiBitmapView hideButton;
        /// <summary>
        /// The down caret shows the non-nav portion of the upper profile.
        /// </summary>
        iiBitmapView showButton;

        BoxView horizLine;
        BoxView horizLine2;

        IProvideProfileNavigation parent;

        public ProfileNavRow(IProvideProfileNavigation parent) {
            this.parent = parent;

            RowSpacing = 0;
            ColumnSpacing = 0;
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            mySubmissionsOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.play_inactive.png", assembly));
            mySubmissionsOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.play.png", assembly));
            mySubmissionsOff.IsVisible = false;  // default to my entries page.

            myLikesOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.unliked.png", assembly));
            myLikesOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.liked.png", assembly));
            myLikesOn.IsVisible = false;

            myMedalsOff = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.contests_inactive.png", assembly));
            myMedalsOn = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.contest.png", assembly));
            myMedalsOn.IsVisible = false;

            hideButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.upCaret.png", assembly));
            showButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.downCaret.png", assembly));
            showButton.IsVisible = false;  // default to showing profile

            TapGestureRecognizer tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnClicked;
            mySubmissionsOff.GestureRecognizers.Add(tapGesture);
            myLikesOff.GestureRecognizers.Add(tapGesture);
            myMedalsOff.GestureRecognizers.Add(tapGesture);
            showButton.GestureRecognizers.Add(tapGesture);
            hideButton.GestureRecognizers.Add(tapGesture);

            // on buttons behave differently...
            TapGestureRecognizer tapGestureOn = new TapGestureRecognizer();
            tapGestureOn.Tapped += OnClickedWhenOn;
            mySubmissionsOn.GestureRecognizers.Add(tapGestureOn);
            myLikesOn.GestureRecognizers.Add(tapGestureOn);
            myMedalsOn.GestureRecognizers.Add(tapGestureOn);

            horizLine = new BoxView { HeightRequest = 1.0, BackgroundColor = GlobalStatusSingleton.highlightColor, HorizontalOptions = LayoutOptions.FillAndExpand, };
            horizLine2 = new BoxView { HeightRequest = 1.0, BackgroundColor = GlobalStatusSingleton.highlightColor, HorizontalOptions = LayoutOptions.FillAndExpand, };
            buildUI();
        }

        public int buildUI() {
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(14, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Children.Add(horizLine, 0, 0);
            Grid.SetColumnSpan(horizLine, 4);
            Children.Add(mySubmissionsOff, 0, 2);
            Children.Add(mySubmissionsOn, 0, 2);
            Children.Add(myLikesOff, 1, 2);
            Children.Add(myLikesOn, 1, 2);
            Children.Add(myMedalsOff, 2, 2);
            Children.Add(myMedalsOn, 2, 2);
            Children.Add(hideButton, 3, 2);
            Children.Add(showButton, 3, 2);
            Children.Add(horizLine2, 0, 5);
            Grid.SetColumnSpan(horizLine2, 4);
            return 1;
        }
        
        private static void OnHighlightedButtonChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as ProfileNavRow;
            Device.BeginInvokeOnMainThread(() => {
                if (myObj.HighlightedButtonIndex == 0) {
                    myObj.mySubmissionsOff.IsVisible = false;
                    myObj.mySubmissionsOn.IsVisible = true;
                    myObj.myLikesOff.IsVisible = true;
                    myObj.myLikesOn.IsVisible = false;
                    myObj.myMedalsOff.IsVisible = true;
                    myObj.myMedalsOn.IsVisible = false;
                } else if (myObj.HighlightedButtonIndex == 1) {
                    myObj.mySubmissionsOff.IsVisible = true;
                    myObj.mySubmissionsOn.IsVisible = false;
                    myObj.myLikesOff.IsVisible = false;
                    myObj.myLikesOn.IsVisible = true;
                    myObj.myMedalsOff.IsVisible = true;
                    myObj.myMedalsOn.IsVisible = false;
                } else if (myObj.HighlightedButtonIndex == 2) {
                    myObj.mySubmissionsOff.IsVisible = true;
                    myObj.mySubmissionsOn.IsVisible = false;
                    myObj.myLikesOff.IsVisible = true;
                    myObj.myLikesOn.IsVisible = false;
                    myObj.myMedalsOff.IsVisible = false;
                    myObj.myMedalsOn.IsVisible = true;
                } else if (myObj.HighlightedButtonIndex == SETTINGS_INDEX) {
                    myObj.mySubmissionsOff.IsVisible = true;
                    myObj.mySubmissionsOn.IsVisible = false;
                    myObj.myLikesOff.IsVisible = true;
                    myObj.myLikesOn.IsVisible = false;
                    myObj.myMedalsOff.IsVisible = true;
                    myObj.myMedalsOn.IsVisible = false;
                } else {
                    // invalid range!!!
                    Debug.WriteLine("DHB:UpperProfileSection:OnHighlightedButtonChanged invalid setting!");
                }
            });
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == mySubmissionsOff) {
                //((IProvideProfileNavigation)Xamarin.Forms.Application.Current.MainPage).gotoSubmissionsPage();
                HighlightedButtonIndex = 0;
                parent.gotoSubmissionsPage();
            } else if (sender == myLikesOff) {
                //((IProvideProfileNavigation)Xamarin.Forms.Application.Current.MainPage).gotoLikesPage();
                // want to go instantly to this...
                //((MainPageSwipeUI)Xamarin.Forms.Application.Current.MainPage).getCamera().takePictureP.Clicked;
                HighlightedButtonIndex = 1;
                parent.gotoLikesPage();
            } else if (sender == myMedalsOff) {
                //((IProvideProfileNavigation)Xamarin.Forms.Application.Current.MainPage).gotoMedalsPage();
                HighlightedButtonIndex = 2;
                parent.gotoMedalsPage();
            } else if (sender == hideButton) {
                //((IProvideProfileNavigation)Xamarin.Forms.Application.Current.MainPage).flipShowProfile();
                flipCaret();
                parent.flipShowProfile();
            } else if (sender == showButton) {
                //((IProvideProfileNavigation)Xamarin.Forms.Application.Current.MainPage).flipShowProfile();
                flipCaret();
                parent.flipShowProfile();
            }
        }

        public void OnClickedWhenOn(object sender, EventArgs e) {
            if (sender == mySubmissionsOn) {
                // no behavior yet
            } else if (sender == myLikesOn) {
                // no behavior yet
            } else if (sender == myMedalsOn) {
                // no behavior yet
            }
        }

        private void flipCaret() {
            hideButton.IsVisible = !hideButton.IsVisible;
            showButton.IsVisible = !showButton.IsVisible;
        }
    }
}
