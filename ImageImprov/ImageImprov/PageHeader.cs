using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// Provides a function pointer for what the back button does when called.
    /// </summary>
    public delegate void BackButtonDelegate(object Sender, EventArgs args);

    /// <summary>
    /// Provides the header (generally the "image improv" text) at the top of the screen.
    /// </summary>
    class PageHeader : ContentView {
        iiBitmapView backCaret;
        iiBitmapView settingsButton;
        iiBitmapView settingsButton_active;

        BackButtonDelegate backButtonDelegate;

        public PageHeader() {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            settingsButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.settings_inactive.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 4, 
                IsVisible = false, // starts invis.
            };
            settingsButton_active = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.settings.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 4,
                IsVisible = false, // starts invis.
            };

            TapGestureRecognizer tapped = new TapGestureRecognizer();
            tapped.Tapped += ((e,s) => {
                MasterPage mp = ((MasterPage)Application.Current.MainPage);
                settingsButton_active.IsVisible = true;
                settingsButton.IsVisible = false;
                mp.thePages.profilePage.gotoSettingsPage();
            });
            settingsButton.GestureRecognizers.Add(tapped);

            backCaret = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly)) {
                HorizontalOptions = LayoutOptions.Start,
                Margin = 4,
                IsVisible = false, // starts invis.
            };

            TapGestureRecognizer goBack = new TapGestureRecognizer();
            goBack.Tapped += ((a, b) => {
                if (backButtonDelegate != null) {
                    backButtonDelegate(a, b);
                }
            });
            backCaret.GestureRecognizers.Add(goBack);

            this.Content = buildTextLogo();
        }

        public static void zoomPageBack(object Sender, EventArgs args) {
            MasterPage mp = ((MasterPage)Application.Current.MainPage);
            //backCaret.IsVisible = false;
            //mp.thePages.profilePage.gotoSettingsPage();
            mp.zoomPage.OnBack(Sender, args);
        }

        public void autoInvisDelegate(object Sender, EventArgs args) {
            backCaret.IsVisible = false;
        }

        public void setHeaderBackCaretDelegate(BackButtonDelegate backDelegate, bool addAutoInvis = true) {
            backButtonDelegate = backDelegate;
            if (addAutoInvis) {
                backButtonDelegate += autoInvisDelegate;
            }
        }

        /// <summary>
        /// Convenience function for returning the default behavior of the back button to register for zoom pages.
        /// </summary>
        public void resetBackButton() {
            backButtonDelegate = new BackButtonDelegate(zoomPageBack);
        }

        private Grid buildTextLogo() {
            Grid textLogo = new Grid();
            textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Star) });
            textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(79, GridUnitType.Star) });
            //textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(9, GridUnitType.Star) });
            textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Image textImg = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.ii_textlogo.png"), };
            BoxView horizLine = new BoxView { HeightRequest = 1.0, BackgroundColor = GlobalStatusSingleton.highlightColor, HorizontalOptions = LayoutOptions.FillAndExpand, };
            textLogo.Children.Add(textImg, 0, 1);
            textLogo.Children.Add(horizLine, 0, 2);
            textLogo.Children.Add(backCaret, 0, 1);
            textLogo.Children.Add(settingsButton, 0, 1);
            textLogo.Children.Add(settingsButton_active, 0, 1);
            return textLogo;
        }

        public int HighlightedButtonIndex {
            get { return (int)GetValue(HighlightedButton); }
            set {
                SetValue(HighlightedButton, value);
            }
        }

        private static void OnHighlightedButtonChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as PageHeader;
            Device.BeginInvokeOnMainThread(() => {
                if (myObj.HighlightedButtonIndex == MainPageSwipeUI.PROFILE_PAGE) {
                    myObj.settingsButton.IsVisible = true;
                } else {
                    myObj.settingsButton.IsVisible = false;
                    myObj.settingsButton_active.IsVisible = false;
                }
            });
        }

        public static readonly BindableProperty HighlightedButton
            = BindableProperty.Create("HighlightedButtonIndex", typeof(int), typeof(PageHeader), 0, BindingMode.Default, null, OnHighlightedButtonChanged);


        public int ProfileNavIndex {
            get { return (int)GetValue(ProfileNav); }
            set {
                SetValue(ProfileNav, value);
            }
        }

        private static void OnProfileNavChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as PageHeader;
            Device.BeginInvokeOnMainThread(() => {
                // only germane if HighlightedButtonIndex is on ProfilePage...
                if (myObj.HighlightedButtonIndex == MainPageSwipeUI.PROFILE_PAGE) {
                    if (myObj.ProfileNavIndex == ProfileNavRow.SETTINGS_INDEX) {
                        myObj.settingsButton_active.IsVisible = true;
                        myObj.settingsButton.IsVisible = false;
                    } else {
                        myObj.settingsButton_active.IsVisible = false;
                        myObj.settingsButton.IsVisible = true;
                    }
                }
            });
        }

        public static readonly BindableProperty ProfileNav
            = BindableProperty.Create("ProfileNavIndex", typeof(int), typeof(PageHeader), 0, BindingMode.Default, null, OnProfileNavChanged);

        public void backCaretInvis() {
            backCaret.IsVisible = false;
        }
        public void backCaretVis() {
            backCaret.IsVisible = true;
        }
    }
}
